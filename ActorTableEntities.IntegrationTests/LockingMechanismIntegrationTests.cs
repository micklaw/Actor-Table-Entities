using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ActorTableEntities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActorTableEntities.IntegrationTests;

/// <summary>
/// Integration tests for the locking mechanism to ensure proper lock acquisition,
/// waiting behavior, and concurrent access handling.
/// </summary>
public class LockingMechanismIntegrationTests : IAsyncLifetime
{
    private ServiceProvider? serviceProvider;
    private IActorTableEntityClient? client;
    private readonly string testPartitionKey = $"locktest-{Guid.NewGuid()}";

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddActorTableEntities(options =>
        {
            options.StorageConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "integration-test-locks";
            options.StateContainerName = "integration-test-state";
            options.WithRetry = true;
            options.RetryIntervalMilliseconds = 50;
        });
        
        serviceProvider = services.BuildServiceProvider();
        client = serviceProvider.GetRequiredService<IActorTableEntityClient>();

        // Give storage emulator time to be ready
        await Task.Delay(100);
    }

    public async Task DisposeAsync()
    {
        if (serviceProvider != null)
        {
            await serviceProvider.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_ShouldAcquireLock_AndNotExitEarly()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await using var state = await client!.GetLocked<TestCounter>(testPartitionKey, testRowKey);
        stopwatch.Stop();
        
        state.Entity.Count++;
        await state.Flush();

        // Assert
        state.Entity.Should().NotBeNull();
        state.IsReleased.Should().BeTrue(); // After Flush, lock should be released
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_WithConcurrentAccess_ShouldWaitForLock()
    {
        // Arrange
        var lockAcquired = new List<DateTime>();
        var lockReleased = new List<DateTime>();
        var tasks = new List<Task>();

        // Act - Start multiple concurrent tasks trying to acquire the same lock
        for (int i = 0; i < 3; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                await using var state = await client!.GetLocked<TestCounter>(testPartitionKey, testRowKey);
                lockAcquired.Add(DateTime.UtcNow);
                
                state.Entity.Count++;
                await Task.Delay(100); // Simulate work while holding lock
                
                await state.Flush();
                lockReleased.Add(DateTime.UtcNow);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        lockAcquired.Should().HaveCount(3);
        lockReleased.Should().HaveCount(3);
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_ShouldMaintainConsistency_WithMultipleUpdates()
    {
        // Arrange
        var updateCount = 10;
        var tasks = new List<Task>();
        var sharedKey = $"{testPartitionKey}-consistency";

        // Act - Multiple tasks incrementing the same counter
        for (int i = 0; i < updateCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await using var state = await client!.GetLocked<TestCounter>(sharedKey, "shared-counter");
                var currentCount = state.Entity.Count;
                await Task.Delay(10); // Simulate processing
                state.Entity.Count = currentCount + 1;
                await state.Flush();
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Final count should be exactly updateCount
        await using var finalState = await client!.GetLocked<TestCounter>(sharedKey, "shared-counter");
        finalState.Entity.Count.Should().Be(updateCount);
        await finalState.Flush();
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_ShouldNotThrowException_OnFirstAcquisition()
    {
        // Arrange
        var uniqueKey = $"{testPartitionKey}-first-{Guid.NewGuid()}";

        // Act
        Func<Task> act = async () =>
        {
            await using var state = await client!.GetLocked<TestCounter>(uniqueKey, "new-entity");
            state.Entity.Count = 1;
            await state.Flush();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_ShouldReleaseAfterDisposal_WhenNotFlushed()
    {
        // Arrange
        var uniqueKey = $"{testPartitionKey}-timeout-{Guid.NewGuid()}";

        // Act - Acquire lock and release it by disposing without explicit flush
        var state = await client!.GetLocked<TestCounter>(uniqueKey, "timeout-entity");
        state.Entity.Count = 1;
        
        // Explicitly dispose to release the lock
        await state.DisposeAsync();

        // Assert - Should be able to acquire the lock again
        Func<Task> act = async () =>
        {
            await using var state2 = await client!.GetLocked<TestCounter>(uniqueKey, "timeout-entity");
            await state2.Flush();
        };

        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_ShouldHandleNewEntity_Correctly()
    {
        // Arrange
        var uniqueKey = $"{testPartitionKey}-new-{Guid.NewGuid()}";

        // Act
        await using var state = await client!.GetLocked<TestCounter>(uniqueKey, "brand-new");
        
        // Assert
        state.IsNew.Should().BeTrue();
        state.Entity.Should().NotBeNull();
        state.Entity.Count.Should().Be(0); // Default value
        state.Entity.PartitionKey.Should().NotBeNullOrEmpty();
        state.Entity.RowKey.Should().NotBeNullOrEmpty();
        
        state.Entity.Count = 42;
        await state.Flush();

        // Verify persistence
        await using var state2 = await client!.GetLocked<TestCounter>(uniqueKey, "brand-new");
        state2.IsNew.Should().BeFalse();
        state2.Entity.Count.Should().Be(42);
        await state2.Flush();
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_ShouldHandleExistingEntity_Correctly()
    {
        // Arrange
        var uniqueKey = $"{testPartitionKey}-existing-{Guid.NewGuid()}";
        
        // Create entity first
        await using (var state = await client!.GetLocked<TestCounter>(uniqueKey, "existing"))
        {
            state.Entity.Count = 100;
            await state.Flush();
        }

        // Act - Get existing entity
        await using var state2 = await client!.GetLocked<TestCounter>(uniqueKey, "existing");
        
        // Assert
        state2.IsNew.Should().BeFalse();
        state2.Entity.Count.Should().Be(100);
        
        state2.Entity.Count = 200;
        await state2.Flush();

        // Verify update
        await using var state3 = await client!.GetLocked<TestCounter>(uniqueKey, "existing");
        state3.Entity.Count.Should().Be(200);
        await state3.Flush();
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task GetLocked_WithRetry_ShouldEventuallyAcquireLock()
    {
        // Arrange
        var uniqueKey = $"{testPartitionKey}-retry-{Guid.NewGuid()}";
        var firstLockAcquired = false;
        var secondLockAcquired = false;

        // Act - First task holds lock briefly
        var task1 = Task.Run(async () =>
        {
            await using var state = await client!.GetLocked<TestCounter>(uniqueKey, "retry-test");
            firstLockAcquired = true;
            state.Entity.Count = 1;
            await Task.Delay(200); // Hold lock for a bit
            await state.Flush();
        });

        // Wait a bit to ensure first task has lock
        await Task.Delay(50);

        // Second task should wait and then acquire
        var task2 = Task.Run(async () =>
        {
            await using var state = await client!.GetLocked<TestCounter>(uniqueKey, "retry-test");
            secondLockAcquired = true;
            state.Entity.Count++;
            await state.Flush();
        });

        await Task.WhenAll(task1, task2);

        // Assert
        firstLockAcquired.Should().BeTrue();
        secondLockAcquired.Should().BeTrue();
        
        // Verify final state
        await using var finalState = await client!.GetLocked<TestCounter>(uniqueKey, "retry-test");
        finalState.Entity.Count.Should().Be(2);
        await finalState.Flush();
    }

    [Fact(Skip = "Requires Azurite/Storage Emulator - Enable in CI/CD")]
    public async Task Flush_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        var uniqueKey = $"{testPartitionKey}-flush-{Guid.NewGuid()}";

        // Act
        var state = await client!.GetLocked<TestCounter>(uniqueKey, "flush-test");
        state.Entity.Count = 99;
        
        await state.Flush();
        
        // Additional flushes should be safe (no-op after first)
        await state.Flush();
        await state.Flush();

        // Assert
        state.IsReleased.Should().BeTrue();
        
        // Verify only one update occurred
        await using var verifyState = await client!.GetLocked<TestCounter>(uniqueKey, "flush-test");
        verifyState.Entity.Count.Should().Be(99);
        await verifyState.Flush();
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
