using System;
using System.Threading.Tasks;
using ActorTableEntities.Internal.Lock;
using ActorTableEntities.Internal.Persistence;
using ActorTableEntities.Internal.Persistence.Extensions;
using Azure;
using Azure.Data.Tables;

namespace ActorTableEntities.Internal
{
    internal class ActorTableEntityClientState<T> : IActorTableEntityClientState<T> where T : class, ITableEntity, new()
    {
        private readonly TableEntityProvider tableStorageProvider;
        private readonly IBlobActorStateStore blobActorStateStore;

        private readonly DistributedLock mutex;

        public bool IsReleased { get; set; }

        public bool IsNew { get; set; }

        public T Entity { get; set; }

        private string partitionKey;
        private string rowKey;

        public ActorTableEntityClientState(DistributedLock mutex, TableEntityProvider tableStorageProvider, IBlobActorStateStore blobActorStateStore = null)
        {
            this.mutex = mutex;
            this.tableStorageProvider = tableStorageProvider;
            this.blobActorStateStore = blobActorStateStore;
        }

        public async Task Get(string partitionKey, string rowKey)
        {
            this.partitionKey = partitionKey;
            this.rowKey = rowKey;

            // Get metadata from table
            var indexEntity = await tableStorageProvider.Get<ActorIndexEntity>(partitionKey, rowKey);

            // Get state from blob
            var state = await blobActorStateStore.GetStateAsync<T>(partitionKey, rowKey);

            if (state == null)
            {
                IsNew = true;
                this.Entity = Activator.CreateInstance<T>();
                this.Entity.PartitionKey = tableStorageProvider.ToKey(partitionKey);
                this.Entity.RowKey = tableStorageProvider.ToKey(rowKey);
            }
            else
            {
                this.Entity = state;
                // Ensure keys are set
                this.Entity.PartitionKey = tableStorageProvider.ToKey(partitionKey);
                this.Entity.RowKey = tableStorageProvider.ToKey(rowKey);

                // Restore metadata if available
                if (indexEntity?.Result != null)
                {
                    this.Entity.ETag = indexEntity.Result.ETag;
                }
            }
        }

        public async Task Hold(string partitionKey, string rowKey)
        {
            this.partitionKey = partitionKey;
            this.rowKey = rowKey;

            await this.mutex.AcquireAsync();

            // Get metadata from table
            var indexEntity = await tableStorageProvider.Get<ActorIndexEntity>(partitionKey, rowKey);

            // Get state from blob
            var state = await blobActorStateStore.GetStateAsync<T>(partitionKey, rowKey);

            if (state == null)
            {
                IsNew = true;
                this.Entity = Activator.CreateInstance<T>();
                this.Entity.PartitionKey = tableStorageProvider.ToKey(partitionKey);
                this.Entity.RowKey = tableStorageProvider.ToKey(rowKey);
            }
            else
            {
                this.Entity = state;
                // Ensure keys are set
                this.Entity.PartitionKey = tableStorageProvider.ToKey(partitionKey);
                this.Entity.RowKey = tableStorageProvider.ToKey(rowKey);

                // Restore metadata if available
                if (indexEntity?.Result != null)
                {
                    this.Entity.ETag = indexEntity.Result.ETag;
                }
            }

            this.Entity.ETag = ETag.All; // ML - Ensure clobbering happens as we have a lock
        }

        public async Task Flush()
        {
            if (IsReleased)
            {
                return;
            }

            try
            {
                if (this.Entity != null)
                {
                    // Save state to blob
                    await blobActorStateStore.SaveStateAsync(partitionKey, rowKey, this.Entity);

                    // Save metadata to table
                    var indexEntity = new ActorIndexEntity(this.Entity.PartitionKey, this.Entity.RowKey)
                    {
                        ETag = ETag.All
                    };

                    var result = await tableStorageProvider.InsertOrReplace(indexEntity);

                    if (result.StatusCode.IsSuccess())
                    {
                        // Update entity metadata from table response
                        this.Entity.ETag = result.ETag != null ? new ETag(result.ETag) : ETag.All;
                    }
                }
            }
            finally
            {
                await this.mutex.ReleaseAsync();
                IsReleased = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Flush();

            this.mutex.Dispose();

            return;
        }
    }
}
