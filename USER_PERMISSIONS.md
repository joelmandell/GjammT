# User-Level Permissions

## Overview

The User Permission system extends the role-based permission model by allowing administrators to assign specific permissions to individual users for particular customers or tenants. This provides fine-grained access control that can override or extend role-based permissions.

## Permission Hierarchy

The system checks permissions in the following order (highest to lowest priority):

1. **User-Level Permissions** - Direct permissions assigned to a user for a specific customer/tenant
2. **Role-Based Permissions** - Permissions inherited from the user's role assignment

## Permission Scopes

User permissions can be assigned at three different scope levels:

### 1. Customer-Level Permissions
- Apply to a specific customer within a tenant
- Most granular level of permission control
- Example: Allow User A to read/write invoices for Customer X

### 2. Tenant-Level Permissions
- Apply to all customers within a specific tenant
- Useful for tenant-wide administrative permissions
- Example: Allow User B to manage all bookings in Tenant Y

### 3. Global Permissions
- Apply across all tenants and customers
- Rarely used, mainly for super-admin capabilities
- Example: Allow User C to view system logs globally

## Database Schema

### UserPermission Table

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | Primary key |
| UserId | Guid | Foreign key to Users table |
| CustomerId | Guid? | Optional: Foreign key to Customers table |
| ClientCustomerId | Guid? | Optional: Foreign key to ClientCustomers table |
| PermissionGroupId | Guid | Foreign key to PermissionGroups table |
| AllowedActions | int | Bitwise flags for permissions (Read, Create, Write, Delete) |
| Notes | string? | Optional notes about why permission was granted |
| SoftDeleted | bool | Soft delete flag |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |

### Scope Logic

- **Customer-level**: `CustomerId` is set, `ClientCustomerId` may be null
- **Tenant-level**: `ClientCustomerId` is set, `CustomerId` is null
- **Global**: Both `CustomerId` and `ClientCustomerId` are null

## Permission Flags

User permissions use the same bitwise permission flags as role permissions:

```csharp
[Flags]
public enum PermissionFlags
{
    None        = 0,      // No permissions
    CanRead     = 1 << 0, // Can view/read (1)
    CanCreate   = 1 << 1, // Can create new items (2)
    CanWrite    = 1 << 2, // Can update existing items (4)
    CanDelete   = 1 << 3, // Can delete items (8)
    All         = ~0      // All permissions
}
```

## Authorization Service

The `AuthorizationService` provides methods to check user permissions:

### HasPermissionAsync

Checks if a user has a specific permission for a customer:

```csharp
public async Task<bool> HasPermissionAsync(
    Guid userId, 
    Guid customerId, 
    string permissionGroupName, 
    PermissionFlags requiredPermission)
```

**Logic:**
1. First checks for user-level permissions for the specific customer
2. If found, returns whether the user permission includes the required flag
3. If no user permission exists, falls back to role-based permissions

### HasTenantPermissionAsync

Checks if a user has a specific permission at the tenant level:

```csharp
public async Task<bool> HasTenantPermissionAsync(
    Guid userId, 
    Guid clientCustomerId, 
    string permissionGroupName, 
    PermissionFlags requiredPermission)
```

**Usage:** For admin-level permissions that apply to all customers in a tenant

## Admin Interface

### User Permission Management Page

**Route:** `/gjadmin/userpermissions`

**Features:**
- List all user permissions with filtering by user and tenant
- Create new user permissions with scope selection
- Edit existing permissions
- Delete permissions with confirmation
- Display permission details including user, scope, and permission flags

**Filters:**
- Filter by User - Show permissions for a specific user
- Filter by Tenant - Show permissions for a specific tenant

**Create/Edit Dialog:**
1. Select User
2. Choose Scope Level (Customer/Tenant/Global)
3. Select Tenant (if tenant or customer scope)
4. Select Customer (if customer scope)
5. Choose Permission Group
6. Set Allowed Actions (checkboxes for Read, Create, Write, Delete)
7. Add optional Notes

## Use Cases

### Example 1: Temporary Access
Grant a user temporary write access to a specific customer's invoices without changing their role:

```
User: John Doe
Scope: Customer-level
Customer: ACME Corporation
Permission Group: Invoices
Allowed Actions: Read, Write
Notes: Temporary access for Q4 2024 audit
```

### Example 2: Tenant Administrator
Grant a user administrative permissions for all customers in a tenant:

```
User: Jane Smith
Scope: Tenant-level
Tenant: City of Springfield
Permission Group: Bookings
Allowed Actions: Read, Create, Write, Delete
Notes: Primary tenant administrator
```

### Example 3: Read-Only Override
Restrict a user to read-only access for a specific customer, overriding their role permissions:

```
User: Bob Wilson
Scope: Customer-level
Customer: Sensitive Client
Permission Group: Financial Records
Allowed Actions: Read
Notes: Training period - read-only access
```

## Best Practices

1. **Use Role-Based Permissions as Default**: Assign permissions through roles whenever possible
2. **Document User Permissions**: Always add notes explaining why a user permission was granted
3. **Regular Audits**: Review user permissions regularly and remove those no longer needed
4. **Prefer Specific Scopes**: Use the most specific scope level appropriate for the use case
5. **Avoid Global Permissions**: Reserve global permissions for super-admin scenarios only

## Migration

To apply the database migration:

```bash
cd GjammT.Models
dotnet ef database update
```

Or with a specific connection string:

```bash
cd GjammT.Models
dotnet ef database update --connection "Server=localhost;Port=5432;Database=postgres;User Id=<username>;"
```

## Security Considerations

1. **Audit Trail**: All user permissions include creation and update timestamps
2. **Soft Deletes**: Permissions are soft-deleted to maintain history
3. **Cascade Deletes**: Deleting a user cascades to their permissions
4. **Restrict Deletes**: Deleting a customer or tenant restricts (not cascades) permission deletion to prevent accidental data loss
5. **Authorization Required**: All admin operations require authentication via the `[Authorize]` attribute

## Future Enhancements

- [ ] Time-based permissions (expiration dates)
- [ ] Bulk permission assignment
- [ ] Permission templates/presets
- [ ] Audit log for permission changes
- [ ] Permission inheritance visualization
- [ ] Export permission reports
