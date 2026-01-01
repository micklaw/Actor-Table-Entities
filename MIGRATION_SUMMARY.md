# Migration Summary: v1.x to v2.0

## Overview
This migration successfully modernized the Actor-Table-Entities library from .NET Core 3.1 and Azure DevOps to .NET 8.0 LTS with GitHub Actions.

## Key Changes

### 1. Build & CI/CD Migration
- ✅ **Removed**: Azure DevOps pipelines (`azure-pipelines.yml`, `Build/` directory)
- ✅ **Removed**: Nerdbank.GitVersioning (`version.json`, `Directory.Build.props`)
- ✅ **Added**: GitHub Actions workflows
  - `.github/workflows/pr-build.yml` - Builds and tests on PRs
  - `.github/workflows/release.yml` - Publishes to NuGet on tags (e.g., `v2.0.0`)
- ✅ **Added**: Manual version management in `ActorTableEntities.csproj`

### 2. .NET Upgrade
- ✅ **Upgraded**: .NET Core 3.1 → .NET 8.0 LTS
- ✅ **Upgraded**: Azure Functions v3 → v4
- ✅ **Migrated**: In-process model → Isolated worker model

### 3. Azure SDK Migration
- ✅ **Replaced**: `WindowsAzure.Storage 9.3.3` → Modern Azure SDKs
  - `Azure.Data.Tables 12.9.1` for Table Storage
  - `Azure.Storage.Blobs 12.23.0` for Blob Storage
- ✅ **Updated**: All code to use new SDK patterns
  - `TableEntity` → `ITableEntity`
  - `CloudStorageAccount` → `TableServiceClient` / `BlobServiceClient`
  - `StorageException` → `RequestFailedException`
  - `EntityProperty` → Direct property serialization

### 4. Testing Infrastructure
- ✅ **Created**: `ActorTableEntities.Tests` - Unit tests (6 tests passing)
- ✅ **Created**: `ActorTableEntities.IntegrationTests` - Integration tests with Aspire
- ✅ **Added**: Test frameworks
  - xUnit 2.9.3
  - Moq 4.20.72
  - FluentAssertions 6.12.2

### 5. Aspire Integration
- ✅ **Created**: `SampleHttpFunctions.AppHost` - Aspire AppHost project
- ✅ **Configured**: Azure Storage emulator (Azurite) support
- ✅ **Added**: Development environment with Aspire Dashboard

### 6. OpenTelemetry Support
- ✅ **Added**: Application Insights worker service integration
- ✅ **Added**: Telemetry configuration in sample project
- ✅ **Enabled**: Distributed tracing support for consumers

### 7. Code Changes

#### Breaking Changes
1. **ITableEntity**: `ActorTableEntity` now implements `ITableEntity` (Azure SDK v12) instead of inheriting from `TableEntity`
2. **ETag**: Changed from `string "*"` to `ETag.All`
3. **Extension Methods**: Added new overload `AddActorTableEntities(IServiceCollection)` for isolated worker model

#### Non-Breaking Changes
1. **Backward Compatible**: Original `AddActorTableEntities(IWebJobsBuilder)` still supported for legacy projects
2. **Complex Properties**: Still supported via `ActorTableEntityComplexPropertyAttribute`
3. **Blob State Storage**: Still supported via `StateContainerName` option

### 8. Sample Project Updates
- ✅ **Migrated**: SampleHttpFunctions to isolated worker model
- ✅ **Added**: `Program.cs` with modern hosting configuration
- ✅ **Updated**: Dependency injection pattern for `IActorTableEntityClient`
- ✅ **Updated**: `host.json` for .NET 8

### 9. Documentation
- ✅ **Updated**: README.md with:
  - GitHub Actions badges
  - .NET 8 setup instructions
  - Both worker models (isolated and in-process)
  - Aspire development instructions
  - Testing instructions
  - CI/CD usage

## Migration Guide for Consumers

### For New Projects (.NET 8+)
```csharp
// Program.cs
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddActorTableEntities(options =>
        {
            options.StorageConnectionString = "...";
            options.ContainerName = "entitylocks";
        });
    })
    .Build();
```

### For Existing Projects (In-Process Model)
No code changes required! The library maintains backward compatibility.

### NuGet Package Updates
Update your `.csproj`:
```xml
<PackageReference Include="ActorTableEntities" Version="2.0.0" />
```

## Build & Test Status
- ✅ Main library builds successfully
- ✅ Sample functions build successfully
- ✅ 6 unit tests passing
- ✅ Integration tests ready (requires Azurite)

## Release Process
To release a new version:
```bash
# Update version in ActorTableEntities.csproj
git tag v2.0.0
git push origin v2.0.0
# GitHub Actions will automatically build and publish to NuGet
```

## Breaking Changes from v1.x
1. Minimum .NET version is now 8.0 (was 3.1)
2. Uses Azure SDK v12 (requires code changes if consuming SDK types directly)
3. ETag property type changed from `string` to `Azure.ETag`

## Compatibility Notes
- ✅ All existing functionality preserved
- ✅ Blob locking mechanism unchanged
- ✅ Table/Blob storage patterns unchanged
- ✅ Complex property serialization unchanged
