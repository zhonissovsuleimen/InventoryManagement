using InventoryManagement.Data;
using InventoryManagement.Database;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using InventoryManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Pages.Items
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class CreateInputModel
        {
            public string? SingleLine1 { get; set; }
            public string? SingleLine2 { get; set; }
            public string? SingleLine3 { get; set; }
            public string? MultiLine1 { get; set; }
            public string? MultiLine2 { get; set; }
            public string? MultiLine3 { get; set; }
            public double? NumericLine1 { get; set; }
            public double? NumericLine2 { get; set; }
            public double? NumericLine3 { get; set; }
            public bool? BoolLine1 { get; set; }
            public bool? BoolLine2 { get; set; }
            public bool? BoolLine3 { get; set; }
        }

        public Models.Inventory.Inventory? Inventory { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? InventoryGuid { get; set; }

        [BindProperty]
        public CreateInputModel Input { get; set; } = new CreateInputModel();

        public async Task<IActionResult> OnGetAsync(Guid? guid)
        {
            if (guid != null)
            {
                Inventory = await _context.Inventories
                    .Include(i => i.CustomId)
                        .ThenInclude(c => c.Elements)
                    .FirstOrDefaultAsync(i => i.Guid == guid.Value);

                InventoryGuid = guid;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (InventoryGuid != null)
            {
                Inventory = await _context.Inventories
                    .Include(i => i.CustomId)
                        .ThenInclude(c => c.Elements)
                    .FirstOrDefaultAsync(i => i.Guid == InventoryGuid.Value);
            }

            var owner = await _userManager.GetUserAsync(User);

            if (Inventory != null)
            {
                void AddFieldError(string propName, string message)
                {
                    ModelState.AddModelError($"Input.{propName}", message);
                }

                var fields = new (string PropName, string Kind, CustomField? Def)[]
                {
                    ("SingleLine1", "single", Inventory.SingleLine1),
                    ("SingleLine2", "single", Inventory.SingleLine2),
                    ("SingleLine3", "single", Inventory.SingleLine3),
                    ("MultiLine1", "multi", Inventory.MultiLine1),
                    ("MultiLine2", "multi", Inventory.MultiLine2),
                    ("MultiLine3", "multi", Inventory.MultiLine3),
                    ("NumericLine1", "numeric", Inventory.NumericLine1),
                    ("NumericLine2", "numeric", Inventory.NumericLine2),
                    ("NumericLine3", "numeric", Inventory.NumericLine3),
                    ("BoolLine1", "bool", Inventory.BoolLine1),
                    ("BoolLine2", "bool", Inventory.BoolLine2),
                    ("BoolLine3", "bool", Inventory.BoolLine3),
                };

                foreach (var (PropName, Kind, CustomField) in fields)
                {
                    if (CustomField == null || !CustomField.IsUsed) continue;

                    object? val = PropName switch
                    {
                        "SingleLine1" => Input.SingleLine1,
                        "SingleLine2" => Input.SingleLine2,
                        "SingleLine3" => Input.SingleLine3,
                        "MultiLine1" => Input.MultiLine1,
                        "MultiLine2" => Input.MultiLine2,
                        "MultiLine3" => Input.MultiLine3,
                        "NumericLine1" => Input.NumericLine1,
                        "NumericLine2" => Input.NumericLine2,
                        "NumericLine3" => Input.NumericLine3,
                        "BoolLine1" => Input.BoolLine1,
                        "BoolLine2" => Input.BoolLine2,
                        "BoolLine3" => Input.BoolLine3,
                        _ => null
                    };

                    if (Kind == "single" || Kind == "multi")
                    {
                        var s = val as string;
                        if (string.IsNullOrWhiteSpace(s)) AddFieldError(PropName, $"The field '{(CustomField.Title ?? PropName)}' is required.");
                    }
                    else if (Kind == "numeric")
                    {
                        if (val == null) AddFieldError(PropName, $"The numeric field '{(CustomField.Title ?? PropName)}' is required and must be a number.");
                    }
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Inventory not found.");
            }

            if (!ModelState.IsValid)
            {
                if (InventoryGuid != null && Inventory == null)
                {
                    Inventory = await _context.Inventories
                        .Include(i => i.CustomId)
                        .ThenInclude(c => c.Elements)
                        .FirstOrDefaultAsync(i => i.Guid == InventoryGuid.Value);
                }
                return Page();
            }

            var item = new Item
            {
                Guid = Guid.NewGuid(),
                Inventory = Inventory!,
                Owner = owner!,
                SingleLine1 = Input.SingleLine1,
                SingleLine2 = Input.SingleLine2,
                SingleLine3 = Input.SingleLine3,
                MultiLine1 = Input.MultiLine1,
                MultiLine2 = Input.MultiLine2,
                MultiLine3 = Input.MultiLine3,
                NumericLine1 = Input.NumericLine1,
                NumericLine2 = Input.NumericLine2,
                NumericLine3 = Input.NumericLine3,
                BoolLine1 = Input.BoolLine1,
                BoolLine2 = Input.BoolLine2,
                BoolLine3 = Input.BoolLine3
            };

            if (Inventory?.CustomId != null)
            {
                item.CustomId = Inventory.GenerateCustomId();
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Redirect to the details page with the created item's GUID
            return RedirectToPage("./Details", new { guid = item.Guid });
        }
    }
}
