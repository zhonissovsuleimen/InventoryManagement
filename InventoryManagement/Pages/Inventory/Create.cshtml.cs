using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using InventoryManagement.Data;
using InventoryManagement.Services;
using System.Text.Json;
using Markdig;
using InventoryManagement.Models.Inventory.CustomId;
using InventoryManagement.Models.Inventory.CustomId.Element;

namespace InventoryManagement.Pages.Inventory
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public int Step { get; set; } = 1;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public class CreateInputModel
        {
            [Required]
            public string Title { get; set; }

            [Required]
            public string Description { get; set; }

            [Required]
            public int CategoryId { get; set; }

            public IFormFile? Image { get; set; }

            [Required]
            public bool IsPublic { get; set; } = false;
            public List<string> UserIds { get; set; } = [];

            public List<CustomIdElementInput> CustomIdElements { get; set; } = [];
            public CustomField? SingleLine1 { get; set; }
            public CustomField? SingleLine2 { get; set; }
            public CustomField? SingleLine3 { get; set; }
            public CustomField? MultiLine1 { get; set; }
            public CustomField? MultiLine2 { get; set; }
            public CustomField? MultiLine3 { get; set; }
            public CustomField? NumericLine1 { get; set; }
            public CustomField? NumericLine2 { get; set; }
            public CustomField? NumericLine3 { get; set; }
            public CustomField? BoolLine1 { get; set; }
            public CustomField? BoolLine2 { get; set; }
            public CustomField? BoolLine3 { get; set; }
        }

        public class CustomIdElementInput
        {
            [Required]
            public string Type { get; set; } = string.Empty;
            public string? SeparatorBefore { get; set; }
            public string? SeparatorAfter { get; set; }

            public string? FixedText { get; set; }
            public string? DateTimeFormat { get; set; }
            public string? PaddingChar { get; set; }
            public string? Radix { get; set; }
        }

        public SelectList Categories { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Step = ClampStep(Step);
            await LoadCategoriesAsync();
            return Page();
        }

        [BindProperty]
        public CreateInputModel Input { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Step = DetermineStepFromModelState();
                await LoadCategoriesAsync();
                return Page();
            }

            var category = await _context.Categories.FindAsync(Input.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("Input.CategoryId", "Invalid category selected.");
                Step = 1;
                await LoadCategoriesAsync();
                return Page();
            }

            var owner = await _userManager.GetUserAsync(User);
            if (owner == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                Step = 1;
                await LoadCategoriesAsync();
                return Page();
            }

            bool HasValid(CustomField? cf) => cf != null && cf.IsUsed && !string.IsNullOrWhiteSpace(cf.Title) && !string.IsNullOrWhiteSpace(cf.Description);
            var anyUsedAndValid = HasValid(Input.SingleLine1)
                           || HasValid(Input.SingleLine2)
                           || HasValid(Input.SingleLine3)
                           || HasValid(Input.MultiLine1)
                           || HasValid(Input.MultiLine2)
                           || HasValid(Input.MultiLine3)
                           || HasValid(Input.NumericLine1)
                           || HasValid(Input.NumericLine2)
                           || HasValid(Input.NumericLine3)
                           || HasValid(Input.BoolLine1)
                           || HasValid(Input.BoolLine2)
                           || HasValid(Input.BoolLine3);

            var fields = new (string Name, CustomField? Field)[]
            {
                ("SingleLine1", Input.SingleLine1),
                ("SingleLine2", Input.SingleLine2),
                ("SingleLine3", Input.SingleLine3),
                ("MultiLine1", Input.MultiLine1),
                ("MultiLine2", Input.MultiLine2),
                ("MultiLine3", Input.MultiLine3),
                ("NumericLine1", Input.NumericLine1),
                ("NumericLine2", Input.NumericLine2),
                ("NumericLine3", Input.NumericLine3),
                ("BoolLine1", Input.BoolLine1),
                ("BoolLine2", Input.BoolLine2),
                ("BoolLine3", Input.BoolLine3),
            };

            bool anyValid = false;
            bool anyUsed = false;
            foreach (var f in fields)
            {
                var cf = f.Field;
                if (cf == null) continue;
                if (!cf.IsUsed) continue;
                anyUsed = true;
                if (string.IsNullOrWhiteSpace(cf.Title))
                {
                    ModelState.AddModelError($"Input.{f.Name}.Title", "The title of the custom field is required.");
                }
                if (string.IsNullOrWhiteSpace(cf.Description))
                {
                    ModelState.AddModelError($"Input.{f.Name}.Description", "The description of the custom field is required.");
                }
                if (!string.IsNullOrWhiteSpace(cf.Title) && !string.IsNullOrWhiteSpace(cf.Description))
                {
                    anyValid = true;
                }
            }

            if (!anyValid)
            {
                if (!anyUsed && fields.Length > 0)
                {
                    ModelState.AddModelError($"Input.{fields[0].Name}.IsUsed", "Enable and fill at least one custom field.");
                }
                Step = 3;
                await LoadCategoriesAsync();
                return Page();
            }

            string? imageUrl = null;
            if (Input.Image != null)
            {
                imageUrl = await AzureBlobulator3000.UploadImageAsync(Input.Image);
            }

            List<AppUser> users = [];
            if (!Input.IsPublic && Input.UserIds.Count > 0)
            {
                users = await _context.Users
                    .Where(u => Input.UserIds.Contains(u.Id))
                    .ToListAsync();
            }

            CustomId? customId = null;
            if (Input.CustomIdElements.Count > 0 )
            {
                customId = new CustomId
                {
                    Guid = Guid.NewGuid(),
                    Elements = []
                };

                foreach (var e in Input.CustomIdElements)
                {
                    AbstractElement? element = e.Type switch
                    {
                        "FixedText" => new FixedTextElement { FixedText = e.FixedText ?? string.Empty },
                        "Digit6" => new Digit6Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "Digit9" => new Digit9Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "Bit20" => new Bit20Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "Bit32" => new Bit32Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "DateTime" => new DateTimeElement { DateTimeFormat = string.IsNullOrWhiteSpace(e.DateTimeFormat) ? "yyyy" : e.DateTimeFormat },
                        "Guid" => new GuidElement(),
                        _ => null
                    };
                    if (element == null) continue;
                    element.SeparatorBefore = ParseChar(e.SeparatorBefore);
                    element.SeparatorAfter = ParseChar(e.SeparatorAfter);
                    customId.Elements.Add(element);
                }
            }

            var inventory = new Models.Inventory.Inventory
            {
                Guid = Guid.NewGuid(),
                Title = Input.Title,
                Description = Input.Description,
                Category = category,
                ImageUrl = imageUrl,
                IsPublic = Input.IsPublic,
                AllowedUsers = users,
                Owner = owner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CustomId = customId!,
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

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostGenerateMarkdown()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return Content(string.Empty, "text/html");
            }

            var req = JsonSerializer.Deserialize<GenerateMarkdownRequest>(body, JsonOptions);
            var raw = req?.Text ?? string.Empty;
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(raw, pipeline);
            return Content(html, "text/html");
        }

        public async Task<IActionResult> OnPostGenerateCustomId()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return Content(string.Empty, "text/plain");
            }

            var req = JsonSerializer.Deserialize<GenerateCustomIdRequest>(body, JsonOptions);
            if (req == null || req.Elements == null || req.Elements.Count == 0)
            {
                return Content(string.Empty, "text/plain");
            }

            CustomId? customId = new CustomId
            {
                Guid = Guid.NewGuid(),
                Elements = []
            };

            foreach (var e in req.Elements)
            {
                AbstractElement? element = e.Type switch
                {
                    "FixedText" => new FixedTextElement { FixedText = e.FixedText ?? string.Empty },
                    "Digit6" => new Digit6Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                    "Digit9" => new Digit9Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                    "Bit20" => new Bit20Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                    "Bit32" => new Bit32Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                    "DateTime" => new DateTimeElement { DateTimeFormat = string.IsNullOrWhiteSpace(e.DateTimeFormat) ? "yyyy" : e.DateTimeFormat },
                    "Guid" => new GuidElement(),
                    _ => null
                };
                if (element == null) continue;
                element.SeparatorBefore = ParseChar(e.SeparatorBefore);
                element.SeparatorAfter = ParseChar(e.SeparatorAfter);
                customId.Elements.Add(element);
            }

            var tmp = new Models.Inventory.Inventory { CustomId = customId };
            var result = tmp.GenerateCustomId(req.Seed);
            return Content(result, "text/plain");
        }

        private sealed class GenerateMarkdownRequest
        {
            public string? Text { get; set; }
        }

        private sealed class GenerateCustomIdRequest
        {
            public int? Seed { get; set; }
            public List<CustomIdElementInput>? Elements { get; set; }
        }

        private static char? ParseChar(string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            return s[0];
        }

        private static Radix ParseRadix(string? s)
        {
            return s switch
            {
                "2" => Radix.Binary,
                "8" => Radix.Octal,
                "10" => Radix.Decimal,
                "16" => Radix.Hexadecimal,
                _ => Radix.Decimal
            };
        }

        private int ClampStep(int step) => Math.Clamp(step, 1, 4);

        private int DetermineStepFromModelState()
        {
            var keysWithErrors = ModelState
                .Where(kvp => kvp.Value is { Errors.Count: > 0 })
                .Select(kvp => kvp.Key)
                .ToList();

            bool isStep1 = keysWithErrors.Any(k =>
                k.StartsWith("Input.Title") ||
                k.StartsWith("Input.Description") ||
                k.StartsWith("Input.CategoryId") ||
                k.StartsWith("Input.Image"));
            if (isStep1) return 1;

            bool isStep2 = keysWithErrors.Any(k =>
                k.StartsWith("Input.CustomIdElements"));
            if (isStep2) return 2;

            bool isStep4 = keysWithErrors.Any(k =>
                k.StartsWith("Input.IsPublic") ||
                k.StartsWith("Input.UserIds"));
            if (isStep4) return 4;

            return 0;
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            Categories = new SelectList(categories, "Id", "Name");
        }
    }
}
