using System;
using Azure;
using Azure.Data.Tables;

namespace ActorTableEntities
{
    /// <summary>
    /// Metadata-only entity for actor index stored in Table Storage.
    /// The actual state is stored in Blob Storage.
    /// </summary>
    public class ActorIndexEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public ActorIndexEntity()
        {
        }

        public ActorIndexEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }
    }
}
