using System;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Newtonsoft.Json;

namespace ActorTableEntities.Internal.Persistence
{
    /// <summary>
    /// Manages blob operations for reading and writing state.
    /// </summary>
    internal class BlobStateProvider
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly string containerName;

        public BlobStateProvider(string storageConnection, string containerName)
        {
            if (string.IsNullOrWhiteSpace(storageConnection))
            {
                throw new ArgumentNullException(nameof(storageConnection));
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentNullException(nameof(containerName));
            }

            try
            {
                this.blobServiceClient = new BlobServiceClient(storageConnection);
                this.containerName = containerName;
            }
            catch (RequestFailedException ex)
            {
                throw new ArgumentException(
                    "Storage account configuration error for blob state storage. " +
                    "Please verify the StorageConnectionString in ActorTableEntityOptions. " +
                    $"Error: {ex.ErrorCode}", 
                    nameof(storageConnection), 
                    ex);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException)
            {
                throw new ArgumentException(
                    "Invalid storage connection string provided for blob state storage. " +
                    "Please verify the StorageConnectionString in ActorTableEntityOptions.", 
                    nameof(storageConnection), 
                    ex);
            }
        }

        public async Task<string> ReadBlobAsync(string blobName)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToString();
        }

        public async Task WriteBlobAsync(string blobName, string content)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: true);
        }

        public async Task<bool> BlobExistsAsync(string blobName)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobName);
            return await blobClient.ExistsAsync();
        }

        public string ToBlobName(string partitionKey, string rowKey)
        {
            return $"{partitionKey}/{rowKey}.json";
        }
    }
}
