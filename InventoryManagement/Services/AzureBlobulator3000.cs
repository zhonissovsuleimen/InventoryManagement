using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using Azure.Identity;
using Microsoft.AspNetCore.Http;

namespace InventoryManagement.Services
{
    public static class AzureBlobulator3000
    {
        private static readonly string? connectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTION_STRING");

        private static readonly BlobServiceClient serviceClient = new(connectionString);

        public static async Task<string> UploadImageAsync(IFormFile file)
        {
            var container = serviceClient.GetBlobContainerClient("images");
            var name = Guid.NewGuid().ToString();

            var blobClient = container.GetBlobClient(name);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            await blobClient.UploadAsync(file.OpenReadStream(), new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return blobClient.Uri.ToString();
        }
    }
}
