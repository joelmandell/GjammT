# Agent Instructions for GjammT Development

**Creation Date**: 2025-10-17 20:33:55 UTC  
**Target Branch**: auth (or as specified by user)  
**Last Updated**: 2025-10-17 20:33:55 UTC

## Purpose
This document provides guidelines for AI agents working on the GjammT booking system. It should be used to maintain consistency and understand the architectural decisions made during development.

## Commit History Reference
When working on this branch, you can trace changes by checking the commit log:
```bash
git log --oneline --graph --decorate
```

To see changes specific to the admin interface:
```bash
git log --oneline --grep="admin\|Admin" --all
```

## Architecture Overview

### Multi-Tenant System
GjammT uses a sophisticated multi-tenant architecture with shared authorization:
- **Tenants (ClientCustomer)**: Represent companies/organizations
- **Global Entities**: Users, Roles, PermissionGroups are shared across all tenants
- **Tenant-Specific Entities**: Customers, Addresses are isolated per tenant
- **Cross-Tenant Bridge**: UserCustomerRole links users to customers with roles

### Key Concepts
1. **IMultiTenant Interface**: Entities implementing this are automatically filtered by tenant
2. **AppDbContext**: Accepts optional tenantId for data isolation
3. **UserCustomerRole**: Allows users to work across multiple tenants with different roles

## Admin Interface (/gjadmin)

### Route Structure
All admin pages are under the `/gjadmin` base route:
- `/gjadmin` - Admin dashboard/home
- `/gjadmin/tenants` - Tenant (ClientCustomer) management
- `/gjadmin/companies` - Company management (alias for tenants)
- `/gjadmin/users` - Global user management
- `/gjadmin/customers` - Customer management (tenant-specific)
- `/gjadmin/roles` - Role management
- `/gjadmin/permissions` - Permission group management

### Component Organization
```
GjammT/Components/
├── Pages/
│   └── Admin/
│       ├── Index.razor                    # Admin dashboard
│       ├── TenantManagement.razor         # Tenant CRUD
│       ├── UserManagement.razor           # User CRUD
│       ├── CustomerManagement.razor       # Customer CRUD
│       ├── RoleManagement.razor           # Role CRUD
│       └── PermissionManagement.razor     # Permission CRUD
└── Layout/
    └── AdminLayout.razor                  # Layout for admin pages
```

### Syncfusion Components
The project uses Syncfusion Blazor components (v29.2.4):
- **SfGrid**: For data tables with CRUD operations
- **SfDialog**: For modal dialogs
- **SfButton**: For buttons
- **SfTextBox**: For input fields
- **SfDropDownList**: For dropdowns

### Permission System

#### Current Implementation
The system uses a flags-based permission model:
- **PermissionGroup**: Named resource/feature (e.g., "Products", "Invoices")
- **PermissionFlags**: Bitwise flags (CanRead=1, CanCreate=2, CanWrite=4, CanDelete=8)
- **RolePermission**: Links roles to permission groups with allowed actions

#### Future Booking Permissions
For the booking system, the permission model needs to support:
- **Object-based permissions**: Different users can book different object types
- **Rate limiting**: Time-based booking restrictions (per day/month)
- **Consecutive booking limits**: Maximum bookings in a row on same day
- **Dynamic configuration**: Rules should be configurable per object type

This will require extending the permission model to include:
1. Object type identifiers
2. Rate limit configuration (count, time period)
3. Consecutive booking rules
4. Custom validation logic per object type

## Development Guidelines

### 1. Code Style
- Use nullable reference types (`#nullable enable`)
- Follow existing naming conventions
- Use `required` keyword for mandatory properties in .NET 9+
- Prefer `ICollection<T>` for navigation properties

### 2. Entity Creation
When creating new entities:
- Inherit from `BaseEntity` for common properties (Id, CreatedAt, UpdatedAt)
- Implement `IMultiTenant` for tenant-specific entities
- Add navigation properties for relationships
- Update `AppDbContext.OnModelCreating()` for custom configurations

