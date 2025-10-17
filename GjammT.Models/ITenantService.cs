namespace GjammT.Models;

/// <summary>
/// Service to manage the current tenant context in a multi-tenant application
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the current tenant ID from the application context
    /// </summary>
    Guid? GetCurrentTenantId();
    
    /// <summary>
    /// Sets the current tenant ID in the application context
    /// </summary>
    void SetCurrentTenantId(Guid tenantId);
}
