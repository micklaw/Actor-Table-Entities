using System.Threading.Tasks;
using Azure.Data.Tables;

namespace ActorTableEntities
{
    public interface IActorTableEntityClient
    {
        Task<T> Get<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();

        Task<IActorTableEntityClientState<T>> GetLocked<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
    }
}