### 3. Data Access
- Always use `AppDbContext` with appropriate tenantId when accessing tenant-specific data
- Use `Include()` for eager loading related entities
- Implement query filters carefully to avoid N+1 problems

### 4. Blazor Components
- Use `@page` directive for routable components
- Implement `IDisposable` when needed
- Use `@inject` for dependency injection
- Follow Blazor lifecycle methods (OnInitialized, OnParametersSet, etc.)

### 5. Authentication & Authorization
- Use `[Authorize]` attribute for protected pages
- Check user permissions using the permission system
- Handle tenant context in authentication state

### 6. Testing Strategy
- The .NET 10.0 SDK is not available in the standard environment
- Components should be designed to work with both .NET 9.0 and .NET 10.0
- Manual testing is required until SDK compatibility is resolved

## Database Schema

### Key Tables
- **ClientCustomers**: Tenants
- **Users**: Global users
- **Customers**: Tenant-specific customers
- **Roles**: Global roles
- **PermissionGroups**: Named permission resources
- **RolePermissions**: Role-to-permission mappings with flags
- **UserCustomerRoles**: User-Customer-Role associations
- **CustomerTenants**: Many-to-many relationship between customers and tenants

### Relationships
```
ClientCustomer (1) -----> (N) Customer
User (N) <----> (N) Customer (through UserCustomerRole)
Role (N) <----> (N) PermissionGroup (through RolePermission)
UserCustomerRole (N) ----> (1) Role
```

## Common Operations

### Creating a New Tenant
```csharp
var tenant = new ClientCustomer 
{ 
    CompanyName = "New Company",
    Subdomain = "newco"
};
await context.ClientCustomers.AddAsync(tenant);
await context.SaveChangesAsync();
```

### Creating a User with Role
```csharp
var user = new User 
{ 
    Email = "user@example.com",
    FirstName = "John",
    LastName = "Doe",
    IsActive = true
};
await context.Users.AddAsync(user);

var userCustomerRole = new UserCustomerRole
{
    UserId = user.Id,
    CustomerId = customerId,
    RoleId = roleId
};
await context.UserCustomerRoles.AddAsync(userCustomerRole);
await context.SaveChangesAsync();
```

### Checking Permissions
```csharp
var rolePermissions = await context.RolePermissions
    .Include(rp => rp.PermissionGroup)
    .Where(rp => rp.RoleId == roleId)
    .ToListAsync();

foreach (var rp in rolePermissions)
{
    if ((rp.AllowedActions & (int)PermissionFlags.CanRead) != 0)
    {
        // User can read this resource
    }
}
```

## Future Considerations

### Booking System Requirements
When implementing the booking system, consider:
1. **Object Types**: Create a new entity for bookable objects
2. **Booking Rules**: Implement rule engine for complex booking restrictions
3. **Calendar Integration**: Use Syncfusion Scheduler component
4. **Availability Tracking**: Real-time availability calculations
5. **Conflict Detection**: Prevent double-bookings
6. **Permission Extensions**: Add booking-specific permissions

### Scalability
- Implement caching for frequently accessed data (roles, permissions)
- Consider read replicas for tenant-specific queries
- Monitor query performance with Application Insights
- Implement background jobs for heavy operations

### Security
- Implement row-level security in addition to EF filters
- Audit logging for admin operations
- Rate limiting on API endpoints
- Input validation and sanitization

## Troubleshooting

### Common Issues
1. **Tenant filter not applying**: Ensure tenantId is passed to AppDbContext constructor
2. **Foreign key violations**: Check that related entities exist before creating relationships
3. **Permission checks failing**: Verify role permissions are configured correctly
4. **Migration errors**: Use separate migration contexts for schema changes

## Resources
- [Multi-Tenant Architecture](MULTI_TENANT_ARCHITECTURE.md)
- [Multi-Tenant Quick Reference](MULTI_TENANT_QUICK_REFERENCE.md)
- [Implementation Guide](IMPLEMENTATION_GUIDE.md)
- [Architecture Diagrams](ARCHITECTURE_DIAGRAMS.md)

## Version History
- **2025-10-17**: Initial creation with admin interface guidelines
