using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Pages.Users
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public DetailsModel(ApplicationDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public AppUser? UserModel { get; set; }
        public List<Models.Inventory.Inventory> CreatedInventories { get; set; } = [];
        public List<Models.Inventory.Inventory> AllowedInventories { get; set; } = [];
        public List<Item> LikedItems { get; set; } = [];
        public bool IsSelf { get; set; }
        public bool EditMode { get; set; }
        public bool IsAdmin { get; set; }

        public class EditInput
        {
            [Display(Name = "First name")]
            public string? FirstName { get; set; }

            [Display(Name = "Last name")]
            public string? LastName { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string? CurrentPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            [Display(Name = "Confirm new password")]
            public string? ConfirmPassword { get; set; }
        }

        [BindProperty]
        public EditInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(bool? edit)
        {
            if (string.IsNullOrWhiteSpace(Id)) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            IsAdmin = currentUser?.IsAdmin == true;
            var currentUserId = currentUser?.Id;
            IsSelf = !string.IsNullOrEmpty(currentUserId) && string.Equals(currentUserId, Id, StringComparison.Ordinal);

            UserModel = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Id);
            if (UserModel == null) return NotFound();

            CreatedInventories = await _context.Inventories
                .Include(i => i.Owner)
                .AsNoTracking()
                .Where(i => i.Owner != null && i.Owner.Id == Id)
                .OrderBy(i => i.Id)
                .ToListAsync();

            AllowedInventories = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.AllowedUsers)
                .AsNoTracking()
                .Where(i => i.AllowedUsers.Any(u => u.Id == Id))
                .OrderBy(i => i.Id)
                .ToListAsync();

            LikedItems = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .Include(i => i.Likes).ThenInclude(l => l.User)
                .AsNoTracking()
                .Where(i => i.Likes.Any(l => l.User != null && l.User.Id == Id))
                .OrderBy(i => i.Id)
                .ToListAsync();

            if (edit == true && (IsSelf || IsAdmin))
            {
                EditMode = true;
                if (IsSelf)
                {
                    Input.FirstName = UserModel.FirstName;
                    Input.LastName = UserModel.LastName;
                }
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditAsync()
        {
            if (string.IsNullOrWhiteSpace(Id)) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !string.Equals(currentUser.Id, Id, StringComparison.Ordinal))
            {
                return Forbid();
            }

            // Update names
            bool changed = false;
            var newFirst = Input.FirstName?.Trim() ?? string.Empty;
            var newLast = Input.LastName?.Trim() ?? string.Empty;
            if (!string.Equals(currentUser.FirstName ?? string.Empty, newFirst, StringComparison.Ordinal))
            {
                currentUser.FirstName = newFirst;
                changed = true;
            }
            if (!string.Equals(currentUser.LastName ?? string.Empty, newLast, StringComparison.Ordinal))
            {
                currentUser.LastName = newLast;
                changed = true;
            }

            if (changed)
            {
                var updateRes = await _userManager.UpdateAsync(currentUser);
                if (!updateRes.Succeeded)
                {
                    foreach (var e in updateRes.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return await ReloadWithErrorsAsync();
                }
            }

            // Change password if requested
            var newPwd = (Input.NewPassword ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(newPwd))
            {
                var currentPwd = Input.CurrentPassword ?? string.Empty;
                var res = await _userManager.ChangePasswordAsync(currentUser, currentPwd, newPwd);
                if (!res.Succeeded)
                {
                    foreach (var e in res.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return await ReloadWithErrorsAsync();
                }
                await _signInManager.RefreshSignInAsync(currentUser);
            }

            return RedirectToPage(new { id = Id });
        }

        public sealed class DeleteCreatedInventoriesRequest
        {
            public List<Guid>? Guids { get; set; }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteCreatedInventoriesAsync([FromBody] DeleteCreatedInventoriesRequest request)
        {
            // Must be logged in and deleting own inventories only
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || string.IsNullOrWhiteSpace(Id) || !string.Equals(currentUser.Id, Id, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var ids = request?.Guids?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
            {
                return new JsonResult(new { deleted = Array.Empty<Guid>() });
            }

            var inventories = await _context.Inventories
                .Include(i => i.Owner)
                .Where(i => ids.Contains(i.Guid) && i.Owner != null && i.Owner.Id == currentUser.Id)
                .ToListAsync();

            if (inventories.Count > 0)
            {
                _context.Inventories.RemoveRange(inventories);
                await _context.SaveChangesAsync();
            }

            var deleted = inventories.Select(i => i.Guid).ToList();
            return new JsonResult(new { deleted });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostMakeAdminAsync()
        {
            var current = await _userManager.GetUserAsync(User);
            if (current?.IsAdmin != true) return Forbid();
            if (string.IsNullOrWhiteSpace(Id)) return NotFound();

            var target = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (target == null) return NotFound();

            target.IsAdmin = true;
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = Id, edit = true });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveAdminAsync()
        {
            var current = await _userManager.GetUserAsync(User);
            if (current?.IsAdmin != true) return Forbid();
            if (string.IsNullOrWhiteSpace(Id)) return NotFound();

            var target = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (target == null) return NotFound();

            target.IsAdmin = false;
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = Id, edit = true });
        }

        private async Task<PageResult> ReloadWithErrorsAsync()
        {
            UserModel = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Id);
            CreatedInventories = await _context.Inventories
                .Include(i => i.Owner)
                .AsNoTracking()
                .Where(i => i.Owner != null && i.Owner.Id == Id)
                .OrderBy(i => i.Id)
                .ToListAsync();
            AllowedInventories = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.AllowedUsers)
                .AsNoTracking()
                .Where(i => i.AllowedUsers.Any(u => u.Id == Id))
                .OrderBy(i => i.Id)
                .ToListAsync();
            LikedItems = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .Include(i => i.Likes).ThenInclude(l => l.User)
                .AsNoTracking()
                .Where(i => i.Likes.Any(l => l.User != null && l.User.Id == Id))
                .OrderBy(i => i.Id)
                .ToListAsync();
            IsSelf = true;
            EditMode = true;
            var currentUser = await _userManager.GetUserAsync(User);
            IsAdmin = currentUser?.IsAdmin == true;
            return Page();
        }
    }
}
