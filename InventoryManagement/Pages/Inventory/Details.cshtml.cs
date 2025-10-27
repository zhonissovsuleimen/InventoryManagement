using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models.Inventory;

namespace InventoryManagement.Pages.Inventory
{
    public class DetailsModel : PageModel
    {
        private readonly InventoryManagement.Data.ApplicationDbContext _context;

        public DetailsModel(InventoryManagement.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.Inventory.Inventory Inventory { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? guid)
        {
            if (guid == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.AllowedUsers)
                .FirstOrDefaultAsync(i => i.Guid == guid);

            if (inventory != null)
            {
                Inventory = inventory;

                return Page();
            }

            return NotFound();
        }
    }
}
