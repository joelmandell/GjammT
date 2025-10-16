# Multi-Tenant Quick Reference

## Key Components

| Component | Purpose | Location |
|-----------|---------|----------|
| `IMultiTenant` | Interface for tenant-specific entities | `GjammT.Models/Base/IMultiTenant.cs` |
| `ClientCustomer` | Represents a tenant | `GjammT.Models/Base/ClientCustomer.cs` |
| `ITenantService` | Manages current tenant context | `GjammT.SharedKernel/ITenantService.cs` |
| `AppDbContext` | Tenant-aware database context | `GjammT.Models/Data/AppDbContext.cs` |
| `AppDbContextFactory` | Creates tenant-aware contexts | `GjammT.Models/Data/AppDbContextFactory.cs` |
| `UserTenantService` | Manages user-tenant relationships | `GjammT.SharedKernel/UserTenantService.cs` |
| `AuthorizationService` | Permission checking | `GjammT.SharedKernel/AuthorizationService.cs` |
| `TenantMiddleware` | Auto-sets tenant from HTTP context | `GjammT.Models/Middleware/TenantMiddleware.cs` |

## Entity Classification

### Global Entities (NOT tenant-specific)
These are shared across all tenants:
- ✅ `User` - Users can work across multiple tenants
- ✅ `Role` - Roles are defined globally
- ✅ `PermissionGroup` - Permission groups are global
- ✅ `RolePermission` - Permission configurations are shared
- ✅ `UserCustomerRole` - Bridge table for cross-tenant access

### Tenant-Specific Entities
These belong to a specific tenant (implement `IMultiTenant`):
- 🏢 `Customer` - Each customer belongs to one tenant
- 🏢 `Address` - Addresses are tenant-specific

## Common Operations

### 1. Create a Tenant
```csharp
using var context = new AppDbContext(options, tenantId: null);
var tenant = new ClientCustomer 
{ 
    CompanyName = "ACME Corp", 
    Subdomain = "acme" 
};
context.ClientCustomers.Add(tenant);
await context.SaveChangesAsync();
```

### 2. Set Current Tenant
```csharp
// In controller or middleware
tenantService.SetCurrentTenantId(tenantId);
```

### 3. Create Tenant-Specific Data
```csharp
// Set tenant context first
tenantService.SetCurrentTenantId(tenantId);

using var context = contextFactory.CreateDbContext();
var customer = new Customer { Name = "ACME Corp" };
context.Customers.Add(customer);
await context.SaveChangesAsync(); // ClientCustomerId set automatically
```

### 4. Create Global User
```csharp
using var context = new AppDbContext(options, tenantId: null);
var user = new User { Email = "user@example.com", ... };
context.Users.Add(user);
await context.SaveChangesAsync();
```

### 5. Assign User to Customer
```csharp
var userTenantService = new UserTenantService(context);
await userTenantService.AssignUserToCustomerAsync(userId, customerId, roleId);
```

### 6. Check Permissions
```csharp
var authService = new AuthorizationService(context);
bool canWrite = await authService.HasPermissionAsync(
    userId, 
    customerId, 
    "Products", 
    PermissionFlags.CanWrite
);
```

### 7. Get User's Tenants
```csharp
var tenants = await userTenantService.GetUserTenantsAsync(userId);
```

### 8. Query with Specific Tenant
```csharp
// Option 1: Use factory
var context = contextFactory.CreateDbContext(tenantId);

// Option 2: Create directly
var context = new AppDbContext(options, tenantId);

// Queries are automatically filtered
var customers = await context.Customers.ToListAsync();
```

### 9. Query Without Tenant Filter
```csharp
// Use null tenant for admin/global operations
var context = new AppDbContext(options, tenantId: null);

// No filtering applied
var allCustomers = await context.Customers.ToListAsync();
```

## Permission Flags

```csharp
[Flags]
public enum PermissionFlags
{
    None        = 0,      // No permissions
    CanRead     = 1,      // Read access
    CanCreate   = 2,      // Create access
    CanWrite    = 4,      // Update access
    CanDelete   = 8,      // Delete access
    All         = ~0      // All permissions
}
```

