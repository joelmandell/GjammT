using GjammT.Models;
using GjammT.Models.Base;
using GjammT.Models.CustomerRegister;
using GjammT.Models.Data;
using GjammT.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GjammT.Examples;

/// <summary>
/// Example service demonstrating multi-tenant usage patterns
/// </summary>
public class MultiTenantExampleService
{
    private readonly ITenantService _tenantService;
    private readonly AppDbContextFactory _contextFactory;
    private readonly DbContextOptions<AppDbContext> _options;

    public MultiTenantExampleService(
        ITenantService tenantService,
        AppDbContextFactory contextFactory,
        DbContextOptions<AppDbContext> options)
    {
        _tenantService = tenantService;
        _contextFactory = contextFactory;
        _options = options;
    }

    /// <summary>
    /// Example 1: Creating a new tenant
    /// </summary>
    public async Task<ClientCustomer> CreateTenantExample(string companyName, string subdomain)
    {
        // For administrative operations, use context without tenant filtering
        using var context = new AppDbContext(_options, tenantId: null);
        
        var tenant = new ClientCustomer
        {
            Id = Guid.NewGuid(),
            CompanyName = companyName,
            Subdomain = subdomain
        };
        
        context.ClientCustomers.Add(tenant);
        await context.SaveChangesAsync();
        
        return tenant;
    }

    /// <summary>
    /// Example 2: Creating a customer within a tenant
    /// </summary>
    public async Task<Customer> CreateCustomerInTenantExample(Guid tenantId, string customerName)
    {
        // Set the tenant context
        _tenantService.SetCurrentTenantId(tenantId);
        
        // Create context with tenant filtering
        using var context = _contextFactory.CreateDbContext();
        
        var customer = new Customer
        {
            Name = customerName,
            LegacyCode = $"CUST-{Guid.NewGuid().ToString().Substring(0, 8)}",
            // ClientCustomerId will be set automatically by AppDbContext
        };
        
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        
        return customer;
    }

    /// <summary>
    /// Example 3: Creating a global user (not tenant-specific)
    /// </summary>
    public async Task<User> CreateGlobalUserExample(string email, string password, string firstName, string lastName)
    {
        // Users are global - no tenant filtering needed
        using var context = new AppDbContext(_options, tenantId: null);
        var userService = new UserService(context);
        
        var user = await userService.CreateUserAsync(email, password, firstName, lastName);
        
        return user;
    }

    /// <summary>
    /// Example 4: Assigning a user to work for a customer (possibly in a different tenant)
    /// </summary>
    public async Task AssignUserToCustomerExample(Guid userId, Guid customerId, Guid roleId)
    {
        // UserCustomerRole is global - no tenant filtering
        using var context = new AppDbContext(_options, tenantId: null);
        
        var userCustomerRole = new UserCustomerRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CustomerId = customerId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        context.UserCustomerRoles.Add(userCustomerRole);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Example 5: User working across multiple tenants
    /// </summary>
    public async Task<Dictionary<string, List<Customer>>> GetUserWorkEnvironmentsExample(Guid userId)
    {
        // Get all customers the user has access to, across all tenants
        using var context = new AppDbContext(_options, tenantId: null);
        
        var userEnvironments = await context.UserCustomerRoles
            .Include(ucr => ucr.Customer)
            .ThenInclude(c => c.ClientCustomer)
            .Include(ucr => ucr.Role)
            .Where(ucr => ucr.UserId == userId)
            .Select(ucr => new
            {
                TenantName = ucr.Customer.ClientCustomer.CompanyName,
                Customer = ucr.Customer,
                Role = ucr.Role
            })
            .ToListAsync();
        
        // Group by tenant
        var result = userEnvironments
            .GroupBy(x => x.TenantName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Customer).ToList()
            );
        
        return result;
    }

    /// <summary>
    /// Example 6: Querying customers in current tenant context
    /// </summary>
    public async Task<List<Customer>> GetCustomersInCurrentTenantExample()
    {
        // Uses current tenant context from TenantService
        using var context = _contextFactory.CreateDbContext();
        
        // Only customers in the current tenant will be returned
        return await context.Customers.ToListAsync();
    }

    /// <summary>
    /// Example 7: Checking user permissions
    /// </summary>
    public async Task<bool> CheckUserPermissionsExample(Guid userId, Guid customerId)
    {
        using var context = new AppDbContext(_options, tenantId: null);
        var authService = new AuthorizationService(context);
        
        // Check if user can write to products for this customer
        var canWrite = await authService.HasPermissionAsync(
            userId,
            customerId,
            "Products",
            PermissionFlags.CanWrite
        );
        
        return canWrite;
    }

    /// <summary>
    /// Example 8: Complete workflow - Setting up a user to work in multiple tenants
    /// </summary>
    public async Task CompleteMultiTenantWorkflowExample()
    {
        // Step 1: Create two tenants
        var tenantA = await CreateTenantExample("Company A", "companya");
        var tenantB = await CreateTenantExample("Company B", "companyb");
        
        // Step 2: Create a user (global)
        var user = await CreateGlobalUserExample("john@example.com", "SecurePassword123!", "John", "Doe");
        
        // Step 3: Create a role (would normally be done once during setup)
        Guid adminRoleId;
        using (var context = new AppDbContext(_options, tenantId: null))
        {
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Description = "Administrator with full access"
            };
            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();
            adminRoleId = adminRole.Id;
        }
        
        // Step 4: Create customers in both tenants
        var customerA = await CreateCustomerInTenantExample(tenantA.Id, "Customer A");
        var customerB = await CreateCustomerInTenantExample(tenantB.Id, "Customer B");
        
        // Step 5: Assign user to both customers
        await AssignUserToCustomerExample(user.Id, customerA.Id, adminRoleId);
        await AssignUserToCustomerExample(user.Id, customerB.Id, adminRoleId);
        
        // Step 6: Verify user can work in both tenants
        var environments = await GetUserWorkEnvironmentsExample(user.Id);
        
        // Result: User John can now work with customers in both Company A and Company B
        Console.WriteLine($"User {user.Email} has access to {environments.Count} tenants");
        foreach (var env in environments)
        {
            Console.WriteLine($"  - {env.Key}: {env.Value.Count} customer(s)");
        }
    }
}
