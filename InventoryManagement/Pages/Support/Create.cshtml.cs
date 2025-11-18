using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InventoryManagement.Services;

namespace InventoryManagement.Pages.Support
{
    public class CreateModel : PageModel
    {
        private readonly DropboxService _dropbox;

        public CreateModel(DropboxService dropbox)
        {
            _dropbox = dropbox;
        }

        [BindProperty]
        public string? InventoryTitle { get; set; }
        [BindProperty]
        public string Priority { get; set; } = "Average";
        [BindProperty]
        public string AdminEmail { get; set; } = "rixikap583@agenra.com";
        [BindProperty]
        public string Summary { get; set; } = string.Empty;
        [BindProperty]
        public string ReportedBy { get; set; } = string.Empty;
        [BindProperty]
        public string FromLink { get; set; } = string.Empty;

        public bool IsInventoryFixed { get; set; } = false;

        public string? Message { get; set; }

        public void OnGet(string? fromUrl, string? inventoryTitle)
        {
            FromLink = fromUrl ?? string.Empty;
            InventoryTitle = inventoryTitle ?? string.Empty;
            ReportedBy = User.Identity?.Name ?? string.Empty;
            IsInventoryFixed = !string.IsNullOrEmpty(inventoryTitle);
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