### Combining Permissions
```csharp
// Give read and write permissions
var permissions = PermissionFlags.CanRead | PermissionFlags.CanWrite;

// Check if has specific permission
if (permissions.HasFlag(PermissionFlags.CanWrite))
{
    // Allow write
}
```

## Middleware Setup

```csharp
// In Program.cs
builder.Services.AddSingleton<ITenantService, TenantService>();
builder.Services.AddScoped<AppDbContextFactory>();
builder.Services.AddDbContext<AppDbContext>(...);

var app = builder.Build();

// Add middleware AFTER authentication
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
```

## API Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContextFactory _contextFactory;
    private readonly ITenantService _tenantService;
    private readonly UserTenantService _userTenantService;
    
    [HttpGet]
    public async Task<IActionResult> GetCustomers()
    {
        // Tenant already set by middleware
        using var context = _contextFactory.CreateDbContext();
        var customers = await context.Customers.ToListAsync();
        return Ok(customers);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var userId = GetCurrentUserId();
        var tenantId = _tenantService.GetCurrentTenantId();
        
        // Verify user has access
        using var context = _contextFactory.CreateDbContext();
        var hasAccess = await _userTenantService.UserHasAccessToTenantAsync(
            userId, 
            tenantId.Value
        );
        
        if (!hasAccess)
            return Forbid();
        
        var customer = await context.Customers.FindAsync(id);
        return customer == null ? NotFound() : Ok(customer);
    }
}
```

## Testing Pattern

```csharp
[Fact]
public async Task User_Can_Work_Across_Multiple_Tenants()
{
    // Arrange
    var tenant1 = await CreateTenant("Tenant1");
    var tenant2 = await CreateTenant("Tenant2");
    var user = await CreateUser("user@test.com");
    
    // Create data in tenant1
    _tenantService.SetCurrentTenantId(tenant1.Id);
    var customer1 = await CreateCustomer("Customer1");
    await AssignUser(user.Id, customer1.Id);
    
    // Create data in tenant2
    _tenantService.SetCurrentTenantId(tenant2.Id);
    var customer2 = await CreateCustomer("Customer2");
    await AssignUser(user.Id, customer2.Id);
    
    // Assert - user has access to both
    var tenants = await _userTenantService.GetUserTenantsAsync(user.Id);
    Assert.Equal(2, tenants.Count);
}
```

## Database Migration

To apply the migration:
```bash
dotnet ef migrations add AddMultiTenantCustomer --project GjammT.Models
dotnet ef database update --project GjammT.Models
```

Or if migration file already exists:
```bash
dotnet ef database update --project GjammT.Models
```

## Troubleshooting

### Problem: Customers not showing up
**Solution**: Ensure tenant context is set
```csharp
tenantService.SetCurrentTenantId(tenantId);
```

### Problem: Cannot save customer
**Solution**: Either set tenant context or set ClientCustomerId explicitly
```csharp
// Option 1
tenantService.SetCurrentTenantId(tenantId);
customer.ClientCustomerId = tenantId; // Auto-set

// Option 2
customer.ClientCustomerId = tenantId; // Set explicitly
```

### Problem: User not authorized
**Solution**: Check UserCustomerRole exists
```csharp
var hasAccess = await userTenantService.UserHasAccessToCustomerAsync(userId, customerId);
```

## Best Practices

1. ✅ **Always validate tenant access** in controllers
2. ✅ **Use AppDbContextFactory** instead of direct DbContext
3. ✅ **Set tenant context early** (in middleware)
4. ✅ **Use null tenant** for admin operations
5. ✅ **Test cross-tenant scenarios**
6. ✅ **Log tenant context** for audit trails
7. ✅ **Document which entities are tenant-specific**

## References

- 📖 [MULTI_TENANT_ARCHITECTURE.md](MULTI_TENANT_ARCHITECTURE.md) - Architecture details
- 📖 [MULTI_TENANT_USAGE.md](MULTI_TENANT_USAGE.md) - Usage examples
- 📖 [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) - Implementation guide
- 💻 [MultiTenantExampleService.cs](GjammT.SharedKernel/MultiTenantExampleService.cs) - Code examples
