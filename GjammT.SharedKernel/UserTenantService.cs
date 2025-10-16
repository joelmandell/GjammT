using GjammT.Models.Base;
using GjammT.Models.CustomerRegister;
using GjammT.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace GjammT.SharedKernel;

/// <summary>
/// Service for managing user-customer-role relationships across tenants
/// </summary>
public class UserTenantService
{
    private readonly AppDbContext _context;

    public UserTenantService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Assigns a user to a customer with a specific role
    /// </summary>
    public async Task<UserCustomerRole> AssignUserToCustomerAsync(Guid userId, Guid customerId, Guid roleId)
    {
        // Check if assignment already exists
        var existing = await _context.UserCustomerRoles
            .FirstOrDefaultAsync(ucr => 
                ucr.UserId == userId && 
                ucr.CustomerId == customerId);

        if (existing != null)
        {
            // Update role if different
            if (existing.RoleId != roleId)
            {
                existing.RoleId = roleId;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        // Create new assignment
        var userCustomerRole = new UserCustomerRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CustomerId = customerId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserCustomerRoles.Add(userCustomerRole);
        await _context.SaveChangesAsync();

        return userCustomerRole;
    }

    /// <summary>
    /// Removes a user's access to a customer
    /// </summary>
    public async Task<bool> RemoveUserFromCustomerAsync(Guid userId, Guid customerId)
    {
        var userCustomerRole = await _context.UserCustomerRoles
            .FirstOrDefaultAsync(ucr => 
                ucr.UserId == userId && 
                ucr.CustomerId == customerId);

        if (userCustomerRole == null)
        {
            return false;
        }

        _context.UserCustomerRoles.Remove(userCustomerRole);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Gets all tenants (ClientCustomers) that a user has access to
    /// </summary>
    public async Task<List<ClientCustomer>> GetUserTenantsAsync(Guid userId)
    {
        return await _context.UserCustomerRoles
            .AsNoTracking()
            .Include(ucr => ucr.Customer)
            .ThenInclude(c => c.ClientCustomer)
            .Where(ucr => ucr.UserId == userId)
            .Select(ucr => ucr.Customer.ClientCustomer)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Gets all customers a user has access to within a specific tenant
    /// </summary>
    public async Task<List<Customer>> GetUserCustomersInTenantAsync(Guid userId, Guid tenantId)
    {
        return await _context.UserCustomerRoles
            .AsNoTracking()
            .Include(ucr => ucr.Customer)
            .Where(ucr => 
                ucr.UserId == userId && 
                ucr.Customer.ClientCustomerId == tenantId)
            .Select(ucr => ucr.Customer)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a user's role for a specific customer
    /// </summary>
    public async Task<Role?> GetUserRoleForCustomerAsync(Guid userId, Guid customerId)
    {
        var userCustomerRole = await _context.UserCustomerRoles
            .AsNoTracking()
            .Include(ucr => ucr.Role)
            .FirstOrDefaultAsync(ucr => 
                ucr.UserId == userId && 
                ucr.CustomerId == customerId);

        return userCustomerRole?.Role;
    }

    /// <summary>
    /// Checks if a user has access to a specific tenant
    /// </summary>
    public async Task<bool> UserHasAccessToTenantAsync(Guid userId, Guid tenantId)
    {
        return await _context.UserCustomerRoles
            .AsNoTracking()
            .Include(ucr => ucr.Customer)
            .AnyAsync(ucr => 
                ucr.UserId == userId && 
                ucr.Customer.ClientCustomerId == tenantId);
    }
}
