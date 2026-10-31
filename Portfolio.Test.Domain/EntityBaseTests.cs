using FluentAssertions;
using Portfolio.Domain.Entities;

namespace Portfolio.Test.Domain;

public class EntityBaseTests
{
    private sealed class TestEntity : EntityBase
    {
    }

    [Fact]
    public void EntityBase_Id_ShouldDefaultToZero()
    {
        var entity = new TestEntity();

        entity.Id.Should().Be(0);
    }

    [Fact]
    public void EntityBase_CreatedAt_ShouldDefaultToUtcNowWithZeroOffset()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new TestEntity();
        var after = DateTimeOffset.UtcNow;

        entity.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        entity.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void EntityBase_CreatedAt_ShouldAllowExplicitInitialization()
    {
        var customTime = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var entity = new TestEntity { CreatedAt = customTime };

        entity.CreatedAt.Should().Be(customTime);
    }
}
