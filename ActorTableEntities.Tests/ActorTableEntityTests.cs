using ActorTableEntities;
using FluentAssertions;
using Xunit;

namespace ActorTableEntities.Tests;

public class ActorTableEntityTests
{
    [Fact]
    public void ActorTableEntity_ShouldSetPartitionAndRowKey()
    {
        // Arrange & Act
        var entity = new TestEntity("partition1", "row1");

        // Assert
        entity.PartitionKey.Should().Be("partition1");
        entity.RowKey.Should().Be("row1");
    }

    [Fact]
    public void ActorTableEntity_ShouldInitializeWithDefaultConstructor()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Should().NotBeNull();
        entity.PartitionKey.Should().BeNull();
        entity.RowKey.Should().BeNull();
    }

    private class TestEntity : ActorTableEntity
    {
        public TestEntity() : base()
        {
        }

        public TestEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
        }

        public string TestProperty { get; set; } = string.Empty;
    }
}
