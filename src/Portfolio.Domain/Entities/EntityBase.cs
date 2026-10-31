namespace Portfolio.Domain.Entities;

public abstract class EntityBase
{
    public int Id { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
