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

    public async Task<bool> HasPermissionAsync(Guid userId, Guid customerId, string permissionGroupName, PermissionFlags requiredPermission)
    {
        // 1. Find the user's role for the specific customer
        var userRole = await _context.UserCustomerRoles
            .AsNoTracking()
            .Include(ucr => ucr.Role)
            .ThenInclude(r => r.Permissions)
            .ThenInclude(p => p.PermissionGroup)
            .FirstOrDefaultAsync(ucr => ucr.UserId == userId && ucr.CustomerId == customerId);

        if (userRole == null) return false;

        // 2. Find the specific permission set for the requested group (e.g., "Products")
        var rolePermission = userRole.Role.Permissions
            .FirstOrDefault(p => p.PermissionGroup.Name == permissionGroupName);
        
        if (rolePermission == null) return false;

        // 3. Perform the bitwise check
        // This is the magic: (user's flags & required flag) == required flag
        var allowed = (PermissionFlags)rolePermission.AllowedActions;
        return allowed.HasFlag(requiredPermission);
    }
}