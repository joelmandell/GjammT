namespace GjammT.Models.Base;

public class BaseEntity
{
    public string Id { get; set; }
    public bool SoftDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; }
}