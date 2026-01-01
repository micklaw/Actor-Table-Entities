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

        /// <summary>
        /// Determines if blob storage should be used for state management.
        /// Blob storage is used when configured and T is or derives from ActorTableEntity.
        /// </summary>
        private bool ShouldUseBlobStorage()
        {
            return blobActorStateStore != null && typeof(ActorTableEntity).IsAssignableFrom(typeof(T));
        }

        public async Task Hold(string partitionKey, string rowKey)
        {
            this.partitionKey = partitionKey;
            this.rowKey = rowKey;

            await this.mutex.AcquireAsync();

            if (ShouldUseBlobStorage())
            {
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
                        this.Entity.Timestamp = indexEntity.Result.Timestamp;
                        this.Entity.ETag = indexEntity.Result.ETag;
                    }
                }
            }
            else
            {
                // Legacy approach: get full entity from table
                var entity = await tableStorageProvider.Get<T>(partitionKey, rowKey);

                this.Entity = entity.Result;

                if (this.Entity == null)
                {
                    IsNew = true;

                    this.Entity = Activator.CreateInstance<T>();
                    this.Entity.PartitionKey = tableStorageProvider.ToKey(partitionKey);
                    this.Entity.RowKey = tableStorageProvider.ToKey(rowKey);
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
                    if (ShouldUseBlobStorage())
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
                            this.Entity.Timestamp = result.Result.Timestamp;
                            this.Entity.ETag = result.ETag != null ? new ETag(result.ETag) : ETag.All;
                        }
                    }
                    else
                    {
                        // Legacy approach: save full entity to table
                        var result = await tableStorageProvider.InsertOrReplace(this.Entity);

                        if (result.StatusCode.IsSuccess())
                        {
                            this.Entity = result.Result;
                        }
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
