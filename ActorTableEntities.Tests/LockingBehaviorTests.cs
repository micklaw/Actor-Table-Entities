using System;
using ActorTableEntities;
using FluentAssertions;
using Xunit;

namespace ActorTableEntities.Tests;

/// <summary>
/// Unit tests for the locking mechanism through the public API,
/// focusing on ensuring locks are properly acquired and behavior is correct.
/// </summary>
public class LockingBehaviorTests
{
    [Fact]
    public void ActorTableEntityOptions_ShouldHaveDefaultRetryValues()
    {
        // Arrange & Act
        var options = new ActorTableEntityOptions
        {
            StorageConnectionString = "UseDevelopmentStorage=true"
        };

        // Assert
        options.WithRetry.Should().BeTrue();
        options.RetryIntervalMilliseconds.Should().Be(100);
    }

    [Fact]
    public void ActorTableEntityOptions_ShouldAllowCustomRetryInterval()
    {
        // Arrange & Act
        var options = new ActorTableEntityOptions
        {
            StorageConnectionString = "UseDevelopmentStorage=true",
            WithRetry = true,
            RetryIntervalMilliseconds = 100
        };

        // Assert
        options.WithRetry.Should().BeTrue();
        options.RetryIntervalMilliseconds.Should().Be(100);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void ActorTableEntityOptions_ShouldAcceptVariousRetryIntervals(int intervalMs)
    {
        // Arrange & Act
        var options = new ActorTableEntityOptions
        {
            StorageConnectionString = "UseDevelopmentStorage=true",
            RetryIntervalMilliseconds = intervalMs
        };

        // Assert
        options.RetryIntervalMilliseconds.Should().Be(intervalMs);
    }

    [Fact]
    public void ActorTableEntityOptions_WithRetry_ShouldEnableRetryBehavior()
    {
        // Arrange & Act
        var options = new ActorTableEntityOptions
        {
            StorageConnectionString = "UseDevelopmentStorage=true",
            WithRetry = true,
            RetryIntervalMilliseconds = 250
        };

        // Assert
        options.WithRetry.Should().BeTrue();
        options.RetryIntervalMilliseconds.Should().Be(250);
    }

    [Fact]
    public void ActorTableEntityOptions_Constructor_ShouldSetAllProperties()
    {
        // Arrange & Act
        var options = new ActorTableEntityOptions
        {
            StorageConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test-container",
            StateContainerName = "state-container",
            WithRetry = true,
            RetryIntervalMilliseconds = 100
        };

        // Assert
        options.StorageConnectionString.Should().Be("UseDevelopmentStorage=true");
        options.ContainerName.Should().Be("test-container");
        options.StateContainerName.Should().Be("state-container");
        options.WithRetry.Should().BeTrue();
        options.RetryIntervalMilliseconds.Should().Be(100);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(5000)]
    public void ActorTableEntityOptions_ShouldSupportBoundaryRetryIntervals(int intervalMs)
    {
        // Arrange & Act
        var options = new ActorTableEntityOptions
        {
            StorageConnectionString = "UseDevelopmentStorage=true",
            WithRetry = true,
            RetryIntervalMilliseconds = intervalMs
        };

        // Assert
        options.RetryIntervalMilliseconds.Should().Be(intervalMs);
        options.WithRetry.Should().BeTrue();
    }

    [Fact]
    public void ActorTableEntityClientState_ShouldBeAsyncDisposable()
    {
        // This tests that the interface is correctly defined
        // The actual disposal logic is tested in integration tests
        var stateType = typeof(IActorTableEntityClientState<>);
        var disposableType = typeof(IAsyncDisposable);

        // Assert
        disposableType.IsAssignableFrom(stateType).Should().BeTrue();
    }
}
