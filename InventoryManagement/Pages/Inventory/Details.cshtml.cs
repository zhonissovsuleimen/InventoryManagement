using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models.Discussion;
using InventoryManagement.Models.Inventory;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory.CustomId;
using InventoryManagement.Models.Inventory.CustomId.Element;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using InventoryManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Pages.Inventory
{
    //todo azure signalr later?
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.Inventory.Inventory Inventory { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public bool EditMode { get; set; }

        public bool CanEdit { get; set; }
        public bool CanModerate { get; set; }

        public SelectList? Categories { get; set; }

        [BindProperty]
        public EditInputModel Edit { get; set; } = new();

        public class EditInputModel
        {
            // General info
            [Required]
            public string? Title { get; set; }
            [Required]
            public string? Description { get; set; }
            [Required]
            public int? CategoryId { get; set; }
            public IFormFile? Image { get; set; }
            public bool IsPublic { get; set; }
            public List<string> UserIds { get; set; } = new();

            // Custom Id
            public List<CustomIdElementInput> CustomIdElements { get; set; } = new();

            // Custom fields configuration
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
            public string Type { get; set; } = string.Empty;
            public string? SeparatorBefore { get; set; }
            public string? SeparatorAfter { get; set; }
            public string? FixedText { get; set; }
            public string? DateTimeFormat { get; set; }
            public string? PaddingChar { get; set; }
            public string? Radix { get; set; }
            public short? Position { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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
                .Include(i => i.Items)
                    .ThenInclude(it => it.Owner)
                .Include(i => i.Items)
                    .ThenInclude(it => it.Likes)
                        .ThenInclude(l => l.User)
                .Include(i => i.CustomId)
                    .ThenInclude(cid => cid.Elements)
                .Include(i => i.Tags)
                .FirstOrDefaultAsync(i => i.Guid == guid);

            if (inventory != null)
            {
                Inventory = inventory;

                // compute edit permission
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = string.IsNullOrEmpty(userId)
                    ? null
                    : await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                CanEdit = (currentUser?.IsAdmin == true) || (inventory.Owner?.Id == userId);
                var isAuthenticated = !string.IsNullOrEmpty(userId);
                var isAllowed = (inventory.AllowedUsers?.Any(u => u.Id == userId) == true);
                CanModerate = CanEdit || (inventory.IsPublic ? isAuthenticated : isAllowed);

                if (EditMode && CanEdit)
                {
                    PopulateEditModelFromInventory(inventory);
                    await PopulateCategoriesSelectListAsync(Edit.CategoryId);
                }

                return Page();
            }

            return NotFound();
        }

        private async Task PopulateCategoriesSelectListAsync(int? selectedId)
        {
            var cats = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            Categories = new SelectList(cats, nameof(Category.Id), nameof(Category.Name), selectedId);
        }

        private void PopulateEditModelFromInventory(Models.Inventory.Inventory inv)
        {
            // General info
            Edit.Title = inv.Title;
            Edit.Description = inv.Description;
            Edit.CategoryId = inv.Category?.Id;
            Edit.IsPublic = inv.IsPublic;
            Edit.UserIds = inv.AllowedUsers?.Select(u => u.Id).ToList() ?? new List<string>();

            // Custom fields
            Edit.SingleLine1 = CloneCustomField(inv.SingleLine1);
            Edit.SingleLine2 = CloneCustomField(inv.SingleLine2);
            Edit.SingleLine3 = CloneCustomField(inv.SingleLine3);
            Edit.MultiLine1 = CloneCustomField(inv.MultiLine1);
            Edit.MultiLine2 = CloneCustomField(inv.MultiLine2);
            Edit.MultiLine3 = CloneCustomField(inv.MultiLine3);
            Edit.NumericLine1 = CloneCustomField(inv.NumericLine1);
            Edit.NumericLine2 = CloneCustomField(inv.NumericLine2);
            Edit.NumericLine3 = CloneCustomField(inv.NumericLine3);
            Edit.BoolLine1 = CloneCustomField(inv.BoolLine1);
            Edit.BoolLine2 = CloneCustomField(inv.BoolLine2);
            Edit.BoolLine3 = CloneCustomField(inv.BoolLine3);

            // Custom ID
            Edit.CustomIdElements = new List<CustomIdElementInput>();
            if (inv.CustomId?.Elements != null)
            {
                foreach (var e in inv.CustomId.Elements
                    .Where(e => e != null)
                    .OrderBy(e => e.Position ?? short.MaxValue)
                    .ThenBy(e => e.Id))
                {
                    var type = e.GetType().Name.Replace("Element", "");
                    var item = new CustomIdElementInput
                    {
                        Type = type,
                        SeparatorBefore = e.SeparatorBefore?.ToString(),
                        SeparatorAfter = e.SeparatorAfter?.ToString(),
                        Position = e.Position
                    };
                    if (e is AbstractNumericElement num)
                    {
                        item.PaddingChar = num.PaddingChar?.ToString();
                        item.Radix = ((int)num.Radix).ToString();
                    }
                    else if (e is DateTimeElement dt)
                    {
                        item.DateTimeFormat = dt.DateTimeFormat;
                    }
                    else if (e is FixedTextElement ft)
                    {
                        item.FixedText = ft.FixedText;
                    }
                    Edit.CustomIdElements.Add(item);
                }
            }
        }

        private static CustomField? CloneCustomField(CustomField? input)
        {
            if (input == null) return null;
            return new CustomField
            {
                Title = input.Title,
                Description = input.Description,
                IsUsed = input.IsUsed,
                Position = input.Position
            };
        }

        public class PostInput
        {
            public string? Content { get; set; }
        }

        public async Task<IActionResult> OnGetDiscussionAsync(Guid guid, int? afterId)
        {
            var inventory = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.Guid == guid);
            if (inventory == null) return NotFound();

            var query = _context.DiscussionPosts
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => p.InventoryId == inventory.Id);

            if (afterId.HasValue && afterId.Value > 0)
            {
                query = query.Where(p => p.Id > afterId.Value);
            }

            var posts = await query.OrderBy(p => p.Id).Take(200).ToListAsync();

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            var result = posts.Select(p => new
            {
                id = p.Id,
                guid = p.Guid,
                createdAtUtc = p.CreatedAtUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                user = new
                {
                    id = p.User.Id,
                    firstName = p.User.FirstName,
                    lastName = p.User.LastName,
                    userName = p.User.UserName
                },
                html = Markdown.ToHtml(p.ContentMarkdown ?? string.Empty, pipeline)
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostDiscussionAsync(Guid guid, [FromBody] PostInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Content))
            {
                return BadRequest();
            }

            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.Guid == guid);
            if (inventory == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var now = DateTime.UtcNow;
            var post = new DiscussionPost
            {
                InventoryId = inventory.Id,
                UserId = userId,
                ContentMarkdown = input.Content.Trim(),
                CreatedAtUtc = now
            };

            _context.DiscussionPosts.Add(post);
            await _context.SaveChangesAsync();

            post = await _context.DiscussionPosts.Include(p => p.User).FirstAsync(p => p.Id == post.Id);

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            var result = new
            {
                id = post.Id,
                guid = post.Guid,
                createdAtUtc = post.CreatedAtUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                user = new
                {
                    id = post.User.Id,
                    firstName = post.User.FirstName,
                    lastName = post.User.LastName,
                    userName = post.User.UserName
                },
                html = Markdown.ToHtml(post.ContentMarkdown ?? string.Empty, pipeline)
            };

            return new JsonResult(result);
        }

        public class DeletePostsInput
        {
            public List<int>? Ids { get; set; }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteDiscussionAsync(Guid guid, [FromBody] DeletePostsInput input)
        {
            if (input?.Ids == null || input.Ids.Count == 0) return BadRequest();

            var inv = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.AllowedUsers)
                .FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = string.IsNullOrEmpty(userId)
                ? null
                : await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var isAuthenticated = !string.IsNullOrEmpty(userId);
            var isAllowed = inv.AllowedUsers.Any(u => u.Id == userId);
            var canModerate = (currentUser?.IsAdmin == true) || (inv.Owner?.Id == userId) || (inv.IsPublic ? isAuthenticated : isAllowed);
            if (!canModerate) return Forbid();

            var ids = input.Ids.Distinct().ToList();
            var posts = await _context.DiscussionPosts
                .Where(p => p.InventoryId == inv.Id && ids.Contains(p.Id))
                .ToListAsync();

            if (posts.Count == 0)
            {
                return new JsonResult(new { deletedIds = Array.Empty<int>() });
            }

            _context.DiscussionPosts.RemoveRange(posts);
            await _context.SaveChangesAsync();

            return new JsonResult(new { deletedIds = posts.Select(p => p.Id).ToList() });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddTagAsync(Guid guid, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return RedirectToPage(new { guid });
            var inv = await _context.Inventories.Include(i => i.Tags).FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var name = tag.Trim().ToLowerInvariant();
            if (name.Length > 64) name = name.Substring(0, 64);
            if (!inv.Tags.Any(t => t.Name == name))
            {
                var tagEntity = await _context.Tags.FirstOrDefaultAsync(t => t.Name == name) ?? new Tag { Name = name };
                if (tagEntity.Id == 0)
                {
                    _context.Tags.Add(tagEntity);
                    await _context.SaveChangesAsync();
                }
                inv.Tags.Add(tagEntity);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { guid });
        }

        public sealed class TagNameInput { public string? Tag { get; set; } }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveTagAsync(Guid guid, [FromBody] TagNameInput input)
        {
            var name = input?.Tag?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest();

            var inv = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Tags)
                .FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = string.IsNullOrEmpty(userId)
                ? null
                : await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var canEdit = (currentUser?.IsAdmin == true) || (inv.Owner?.Id == userId);
            if (!canEdit) return Forbid();

            var tag = inv.Tags.FirstOrDefault(t => t.Name == name);
            if (tag == null)
            {
                return new JsonResult(new { removed = false });
            }

            inv.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return new JsonResult(new { removed = true, tag = name });
        }

        public sealed class DeleteItemsInput { public List<Guid>? Guids { get; set; } }

        // bulk delete items from this inventory
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteItemsAsync(Guid guid, [FromBody] DeleteItemsInput input)
        {
            if (input?.Guids == null || input.Guids.Count == 0) return BadRequest();

            var inv = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.AllowedUsers)
                .FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = string.IsNullOrEmpty(userId)
                ? null
                : await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var isAuthenticated = !string.IsNullOrEmpty(userId);
            var isAllowed = inv.AllowedUsers.Any(u => u.Id == userId);
            var canModerate = (currentUser?.IsAdmin == true) || (inv.Owner?.Id == userId) || (inv.IsPublic ? isAuthenticated : isAllowed);
            if (!canModerate) return Forbid();

            var distinct = input.Guids.Distinct().ToList();
            var items = await _context.Items
                .Where(it => it.Inventory != null && it.Inventory.Id == inv.Id && distinct.Contains(it.Guid))
                .ToListAsync();

            if (items.Count == 0)
            {
                return new JsonResult(new { deleted = Array.Empty<Guid>() });
            }

            _context.Items.RemoveRange(items);
            await _context.SaveChangesAsync();

            return new JsonResult(new { deleted = items.Select(i => i.Guid).ToList() });
        }

        // Toggle like for an item. Returns JSON: { liked: bool, count: int }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLikeAsync(Guid guid, int itemId)
        {
            var inv = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // check for existing like via shadow properties
            var existing = await _context.ItemLikes
                .FirstOrDefaultAsync(l => EF.Property<int>(l, "ItemId") == itemId && EF.Property<string>(l, "UserId") == userId);

            bool liked;
            if (existing == null)
            {
                // Attach stubs for navigation-only entity
                var itemRef = new Item { Id = itemId };
                _context.Attach(itemRef);
                var userRef = new AppUser { Id = userId };
                _context.Attach(userRef);
                _context.ItemLikes.Add(new ItemLike { Item = itemRef, User = userRef });
                liked = true;
            }
            else
            {
                _context.ItemLikes.Remove(existing);
                liked = false;
            }
            await _context.SaveChangesAsync();

            var count = await _context.ItemLikes.CountAsync(l => EF.Property<int>(l, "ItemId") == itemId);
            return new JsonResult(new { liked, count });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditAsync(Guid guid)
        {
            var inv = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.AllowedUsers)
                .Include(i => i.CustomId)
                    .ThenInclude(cid => cid.Elements)
                .FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = string.IsNullOrEmpty(userId)
                ? null
                : await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var canEdit = (currentUser?.IsAdmin == true) || (inv.Owner?.Id == userId);
            if (!canEdit) return Forbid();

            // Server-side validation
            if (!ModelState.IsValid)
            {
                Inventory = inv; // to render page again
                CanEdit = true;
                EditMode = true;
                await PopulateCategoriesSelectListAsync(Edit.CategoryId);
                return Page();
            }

            // Update general info (non auto-generated fields)
            if (!string.IsNullOrWhiteSpace(Edit.Title)) inv.Title = Edit.Title.Trim();
            inv.Description = Edit.Description ?? string.Empty;

            if (Edit.CategoryId.HasValue)
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == Edit.CategoryId.Value);
                if (category != null)
                {
                    inv.Category = category;
                }
            }

            // Image upload via AzureBlobulator3000
            if (Edit.Image != null && Edit.Image.Length > 0)
            {
                var uploadedUrl = await AzureBlobulator3000.UploadImageAsync(Edit.Image);
                if (!string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    inv.ImageUrl = uploadedUrl;
                }
            }

            inv.IsPublic = Edit.IsPublic;
            // Allowed users management
            if (!inv.IsPublic)
            {
                var selectedIds = (Edit.UserIds ?? new List<string>()).Distinct().ToList();
                var users = await _context.Users.Where(u => selectedIds.Contains(u.Id)).ToListAsync();
                inv.AllowedUsers.Clear();
                foreach (var u in users) inv.AllowedUsers.Add(u);
            }
            else
            {
                inv.AllowedUsers.Clear();
            }

            // Update Custom Fields
            inv.SingleLine1 = Edit.SingleLine1;
            inv.SingleLine2 = Edit.SingleLine2;
            inv.SingleLine3 = Edit.SingleLine3;
            inv.MultiLine1 = Edit.MultiLine1;
            inv.MultiLine2 = Edit.MultiLine2;
            inv.MultiLine3 = Edit.MultiLine3;
            inv.NumericLine1 = Edit.NumericLine1;
            inv.NumericLine2 = Edit.NumericLine2;
            inv.NumericLine3 = Edit.NumericLine3;
            inv.BoolLine1 = Edit.BoolLine1;
            inv.BoolLine2 = Edit.BoolLine2;
            inv.BoolLine3 = Edit.BoolLine3;

            // Update CustomId
            if (Edit.CustomIdElements != null && Edit.CustomIdElements.Count > 0)
            {
                if (inv.CustomId == null)
                {
                    inv.CustomId = new CustomId { Guid = Guid.NewGuid(), Elements = new List<AbstractElement>() };
                }
                else if (inv.CustomId.Elements != null)
                {
                    inv.CustomId.Elements.Clear();
                }
                else
                {
                    inv.CustomId.Elements = new List<AbstractElement>();
                }

                short pos = 0;
                foreach (var e in Edit.CustomIdElements
                    .OrderBy(e => e.Position ?? short.MaxValue))
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
                    element.Position = pos++;
                    inv.CustomId.Elements.Add(element);
                }
            }
            else
            {
                // No elements -> remove custom id
                inv.CustomId = null!;
            }

            inv.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToPage(new { guid });
        }

        public async Task<IActionResult> OnPostGenerateCustomId()
        {
            using var reader = new System.IO.StreamReader(Request.Body);
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
                Elements = new List<AbstractElement>()
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
    }
}
