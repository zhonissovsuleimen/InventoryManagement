using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models;

namespace InventoryManagement.Pages.Items
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Item> Items { get; set; } = new List<Item>();

        public async Task OnGetAsync()
        {
            Items = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .Include(i => i.Likes)
                .OrderBy(i => i.Id)
                .ToListAsync();
        }
    }
}
