using GjammT.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GjammT.Models.Data;

/// <summary>
/// Factory for creating AppDbContext instances with tenant awareness
/// </summary>
public class AppDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly ITenantService _tenantService;
    
    public AppDbContextFactory(DbContextOptions<AppDbContext> options, ITenantService tenantService)
    {
        _options = options;
        _tenantService = tenantService;
    }
    
    /// <summary>
    /// Creates a new AppDbContext with the current tenant ID
    /// </summary>
    public AppDbContext CreateDbContext()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        return new AppDbContext(_options, tenantId);
    }
    
    /// <summary>
    /// Creates a new AppDbContext with a specific tenant ID
    /// </summary>
    public AppDbContext CreateDbContext(Guid tenantId)
    {
        return new AppDbContext(_options, tenantId);
    }
}
