using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InventoryManagement.Services
{
    public class DropboxService
    {
        private readonly HttpClient _client;
        private readonly string _token;
        private const string TargetFolder = "/powerautomate";

        public DropboxService(IHttpClientFactory httpFactory)
        {
            _client = httpFactory.CreateClient();
            _token = Environment.GetEnvironmentVariable("DROPBOX_ACCESS_TOKEN") ?? string.Empty;
        }

        private void EnsureAuthHeader()
        {
            if (!string.IsNullOrEmpty(_token) && _client.DefaultRequestHeaders.Authorization == null)
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
        }

        public async Task<(bool Ok, string? Error)> UploadJsonAsync(object payload, string fileNameWithoutExt)
        {
            if (string.IsNullOrEmpty(_token)) return (false, "Dropbox access token is not configured.");
            EnsureAuthHeader();

            var metadata = new
            {
                path = $"{TargetFolder}/{fileNameWithoutExt}.json",
                mode = "add",
                autorename = true,
                mute = false
            };

            var contentJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var contentBytes = Encoding.UTF8.GetBytes(contentJson);

            try
            {
                using var ms = new MemoryStream(contentBytes);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload");
                request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(metadata));
                request.Content = new StreamContent(ms);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var res = await _client.SendAsync(request);
                if (res.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                string? body = null;
                try
                {
                    body = await res.Content.ReadAsStringAsync();
                }
                catch { }

                var err = $"Dropbox API returned {(int)res.StatusCode} {res.ReasonPhrase}" + (string.IsNullOrEmpty(body) ? string.Empty : $": {body}");
                return (false, err);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
