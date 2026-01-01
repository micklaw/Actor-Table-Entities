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
        /// Extension method for Azure Functions in-process model (legacy)
        /// </summary>
        public static IWebJobsBuilder AddActorTableEntities(this IWebJobsBuilder builder, Action<ActorTableEntityOptions> optionsDelegate = null)
        {
            var options = new ActorTableEntityOptions();

            optionsDelegate?.Invoke(options);

            DistributedLockFactory.Initialise(options);

            builder.Services.AddSingleton(new TableStorageProvider(options.StorageConnectionString));
            builder.Services.AddSingleton<TableEntityProvider>();
            
            // Add blob state store components if StateContainerName is configured
            if (!string.IsNullOrWhiteSpace(options.StateContainerName))
            {
                builder.Services.AddSingleton(new BlobStateProvider(options.StorageConnectionString, options.StateContainerName));
                builder.Services.AddSingleton<IBlobActorStateStore, BlobActorStateStore>();
            }
            
            builder.AddExtension<ActorTableEntityBindingExtension>();

            return builder;
        }

        /// <summary>
        /// Extension method for Azure Functions isolated worker model (.NET 8+)
        /// </summary>
        public static IServiceCollection AddActorTableEntities(this IServiceCollection services, Action<ActorTableEntityOptions> optionsDelegate = null)
        {
            var options = new ActorTableEntityOptions();

            optionsDelegate?.Invoke(options);

            DistributedLockFactory.Initialise(options);

            services.AddSingleton(new TableStorageProvider(options.StorageConnectionString));
            services.AddSingleton<TableEntityProvider>();
            
            // Add blob state store components if StateContainerName is configured
            if (!string.IsNullOrWhiteSpace(options.StateContainerName))
            {
                services.AddSingleton(new BlobStateProvider(options.StorageConnectionString, options.StateContainerName));
                services.AddSingleton<IBlobActorStateStore, BlobActorStateStore>();
            }
            
            services.AddSingleton<IActorTableEntityClient, ActorTableEntityClient>();

            return services;
        }
    }
}
