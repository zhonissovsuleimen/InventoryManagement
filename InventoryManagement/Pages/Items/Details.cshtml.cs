using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models;
using System.Security.Claims;

namespace InventoryManagement.Pages.Items
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Item Item { get; set; } = default!;
        public bool CanEdit { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? guid)
        {
            if (guid == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .Include(i => i.Likes)
                    .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(i => i.Guid == guid.Value);

            if (item == null) return NotFound();

            Item = item;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                CanEdit = (currentUser?.IsAdmin == true) || (Item.Owner?.Id == userId);
            }
            else
            {
                CanEdit = false;
            }

            return Page();
        }
    }
}
