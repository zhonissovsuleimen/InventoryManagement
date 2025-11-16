using System.Text.Json;
using System.Web;
using System.Security.Cryptography;
using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Pages.Users
{
    public class ExportToSalesforceModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;

        public ExportToSalesforceModel(UserManager<AppUser> userManager, ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _userManager = userManager;
            _context = context;
            _protector = dataProtectionProvider.CreateProtector("SalesforceTokens:v1");
        }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public string? ErrorMessage { get; set; }

        public bool IsLinked { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Account name")]
            public string? AccountName { get; set; }

            [Display(Name = "Contact first name")]
            public string? ContactFirstName { get; set; }

            [Display(Name = "Contact last name")]
            public string? ContactLastName { get; set; }

            [Display(Name = "Title")]
            public string? Title { get; set; }

            [EmailAddress]
            [Display(Name = "Contact email")]
            public string? ContactEmail { get; set; }

            [Phone]
            [Display(Name = "Phone")]
            public string? Phone { get; set; }

            [Phone]
            [Display(Name = "Mobile")]
            public string? MobilePhone { get; set; }

            [Display(Name = "Mailing street")]
            public string? MailingStreet { get; set; }

            [Display(Name = "Mailing city")]
            public string? MailingCity { get; set; }

            [Display(Name = "Mailing state")]
            public string? MailingState { get; set; }

            [Display(Name = "Mailing postal code")]
            public string? MailingPostalCode { get; set; }

            [Display(Name = "Mailing country")]
            public string? MailingCountry { get; set; }

            [Display(Name = "Notes")]
            public string? Description { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string GenerateCodeVerifier()
        {
            // 32 bytes -> 43 characters base64url
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        private static string CreateCodeChallenge(string codeVerifier)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Id)) return NotFound();
            var current = await _userManager.GetUserAsync(User);
            if (current == null) return Challenge();
            var isSelf = string.Equals(current.Id, Id, StringComparison.Ordinal);
            if (!isSelf && current.IsAdmin != true) return Forbid();

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null) return NotFound();

            IsLinked = !string.IsNullOrWhiteSpace(user.SalesforceContactId);

            if (IsLinked && !string.IsNullOrWhiteSpace(user.SalesforceInstanceUrl))
            {
                try
                {
                    string? accessToken = null;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(user.SalesforceAccessToken))
                        {
                            accessToken = _protector.Unprotect(user.SalesforceAccessToken);
                        }
                    }
                    catch { accessToken = null; }

                    if (string.IsNullOrWhiteSpace(accessToken) || (user.SalesforceTokenExpiresAt.HasValue && user.SalesforceTokenExpiresAt.Value <= DateTime.UtcNow))
                    {
                        if (!string.IsNullOrWhiteSpace(user.SalesforceRefreshToken))
                        {
                            try
                            {
                                var refreshTokenPlain = _protector.Unprotect(user.SalesforceRefreshToken);
                                var refreshed = await RefreshAccessTokenAsync(refreshTokenPlain);
                                if (refreshed.success)
                                {
                                    accessToken = refreshed.accessToken;
                                    try
                                    {
                                        var udb = await _context.Users.FirstOrDefaultAsync(x => x.Id == user.Id);
                                        if (udb != null)
                                        {
                                            if (!string.IsNullOrEmpty(refreshed.accessToken)) udb.SalesforceAccessToken = _protector.Protect(refreshed.accessToken);
                                            if (!string.IsNullOrEmpty(refreshed.refreshToken)) udb.SalesforceRefreshToken = _protector.Protect(refreshed.refreshToken);
                                            if (refreshed.expiresIn.HasValue) udb.SalesforceTokenExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.expiresIn.Value);
                                            await _context.SaveChangesAsync();
                                        }
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        var contact = await FetchContactAsync(user.SalesforceInstanceUrl!, accessToken, user.SalesforceContactId!);
                        if (contact.success)
                        {
                            Input.ContactFirstName = contact.firstName;
                            Input.ContactLastName = contact.lastName;
                            Input.ContactEmail = contact.email;
                            Input.Phone = contact.phone;
                            Input.MobilePhone = contact.mobile;
                            Input.Title = contact.title;
                            Input.MailingStreet = contact.mailingStreet;
                            Input.MailingCity = contact.mailingCity;
                            Input.MailingState = contact.mailingState;
                            Input.MailingPostalCode = contact.mailingPostalCode;
                            Input.MailingCountry = contact.mailingCountry;
                            Input.Description = contact.description;
                            if (!string.IsNullOrWhiteSpace(user.SalesforceAccountId))
                            {
                                var acc = await FetchAccountAsync(user.SalesforceInstanceUrl!, accessToken, user.SalesforceAccountId!);
                                if (acc.success) Input.AccountName = acc.name;
                            }
                            return Page();
                        }
                    }
                }
                catch { }
            }

            Input.AccountName = user.UserName ?? user.Email;
            Input.ContactFirstName = user.FirstName;
            Input.ContactLastName = user.LastName;
            Input.ContactEmail = user.Email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Id)) return NotFound();
            var current = await _userManager.GetUserAsync(User);
            if (current == null) return Challenge();
            var isSelf = string.Equals(current.Id, Id, StringComparison.Ordinal);
            if (!isSelf && current.IsAdmin != true) return Forbid();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var flowId = Guid.NewGuid().ToString("N");
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = CreateCodeChallenge(codeVerifier);

            TempData[$"sf_pkce_{flowId}"] = codeVerifier;

            var stateObj = new
            {
                flowId,
                userId = Id,
                form = Input
            };
            var stateJson = JsonSerializer.Serialize(stateObj);
            var stateBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stateJson));

            var clientId = Environment.GetEnvironmentVariable("SALESFORCE_CONSUMER_KEY") ?? string.Empty;
            var callback = new UriBuilder(Request.Scheme, Request.Host.Host, Request.Host.Port ?? -1, "/signin-salesforce").Uri.ToString();
            var scope = HttpUtility.UrlEncode("api refresh_token offline_access web");

            var authUrl = $"https://login.salesforce.com/services/oauth2/authorize?response_type=code&client_id={HttpUtility.UrlEncode(clientId)}&redirect_uri={HttpUtility.UrlEncode(callback)}&state={HttpUtility.UrlEncode(stateBase64)}&scope={scope}&code_challenge={HttpUtility.UrlEncode(codeChallenge)}&code_challenge_method=S256";

            return Redirect(authUrl);
        }

        private async Task<(bool success, string? firstName, string? lastName, string? email, string? phone, string? mobile, string? title, string? mailingStreet, string? mailingCity, string? mailingState, string? mailingPostalCode, string? mailingCountry, string? description)> FetchContactAsync(string instanceUrl, string accessToken, string contactId)
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var url = instanceUrl.TrimEnd('/') + $"/services/data/v58.0/sobjects/Contact/{contactId}?fields=FirstName,LastName,Email,Phone,MobilePhone,Title,MailingStreet,MailingCity,MailingState,MailingPostalCode,MailingCountry,Description";
                var res = await http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return (false, null, null, null, null, null, null, null, null, null, null, null, null);
                var body = await res.Content.ReadAsStringAsync();
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                var fn = j.TryGetProperty("FirstName", out JsonElement fnj) ? fnj.GetString() : null;
                var ln = j.TryGetProperty("LastName", out JsonElement lnj) ? lnj.GetString() : null;
                var em = j.TryGetProperty("Email", out JsonElement emj) ? emj.GetString() : null;
                var ph = j.TryGetProperty("Phone", out JsonElement phj) ? phj.GetString() : null;
                var mob = j.TryGetProperty("MobilePhone", out JsonElement mbj) ? mbj.GetString() : null;
                var title = j.TryGetProperty("Title", out JsonElement tj) ? tj.GetString() : null;
                var mstreet = j.TryGetProperty("MailingStreet", out JsonElement msj) ? msj.GetString() : null;
                var mcity = j.TryGetProperty("MailingCity", out JsonElement mcj) ? mcj.GetString() : null;
                var mstate = j.TryGetProperty("MailingState", out JsonElement mstj) ? mstj.GetString() : null;
                var mpost = j.TryGetProperty("MailingPostalCode", out JsonElement mpj) ? mpj.GetString() : null;
                var mcountry = j.TryGetProperty("MailingCountry", out JsonElement mcounj) ? mcounj.GetString() : null;
                var desc = j.TryGetProperty("Description", out JsonElement dj) ? dj.GetString() : null;
                return (true, fn, ln, em, ph, mob, title, mstreet, mcity, mstate, mpost, mcountry, desc);
            }
            catch { return (false, null, null, null, null, null, null, null, null, null, null, null, null); }
        }

        private async Task<(bool success, string? name)> FetchAccountAsync(string instanceUrl, string accessToken, string accountId)
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var url = instanceUrl.TrimEnd('/') + $"/services/data/v58.0/sobjects/Account/{accountId}?fields=Name";
                var res = await http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return (false, null);
                var body = await res.Content.ReadAsStringAsync();
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                var name = j.TryGetProperty("Name", out JsonElement nj) ? nj.GetString() : null;
                return (true, name);
            }
            catch { return (false, null); }
        }

        private async Task<(bool success, string? accessToken, string? refreshToken, long? expiresIn)> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var clientId = Environment.GetEnvironmentVariable("SALESFORCE_CONSUMER_KEY") ?? string.Empty;
                var clientSecret = Environment.GetEnvironmentVariable("SALESFORCE_CONSUMER_SECRET") ?? string.Empty;
                using var http = new HttpClient();
                var req = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["refresh_token"] = refreshToken
                };
                var res = await http.PostAsync("https://login.salesforce.com/services/oauth2/token", new FormUrlEncodedContent(req));
                if (!res.IsSuccessStatusCode) return (false, null, null, null);
                var body = await res.Content.ReadAsStringAsync();
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                var at = j.TryGetProperty("access_token", out JsonElement atj) ? atj.GetString() : null;
                var rt = j.TryGetProperty("refresh_token", out JsonElement rtj) ? rtj.GetString() : null;
                var ei = j.TryGetProperty("expires_in", out JsonElement eij) && eij.TryGetInt64(out long s) ? s : (long?)null;
                return (true, at, rt, ei);
            }
            catch { return (false, null, null, null); }
        }
    }
}
