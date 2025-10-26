using GjammT.Models.CustomerRegister;

namespace GjammT.Models.Base;

/// <summary>
/// Represents user-level permissions that can override or extend role-based permissions.
/// Allows assigning specific permissions to users for particular customers or tenants.
/// </summary>
public class UserPermission : BaseEntity
{
    // Foreign key to the User
    public Guid UserId { get; set; }
    public User User { get; set; }

    // Optional: Foreign key to the Customer (null means permission applies at tenant level)
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Optional: Foreign key to the Tenant/ClientCustomer (null means permission applies globally)
    public Guid? ClientCustomerId { get; set; }
    public ClientCustomer? ClientCustomer { get; set; }

    // Foreign key to the PermissionGroup
    public Guid PermissionGroupId { get; set; }
    public PermissionGroup PermissionGroup { get; set; }

    // This column stores the combined bitwise flags for allowed actions.
    // For example, the value 5 means CanRead (1) and CanWrite (4).
    public int AllowedActions { get; set; }

    // Optional: Description of why this permission was granted
    public string? Notes { get; set; }
}
