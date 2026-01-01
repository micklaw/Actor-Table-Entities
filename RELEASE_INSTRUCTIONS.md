# Release Instructions for ActorTableEntities v2.0.0

## Overview
This document provides instructions for releasing version 2.0.0 of ActorTableEntities to NuGet.

## Current Status
- ✅ Version set to 2.0.0 in `ActorTableEntities/ActorTableEntities.csproj`
- ✅ Package metadata configured (license, readme, description, tags)
- ✅ README.md includes v2.0 features and documentation
- ✅ Build succeeds without errors
- ✅ All unit tests pass (19/19)
- ✅ Integration tests configured (require Azurite)
- ✅ Release workflow configured in `.github/workflows/release.yml`
- ✅ MIT License file created

## Release Workflow
The release process is fully automated through GitHub Actions. When you push a tag matching the pattern `v*.*.*`, the release workflow will:

1. **Checkout** the code
2. **Setup** .NET 8.0
3. **Extract** the version from the tag (e.g., `v2.0.0` → `2.0.0`)
4. **Restore** dependencies
5. **Build** the solution in Release configuration
6. **Test** both unit and integration test projects
7. **Pack** the NuGet package with the extracted version
8. **Publish** to NuGet.org (requires `NUGET_API_KEY` secret)
9. **Create** a GitHub Release with auto-generated release notes

## Prerequisites
Before creating a release, ensure:

1. **NuGet API Key**: The `NUGET_API_KEY` secret must be configured in the repository settings
   - Go to: Repository Settings → Secrets and variables → Actions
   - Create or verify the `NUGET_API_KEY` secret exists
   - Get your API key from: https://www.nuget.org/account/apikeys

2. **All Tests Pass**: Verify locally if possible
   ```bash
   dotnet test ActorTableEntities.Tests/ActorTableEntities.Tests.csproj --configuration Release
   ```

3. **Clean Working Directory**: Ensure all changes are committed
   ```bash
   git status
   ```

## Creating the v2.0.0 Release

### Step 1: Verify Current State
```bash
# Ensure you're on the correct branch (typically main or master)
git checkout main
git pull origin main

# Verify the version in the project file
grep "<Version>" ActorTableEntities/ActorTableEntities.csproj
# Should show: <Version>2.0.0</Version>
```

### Step 2: Create and Push the Tag
```bash
# Create an annotated tag for v2.0.0
git tag -a v2.0.0 -m "Release version 2.0.0 - .NET 8.0 upgrade with Azure SDK v12"

# Verify the tag was created
git tag -l "v2.0.0"

# Push the tag to GitHub (this triggers the release workflow)
git push origin v2.0.0
```

### Step 3: Monitor the Release
1. Go to the repository on GitHub
2. Navigate to **Actions** tab
3. Look for the "Release to NuGet" workflow run
4. Monitor the progress - it should:
   - ✅ Build successfully
   - ✅ Pass all tests
   - ✅ Create the NuGet package
   - ✅ Publish to NuGet.org
   - ✅ Create a GitHub Release

### Step 4: Verify the Release
After the workflow completes:

1. **Check NuGet.org**:
   - Visit: https://www.nuget.org/packages/ActorTableEntities/
   - Verify version 2.0.0 appears in the version list
   - It may take a few minutes for the package to be indexed and searchable

2. **Check GitHub Releases**:
   - Navigate to: https://github.com/micklaw/Actor-Table-Entities/releases
   - Verify the v2.0.0 release was created
   - Review the auto-generated release notes

3. **Test Installation**:
   ```bash
   # In a test project, verify the package can be installed
   dotnet add package ActorTableEntities --version 2.0.0
   ```

## Package Contents
The v2.0.0 NuGet package includes:
- **Target Framework**: .NET 8.0
- **Dependencies**:
  - Azure.Data.Tables 12.9.1
  - Azure.Storage.Blobs 12.23.0
  - Microsoft.Azure.Functions.Extensions 1.1.0
  - Microsoft.Extensions.Azure 1.7.6
  - System.Diagnostics.DiagnosticSource 8.0.1
- **Documentation**: README.md included in the package
- **License**: MIT License

## Key Features in v2.0.0
- ✅ Upgraded to .NET 8.0 LTS
- ✅ Migrated to Azure SDK v12 (Azure.Data.Tables and Azure.Storage.Blobs)
- ✅ Azure Functions v4 support with isolated worker model
- ✅ Aspire integration for local development
- ✅ OpenTelemetry support with Application Insights
- ✅ Comprehensive unit and integration tests
- ✅ Manual versioning for simplified package management

## Troubleshooting

### Issue: Workflow Fails to Publish
**Possible Causes**:
- Missing or invalid `NUGET_API_KEY` secret
- API key lacks permissions to publish packages
- Package with same version already exists on NuGet

**Solution**:
- Verify the API key in repository secrets
- Ensure the API key has "Push" permission
- Check NuGet.org for existing versions

### Issue: Tests Fail in CI
**Possible Causes**:
- Integration tests requiring Azurite fail
- Environment-specific issues

**Solution**:
- Check the workflow logs in GitHub Actions
- Integration tests are configured to skip if Azurite is not available
- Verify the test failure is not in unit tests

### Issue: Tag Already Exists
**Solution**:
```bash
# If you need to recreate the tag, delete it locally and remotely
git tag -d v2.0.0
git push origin :refs/tags/v2.0.0

# Then recreate and push
git tag -a v2.0.0 -m "Release version 2.0.0"
git push origin v2.0.0
```

## Post-Release Checklist
- [ ] Verify package appears on NuGet.org
- [ ] Verify GitHub Release is created
- [ ] Update any external documentation referencing the package
- [ ] Announce the release (if applicable)
- [ ] Consider updating the README badge if needed

## For Future Releases

### Bumping to a New Version (e.g., 2.1.0 or 3.0.0)
1. Update the version in `ActorTableEntities/ActorTableEntities.csproj`
2. Update README.md if there are new features
3. Commit and push changes
4. Create and push a new tag following the same process
   ```bash
   git tag -a v2.1.0 -m "Release version 2.1.0 - [brief description]"
   git push origin v2.1.0
   ```

## Support
For issues with the release process:
- Check GitHub Actions logs
- Review NuGet.org package status
- Open an issue in the repository

## References
- NuGet Package: https://www.nuget.org/packages/ActorTableEntities/
- Repository: https://github.com/micklaw/Actor-Table-Entities
- Release Workflow: `.github/workflows/release.yml`
