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

                return Page();
            }

            return NotFound();
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
    }
}
