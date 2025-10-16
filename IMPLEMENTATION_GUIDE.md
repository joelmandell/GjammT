# Multi-Tenant Authorization Implementation

## Summary

This implementation adds comprehensive multi-tenant support to GjammT, allowing:
1. **Multiple tenants** (ClientCustomers) to share the same database
2. **Authorization and user role logic** to be centralized and shared across tenants
3. **Users to work for multiple tenants** simultaneously with different roles

## What Changed

### 1. Customer Entity - Now Tenant-Specific
**File**: `GjammT.Models/CustomerRegister/Customer.cs`

The `Customer` entity now implements `IMultiTenant`, making it tenant-specific:
```csharp
public class Customer : BaseEntity, IMultiTenant
{
    // ... existing properties ...
    
    // New: Links customer to a specific tenant
    public Guid ClientCustomerId { get; set; }
    public ClientCustomer ClientCustomer { get; set; }
}
```

### 2. AppDbContext - Tenant-Aware Database Context
**File**: `GjammT.Models/Data/AppDbContext.cs`

Updated to accept an optional tenant ID:
```csharp
public AppDbContext(DbContextOptions<AppDbContext> options, Guid? tenantId) : base(options)
{
    _tenantId = tenantId;
}
```

Key behaviors:
- When `tenantId` is provided: automatically filters all `IMultiTenant` entities
- When `tenantId` is null: no filtering (for admin operations or global entities)
- Automatically sets `ClientCustomerId` on new tenant-specific entities

### 3. New Services

#### ITenantService & TenantService
**Files**: `GjammT.SharedKernel/ITenantService.cs`, `GjammT.SharedKernel/TenantService.cs`

Manages the current tenant context:
```csharp
public interface ITenantService
{
    Guid? GetCurrentTenantId();
    void SetCurrentTenantId(Guid tenantId);
}
```

#### AppDbContextFactory
**File**: `GjammT.Models/Data/AppDbContextFactory.cs`

Creates tenant-aware database contexts:
```csharp
var context = factory.CreateDbContext(); // Uses current tenant
var context = factory.CreateDbContext(specificTenantId); // Uses specific tenant
```

#### UserTenantService
**File**: `GjammT.SharedKernel/UserTenantService.cs`

Manages user-tenant relationships:
- Assign users to customers
- Remove user access
- Get user's tenants and customers
- Check tenant access

#### Enhanced AuthorizationService
**File**: `GjammT.SharedKernel/AuthorizationService.cs`

Added helper methods:
- `UserHasAccessToCustomerAsync`
- `GetUserCustomerIdsAsync`

### 4. Middleware
**File**: `GjammT.Models/Middleware/TenantMiddleware.cs`

ASP.NET Core middleware to automatically set tenant context from:
- HTTP headers (`X-Tenant-Id`)
- Subdomain
- User claims

### 5. Database Migration
**File**: `GjammT.Models/Migrations/20251016000000_AddMultiTenantCustomer.cs`

Adds `ClientCustomerId` column to the Customers table with:
- Foreign key to ClientCustomer
- Index for performance

## Architecture Overview

### Global Entities (Shared Across Tenants)
- **User**: Users can work across multiple tenants
- **Role**: Roles are defined once, used by all tenants
- **PermissionGroup**: Permission groups are global
- **RolePermission**: Permission configurations are shared

### Tenant-Specific Entities
- **Customer**: Each customer belongs to one tenant
- **Address**: Addresses are tenant-specific

### Cross-Tenant Bridge
- **UserCustomerRole**: Links users to customers with specific roles, enabling cross-tenant access

## How It Works

1. **Tenant Context**: Applications set the current tenant using `ITenantService`
2. **Automatic Filtering**: `AppDbContext` automatically filters tenant-specific data
3. **User Access**: `UserCustomerRole` table links users to customers across tenants
4. **Authorization**: `AuthorizationService` checks permissions based on user-customer-role relationships

## Usage

### Setting Up (In ASP.NET Core)

```csharp
// In Program.cs
builder.Services.AddSingleton<ITenantService, TenantService>();
builder.Services.AddScoped<AppDbContextFactory>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<UserTenantService>();

// Add middleware
app.UseMiddleware<TenantMiddleware>();
```

### Creating Multi-Tenant Data

```csharp
// Create a tenant
var tenant = new ClientCustomer { CompanyName = "ACME Corp", Subdomain = "acme" };

// Set tenant context
tenantService.SetCurrentTenantId(tenant.Id);

// Create customer (automatically associated with tenant)
var customer = new Customer { Name = "John's Organization" };
context.Customers.Add(customer);
await context.SaveChangesAsync();

// Create a global user
var user = new User { Email = "john@example.com", ... };
context.Users.Add(user);
await context.SaveChangesAsync();

// Assign user to customer
var userTenantService = new UserTenantService(context);
await userTenantService.AssignUserToCustomerAsync(user.Id, customer.Id, roleId);
```

## Documentation

Comprehensive documentation is available in:
- **MULTI_TENANT_ARCHITECTURE.md**: Detailed architecture explanation
- **MULTI_TENANT_USAGE.md**: Usage examples and best practices
- **MultiTenantExampleService.cs**: Working code examples

## Migration Guide

If you have existing data:

1. **Apply the migration** to add `ClientCustomerId` to Customers table
2. **Create default tenant(s)** if needed
3. **Update existing customers** to link them to appropriate tenants:
   ```sql
   UPDATE "Customers" SET "ClientCustomerId" = '<tenant-id>' WHERE ...;
   ```

## Security Considerations

1. **Always validate tenant access** before operations
2. **Use TenantMiddleware** to automatically set context from secure sources
3. **Audit logging**: Track which tenant context is used for each operation
4. **Test tenant isolation**: Ensure data leakage doesn't occur between tenants

## Testing

Example test pattern:
```csharp
[Fact]
public async Task User_Can_Access_Customers_In_Multiple_Tenants()
{
    // Arrange
    var tenant1 = CreateTenant("Tenant1");
    var tenant2 = CreateTenant("Tenant2");
    var user = CreateUser();
    
    // Create customer in each tenant
    tenantService.SetCurrentTenantId(tenant1.Id);
    var customer1 = await CreateCustomer("Customer1");
    
    tenantService.SetCurrentTenantId(tenant2.Id);
    var customer2 = await CreateCustomer("Customer2");
    
    // Assign user to both
    await AssignUserToCustomer(user.Id, customer1.Id, roleId);
    await AssignUserToCustomer(user.Id, customer2.Id, roleId);
    
    // Assert - user has access to both tenants
    var tenants = await userTenantService.GetUserTenantsAsync(user.Id);
    Assert.Equal(2, tenants.Count);
}
```

## Benefits

✅ **Single User Identity**: Users maintain one account across all tenants  
✅ **Centralized Management**: Roles and permissions defined once  
✅ **Data Isolation**: Customer data automatically isolated by tenant  
✅ **Flexible Access**: Users can have different roles in different tenants  
✅ **Scalable**: Add new tenants without schema changes  

## Next Steps

1. Apply the database migration
2. Update your application to use `ITenantService` and `AppDbContextFactory`
3. Add `TenantMiddleware` to your web application
4. Review and test existing authorization logic
5. Update API endpoints to handle tenant context
