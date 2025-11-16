using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InventoryManagement.Data;
using InventoryManagement.Models;
using InventoryManagement.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Pages.External
{
    public class SigninSalesforceModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;
        private readonly SalesforceService _salesforce;

        public SigninSalesforceModel(UserManager<AppUser> userManager, ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider, SalesforceService salesforce)
        {
            _userManager = userManager;
            _context = context;
            _protector = dataProtectionProvider.CreateProtector("SalesforceTokens:v1");
            _salesforce = salesforce;
        }

        [BindProperty(SupportsGet = true)]
        public string? code { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? state { get; set; }

        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                Message = "Missing code or state.";
                return Page();
            }

            JsonElement stateObj;
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(state));
                stateObj = JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch
            {
                Message = "Invalid state.";
                return Page();
            }

            string? userId = null;
            string? flowId = null;
            if (stateObj.ValueKind == JsonValueKind.Object)
            {
                if (stateObj.TryGetProperty("userId", out JsonElement uid)) userId = uid.GetString();
                if (stateObj.TryGetProperty("flowId", out JsonElement fid)) flowId = fid.GetString();
            }

            if (string.IsNullOrEmpty(userId))
            {
                Message = "State did not contain user id.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                Message = "User not found.";
                return Page();
            }

            var callback = new UriBuilder(Request.Scheme, Request.Host.Host, Request.Host.Port ?? -1, "/signin-salesforce").Uri.ToString();
            string? codeVerifier = null;
            try
            {
                if (!string.IsNullOrEmpty(flowId))
                {
                    var key = $"sf_pkce_{flowId}";
                    if (TempData.ContainsKey(key))
                    {
                        codeVerifier = TempData[key] as string;
                    }
                }
            }
            catch { }

            var tokenRes = await _salesforce.ExchangeCodeForTokenAsync(code!, callback, codeVerifier);
            if (!string.IsNullOrEmpty(tokenRes.Error))
            {
                Message = "Token exchange failed: " + tokenRes.Error;
                return Page();
            }

            var accessToken = tokenRes.AccessToken;
            var instanceUrl = tokenRes.InstanceUrl;
            var refreshToken = tokenRes.RefreshToken;
            var expiresIn = tokenRes.ExpiresIn;

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(instanceUrl))
            {
                Message = "Token response missing required fields.";
                return Page();
            }

            string? accountName = null;
            string? contactFirst = null;
            string? contactLast = null;
            string? contactEmail = null;
            string? contactPhone = null;
            string? contactMobile = null;
            string? contactTitle = null;
            string? mailingStreet = null;
            string? mailingCity = null;
            string? mailingState = null;
            string? mailingPostal = null;
            string? mailingCountry = null;
            string? description = null;
            try
            {
                if (stateObj.ValueKind == JsonValueKind.Object && stateObj.TryGetProperty("form", out JsonElement form))
                {
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("AccountName", out JsonElement an)) accountName = an.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("ContactFirstName", out JsonElement cf)) contactFirst = cf.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("ContactLastName", out JsonElement cl)) contactLast = cl.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("ContactEmail", out JsonElement ce)) contactEmail = ce.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("Phone", out JsonElement ph)) contactPhone = ph.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("MobilePhone", out JsonElement mb)) contactMobile = mb.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("Title", out JsonElement tt)) contactTitle = tt.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("MailingStreet", out JsonElement ms)) mailingStreet = ms.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("MailingCity", out JsonElement mc)) mailingCity = mc.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("MailingState", out JsonElement mst)) mailingState = mst.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("MailingPostalCode", out JsonElement mp)) mailingPostal = mp.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("MailingCountry", out JsonElement mco)) mailingCountry = mco.GetString();
                    if (form.ValueKind == JsonValueKind.Object && form.TryGetProperty("Description", out JsonElement desc)) description = desc.GetString();
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(accountName))
            {
                accountName = user.UserName ?? (user.Email ?? "Account");
            }

            var acctReq = new Dictionary<string, object> { ["Name"] = accountName };
            var acctRes = await _salesforce.CreateSObjectAsync(instanceUrl, accessToken, "Account", acctReq);
            if (!acctRes.success)
            {
                Message = "Failed to create Account: " + acctRes.error;
                return Page();
            }

            var accountId = acctRes.id;

            var contactReq = new Dictionary<string, object> { ["AccountId"] = accountId };
            if (!string.IsNullOrWhiteSpace(contactFirst)) contactReq["FirstName"] = contactFirst;
            if (!string.IsNullOrWhiteSpace(contactLast)) contactReq["LastName"] = contactLast;
            if (!string.IsNullOrWhiteSpace(contactEmail)) contactReq["Email"] = contactEmail;
            if (!string.IsNullOrWhiteSpace(contactPhone)) contactReq["Phone"] = contactPhone;
            if (!string.IsNullOrWhiteSpace(contactMobile)) contactReq["MobilePhone"] = contactMobile;
            if (!string.IsNullOrWhiteSpace(contactTitle)) contactReq["Title"] = contactTitle;
            if (!string.IsNullOrWhiteSpace(mailingStreet)) contactReq["MailingStreet"] = mailingStreet;
            if (!string.IsNullOrWhiteSpace(mailingCity)) contactReq["MailingCity"] = mailingCity;
            if (!string.IsNullOrWhiteSpace(mailingState)) contactReq["MailingState"] = mailingState;
            if (!string.IsNullOrWhiteSpace(mailingPostal)) contactReq["MailingPostalCode"] = mailingPostal;
            if (!string.IsNullOrWhiteSpace(mailingCountry)) contactReq["MailingCountry"] = mailingCountry;
            if (!string.IsNullOrWhiteSpace(description)) contactReq["Description"] = description;

            string? contactId = null;

            if (!string.IsNullOrWhiteSpace(contactEmail))
            {
                try
                {
                    var found = await _salesforce.QueryContactByEmailAsync(instanceUrl, accessToken, contactEmail);
                    if (found.success && !string.IsNullOrWhiteSpace(found.id))
                    {
                        var upd = await _salesforce.UpdateSObjectAsync(instanceUrl, accessToken, "Contact", found.id!, contactReq);
                        if (upd.success)
                        {
                            contactId = found.id;
                        }
                        else
                        {
                            contactId = null;
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(contactId))
            {
                var contactRes = await _salesforce.CreateSObjectAsync(instanceUrl, accessToken, "Contact", contactReq);
                contactId = contactRes.id;
                if (!contactRes.success)
                {
                    try
                    {
                        var j = JsonSerializer.Deserialize<JsonElement>(contactRes.error ?? "{}");
                        if (j.ValueKind == JsonValueKind.Object && j.TryGetProperty("duplicateResult", out JsonElement dup))
                        {
                            string? existingId = null;
                            try
                            {
                                if (dup.TryGetProperty("matchResults", out JsonElement mr) && mr.ValueKind == JsonValueKind.Array && mr.GetArrayLength() > 0)
                                {
                                    var first = mr[0];
                                    if (first.TryGetProperty("matchRecords", out JsonElement mrec) && mrec.ValueKind == JsonValueKind.Array && mrec.GetArrayLength() > 0)
                                    {
                                        var rec = mrec[0];
                                        if (rec.TryGetProperty("record", out JsonElement record) && record.ValueKind == JsonValueKind.Object)
                                        {
                                            if (record.TryGetProperty("Id", out JsonElement rid)) existingId = rid.GetString();
                                            else if (record.TryGetProperty("id", out JsonElement rid2)) existingId = rid2.GetString();
                                        }
                                        if (existingId == null && rec.TryGetProperty("Id", out JsonElement recId)) existingId = recId.GetString();
                                    }
                                }
                            }
                            catch { existingId = null; }

                            if (!string.IsNullOrEmpty(existingId))
                            {
                                var upd = await _salesforce.UpdateSObjectAsync(instanceUrl, accessToken, "Contact", existingId, contactReq);
                                if (upd.success)
                                {
                                    contactId = existingId;
                                }
                                else
                                {
                                    Message = "Account created but failed to update existing Contact: " + (upd.error ?? "unknown");
                                    return Page();
                                }
                            }
                            else
                            {
                                Message = "Account created but failed to create Contact: " + contactRes.error;
                                return Page();
                            }
                        }
                        else
                        {
                            Message = "Account created but failed to create Contact: " + contactRes.error;
                            return Page();
                        }
                    }
                    catch
                    {
                        Message = "Account created but failed to create Contact: " + contactRes.error;
                        return Page();
                    }
                }
            }

            Message = "Salesforce Account and Contact created/updated successfully (AccountId=" + accountId + ", ContactId=" + contactId + ")";

            try
            {
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Id == user.Id);
                if (u != null)
                {
                    u.SalesforceAccountId = accountId;
                    u.SalesforceContactId = contactId;
                    u.SalesforceInstanceUrl = instanceUrl;
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        try { u.SalesforceAccessToken = _protector.Protect(accessToken); } catch { u.SalesforceAccessToken = null; }
                    }
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        try { u.SalesforceRefreshToken = _protector.Protect(refreshToken); } catch { /* ignore */ }
                    }
                    if (expiresIn.HasValue) u.SalesforceTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn.Value);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            return Page();
        }
    }
}
