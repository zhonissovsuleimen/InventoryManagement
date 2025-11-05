using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Pages.Inventory
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Models.Inventory.Inventory> Inventory { get; set; } = new List<Models.Inventory.Inventory>();
        public bool IsAdmin { get; set; }

        public async Task OnGetAsync()
        {
            Inventory = await _context.Inventories
                .Include(i => i.Owner)
                .AsNoTracking()
                .ToListAsync();
            var user = await _userManager.GetUserAsync(User);
            IsAdmin = user?.IsAdmin == true;
        }

        public class DeleteInventoriesRequest
        {
            public List<Guid>? Guids { get; set; }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteInventoriesAsync([FromBody] DeleteInventoriesRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return new UnauthorizedResult();
            if (!user.IsAdmin) return new ForbidResult();

            var ids = request?.Guids?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
            {
                return new JsonResult(new { deleted = Array.Empty<Guid>() });
            }

            var inventories = await _context.Inventories
                .Where(i => ids.Contains(i.Guid))
                .ToListAsync();

            if (inventories.Count > 0)
            {
                _context.Inventories.RemoveRange(inventories);
                await _context.SaveChangesAsync();
            }

            var deleted = inventories.Select(i => i.Guid).ToList();
            return new JsonResult(new { deleted });
        }
    }
}
