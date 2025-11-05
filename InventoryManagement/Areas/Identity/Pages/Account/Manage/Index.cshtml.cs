using InventoryManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventoryManagement.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        public IndexModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            var id = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(id)) return RedirectToPage("/Account/Login", new { area = "Identity" });
            return RedirectToPage("/Users/Details", new { area = "", id });
        }
    }
}
