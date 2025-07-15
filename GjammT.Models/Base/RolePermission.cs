namespace GjammT.Models.Base;

public class RolePermission : BaseEntity
{
    // Foreign key to the Role
    public Guid RoleId { get; set; }
    public Role Role { get; set; }

    // Foreign key to the PermissionGroup
    public Guid PermissionGroupId { get; set; }
    public PermissionGroup PermissionGroup { get; set; }

    // This column stores the combined bitwise flags.
    // For example, the value 5 means CanRead (1) and CanWrite (4).
    public int AllowedActions { get; set; }
}