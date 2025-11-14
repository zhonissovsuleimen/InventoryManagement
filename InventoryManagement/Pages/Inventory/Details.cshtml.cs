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

        // Aggregation view model for insights tab
        public AggregationResult? Aggregates { get; set; }

        public sealed class AggregationResult
        {
            public int TotalItems { get; set; }
            public double AverageLikesPerItem { get; set; }
            public int MaxLikesPerItem { get; set; }
            public double GlobalPercentPublicInventories { get; set; }
            public double GlobalAvgAllowedUsersForPrivateInventories { get; set; }
            public Dictionary<string, NumericStats> Numeric { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, TextStats> Text { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, BoolStats> Bool { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            public sealed class NumericStats
            {
                public int Count { get; set; }
                public double? Average { get; set; }
                public double? Min { get; set; }
                public double? Max { get; set; }
            }

            public sealed class CountedValue
            {
                public string Value { get; set; } = string.Empty;
                public int Count { get; set; }
            }

            public sealed class TextStats
            {
                public int Count { get; set; }
                public List<CountedValue> Top { get; set; } = new();
            }

            public sealed class BoolStats
            {
                public int TrueCount { get; set; }
                public int FalseCount { get; set; }
                public int NullCount { get; set; }
            }
        }

        public class EditInputModel
        {
            public int Version { get; set; }

            [Required]
            public string? Title { get; set; }
            [Required]
            public string? Description { get; set; }
            [Required]
            public int? CategoryId { get; set; }
            public IFormFile? Image { get; set; }
            public bool IsPublic { get; set; }
            public List<string> UserIds { get; set; } = new();

            public List<CustomIdElementInput> CustomIdElements { get; set; } = new();

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
            public CustomField? LinkLine1 { get; set; }
            public CustomField? LinkLine2 { get; set; }
            public CustomField? LinkLine3 { get; set; }
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

        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
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

                var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                var currentUser = string.IsNullOrEmpty(userId)
                    ? null
                    : await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                CanEdit = (currentUser?.IsAdmin == true) || (inventory.Owner?.Id == userId);
                var isAuthenticated = !string.IsNullOrEmpty(userId);
                var isAllowed = (inventory.AllowedUsers?.Any(u => u.Id == userId) == true);
                CanModerate = CanEdit || (inventory.IsPublic ? isAuthenticated : isAllowed);

                // Defer insights calculation to tab click
                Aggregates = null;

                if (EditMode && CanEdit)
                {
                    PopulateEditModelFromInventory(inventory);
                    Edit.Version = inventory.Version;
                    await PopulateCategoriesSelectListAsync(Edit.CategoryId);
                }

                return Page();
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> OnGetInsightsAsync(Guid guid)
        {
            var inv = await _context.Inventories
                .Include(i => i.Items)
                    .ThenInclude(it => it.Likes)
                .FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();

            var result = await BuildAggregatesAsync(inv);
            return new JsonResult(result);
        }

        private async Task<AggregationResult> BuildAggregatesAsync(Models.Inventory.Inventory inv)
        {
            var result = new AggregationResult();
            var items = inv.Items ?? new List<Item>();
            result.TotalItems = items.Count;

            // Likes stats
            if (items.Count > 0)
            {
                var likeCounts = items.Select(it => it.Likes?.Count ?? 0).ToList();
                result.AverageLikesPerItem = likeCounts.Count > 0 ? likeCounts.Average() : 0.0;
                result.MaxLikesPerItem = likeCounts.Count > 0 ? likeCounts.Max() : 0;
            }

            // Global visibility stats across all inventories
            var totalInv = await _context.Inventories.AsNoTracking().CountAsync();
            if (totalInv > 0)
            {
                var publicInv = await _context.Inventories.AsNoTracking().CountAsync(i => i.IsPublic);
                result.GlobalPercentPublicInventories = (double)publicInv / totalInv * 100.0;

                var privateAllowedCounts = await _context.Inventories.AsNoTracking()
                    .Where(i => !i.IsPublic)
                    .Select(i => i.AllowedUsers.Count)
                    .ToListAsync();
                result.GlobalAvgAllowedUsersForPrivateInventories = privateAllowedCounts.Count > 0 ? privateAllowedCounts.Average() : 0.0;
            }

            string Label(string fallback, CustomField? cf)
            {
                var t = cf?.Title;
                return string.IsNullOrWhiteSpace(t) ? fallback : t!;
            }

            void AddNumeric(string label, Func<Item, double?> selector, CustomField? cf)
            {
                if (cf?.IsUsed != true) return;
                var vals = items.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                if (vals.Count == 0) return;
                result.Numeric[label] = new AggregationResult.NumericStats
                {
                    Count = vals.Count,
                    Average = vals.Average(),
                    Min = vals.Min(),
                    Max = vals.Max()
                };
            }

            void AddText(string label, Func<Item, string?> selector, CustomField? cf)
            {
                if (cf?.IsUsed != true) return;
                var groups = items
                    .Select(selector)
                    .Select(s => (s ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new { Key = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Key)
                    .Take(5)
                    .ToList();
                if (groups.Count == 0) return;
                result.Text[label] = new AggregationResult.TextStats
                {
                    Count = groups.Sum(g => g.Count),
                    Top = groups.Select(g => new AggregationResult.CountedValue { Value = g.Key, Count = g.Count }).ToList()
                };
            }

            void AddBool(string label, Func<Item, bool?> selector, CustomField? cf)
            {
                if (cf?.IsUsed != true) return;
                int t = 0, f = 0, n = 0;
                foreach (var v in items.Select(selector))
                {
                    if (!v.HasValue) n++;
                    else if (v.Value) t++;
                    else f++;
                }
                if (t + f + n == 0) return;
                result.Bool[label] = new AggregationResult.BoolStats { TrueCount = t, FalseCount = f, NullCount = n };
            }

            // Numeric fields
            AddNumeric(Label("Numeric 1", inv.NumericLine1), it => it.NumericLine1, inv.NumericLine1);
            AddNumeric(Label("Numeric 2", inv.NumericLine2), it => it.NumericLine2, inv.NumericLine2);
            AddNumeric(Label("Numeric 3", inv.NumericLine3), it => it.NumericLine3, inv.NumericLine3);

            // Single-line text fields
            AddText(Label("Single line 1", inv.SingleLine1), it => it.SingleLine1, inv.SingleLine1);
            AddText(Label("Single line 2", inv.SingleLine2), it => it.SingleLine2, inv.SingleLine2);
            AddText(Label("Single line 3", inv.SingleLine3), it => it.SingleLine3, inv.SingleLine3);

            // Multi-line text fields
            AddText(Label("Multi line 1", inv.MultiLine1), it => it.MultiLine1, inv.MultiLine1);
            AddText(Label("Multi line 2", inv.MultiLine2), it => it.MultiLine2, inv.MultiLine2);
            AddText(Label("Multi line 3", inv.MultiLine3), it => it.MultiLine3, inv.MultiLine3);

            // Boolean fields
            AddBool(Label("Boolean 1", inv.BoolLine1), it => it.BoolLine1, inv.BoolLine1);
            AddBool(Label("Boolean 2", inv.BoolLine2), it => it.BoolLine2, inv.BoolLine2);
            AddBool(Label("Boolean 3", inv.BoolLine3), it => it.BoolLine3, inv.BoolLine3);

            return result;
        }

        private async Task PopulateCategoriesSelectListAsync(int? selectedId)
        {
            var cats = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            Categories = new SelectList(cats, nameof(Category.Id), nameof(Category.Name), selectedId);
        }

        private void PopulateEditModelFromInventory(Models.Inventory.Inventory inv)
        {
            Edit.Title = inv.Title;
            Edit.Description = inv.Description;
            Edit.CategoryId = inv.Category?.Id;
            Edit.IsPublic = inv.IsPublic;
            Edit.UserIds = inv.AllowedUsers?.Select(u => u.Id).ToList() ?? new List<string>();

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
            Edit.LinkLine1 = CloneCustomField(inv.LinkLine1);
            Edit.LinkLine2 = CloneCustomField(inv.LinkLine2);
            Edit.LinkLine3 = CloneCustomField(inv.LinkLine3);

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
                    if (e is AbstractNumericElement num && e is not SequentialElement)
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

        public sealed class TagNameInput { public string? Tag { get; set; } }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddTagAsync(Guid guid, [FromForm] string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return BadRequest();

            var inv = await _context.Inventories
                .Include(i => i.Tags)
                .FirstOrDefaultAsync(i => i.Guid == guid);
            if (inv == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var name = tag.Trim();
            if (name.Length > 64) name = name.Substring(0, 64);

            var existingTag = await _context.Set<Tag>()
                .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

            if (existingTag == null)
            {
                existingTag = new Tag { Name = name };
                _context.Tags.Add(existingTag);
            }

            if (inv.Tags == null) inv.Tags = new List<Tag>();

            var already = inv.Tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (!already)
            {
                inv.Tags.Add(existingTag);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { guid });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveTagAsync(Guid guid, [FromBody] TagNameInput input)
        {
            var raw = input?.Tag?.Trim();
            if (string.IsNullOrWhiteSpace(raw)) return BadRequest();

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

            var tag = inv.Tags.FirstOrDefault(t => string.Equals(t.Name, raw, StringComparison.OrdinalIgnoreCase));
            if (tag == null)
            {
                return new JsonResult(new { removed = false });
            }

            inv.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return new JsonResult(new { removed = true, tag = tag.Name });
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

            if (!ModelState.IsValid)
            {
                Inventory = inv;
                CanEdit = true;
                EditMode = true;
                await PopulateCategoriesSelectListAsync(Edit.CategoryId);
                return Page();
            }

            if (Edit.Version != inv.Version)
            {
                ModelState.AddModelError(string.Empty, $"The inventory was changed by someone else. Your version {Edit.Version}, current version {inv.Version}. Please review changes or try again.");
                Inventory = inv;
                CanEdit = true;
                EditMode = true;
                PopulateEditModelFromInventory(inv);
                Edit.Version = inv.Version;
                await PopulateCategoriesSelectListAsync(Edit.CategoryId);
                return Page();
            }

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

            if (Edit.Image != null && Edit.Image.Length > 0)
            {
                var uploadedUrl = await AzureBlobulator3000.UploadImageAsync(Edit.Image);
                if (!string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    inv.ImageUrl = uploadedUrl;
                }
            }

            inv.IsPublic = Edit.IsPublic;
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
            inv.LinkLine1 = Edit.LinkLine1;
            inv.LinkLine2 = Edit.LinkLine2;
            inv.LinkLine3 = Edit.LinkLine3;

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
                        "Sequential" => new SequentialElement(),
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
                inv.CustomId = null!;
            }

            inv.UpdatedAt = DateTime.UtcNow;
            inv.Version += 1;
            await _context.SaveChangesAsync();

            return RedirectToPage(new { guid });
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

            var result = new AutoSaveResult { Ok = true, Version = inv.Version };
            var changed = req.ChangedGroups ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool appliedAny = false;
            bool sameVersion = req.Version == inv.Version;

            bool GeneralEquals(EditInputModel? original)
            {
                if (original == null) return false;
                var catId = inv.Category?.Id;
                return string.Equals(inv.Title ?? string.Empty, original.Title ?? string.Empty, StringComparison.Ordinal)
                    && string.Equals(inv.Description ?? string.Empty, original.Description ?? string.Empty, StringComparison.Ordinal)
                    && Nullable.Equals(catId, original.CategoryId);
            }
            bool VisibilityEquals(EditInputModel? original)
            {
                if (original == null) return false;
                var isPublicEq = inv.IsPublic == original.IsPublic;
                var origIds = new HashSet<string>(original.UserIds ?? new List<string>());
                var serverIds = new HashSet<string>(inv.AllowedUsers?.Select(u => u.Id) ?? Enumerable.Empty<string>());
                return isPublicEq && origIds.SetEquals(serverIds);
            }
            static bool CustomFieldEquals(CustomField? a, CustomField? b)
            {
                if (a == null && b == null) return true;
                if (a == null || b == null) return false;
                return a.IsUsed == b.IsUsed
                    && (a.Position ?? short.MaxValue) == (b.Position ?? short.MaxValue)
                    && string.Equals(a.Title ?? string.Empty, b.Title ?? string.Empty, StringComparison.Ordinal)
                    && string.Equals(a.Description ?? string.Empty, b.Description ?? string.Empty, StringComparison.Ordinal);
            }
            bool CustomFieldsEquals(EditInputModel? original)
            {
                if (original == null) return false;
                return CustomFieldEquals(inv.SingleLine1, original.SingleLine1)
                    && CustomFieldEquals(inv.SingleLine2, original.SingleLine2)
                    && CustomFieldEquals(inv.SingleLine3, original.SingleLine3)
                    && CustomFieldEquals(inv.MultiLine1, original.MultiLine1)
                    && CustomFieldEquals(inv.MultiLine2, original.MultiLine2)
                    && CustomFieldEquals(inv.MultiLine3, original.MultiLine3)
                    && CustomFieldEquals(inv.NumericLine1, original.NumericLine1)
                    && CustomFieldEquals(inv.NumericLine2, original.NumericLine2)
                    && CustomFieldEquals(inv.NumericLine3, original.NumericLine3)
                    && CustomFieldEquals(inv.BoolLine1, original.BoolLine1)
                    && CustomFieldEquals(inv.BoolLine2, original.BoolLine2)
                    && CustomFieldEquals(inv.BoolLine3, original.BoolLine3)
                    && CustomFieldEquals(inv.LinkLine1, original.LinkLine1)
                    && CustomFieldEquals(inv.LinkLine2, original.LinkLine2)
                    && CustomFieldEquals(inv.LinkLine3, original.LinkLine3);
            }
            bool CustomIdEquals(List<CustomIdElementInput>? original)
            {
                var server = new List<CustomIdElementInput>();
                if (inv.CustomId?.Elements != null)
                {
                    foreach (var e in inv.CustomId.Elements.Where(x => x != null).OrderBy(x => x.Position ?? short.MaxValue).ThenBy(x => x.Id))
                    {
                        var item = new CustomIdElementInput
                        {
                            Type = e.GetType().Name.Replace("Element", ""),
                            SeparatorBefore = e.SeparatorBefore?.ToString(),
                            SeparatorAfter = e.SeparatorAfter?.ToString(),
                            Position = e.Position
                        };
                        if (e is AbstractNumericElement num && e is not SequentialElement)
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
                        server.Add(item);
                    }
                }

                var a = server;
                var b = (original ?? new List<CustomIdElementInput>())
                        .Where(x => x != null)
                        .OrderBy(x => x.Position ?? short.MaxValue)
                        .ThenBy(x => x.Type)
                        .ToList();
                if (a.Count != b.Count) return false;
                for (int i = 0; i < a.Count; i++)
                {
                    var x = a[i];
                    var y = b[i];
                    if (!string.Equals(x.Type, y.Type, StringComparison.Ordinal)) return false;
                    if (!string.Equals(x.SeparatorBefore ?? string.Empty, y.SeparatorBefore ?? string.Empty, StringComparison.Ordinal)) return false;
                    if (!string.Equals(x.SeparatorAfter ?? string.Empty, y.SeparatorAfter ?? string.Empty, StringComparison.Ordinal)) return false;
                    if (!string.Equals(x.FixedText ?? string.Empty, y.FixedText ?? string.Empty, StringComparison.Ordinal)) return false;
                    if (!string.IsNullOrEmpty(x.DateTimeFormat) || !string.IsNullOrEmpty(y.DateTimeFormat))
                    {
                        // tolerate null vs empty
                        if (!string.Equals(x.DateTimeFormat ?? string.Empty, y.DateTimeFormat ?? string.Empty, StringComparison.Ordinal)) return false;
                    }
                    if (!string.Equals(x.PaddingChar ?? string.Empty, y.PaddingChar ?? string.Empty, StringComparison.Ordinal)) return false;
                    if (!string.Equals(x.Radix ?? string.Empty, y.Radix ?? string.Empty, StringComparison.Ordinal)) return false;
                }
                return true;
            }

            void ApplyGeneral(EditInputModel? change)
            {
                if (change == null) return;
                if (!string.IsNullOrWhiteSpace(change.Title)) inv.Title = change.Title.Trim();
                inv.Description = change.Description ?? string.Empty;
                if (change.CategoryId.HasValue)
                {
                    var cat = _context.Categories.FirstOrDefault(c => c.Id == change.CategoryId.Value);
                    if (cat != null) inv.Category = cat;
                }
            }
            async Task ApplyVisibilityAsync(EditInputModel? change)
            {
                if (change == null) return;
                inv.IsPublic = change.IsPublic;
                if (!inv.IsPublic)
                {
                    var ids = (change.UserIds ?? new List<string>()).Distinct().ToList();
                    var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
                    inv.AllowedUsers.Clear();
                    foreach (var u in users) inv.AllowedUsers.Add(u);
                }
                else
                {
                    inv.AllowedUsers.Clear();
                }
            }
            void ApplyCustomFields(EditInputModel? change)
            {
                if (change == null) return;
                inv.SingleLine1 = change.SingleLine1;
                inv.SingleLine2 = change.SingleLine2;
                inv.SingleLine3 = change.SingleLine3;
                inv.MultiLine1 = change.MultiLine1;
                inv.MultiLine2 = change.MultiLine2;
                inv.MultiLine3 = change.MultiLine3;
                inv.NumericLine1 = change.NumericLine1;
                inv.NumericLine2 = change.NumericLine2;
                inv.NumericLine3 = change.NumericLine3;
                inv.BoolLine1 = change.BoolLine1;
                inv.BoolLine2 = change.BoolLine2;
                inv.BoolLine3 = change.BoolLine3;
                inv.LinkLine1 = change.LinkLine1;
                inv.LinkLine2 = change.LinkLine2;
                inv.LinkLine3 = change.LinkLine3;
            }
            void ApplyCustomId(EditInputModel? change)
            {
                if (change == null) return;
                var list = change.CustomIdElements ?? new List<CustomIdElementInput>();
                if (list.Count == 0)
                {
                    inv.CustomId = null!;
                    return;
                }
                if (inv.CustomId == null)
                {
                    inv.CustomId = new CustomId { Guid = Guid.NewGuid(), Elements = new List<AbstractElement>() };
                }
                else if (inv.CustomId.Elements != null) inv.CustomId.Elements.Clear();
                else inv.CustomId.Elements = new List<AbstractElement>();

                short pos = 0;
                foreach (var e in list.OrderBy(e => e.Position ?? short.MaxValue))
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
                        "Sequential" => new SequentialElement(),
                        _ => null
                    };
                    if (element == null) continue;
                    element.SeparatorBefore = ParseChar(e.SeparatorBefore);
                    element.SeparatorAfter = ParseChar(e.SeparatorAfter);
                    element.Position = pos++;
                    inv.CustomId.Elements.Add(element);
                }
            }

            async Task<bool> ApplyGroupAsync(string group)
            {
                var g = group.ToLowerInvariant();
                if (sameVersion)
                {
                    if (g == "general") { ApplyGeneral(req.Changes); return true; }
                    if (g == "visibility") { await ApplyVisibilityAsync(req.Changes); return true; }
                    if (g == "customfields") { ApplyCustomFields(req.Changes); return true; }
                    if (g == "customid") { ApplyCustomId(req.Changes); return true; }
                    return false;
                }
                else
                {
                    if (g == "general")
                    {
                        if (GeneralEquals(req.Original)) { ApplyGeneral(req.Changes); return true; }
                        result.Conflicts.Add(new ConflictInfo { Group = "general", Message = "General info was modified by someone else." });
                        return false;
                    }
                    if (g == "visibility")
                    {
                        if (VisibilityEquals(req.Original)) { await ApplyVisibilityAsync(req.Changes); return true; }
                        result.Conflicts.Add(new ConflictInfo { Group = "visibility", Message = "Access level or allowed users were modified by someone else." });
                        return false;
                    }
                    if (g == "customfields")
                    {
                        if (CustomFieldsEquals(req.Original)) { ApplyCustomFields(req.Changes); return true; }
                        result.Conflicts.Add(new ConflictInfo { Group = "customFields", Message = "Custom fields were modified by someone else." });
                        return false;
                    }
                    if (g == "customid")
                    {
                        if (CustomIdEquals(req.Original?.CustomIdElements)) { ApplyCustomId(req.Changes); return true; }
                        result.Conflicts.Add(new ConflictInfo { Group = "customId", Message = "Custom ID structure was modified by someone else." });
                        return false;
                    }
                    return false;
                }
            }

            foreach (var group in changed)
            {
                var ok = await ApplyGroupAsync(group);
                if (ok)
                {
                    result.AppliedGroups.Add(group);
                    appliedAny = true;
                }
            }

            if (appliedAny)
            {
                inv.UpdatedAt = DateTime.UtcNow;
                inv.Version += 1;
                await _context.SaveChangesAsync();
                result.Version = inv.Version;
            }

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostGenerateCustomId()
        {
            using var reader = new System.IO.StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return Content(string.Empty, "text/plain");
            }

            var req = System.Text.Json.JsonSerializer.Deserialize<GenerateCustomIdRequest>(body, JsonOptions);
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
                    "Sequential" => new SequentialElement(),
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
