using InventoryManagement.Data;
using InventoryManagement.Database;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using InventoryManagement.Models.Inventory.CustomId.Element;
using InventoryManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

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

        private static bool CanCreateItem(Models.Inventory.Inventory inv, AppUser? user)
        {
            if (user == null) return false; // must be authenticated
            if (user.IsAdmin) return true;
            if (inv.Owner?.Id == user.Id) return true;
            if (inv.IsPublic) return true; // any authenticated user
            // private: only allowed users
            return inv.AllowedUsers?.Any(u => u.Id == user.Id) == true;
        }

        public async Task<IActionResult> OnGetAsync(Guid? guid)
        {
            if (guid != null)
            {
                Inventory = await _context.Inventories
                    .Include(i => i.CustomId)
                        .ThenInclude(c => c.Elements)
                    .Include(i => i.AllowedUsers)
                    .Include(i => i.Owner)
                    .FirstOrDefaultAsync(i => i.Guid == guid.Value);

                InventoryGuid = guid;
            }

            if (Inventory == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }
            if (!CanCreateItem(Inventory, currentUser))
            {
                return Forbid();
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
                    .Include(i => i.AllowedUsers)
                    .Include(i => i.Owner)
                    .FirstOrDefaultAsync(i => i.Guid == InventoryGuid.Value);
            }

            if (Inventory == null)
            {
                ModelState.AddModelError(string.Empty, "Inventory not found.");
            }

            var owner = await _userManager.GetUserAsync(User);
            if (owner == null)
            {
                return Unauthorized();
            }

            if (Inventory != null && !CanCreateItem(Inventory, owner))
            {
                return Forbid();
            }

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
                var elements = (Inventory.CustomId.Elements ?? new List<InventoryManagement.Models.Inventory.CustomId.Element.AbstractElement>())
                    .Where(e => e != null)
                    .OrderBy(e => e.Position ?? short.MaxValue)
                    .ThenBy(e => e.Id)
                    .ToList();

                var seqElem = elements.OfType<SequentialElement>().FirstOrDefault();
                if (seqElem == null)
                {
                    item.CustomId = Inventory.GenerateCustomId();
                }
                else
                {
                    // Build a simple pattern: capture numeric group for sequential element
                    var pattern = BuildDecimalPattern(elements, seqElem);
                    var regex = new Regex(pattern, RegexOptions.Compiled);
                    var invId = Inventory.Id;
                    var existingIds = await _context.Items
                        .AsNoTracking()
                        .Where(it => EF.Property<int>(it, "InventoryId") == invId)
                        .Select(it => it.CustomId)
                        .ToListAsync();

                    long maxVal = 0;
                    foreach (var cid in existingIds)
                    {
                        if (string.IsNullOrEmpty(cid)) continue;
                        var m = regex.Match(cid);
                        if (!m.Success || m.Groups.Count < 2) continue;
                        var part = m.Groups[1].Value;
                        if (long.TryParse(part, out var val) && val > maxVal)
                        {
                            maxVal = val;
                        }
                    }

                    long next = Math.Max(0, maxVal) + 1; // start at 1
                    string seqText = next.ToString();

                    // Build the final CustomId with the computed sequence
                    var rng = new Random();
                    var sb = new StringBuilder();
                    foreach (var e in elements)
                    {
                        if (e.SeparatorBefore.HasValue) sb.Append(e.SeparatorBefore.Value);
                        if (e is SequentialElement)
                        {
                            sb.Append(seqText);
                        }
                        else
                        {
                            sb.Append(e.Generate(rng));
                        }
                        if (e.SeparatorAfter.HasValue) sb.Append(e.SeparatorAfter.Value);
                    }
                    item.CustomId = sb.ToString();
                }
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Redirect to the details page with the created item's GUID
            return RedirectToPage("./Details", new { guid = item.Guid });
        }

        private static string Escape(char? ch) => ch.HasValue ? Regex.Escape(ch.Value.ToString()) : string.Empty;

        private static string BuildDecimalPattern(List<InventoryManagement.Models.Inventory.CustomId.Element.AbstractElement> elements, SequentialElement seq)
        {
            var sb = new StringBuilder();
            sb.Append('^');
            foreach (var e in elements)
            {
                if (e.SeparatorBefore.HasValue) sb.Append(Escape(e.SeparatorBefore));
                if (ReferenceEquals(e, seq))
                {
                    sb.Append('(').Append("[0-9]+").Append(')');
                }
                else
                {
                    sb.Append(".*?");
                }
                if (e.SeparatorAfter.HasValue) sb.Append(Escape(e.SeparatorAfter));
            }
            sb.Append('$');
            return sb.ToString();
        }
    }
}
