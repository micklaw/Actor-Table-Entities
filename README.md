# Actor Table Entities
A play on Azure Functions Durable Entities without the queuing. Locks a blob behind the scenes to ensure the actor can only be amended once, then free us for the next connection.

[![Build Status](https://github.com/micklaw/Actor-Table-Entities/actions/workflows/pr-build.yml/badge.svg)](https://github.com/micklaw/Actor-Table-Entities/actions/workflows/pr-build.yml)
[![NuGet](https://img.shields.io/nuget/v/ActorTableEntities.svg)](https://www.nuget.org/packages/ActorTableEntities/)

## What's New in v2.0

- ✅ **Upgraded to .NET 8.0 LTS** - Full support for the latest .NET runtime
- ✅ **Azure SDK v12** - Migrated from legacy WindowsAzure.Storage to modern Azure.Data.Tables and Azure.Storage.Blobs
- ✅ **Azure Functions v4** - Sample project uses the isolated worker model
- ✅ **Aspire Integration** - Sample includes .NET Aspire AppHost for local development
- ✅ **OpenTelemetry Support** - Built-in telemetry via Application Insights
- ✅ **Comprehensive Tests** - Unit and integration tests included
- ✅ **Manual Versioning** - Simplified version management in project files

## Why not use Durable Entities?
I did, honestly, and yes they are amazing, but for my specific use case they did fit well. I wanted something that was:

* Quick to respond
* Wasn't meant for scale on a single entity (Max 10-20 consumers of an entity)
* Controllable via standard functions
* Cheaper

Where as durableEntities are great, due to the nature of the queuing involved using Orchestrator functions, it meant when release I could wait or a good few seconds anywhere between 2-10
for my request to complete, then if it did, I would generally have to get a status endpoint to monitor my result.

Next up I attempted to go straight to the Entity and its operations, but the lack of responses from the operations without meaningful HTTP responses stopped me.

So, I built this...

## Usage
So this is a typical entity, inheriting from ITableEntity, it will allow you to put complex types as properties, it will also allow for you to interact with the actual entity by claiming a lock
just before reading, if it fails to get a lock, it will retry every Xms for X attempts as defined in your config.

```csharp
public class Counter : ActorTableEntity
{
    public int Count { get; set; }

    public Counter Increment()
    {
        Count = Count + 1;

        return this;
    }
}
```

You can see a sample function in the main project, but it looks a bit like this.

```csharp
public class FunctionApis
{
    private readonly IActorTableEntityClient _entityClient;

    public FunctionApis(IActorTableEntityClient entityClient)
    {
        _entityClient = entityClient;
    }

    [Function("UpdateHttpApi")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "update/{name}")] HttpRequest req, 
        string name)
    {
        await using var state = await _entityClient.GetLocked<Counter>("entity", name);

        state.Entity.Increment();

        await state.Flush();

        return new OkObjectResult(state.Entity);
    }
}
```

The code above lets you take a hold of an entity, do some stuff on it, then release the lock, allowing the next punter to take it up.

## Setup
Finally, install the nuget package above, and bootstrap your code like so.

### Azure Functions (.NET 8 Isolated Worker Model)
```csharp
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
```

### Azure Functions (In-Process Model - Legacy)
```csharp
public class Startup : IWebJobsStartup
{
    public void Configure(IWebJobsBuilder builder)
    {
        builder.AddActorTableEntities(options =>
        {
            options.StorageConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "entitylocks";
            options.WithRetry = true;
            options.RetryIntervalMilliseconds = 100;
        });
    }
}
```

### Standard Configuration (Table Storage)
For .NET 8+ with dependency injection:
```csharp
services.AddActorTableEntities(options =>
{
    options.StorageConnectionString = "UseDevelopmentStorage=true";
    options.ContainerName = "entitylocks";
    options.WithRetry = true;
    options.RetryIntervalMilliseconds = 100;
});
```

### Enhanced Configuration (Blob Storage for State)
For improved scalability and to eliminate Table Storage serialization constraints, you can configure the library to store actor state in Azure Blob Storage while keeping metadata in Table Storage:

```csharp
services.AddActorTableEntities(options =>
{
    options.StorageConnectionString = "UseDevelopmentStorage=true";
    options.ContainerName = "entitylocks";
    options.StateContainerName = "actorstate"; // Enable blob-based state storage
    options.WithRetry = true;
    options.RetryIntervalMilliseconds = 100;
});
```

When `StateContainerName` is configured:
- **Actor metadata** (PartitionKey, RowKey, Timestamp, ETag) is stored in Azure Table Storage
- **Actor state** is stored as JSON in Azure Blob Storage
- This approach provides better scalability and removes serialization limitations
- The existing blob locking mechanism ensures thread-safe operations

## Local Development with Aspire

The sample project includes .NET Aspire for easy local development with the Azure Storage emulator:

```bash
# Run the Aspire AppHost
cd SampleHttpFunctions.AppHost
dotnet run
```

This will start:
- Azure Storage Emulator (Azurite) in a container
- Sample Azure Functions application
- Aspire Dashboard for monitoring and logs

## Testing

The project includes comprehensive unit and integration tests:

```bash
# Run unit tests
dotnet test ActorTableEntities.Tests/ActorTableEntities.Tests.csproj

# Run integration tests (requires Azurite)
dotnet test ActorTableEntities.IntegrationTests/ActorTableEntities.IntegrationTests.csproj
```

## CI/CD with GitHub Actions

The project uses GitHub Actions for continuous integration:

- **PR Build**: Runs on all pull requests, executes tests and builds
- **Release**: Publishes to NuGet when a tag is pushed (e.g., `v2.0.0`)

To create a release:
```bash
git tag v2.0.0
git push origin v2.0.0
```

## Built with it

### Cards Against COVID

Have a play and see what you think, I built this with it:

https://stcardshumanity.z33.web.core.windows.net/