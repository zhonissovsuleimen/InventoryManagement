using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using InventoryManagement.Models;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Services
{
    public class GitHubSignInService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public GitHubSignInService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task HandleCreatingTicket(OAuthCreatingTicketContext context)
        {
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                email = await GetPrimaryEmailAsync(context);
            }

            // GitHub does not have separate first/last by default, try to split the full name
            var fullName = context.Principal?.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                try
                {
                    if (context.User.TryGetProperty("name", out var nameProp))
                    {
                        fullName = nameProp.GetString();
                    }
                }
                catch { /* ignore json issues */ }
            }

            string? firstName = null;
            string? lastName = null;
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length > 0) firstName = parts[0];
                if (parts.Length > 1) lastName = string.Join(' ', parts.Skip(1));
            }

            var providerKey = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(email))
            {
                context.Fail("GitHub account has no email.");
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
                if (!logins.Any(l => l.LoginProvider == "GitHub"))
                {
                    await _userManager.AddLoginAsync(
                        user,
                        new UserLoginInfo("GitHub", providerKey, "GitHub"));
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: true);
        }

        private sealed record GitHubEmail(string Email, bool Primary, bool Verified);

        private static async Task<string?> GetPrimaryEmailAsync(OAuthCreatingTicketContext context)
        {
            if (string.IsNullOrEmpty(context.AccessToken))
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
            // Ensure GitHub API user-agent requirement is satisfied
            if (!request.Headers.UserAgent.Any())
            {
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("InventoryManagement", "1.0"));
            }

            using var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var emails = await response.Content.ReadFromJsonAsync<List<GitHubEmail>>(cancellationToken: context.HttpContext.RequestAborted);
            var primaryVerified = emails?.FirstOrDefault(e => e.Primary && e.Verified)?.Email;
            if (!string.IsNullOrWhiteSpace(primaryVerified)) return primaryVerified;

            var anyVerified = emails?.FirstOrDefault(e => e.Verified)?.Email;
            if (!string.IsNullOrWhiteSpace(anyVerified)) return anyVerified;

            return emails?.FirstOrDefault()?.Email;
        }
    }
}
