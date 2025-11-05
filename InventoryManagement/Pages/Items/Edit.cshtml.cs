using System.Security.Claims;
using InventoryManagement.Data;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Pages.Items
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Item? Item { get; set; }
        public InventoryManagement.Models.Inventory.Inventory? Inventory { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? Guid { get; set; }

        [BindProperty]
        public int Version { get; set; }

        public class EditInputModel
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

        [BindProperty]
        public EditInputModel Input { get; set; } = new();

        private static bool CustomFieldsEquals(Item item, EditInputModel? original)
        {
            if (original == null) return false;
            return string.Equals(item.SingleLine1 ?? string.Empty, original.SingleLine1 ?? string.Empty, StringComparison.Ordinal)
                && string.Equals(item.SingleLine2 ?? string.Empty, original.SingleLine2 ?? string.Empty, StringComparison.Ordinal)
                && string.Equals(item.SingleLine3 ?? string.Empty, original.SingleLine3 ?? string.Empty, StringComparison.Ordinal)
                && string.Equals(item.MultiLine1 ?? string.Empty, original.MultiLine1 ?? string.Empty, StringComparison.Ordinal)
                && string.Equals(item.MultiLine2 ?? string.Empty, original.MultiLine2 ?? string.Empty, StringComparison.Ordinal)
                && string.Equals(item.MultiLine3 ?? string.Empty, original.MultiLine3 ?? string.Empty, StringComparison.Ordinal)
                && Nullable.Equals(item.NumericLine1, original.NumericLine1)
                && Nullable.Equals(item.NumericLine2, original.NumericLine2)
                && Nullable.Equals(item.NumericLine3, original.NumericLine3)
                && Nullable.Equals(item.BoolLine1, original.BoolLine1)
                && Nullable.Equals(item.BoolLine2, original.BoolLine2)
                && Nullable.Equals(item.BoolLine3, original.BoolLine3);
        }

        private static void ApplyCustomFields(Item item, EditInputModel change)
        {
            item.SingleLine1 = change.SingleLine1;
            item.SingleLine2 = change.SingleLine2;
            item.SingleLine3 = change.SingleLine3;
            item.MultiLine1 = change.MultiLine1;
            item.MultiLine2 = change.MultiLine2;
            item.MultiLine3 = change.MultiLine3;
            item.NumericLine1 = change.NumericLine1;
            item.NumericLine2 = change.NumericLine2;
            item.NumericLine3 = change.NumericLine3;
            item.BoolLine1 = change.BoolLine1;
            item.BoolLine2 = change.BoolLine2;
            item.BoolLine3 = change.BoolLine3;
        }

        public async Task<IActionResult> OnGetAsync(Guid? guid)
        {
            if (guid.HasValue) Guid = guid;
            if (Guid == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Guid == Guid.Value);
            if (item == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!(user.IsAdmin || (item.Owner?.Id == user.Id))) return Forbid();

            Item = item;
            Inventory = item.Inventory;
            Version = item.Version;

            Input = new EditInputModel
            {
                SingleLine1 = item.SingleLine1,
                SingleLine2 = item.SingleLine2,
                SingleLine3 = item.SingleLine3,
                MultiLine1 = item.MultiLine1,
                MultiLine2 = item.MultiLine2,
                MultiLine3 = item.MultiLine3,
                NumericLine1 = item.NumericLine1,
                NumericLine2 = item.NumericLine2,
                NumericLine3 = item.NumericLine3,
                BoolLine1 = item.BoolLine1,
                BoolLine2 = item.BoolLine2,
                BoolLine3 = item.BoolLine3
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Guid == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Guid == Guid.Value);
            if (item == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!(user.IsAdmin || (item.Owner?.Id == user.Id))) return Forbid();

            Inventory = item.Inventory;
            Item = item;

            if (item.Version != Version)
            {
                ModelState.AddModelError(string.Empty, $"The item was changed by someone else. Your version {Version}, current version {item.Version}. Please review changes or try again.");
                Version = item.Version; // sync client version
                // re-fill Input from current DB values to show latest
                Input = new EditInputModel
                {
                    SingleLine1 = item.SingleLine1,
                    SingleLine2 = item.SingleLine2,
                    SingleLine3 = item.SingleLine3,
                    MultiLine1 = item.MultiLine1,
                    MultiLine2 = item.MultiLine2,
                    MultiLine3 = item.MultiLine3,
                    NumericLine1 = item.NumericLine1,
                    NumericLine2 = item.NumericLine2,
                    NumericLine3 = item.NumericLine3,
                    BoolLine1 = item.BoolLine1,
                    BoolLine2 = item.BoolLine2,
                    BoolLine3 = item.BoolLine3
                };
                return Page();
            }

            if (Inventory != null)
            {
                void AddFieldError(string prop, string message)
                {
                    ModelState.AddModelError($"Input.{prop}", message);
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

                foreach (var (PropName, Kind, Def) in fields)
                {
                    if (Def == null || !Def.IsUsed) continue;
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
                        if (string.IsNullOrWhiteSpace(s)) AddFieldError(PropName, $"The field '{(Def.Title ?? PropName)}' is required.");
                    }
                    else if (Kind == "numeric")
                    {
                        if (val == null) AddFieldError(PropName, $"The numeric field '{(Def.Title ?? PropName)}' is required and must be a number.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update editable fields only
            ApplyCustomFields(item, Input);

            item.UpdatedAt = DateTime.UtcNow;
            item.Version += 1;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { guid = item.Guid });
        }

        public sealed class AutoSaveRequest
        {
            public int Version { get; set; }
            public HashSet<string>? ChangedGroups { get; set; }
            public EditInputModel? Changes { get; set; }
            public EditInputModel? Original { get; set; }
        }

        public sealed class ConflictInfo
        {
            public string Group { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        public sealed class AutoSaveResult
        {
            public bool Ok { get; set; }
            public int Version { get; set; }
            public List<string> AppliedGroups { get; set; } = new();
            public List<ConflictInfo> Conflicts { get; set; } = new();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAutoSaveAsync(Guid guid, [FromBody] AutoSaveRequest req)
        {
            if (req == null) return BadRequest();
            var item = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Guid == guid);
            if (item == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!(user.IsAdmin || (item.Owner?.Id == user.Id))) return Forbid();

            var result = new AutoSaveResult { Ok = true, Version = item.Version };
            var changed = req.ChangedGroups ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool sameVersion = req.Version == item.Version;

            async Task<bool> ApplyGroupAsync(string group)
            {
                var g = (group ?? string.Empty).Trim().ToLowerInvariant();
                if (g != "customfields") return false;

                if (sameVersion)
                {
                    if (req.Changes == null) return false;
                    ApplyCustomFields(item, req.Changes);
                    return true;
                }
                else
                {
                    if (CustomFieldsEquals(item, req.Original))
                    {
                        if (req.Changes == null) return false;
                        ApplyCustomFields(item, req.Changes);
                        return true;
                    }
                    else
                    {
                        result.Conflicts.Add(new ConflictInfo { Group = "customFields", Message = "Custom fields were modified by someone else." });
                        return false;
                    }
                }
            }

            bool appliedAny = false;
            foreach (var g in changed)
            {
                if (await ApplyGroupAsync(g))
                {
                    result.AppliedGroups.Add(g);
                    appliedAny = true;
                }
            }

            if (appliedAny)
            {
                item.UpdatedAt = DateTime.UtcNow;
                item.Version += 1;
                await _context.SaveChangesAsync();
                result.Version = item.Version;
            }

            return new JsonResult(result);
        }
    }
}
