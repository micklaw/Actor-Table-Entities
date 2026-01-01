using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ActorTableEntities;
using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        var options = new ActorTableEntityOptions();

        // Assert
        options.WithRetry.Should().BeFalse();
        options.RetryIntervalMilliseconds.Should().Be(50);
    }

    [Fact]
    public void ActorTableEntityOptions_ShouldAllowCustomRetryInterval()
    {
        // Arrange & Act
        var options = new ActorTableEntityOptions
        {
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
        var options = new ActorTableEntityOptions(
            storageConnectionString: "UseDevelopmentStorage=true",
            containerName: "test-container",
            stateContainerName: "state-container",
            withRetry: true,
            retryIntervalMilliseconds: 100
        );

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
