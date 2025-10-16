# Multi-Tenant Usage Examples

This document provides practical examples for using the multi-tenant features in GjammT.

## Table of Contents
- [Setup](#setup)
- [Basic Usage](#basic-usage)
- [Common Scenarios](#common-scenarios)
- [Best Practices](#best-practices)

## Setup

### 1. Register Services in DI Container

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSingleton<ITenantService, TenantService>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<AppDbContextFactory>();
builder.Services.AddScoped<AuthorizationService>();
```

### 2. Add Tenant Middleware (for web applications)

```csharp
// In Program.cs
app.UseMiddleware<TenantMiddleware>();
```

## Basic Usage

### Creating a New Tenant (ClientCustomer)

```csharp
public async Task<ClientCustomer> CreateTenant(string companyName, string subdomain)
{
    // Use context without tenant filtering for administrative operations
    using var context = new AppDbContext(options, tenantId: null);
    
    var tenant = new ClientCustomer
    {
        Id = Guid.NewGuid(),
        CompanyName = companyName,
        Subdomain = subdomain
    };
    
    context.Add(tenant);
    await context.SaveChangesAsync();
    
    return tenant;
}
```

### Creating a Customer (Tenant-Specific)

```csharp
public async Task<Customer> CreateCustomer(Guid tenantId, string customerName)
{
    // Set tenant context
    tenantService.SetCurrentTenantId(tenantId);
    
    using var context = new AppDbContext(options, tenantId);
    
    var customer = new Customer
    {
        Name = customerName,
        // ClientCustomerId will be set automatically by AppDbContext
    };
    
    context.Customers.Add(customer);
    await context.SaveChangesAsync();
    
    return customer;
}
```

### Creating a Global User

```csharp
public async Task<User> CreateUser(string email, string firstName, string lastName)
{
    // Users are global - no tenant context needed
    using var context = new AppDbContext(options, tenantId: null);
    
    var user = new User
    {
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        IsActive = true
    };
    
    context.Users.Add(user);
    await context.SaveChangesAsync();
    
    return user;
}
```

## Common Scenarios

### Scenario 1: User Works for Multiple Tenants

```csharp
public async Task AssignUserToCustomer(
    Guid userId, 
    Guid customerId, 
    Guid roleId)
{
    // UserCustomerRole is global - no tenant filtering
    using var context = new AppDbContext(options, tenantId: null);
    
    var userCustomerRole = new UserCustomerRole
    {
        UserId = userId,
        CustomerId = customerId,  // Customer is tenant-specific
        RoleId = roleId
    };
    
    context.UserCustomerRoles.Add(userCustomerRole);
    await context.SaveChangesAsync();
}

// Example: John works for two different customers in two different tenants
var john = await CreateUser("john@example.com", "John", "Doe");

// Tenant A - Company X
var tenantA = await CreateTenant("Company X", "companyx");
tenantService.SetCurrentTenantId(tenantA.Id);
var customerA = await CreateCustomer(tenantA.Id, "Customer A");
await AssignUserToCustomer(john.Id, customerA.Id, adminRoleId);

// Tenant B - Company Y
var tenantB = await CreateTenant("Company Y", "companyy");
tenantService.SetCurrentTenantId(tenantB.Id);
var customerB = await CreateCustomer(tenantB.Id, "Customer B");
await AssignUserToCustomer(john.Id, customerB.Id, agentRoleId);

// John now has access to both tenants with different roles
```

### Scenario 2: Querying User's Customers Across Tenants

```csharp
public async Task<List<Customer>> GetUserCustomers(Guid userId)
{
    // Query without tenant filter to get all customers
    using var context = new AppDbContext(options, tenantId: null);
    
    var customers = await context.UserCustomerRoles
        .Include(ucr => ucr.Customer)
        .ThenInclude(c => c.ClientCustomer)
        .Include(ucr => ucr.Role)
        .Where(ucr => ucr.UserId == userId)
        .Select(ucr => new
        {
            Customer = ucr.Customer,
            Tenant = ucr.Customer.ClientCustomer,
            Role = ucr.Role
        })
        .ToListAsync();
    
    return customers;
}
```

### Scenario 3: Checking Permissions in Specific Tenant Context

```csharp
public async Task<bool> CanUserEditProducts(
    Guid userId, 
    Guid customerId)
{
    var authService = new AuthorizationService(context);
    
    return await authService.HasPermissionAsync(
        userId,
        customerId,
        "Products",
        PermissionFlags.CanWrite
    );
}

// Usage in controller
[HttpPut("products/{id}")]
public async Task<IActionResult> UpdateProduct(Guid id, ProductDto productDto)
{
    var userId = GetCurrentUserId();
    var customerId = GetCurrentCustomerId();
    
    if (!await CanUserEditProducts(userId, customerId))
    {
        return Forbid();
    }
    
    // Update product...
}
```

### Scenario 4: Switching Tenant Context

```csharp
public class UserService
{
    private readonly ITenantService _tenantService;
    private readonly AppDbContextFactory _contextFactory;
    
    public async Task<List<Customer>> GetCustomersInTenant(Guid tenantId)
    {
        // Temporarily switch to specific tenant
        _tenantService.SetCurrentTenantId(tenantId);
        
        using var context = _contextFactory.CreateDbContext();
        
        // Only customers in this tenant will be returned
        return await context.Customers.ToListAsync();
    }
    
    public async Task<List<Customer>> GetAllCustomersUserCanAccess(Guid userId)
    {
        // Get user's customer relationships without tenant filter
        using var globalContext = new AppDbContext(options, tenantId: null);
        
        var userCustomers = await globalContext.UserCustomerRoles
            .Include(ucr => ucr.Customer)
            .ThenInclude(c => c.ClientCustomer)
            .Where(ucr => ucr.UserId == userId)
            .ToListAsync();
        
        return userCustomers.Select(uc => uc.Customer).ToList();
    }
}
```

## Best Practices

### 1. Always Validate Tenant Access

```csharp
public async Task<IActionResult> GetCustomer(Guid customerId)
{
    var userId = GetCurrentUserId();
    var tenantId = GetCurrentTenantId();
    
    // Verify user has access to this customer
    var hasAccess = await context.UserCustomerRoles
        .AnyAsync(ucr => 
            ucr.UserId == userId && 
            ucr.CustomerId == customerId &&
            ucr.Customer.ClientCustomerId == tenantId);
    
    if (!hasAccess)
    {
        return Forbid();
    }
    
    // Proceed with operation...
}
```

### 2. Use Factory Pattern

```csharp
// Recommended
public class CustomerService
{
    private readonly AppDbContextFactory _contextFactory;
    
    public async Task<Customer> GetCustomer(Guid id)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Customers.FindAsync(id);
    }
}

// Instead of
public class CustomerService
{
    public async Task<Customer> GetCustomer(Guid id)
    {
        using var context = new AppDbContext(options, someTenantId); // Less flexible
        return await context.Customers.FindAsync(id);
    }
}
```

### 3. Handle Multi-Tenant Queries Explicitly

```csharp
// When you need data across all tenants (e.g., admin dashboard)
public async Task<int> GetTotalCustomersAcrossAllTenants()
{
    // Explicitly use null tenant to bypass filters
    using var context = new AppDbContext(options, tenantId: null);
    return await context.Customers.CountAsync();
}

// When working within a tenant (normal operation)
public async Task<int> GetCustomersInCurrentTenant()
{
    // Uses current tenant context
    using var context = _contextFactory.CreateDbContext();
    return await context.Customers.CountAsync();
}
```

### 4. Audit Trail

```csharp
public class AuditService
{
    public async Task LogOperation(string operation, Guid userId, Guid? tenantId)
    {
        // Log which tenant context was used
        var auditLog = new AuditLog
        {
            Operation = operation,
            UserId = userId,
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow
        };
        
        // Save to audit log...
    }
}
```

### 5. Testing Multi-Tenant Features

```csharp
[Fact]
public async Task User_Can_Access_Customer_In_Different_Tenants()
{
    // Arrange
    var tenantA = CreateTestTenant("TenantA");
    var tenantB = CreateTestTenant("TenantB");
    var user = CreateTestUser();
    
    var customerA = CreateTestCustomer(tenantA.Id);
    var customerB = CreateTestCustomer(tenantB.Id);
    
    AssignUserToCustomer(user.Id, customerA.Id, roleId);
    AssignUserToCustomer(user.Id, customerB.Id, roleId);
    
    // Act & Assert - Tenant A context
    _tenantService.SetCurrentTenantId(tenantA.Id);
    using (var context = new AppDbContext(options, tenantA.Id))
    {
        var customers = await context.Customers.ToListAsync();
        Assert.Single(customers);
        Assert.Equal(customerA.Id, customers[0].Id);
    }
    
    // Act & Assert - Tenant B context
    _tenantService.SetCurrentTenantId(tenantB.Id);
    using (var context = new AppDbContext(options, tenantB.Id))
    {
        var customers = await context.Customers.ToListAsync();
        Assert.Single(customers);
        Assert.Equal(customerB.Id, customers[0].Id);
    }
}
```
