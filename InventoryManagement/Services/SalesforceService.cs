using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InventoryManagement.Services
{
    public class SalesforceTokenResult
    {
        public string? AccessToken { get; set; }
        public string? InstanceUrl { get; set; }
        public string? RefreshToken { get; set; }
        public long? ExpiresIn { get; set; }
        public string? Error { get; set; }
    }

    public class SalesforceService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _authorizeBase = "https://login.salesforce.com/services/oauth2/authorize";
        private readonly string _tokenEndpoint = "https://login.salesforce.com/services/oauth2/token";
        private readonly string _apiVersion = "v58.0";

        public SalesforceService()
        {
            _clientId = Environment.GetEnvironmentVariable("SALESFORCE_CONSUMER_KEY") ?? string.Empty;
            _clientSecret = Environment.GetEnvironmentVariable("SALESFORCE_CONSUMER_SECRET") ?? string.Empty;
        }

        public string BuildAuthorizeUrl(string callback, string stateBase64, string? codeChallenge = null)
        {
            var scope = System.Web.HttpUtility.UrlEncode("api refresh_token offline_access web");
            var url = new StringBuilder(_authorizeBase);
            url.Append($"?response_type=code&client_id={System.Web.HttpUtility.UrlEncode(_clientId)}");
            url.Append($"&redirect_uri={System.Web.HttpUtility.UrlEncode(callback)}");
            url.Append($"&state={System.Web.HttpUtility.UrlEncode(stateBase64)}");
            url.Append($"&scope={scope}");
            if (!string.IsNullOrEmpty(codeChallenge))
            {
                url.Append($"&code_challenge={System.Web.HttpUtility.UrlEncode(codeChallenge)}&code_challenge_method=S256");
            }
            return url.ToString();
        }

        public async Task<SalesforceTokenResult> ExchangeCodeForTokenAsync(string code, string redirectUri, string? codeVerifier = null)
        {
            using var http = new HttpClient();
            var tokenReq = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["redirect_uri"] = redirectUri
            };
            if (!string.IsNullOrEmpty(codeVerifier)) tokenReq["code_verifier"] = codeVerifier;

            var res = await http.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(tokenReq));
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                return new SalesforceTokenResult { Error = body };
            }
            try
            {
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                var at = j.TryGetProperty("access_token", out JsonElement atj) ? atj.GetString() : null;
                var iu = j.TryGetProperty("instance_url", out JsonElement iuj) ? iuj.GetString() : null;
                var rt = j.TryGetProperty("refresh_token", out JsonElement rtj) ? rtj.GetString() : null;
                var ei = j.TryGetProperty("expires_in", out JsonElement eij) && eij.TryGetInt64(out long s) ? s : (long?)null;
                return new SalesforceTokenResult { AccessToken = at, InstanceUrl = iu, RefreshToken = rt, ExpiresIn = ei };
            }
            catch (Exception ex)
            {
                return new SalesforceTokenResult { Error = ex.Message + ": " + body };
            }
        }

        public async Task<SalesforceTokenResult> RefreshAccessTokenAsync(string refreshToken)
        {
            using var http = new HttpClient();
            var req = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["refresh_token"] = refreshToken
            };
            var res = await http.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(req));
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                return new SalesforceTokenResult { Error = body };
            }
            try
            {
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                var at = j.TryGetProperty("access_token", out JsonElement atj) ? atj.GetString() : null;
                var rt = j.TryGetProperty("refresh_token", out JsonElement rtj) ? rtj.GetString() : null;
                var ei = j.TryGetProperty("expires_in", out JsonElement eij) && eij.TryGetInt64(out long s) ? s : (long?)null;
                return new SalesforceTokenResult { AccessToken = at, RefreshToken = rt, ExpiresIn = ei };
            }
            catch (Exception ex)
            {
                return new SalesforceTokenResult { Error = ex.Message + ": " + body };
            }
        }

        public async Task<(bool success, string? id, string? error)> CreateSObjectAsync(string instanceUrl, string accessToken, string sobject, Dictionary<string, object> fields)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = instanceUrl.TrimEnd('/') + $"/services/data/{_apiVersion}/sobjects/{sobject}/";
            var payload = JsonSerializer.Serialize(fields);
            var res = await http.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                return (false, null, body);
            }
            try
            {
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                var id = j.TryGetProperty("id", out JsonElement pid) ? pid.GetString() : null;
                return (true, id, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message + ": " + body);
            }
        }

        public async Task<(bool success, string? error)> UpdateSObjectAsync(string instanceUrl, string accessToken, string sobject, string id, Dictionary<string, object> fields)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = instanceUrl.TrimEnd('/') + $"/services/data/{_apiVersion}/sobjects/{sobject}/{id}";
            var payload = JsonSerializer.Serialize(fields);
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
            var res = await http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                return (false, body);
            }
            return (true, null);
        }

        public async Task<(bool success, string? id)> QueryContactByEmailAsync(string instanceUrl, string accessToken, string email)
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var sanitized = email?.Replace("'", "\\'") ?? string.Empty;
                var q = $"SELECT Id FROM Contact WHERE Email = '{sanitized}' LIMIT 1";
                var url = instanceUrl.TrimEnd('/') + $"/services/data/{_apiVersion}/query?q=" + Uri.EscapeDataString(q);
                var res = await http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return (false, null);
                var body = await res.Content.ReadAsStringAsync();
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                if (j.TryGetProperty("totalSize", out JsonElement ts) && ts.GetInt32() > 0 && j.TryGetProperty("records", out JsonElement recs) && recs.ValueKind == JsonValueKind.Array && recs.GetArrayLength() > 0)
                {
                    var first = recs[0];
                    if (first.TryGetProperty("Id", out JsonElement idj)) return (true, idj.GetString());
                }
                return (false, null);
            }
            catch { return (false, null); }
        }
    }
}
