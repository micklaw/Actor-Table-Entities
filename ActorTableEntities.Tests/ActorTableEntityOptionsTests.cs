using ActorTableEntities;
using FluentAssertions;
using Xunit;

namespace ActorTableEntities.Tests;

public class ActorTableEntityOptionsTests
{
    [Fact]
    public void ActorTableEntityOptions_ShouldSetStorageConnectionString()
    {
        // Arrange
        var options = new ActorTableEntityOptions
        {
            StorageConnectionString = "UseDevelopmentStorage=true"
        };

        // Act & Assert
        options.StorageConnectionString.Should().Be("UseDevelopmentStorage=true");
    }

    [Fact]
    public void ActorTableEntityOptions_ShouldSetContainerName()
    {
        // Arrange
        var options = new ActorTableEntityOptions
        {
            ContainerName = "testcontainer"
        };

        // Act & Assert
        options.ContainerName.Should().Be("testcontainer");
    }

    [Fact]
    public void ActorTableEntityOptions_ShouldSetRetryOptions()
    {
        // Arrange
        var options = new ActorTableEntityOptions
        {
            WithRetry = true,
            RetryIntervalMilliseconds = 200
        };

        // Act & Assert
        options.WithRetry.Should().BeTrue();
        options.RetryIntervalMilliseconds.Should().Be(200);
    }

    [Fact]
    public void ActorTableEntityOptions_ShouldSetStateContainerName()
    {
        // Arrange
        var options = new ActorTableEntityOptions
        {
            StateContainerName = "actorstate"
        };

        // Act & Assert
        options.StateContainerName.Should().Be("actorstate");
    }
}
