using ActorTableEntities;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActorTableEntities.IntegrationTests;

public class ActorTableEntityIntegrationTests
{
    [Fact(Skip = "Requires Azurite to be running - can be enabled in CI/CD with Aspire")]
    public async Task GetLocked_ShouldAcquireLockAndUpdateEntity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddActorTableEntities("UseDevelopmentStorage=true", options =>
        {
            options.ContainerName = "testlocks";
            options.WithRetry = false;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IActorTableEntityClient>();

        // Act
        await using var state = await client.GetLocked<TestCounter>("test", "counter1");
        state.Entity.Count++;
        await state.Flush();

        // Assert
        state.Entity.Count.Should().BeGreaterThan(0);
    }

    private class TestCounter : ActorTableEntity
    {
        public TestCounter() : base()
        {
        }

        public TestCounter(string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
        }

        public int Count { get; set; }
    }
}
