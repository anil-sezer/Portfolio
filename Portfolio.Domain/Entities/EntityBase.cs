using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portfolio.Domain.Entities;

public abstract class EntityBase
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    protected EntityBase()
    {
        CreatedAt = DateTime.UtcNow;
    }
}