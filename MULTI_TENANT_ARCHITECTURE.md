# Multi-Tenant Architecture

## Overview
This system implements a multi-tenant architecture where authorization and user role logic are shared across multiple tenants in the same database. This allows users to work for multiple tenants simultaneously.

## Key Concepts

### Tenant (ClientCustomer)
A `ClientCustomer` represents a tenant in the system (e.g., a company, organization, or department). Each tenant has:
- Unique identifier (Id)
- Company name
- Subdomain for access

### Tenant-Specific Entities (IMultiTenant)
Entities that implement `IMultiTenant` are isolated per tenant:
- **Customer**: End customers/associations that belong to a specific tenant
- **Address**: Physical addresses linked to a tenant

These entities have a `ClientCustomerId` property that links them to their tenant.

### Global/Shared Entities
Entities that are shared across all tenants:
- **User**: Users can work across multiple tenants
- **Role**: Roles are defined globally and can be used by any tenant
- **PermissionGroup**: Permission groups are shared across tenants
- **RolePermission**: Permission configurations are global

### Cross-Tenant User Access (UserCustomerRole)
The `UserCustomerRole` table is the key to multi-tenant user support:
- Links a **User** to a **Customer** (which belongs to a tenant)
- Assigns a specific **Role** for that user-customer relationship
- Allows a single user to have different roles in different customers across different tenants

## Architecture Diagram

```
┌─────────────────┐
│ ClientCustomer  │ (Tenant)
│ (Tenant A)      │
└────────┬────────┘
         │
         │ 1:N
         │
┌────────▼────────┐      ┌──────────────┐
│   Customer      │◄─────┤ UserCustomer │
│  (Tenant-       │ N:N  │    Role      │
│   Specific)     │      │  (Join)      │
└─────────────────┘      └───────┬──────┘
                                 │
                    ┌────────────┼────────────┐
                    │            │            │
             ┌──────▼──────┐ ┌──▼───┐  ┌────▼─────┐
             │    User     │ │ Role │  │ Customer │
             │  (Global)   │ │(Glob)│  │(Tenant-  │
             └─────────────┘ └──────┘  │Specific) │
                                       └──────────┘
```

## How It Works

### 1. Tenant Context
The `AppDbContext` accepts an optional `tenantId`:
```csharp
var context = new AppDbContext(options, tenantId);
```

When a `tenantId` is provided:
- Query filters automatically apply to all `IMultiTenant` entities
- Only data for that tenant is visible
- New tenant-specific entities are automatically tagged with the tenant ID

### 2. User Management Across Tenants
A user can work for multiple tenants:

**Scenario**: User John works for both "Company A" (Tenant 1) and "Company B" (Tenant 2)

```csharp
// User is created once (global)
var john = new User { Email = "john@example.com", ... };

// John works for Customer X in Tenant 1 as Admin
var userRole1 = new UserCustomerRole 
{ 
    UserId = john.Id, 
    CustomerId = customerX.Id,  // customerX belongs to Tenant 1
    RoleId = adminRole.Id 
};

// John also works for Customer Y in Tenant 2 as Agent
var userRole2 = new UserCustomerRole 
{ 
    UserId = john.Id, 
    CustomerId = customerY.Id,  // customerY belongs to Tenant 2
    RoleId = agentRole.Id 
};
```

### 3. Authorization
The `AuthorizationService` checks permissions based on:
- User ID
- Customer ID (which is tenant-specific)
- Required permission

```csharp
bool hasPermission = await authService.HasPermissionAsync(
    userId: johnId,
    customerId: customerX.Id,
    permissionGroupName: "Products",
    requiredPermission: PermissionFlags.CanWrite
);
```

## Implementation Guide

### Setting Up Multi-Tenancy

1. **Configure Tenant Service** (in your DI container):
```csharp
services.AddSingleton<ITenantService, TenantService>();
services.AddDbContext<AppDbContext>((serviceProvider, options) => 
{
    var tenantService = serviceProvider.GetRequiredService<ITenantService>();
    var tenantId = tenantService.GetCurrentTenantId();
    options.UseNpgsql(connectionString);
});
```

2. **Set Tenant Context** (e.g., from subdomain or user claims):
```csharp
// In middleware or controller
var tenantService = serviceProvider.GetRequiredService<ITenantService>();
tenantService.SetCurrentTenantId(extractedTenantId);
```

3. **Use AppDbContextFactory** (recommended):
```csharp
var factory = new AppDbContextFactory(options, tenantService);
var context = factory.CreateDbContext(); // Uses current tenant
// OR
var context = factory.CreateDbContext(specificTenantId);
```

### Creating Tenant-Specific Data

```csharp
// Set tenant context first
tenantService.SetCurrentTenantId(tenantId);

// Create customer (automatically tagged with tenant ID)
var customer = new Customer { Name = "ACME Corp" };
context.Customers.Add(customer);
await context.SaveChangesAsync(); // ClientCustomerId is set automatically
```

### Creating Cross-Tenant User Access

```csharp
// Users are global, create once
var user = new User { Email = "user@example.com", ... };
context.Users.Add(user);
await context.SaveChangesAsync();

// Link user to customers in different tenants
var userRole = new UserCustomerRole
{
    UserId = user.Id,
    CustomerId = customer.Id,  // Customer belongs to a tenant
    RoleId = role.Id
};
context.UserCustomerRoles.Add(userRole);
await context.SaveChangesAsync();
```

## Benefits

1. **Single User Identity**: Users maintain one identity across all tenants
2. **Centralized User Management**: No need to duplicate user accounts
3. **Flexible Access Control**: Users can have different roles in different tenants
4. **Data Isolation**: Customer data is automatically isolated by tenant
5. **Shared Resources**: Roles and permissions are defined once, used by all tenants

## Security Considerations

1. **Always validate tenant access**: Ensure users have permission to access the tenant they're requesting
2. **Audit logging**: Track which tenant context is used for each operation
3. **Token/Session management**: Store tenant context securely in user sessions or JWT claims
4. **Database migrations**: Test carefully when adding/removing tenant isolation from entities
