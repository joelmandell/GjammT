namespace GjammT.Models.Base;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    // A Role is defined by its collection of permissions
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}