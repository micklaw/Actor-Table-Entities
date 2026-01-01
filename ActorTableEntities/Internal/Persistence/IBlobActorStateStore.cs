using System.Threading.Tasks;

namespace ActorTableEntities.Internal.Persistence
{
    /// <summary>
    /// Interface for managing actor state in storage.
    /// </summary>
    internal interface IBlobActorStateStore
    {
        /// <summary>
        /// Retrieves actor state from blob storage.
        /// </summary>
        Task<T> GetStateAsync<T>(string partitionKey, string rowKey) where T : class;

        /// <summary>
        /// Saves actor state to blob storage.
        /// </summary>
        Task SaveStateAsync<T>(string partitionKey, string rowKey, T state) where T : class;

        /// <summary>
        /// Checks if state exists in blob storage.
        /// </summary>
        Task<bool> StateExistsAsync(string partitionKey, string rowKey);
    }
}
