using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<AppUser> Users { get; set; } = [];
        public bool IsAdmin { get; set; }

        public async Task OnGetAsync()
        {
            var current = await _userManager.GetUserAsync(User);
            IsAdmin = current?.IsAdmin == true;
            Users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.SequentialId)
                .ToListAsync();
        }

        public sealed class IdsRequest
        {
            public List<string>? Ids { get; set; }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteUsersAsync([FromBody] IdsRequest request)
        {
            var current = await _userManager.GetUserAsync(User);
            if (current?.IsAdmin != true) return Forbid();

            var ids = request?.Ids?.Distinct().ToList() ?? new List<string>();
            if (ids.Count == 0) return new JsonResult(new { deleted = Array.Empty<string>() });

            // First detach ownership from inventories to avoid FK issues
            var invs = await _context.Inventories
                .Where(i => EF.Property<string>(i, "Owner_UserId") != null && ids.Contains(EF.Property<string>(i, "Owner_UserId")!))
                .ToListAsync();
            foreach (var inv in invs)
            {
                // Set owner to null, allowed by DB configuration
                inv.Owner = null!; // runtime null allowed, model treats as required
            }
            if (invs.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            var deleted = new List<string>();
            foreach (var u in users)
            {
                var res = await _userManager.DeleteAsync(u);
                if (res.Succeeded)
                {
                    deleted.Add(u.Id);
                }
            }

            return new JsonResult(new { deleted });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostMakeAdminAsync([FromBody] IdsRequest request)
        {
            var current = await _userManager.GetUserAsync(User);
            if (current?.IsAdmin != true) return Forbid();

            var ids = request?.Ids?.Distinct().ToList() ?? new List<string>();
            if (ids.Count == 0) return new JsonResult(new { updated = Array.Empty<string>() });

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var u in users)
            {
                u.IsAdmin = true;
            }
            await _context.SaveChangesAsync();
            var updated = users.Select(u => u.Id).ToList();
            return new JsonResult(new { updated });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveAdminAsync([FromBody] IdsRequest request)
        {
            var current = await _userManager.GetUserAsync(User);
            if (current?.IsAdmin != true) return Forbid();

            var ids = request?.Ids?.Distinct().ToList() ?? new List<string>();
            if (ids.Count == 0) return new JsonResult(new { updated = Array.Empty<string>() });

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var u in users)
            {
                u.IsAdmin = false;
            }
            await _context.SaveChangesAsync();
            var updated = users.Select(u => u.Id).ToList();
            return new JsonResult(new { updated });
        }
    }
}
