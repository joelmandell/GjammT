namespace GjammT.Models.Base;

public class BaseEntity
{
    public Guid Id { get; set; }
    public bool SoftDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
}