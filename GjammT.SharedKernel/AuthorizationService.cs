using GjammT.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace GjammT.SharedKernel;

public class AuthorizationService
{
    private readonly AppDbContext _context;

    public AuthorizationService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Checks if a user has a specific permission for a customer.
    /// Checks user-level permissions first, then falls back to role-based permissions.
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, Guid customerId, string permissionGroupName, PermissionFlags requiredPermission)
    {
        // 1. Check for user-level permissions first (highest priority)
        var userPermission = await _context.UserPermissions
            .AsNoTracking()
            .Include(up => up.PermissionGroup)
            .FirstOrDefaultAsync(up => 
                up.UserId == userId && 
                up.CustomerId == customerId && 
                up.PermissionGroup.Name == permissionGroupName);

        if (userPermission != null)
        {
            var userAllowed = (PermissionFlags)userPermission.AllowedActions;
            return userAllowed.HasFlag(requiredPermission);
        }

        // 2. Fall back to role-based permissions
        var userRole = await _context.UserCustomerRoles
            .AsNoTracking()
            .Include(ucr => ucr.Role)
            .ThenInclude(r => r.Permissions)
            .ThenInclude(p => p.PermissionGroup)
            .FirstOrDefaultAsync(ucr => ucr.UserId == userId && ucr.CustomerId == customerId);

        if (userRole == null) return false;

        // 3. Find the specific permission set for the requested group (e.g., "Products")
        var rolePermission = userRole.Role.Permissions
            .FirstOrDefault(p => p.PermissionGroup.Name == permissionGroupName);
        
        if (rolePermission == null) return false;

        // 4. Perform the bitwise check
        var allowed = (PermissionFlags)rolePermission.AllowedActions;
        return allowed.HasFlag(requiredPermission);
    }

    /// <summary>
    /// Checks if a user has a specific permission at the tenant level.
    /// Useful for admin-level permissions that apply to all customers in a tenant.
    /// </summary>
    public async Task<bool> HasTenantPermissionAsync(Guid userId, Guid clientCustomerId, string permissionGroupName, PermissionFlags requiredPermission)
    {
        // Check for user-level tenant permissions
        var userPermission = await _context.UserPermissions
            .AsNoTracking()
            .Include(up => up.PermissionGroup)
            .FirstOrDefaultAsync(up => 
                up.UserId == userId && 
                up.CustomerId == null && 
                up.ClientCustomerId == clientCustomerId && 
                up.PermissionGroup.Name == permissionGroupName);

        if (userPermission != null)
        {
            var userAllowed = (PermissionFlags)userPermission.AllowedActions;
            return userAllowed.HasFlag(requiredPermission);
        }

        return false;
    }
    
    /// <summary>
    /// Checks if a user has access to a specific customer (in any role)
    /// </summary>
    public async Task<bool> UserHasAccessToCustomerAsync(Guid userId, Guid customerId)
    {
        return await _context.UserCustomerRoles
            .AsNoTracking()
            .AnyAsync(ucr => ucr.UserId == userId && ucr.CustomerId == customerId);
    }
    
    /// <summary>
    /// Gets all customers a user has access to across all tenants
    /// </summary>
    public async Task<List<Guid>> GetUserCustomerIdsAsync(Guid userId)
    {
        return await _context.UserCustomerRoles
            .AsNoTracking()
            .Where(ucr => ucr.UserId == userId)
            .Select(ucr => ucr.CustomerId)
            .Distinct()
            .ToListAsync();
    }
}