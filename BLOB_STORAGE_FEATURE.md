# Azure Blob Storage for Actor State Management

## Overview

This feature enhances actor state management by:
- Storing actor metadata in **Azure Table Storage** (PartitionKey, RowKey, Timestamp, ETag)
- Storing actor state in **Azure Blob Storage** as JSON
- Using the existing blob locking mechanism for concurrency control

## Benefits

1. **No Serialization Constraints**: The Table Storage SDK has limitations on property types and sizes. By storing state in Blob Storage as JSON, you can use any serializable .NET type.
2. **Scalability**: Blob Storage can handle larger state objects (up to 4.75 TB per blob vs 1 MB per table entity).
3. **Flexibility**: JSON serialization provides better flexibility for complex object graphs.
4. **Backwards Compatible**: Existing code continues to work without changes if `StateContainerName` is not configured.

## How It Works

### Architecture

1. **Metadata Storage (Table Storage)**:
   - Stores `ActorIndexEntity` with PartitionKey, RowKey, Timestamp, and ETag
   - Provides quick lookups and metadata queries
   - Small footprint (no state data)

2. **State Storage (Blob Storage)**:
   - Stores full actor state as JSON
   - Blob path: `{StateContainerName}/{PartitionKey}/{RowKey}.json`
   - Leverages JSON.NET for serialization

3. **Locking Mechanism**:
   - Uses Azure Blob leases (existing mechanism) for distributed locking
   - Ensures only one process can modify an actor at a time
   - Lock container: `{ContainerName}` (existing behavior)

### Code Flow

#### GetLocked<T>()
1. Acquire distributed lock on `{ContainerName}/{PartitionKey}{RowKey}`
2. If blob state store configured:
   - Fetch metadata from Table Storage (`ActorIndexEntity`)
   - Fetch state from Blob Storage (JSON deserialization)
   - Combine metadata and state into the entity
3. If not configured (legacy):
   - Fetch full entity from Table Storage

#### Flush()
1. If blob state store configured:
   - Serialize state to JSON and save to Blob Storage
   - Save metadata to Table Storage (`ActorIndexEntity`)
2. If not configured (legacy):
   - Save full entity to Table Storage
3. Release distributed lock

## Configuration

### Enable Blob State Storage

```csharp
builder.AddActorTableEntities(options =>
{
    options.StorageConnectionString = "UseDevelopmentStorage=true";
    options.ContainerName = "entitylocks";          // Blob container for locks
    options.StateContainerName = "actorstate";      // NEW: Blob container for state
    options.WithRetry = true;
    options.RetryIntervalMilliseconds = 100;
});
```

### Legacy Configuration (No Changes Required)

```csharp
builder.AddActorTableEntities(options =>
{
    options.StorageConnectionString = "UseDevelopmentStorage=true";
    options.ContainerName = "entitylocks";
    options.WithRetry = true;
    options.RetryIntervalMilliseconds = 100;
});
```

## Usage Example

No code changes are required in your actors or functions. The existing API works seamlessly:

```csharp
[FunctionName("UpdateHttpApi")]
public async Task<IActionResult> Update(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "update/{name}")] 
    HttpRequest req, 
    string name,
    [ActorTableEntity] IActorTableEntityClient entityClient)
{
    await using var state = await entityClient.GetLocked<Counter>("entity", name);

    state.Entity.Increment();

    await state.Flush();

    return new OkObjectResult(state.Entity);
}
```

## Implementation Details

### New Classes

1. **ActorIndexEntity**: Metadata-only table entity
2. **IBlobActorStateStore**: Interface for state management
3. **BlobActorStateStore**: Implementation of blob-based state storage
4. **BlobStateProvider**: Low-level blob operations

### Modified Classes

1. **ActorTableEntityOptions**: Added `StateContainerName` property
2. **ActorTableEntityX**: Registers blob state components when configured
3. **ActorTableEntityBindingExtension**: Passes blob state store to client
4. **ActorTableEntityClient**: Accepts and uses blob state store
5. **ActorTableEntityClientState**: Implements dual-mode logic (table-only vs table+blob)

## Migration Guide

To migrate from table-only to table+blob storage:

1. **Add Configuration**: Set `StateContainerName` in your options
2. **Deploy**: The change is backwards compatible - existing actors in table storage continue to work
3. **Gradual Migration**: 
   - New actors automatically use blob storage
   - Existing actors migrate on next write operation
   - Old table entities can be cleaned up manually if desired

## Storage Costs

Blob storage is generally more cost-effective for larger state objects:

- **Table Storage**: $0.00036 per 10,000 transactions + $0.045 per GB stored
- **Blob Storage**: $0.0005 per 10,000 transactions + $0.018 per GB stored (Hot tier)

For small actors (< 1KB), the difference is negligible. For larger actors, blob storage provides significant savings.

## Performance Considerations

- **Latency**: Blob operations may have slightly higher latency than table operations
- **Throughput**: Blob storage offers better throughput for large objects
- **Concurrency**: Lock mechanism remains unchanged, ensuring consistent concurrency behavior

## Troubleshooting

### Issue: Entities not found after enabling StateContainerName

**Solution**: This is expected for new entities. Existing entities in table storage continue to work. The library automatically handles both storage modes.

### Issue: Deserialization errors

**Solution**: Ensure your actor classes are properly serializable by JSON.NET. Complex types should have public properties and parameterless constructors.
