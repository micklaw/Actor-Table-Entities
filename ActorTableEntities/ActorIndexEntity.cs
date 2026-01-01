using Microsoft.WindowsAzure.Storage.Table;

namespace ActorTableEntities
{
    /// <summary>
    /// Metadata-only entity for actor index stored in Table Storage.
    /// The actual state is stored in Blob Storage.
    /// </summary>
    public class ActorIndexEntity : TableEntity
    {
        public ActorIndexEntity()
        {
        }

        public ActorIndexEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }
    }
}
