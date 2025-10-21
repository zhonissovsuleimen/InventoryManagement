using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using InventoryManagement.Data;

namespace InventoryManagement.Pages
{
    public class InventoryCreateModel : PageModel
    {
        private readonly InventoryManagement.Data.ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public InventoryCreateModel(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

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

        public SelectList Categories { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCategoriesAsync();
            return Page();
        }

        [BindProperty]
        public CreateInputModel Input { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            var category = await _context.Categories.FindAsync(Input.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("Input.CategoryId", "Invalid category selected.");
                await LoadCategoriesAsync();
                return Page();
            }

            var owner = await _userManager.GetUserAsync(User);
            if (owner == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
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

            if (!anyUsedAndValid)
            {
                ModelState.AddModelError(string.Empty, "Add at least one valid custom field.");
                await LoadCategoriesAsync();
                return Page();
            }

            //todo store image on azure web and get a url back
            string? imageUrl = null;

            List<AppUser> users = [];
            if (!Input.IsPublic && Input.UserIds.Count > 0)
            {
                Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                users = await _context.Users
                    .Where(u => Input.UserIds.Contains(u.Id))
                    .ToListAsync();
            }
            Console.WriteLine($"{users}");


            var inventory = new Inventory
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

        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            Categories = new SelectList(categories, "Id", "Name");
        }
    }
}
