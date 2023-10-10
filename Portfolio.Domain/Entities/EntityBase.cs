namespace Portfolio.Domain.Entities;

public abstract class EntityBase
{
    public Guid Id { get; set; }
    public DateTime CreationTime { get; set; }

    protected EntityBase()
    {
        Id = Guid.NewGuid();
        CreationTime = DateTime.Now;
    }
}