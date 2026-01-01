using System;
using ActorTableEntities.Internal;
using ActorTableEntities.Internal.Lock;
using ActorTableEntities.Internal.Persistence;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace ActorTableEntities
{
    public static class ActorTableEntityX
    {
        /// <summary>
        /// Extension method for Azure Functions isolated worker model (.NET 8+)
        /// </summary>
        public static IServiceCollection AddActorTableEntities(this IServiceCollection services, string storageConnection, Action<ActorTableEntityOptions> optionsDelegate = null)
        {
            var options = new ActorTableEntityOptions
            {
                StorageConnectionString = storageConnection
            };

            optionsDelegate?.Invoke(options);

            DistributedLockFactory.Initialise(options);

            services.AddSingleton(new TableStorageProvider(options.StorageConnectionString));
            services.AddSingleton<TableEntityProvider>();
            services.AddSingleton(new BlobStateProvider(options.StorageConnectionString, options.StateContainerName));
            services.AddSingleton<IBlobActorStateStore, BlobActorStateStore>();
            
            services.AddSingleton<IActorTableEntityClient, ActorTableEntityClient>();

            return services;
        }
    }
}
