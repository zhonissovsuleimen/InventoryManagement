using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InventoryManagement.Services;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Pages.Support
{
    public class CreateModel : PageModel
    {
        private readonly DropboxService _dropbox;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(DropboxService dropbox, UserManager<AppUser> userManager)
        {
            _dropbox = dropbox;
            _userManager = userManager;
        }

        [BindProperty]
        public string? InventoryTitle { get; set; }
        [BindProperty]
        public string Priority { get; set; } = "Average";
        [BindProperty]
        public string AdminEmail { get; set; } = string.Empty;
        [BindProperty]
        public string Summary { get; set; } = string.Empty;
        [BindProperty]
        public string ReportedBy { get; set; } = string.Empty;
        [BindProperty]
        public string FromLink { get; set; } = string.Empty;

        public bool IsInventoryFixed { get; set; } = false;

        public string? Message { get; set; }

        public List<string> AdminEmails { get; set; } = new();

        public async Task OnGetAsync(string? fromUrl, string? inventoryTitle)
        {
            FromLink = fromUrl ?? string.Empty;
            InventoryTitle = inventoryTitle ?? string.Empty;
            ReportedBy = User.Identity?.Name ?? string.Empty;
            IsInventoryFixed = !string.IsNullOrEmpty(inventoryTitle);

            // Load admin emails
            AdminEmails = await _userManager.Users
                .Where(u => u.IsAdmin && !string.IsNullOrEmpty(u.Email))
                .Select(u => u.Email!)
                .ToListAsync();

            if (string.IsNullOrEmpty(AdminEmail) && AdminEmails.Count > 0)
            {
                AdminEmail = AdminEmails[0];
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Ensure admin emails available when redisplaying page
            AdminEmails = await _userManager.Users
                .Where(u => u.IsAdmin && !string.IsNullOrEmpty(u.Email))
                .Select(u => u.Email!)
                .ToListAsync();

            ReportedBy = User.Identity?.Name ?? string.Empty;

            var ticket = new
            {
                Inventory = InventoryTitle ?? string.Empty,
                Priority = Priority,
                AdminEmail = AdminEmail,
                Summary = Summary,
                ReportedBy = ReportedBy,
                Link = FromLink
            };

            var fileName = $"support_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            var (ok, err) = await _dropbox.UploadJsonAsync(ticket, fileName);
            if (ok)
            {
                Message = "Support ticket uploaded. Thank you.";
                ModelState.Clear();
                return Page();
            }
            else
            {
                Message = "Failed to upload ticket: " + (string.IsNullOrEmpty(err) ? "Unknown error." : err);
                return Page();
            }
        }
    }
}
