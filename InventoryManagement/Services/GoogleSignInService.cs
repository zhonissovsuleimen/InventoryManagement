using System.Security.Claims;
using InventoryManagement.Models;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Services
{
    public class GoogleSignInService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public GoogleSignInService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task HandleCreatingTicket(OAuthCreatingTicketContext context)
        {
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var firstName = context.Principal?.FindFirstValue(ClaimTypes.GivenName);
            var lastName = context.Principal?.FindFirstValue(ClaimTypes.Surname);
            var providerKey = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(email))
            {
                context.Fail("Google account has no email.");
                return;
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    context.Fail("Failed to create user");
                    return;
                }
            }
            else
            {
                bool changed = false;
                if (string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(firstName))
                {
                    user.FirstName = firstName;
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    user.LastName = lastName;
                    changed = true;
                }
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    changed = true;
                }
                if (changed)
                {
                    await _userManager.UpdateAsync(user);
                }
            }

            if (!string.IsNullOrEmpty(providerKey))
            {
                var logins = await _userManager.GetLoginsAsync(user);
                if (!logins.Any(l => l.LoginProvider == "Google"))
                {
                    await _userManager.AddLoginAsync(
                        user,
                        new UserLoginInfo("Google", providerKey, "Google"));
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: true);
        }
    }
}
