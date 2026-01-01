using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace ActorTableEntities.Internal.Persistence
{
    /// <summary>
    /// Manages blob operations for reading and writing state.
    /// </summary>
    internal class BlobStateProvider
    {
        private readonly CloudBlobClient blobClient;
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

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnection);
            this.blobClient = storageAccount.CreateCloudBlobClient();
            this.containerName = containerName;
        }

        public async Task<string> ReadBlobAsync(string blobName)
        {
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlockBlobReference(blobName);

            if (!await blob.ExistsAsync())
            {
                return null;
            }

            return await blob.DownloadTextAsync();
        }

        public async Task WriteBlobAsync(string blobName, string content)
        {
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadTextAsync(content);
        }

        public async Task<bool> BlobExistsAsync(string blobName)
        {
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlockBlobReference(blobName);
            return await blob.ExistsAsync();
        }

        public string ToBlobName(string partitionKey, string rowKey)
        {
            return $"{partitionKey}/{rowKey}.json";
        }
    }
}
