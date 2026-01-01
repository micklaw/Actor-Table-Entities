using System.Threading.Tasks;
using ActorTableEntities.Internal.Persistence.Extensions;
using Azure;
using Azure.Data.Tables;

namespace ActorTableEntities.Internal.Persistence
{
    internal class TableStorageProvider
    {
        private readonly TableServiceClient client;

        public TableStorageProvider(string storageConnection)
        {
            client = new TableServiceClient(storageConnection.CheckNotNull(nameof(storageConnection)));
        }

        public virtual async Task<Response> InsertOrReplace<T>(T entity) where T : ITableEntity
        {
            TableClient table = await CreateIfNotExists<T>();
            
            return await table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        public virtual async Task<Response<T>> Get<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            TableClient table = await CreateIfNotExists<T>();

            return await table.GetEntityAsync<T>(partitionKey, rowKey);
        }

        private async Task<TableClient> CreateIfNotExists<T>()
        {
            TableClient table = client.GetTableClient(typeof(T).Name);
            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}
