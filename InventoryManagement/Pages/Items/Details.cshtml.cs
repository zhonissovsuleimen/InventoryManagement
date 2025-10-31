using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models;

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

        public async Task<IActionResult> OnGetAsync(Guid? guid)
        {
            if (guid == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Guid == guid.Value);

            if (item == null) return NotFound();

            Item = item;
            return Page();
        }
    }
}
