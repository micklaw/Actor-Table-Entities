var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage emulator for local development
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(container =>
    {
        container.WithLifetime(ContainerLifetime.Persistent);
    });

var tables = storage.AddTables("tables");
var blobs = storage.AddBlobs("blobs");

// Add the Azure Functions sample project
var functions = builder.AddProject<Projects.SampleHttpFunctions>("samplehttpfunctions")
    .WithReference(tables)
    .WithReference(blobs);

builder.Build().Run();
