using ActorTableEntities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Configure ActorTableEntities
        services.AddActorTableEntities(options =>
        {
            options.StorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
                                             ?? "UseDevelopmentStorage=true";
            options.ContainerName = "entitylocks";
            options.WithRetry = true;
            options.RetryIntervalMilliseconds = 100;
        });
    })
    .Build();

host.Run();
