using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ActorTableEntities.Internal.Persistence
{
    /// <summary>
    /// Implementation of blob-based actor state store.
    /// Stores state as JSON in Azure Blob Storage.
    /// </summary>
    internal class BlobActorStateStore : IBlobActorStateStore
    {
        private readonly BlobStateProvider blobStateProvider;
        private readonly TableEntityProvider tableEntityProvider;

        public BlobActorStateStore(BlobStateProvider blobStateProvider, TableEntityProvider tableEntityProvider)
        {
            this.blobStateProvider = blobStateProvider ?? throw new ArgumentNullException(nameof(blobStateProvider));
            this.tableEntityProvider = tableEntityProvider ?? throw new ArgumentNullException(nameof(tableEntityProvider));
        }

        public async Task<T> GetStateAsync<T>(string partitionKey, string rowKey) where T : class
        {
            var normalizedPartitionKey = tableEntityProvider.ToKey(partitionKey);
            var normalizedRowKey = tableEntityProvider.ToKey(rowKey);
            var blobName = blobStateProvider.ToBlobName(normalizedPartitionKey, normalizedRowKey);

            var json = await blobStateProvider.ReadBlobAsync(blobName);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task SaveStateAsync<T>(string partitionKey, string rowKey, T state) where T : class
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var normalizedPartitionKey = tableEntityProvider.ToKey(partitionKey);
            var normalizedRowKey = tableEntityProvider.ToKey(rowKey);
            var blobName = blobStateProvider.ToBlobName(normalizedPartitionKey, normalizedRowKey);

            var json = JsonConvert.SerializeObject(state);
            await blobStateProvider.WriteBlobAsync(blobName, json);
        }

        public async Task<bool> StateExistsAsync(string partitionKey, string rowKey)
        {
            var normalizedPartitionKey = tableEntityProvider.ToKey(partitionKey);
            var normalizedRowKey = tableEntityProvider.ToKey(rowKey);
            var blobName = blobStateProvider.ToBlobName(normalizedPartitionKey, normalizedRowKey);

            return await blobStateProvider.BlobExistsAsync(blobName);
        }
    }
}
