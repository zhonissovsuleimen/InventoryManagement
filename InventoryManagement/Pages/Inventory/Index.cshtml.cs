using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.Pages.Inventory
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Models.Inventory.Inventory> Inventory { get; set; } = new List<Models.Inventory.Inventory>();

        public async Task OnGetAsync()
        {
            Inventory = await _context.Inventories.AsNoTracking().ToListAsync();
        }
    }
}
