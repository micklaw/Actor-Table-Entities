# GitHub Copilot Instructions - Actor Table Entities

## Project Overview

Actor Table Entities is a .NET library that provides a locking mechanism for Azure Table Storage entities, inspired by Azure Durable Entities but optimized for quick responses and lower costs. The library uses blob storage locks to ensure atomic operations on table entities without queuing overhead.

**Tech Stack:**
- .NET Standard 2.1
- C# 
- Azure Functions (for sample implementations)
- Azure Storage (Table Storage and Blob Storage)
- WindowsAzure.Storage SDK
- Microsoft.Azure.Functions.Extensions

## Build and Test

### Build Commands
```bash
dotnet restore
dotnet build --configuration Release
```

### Testing
The project uses Azure Pipelines for CI/CD. The build pipeline is defined in `azure-pipelines.yml` and uses templates in the `Build/` directory.

### Versioning
The project uses Nerdbank.GitVersioning (NBGV) for version management. Version configuration is in `version.json`.

### NuGet Package
- The main library is `ActorTableEntities.csproj`
- Package is published to NuGet from master and develop branches
- Package generation occurs during the build process

## Coding Standards

### C# Conventions
- Follow standard C# coding conventions
- Use meaningful variable and method names
- Prefer `async`/`await` patterns for asynchronous operations
- Use `await using` for disposable resources (e.g., locked entity states)

### Naming Conventions
- Classes: PascalCase (e.g., `ActorTableEntity`, `Counter`)
- Methods: PascalCase (e.g., `GetLocked`, `Increment`)
- Properties: PascalCase (e.g., `Count`, `StorageConnectionString`)
- Private fields: camelCase with underscore prefix if needed
- Interfaces: Prefix with `I` (e.g., `IActorTableEntityClient`)

### Code Organization
- Keep internal implementation details in the `Internal/` namespace
- Use attributes for framework integration (e.g., `ActorTableEntityAttribute`)
- Separate concerns: Lock management, Persistence, Client operations

## Architecture Patterns

### Core Concepts
1. **ActorTableEntity**: Base class for entities that support locking and complex property serialization
2. **IActorTableEntityClient**: Client interface for managing entity operations
3. **Distributed Locking**: Uses blob storage leases to ensure exclusive access
4. **Complex Properties**: JSON serialization for complex types using `ActorTableEntityComplexPropertyAttribute`

### Usage Pattern
```csharp
// Get a locked entity, modify it, and flush changes
await using var state = await entityClient.GetLocked<TEntity>(partitionKey, rowKey);
state.Entity.ModifyProperty();
await state.Flush();
```

## Security Considerations

- **Connection Strings**: Use secure configuration management (Azure Key Vault, App Settings)
- **Authorization**: Implement proper Azure Functions authorization levels (avoid Anonymous in production)
- **Input Validation**: Validate partition keys and row keys to prevent injection attacks
- **Secrets**: Never hardcode connection strings or secrets in code

## Dependencies

### Current Dependencies
- WindowsAzure.Storage 9.3.3
- Microsoft.Azure.Functions.Extensions 1.0.0
- Nerdbank.GitVersioning 3.1.74

### Adding New Dependencies
- Minimize external dependencies to keep the library lightweight
- Prefer stable, well-maintained packages
- Ensure compatibility with .NET Standard 2.1
- Update NuGet package references in `.csproj` files

## Documentation

### XML Documentation Comments
- Add XML documentation comments to all public classes, methods, and properties
- Include `<summary>`, `<param>`, `<returns>`, and `<exception>` tags where appropriate
- Document the purpose and usage patterns clearly

### README Updates
- Update README.md when adding new features or changing public APIs
- Include code examples for new functionality
- Keep setup instructions current

## Testing

### Sample Functions
- The `SampleHttpFunctions` project demonstrates usage patterns
- Include practical examples when adding new features
- Test with Azure Storage Emulator or Azurite for local development

### Configuration
- Use `UseDevelopmentStorage=true` for local development
- Configure blob container names appropriately
- Set retry options based on expected workload

## Common Tasks

### Adding a New Feature
1. Implement the feature in the appropriate namespace
2. Add XML documentation comments
3. Update sample functions to demonstrate usage
4. Update README.md with examples
5. Ensure backward compatibility when possible

### Modifying Entity Behavior
- Changes to `ActorTableEntity` affect all derived entities
- Consider impact on serialization/deserialization
- Maintain compatibility with existing table data

### Lock Management Changes
- Lock operations are in `Internal/Lock/`
- Be careful with timeout and retry logic
- Test thoroughly to avoid deadlocks or race conditions

## Azure Functions Integration

- Use dependency injection for `IActorTableEntityClient`
- Configure in `Startup.cs` using `builder.AddActorTableEntities()`
- Set appropriate storage connection strings and container names
- Consider retry settings based on concurrency requirements

## Best Practices

1. **Resource Disposal**: Always use `await using` with locked entity states to ensure locks are released
2. **Error Handling**: Handle lock acquisition failures gracefully with appropriate retry logic
3. **Concurrency**: Design entities to handle concurrent access attempts
4. **Performance**: Keep entity operations fast to minimize lock hold times
5. **Testing**: Test with the Azure Storage Emulator before deploying to Azure

## Version Control

- Main development happens on `develop` branch
- `master` branch is for stable releases
- PRs are built and validated via Azure Pipelines
- Commit messages should be clear and descriptive
