namespace GjammT.SharedKernel;

/// <summary>
/// Simple implementation of ITenantService using AsyncLocal for thread-safe tenant context
/// In a real application, this might use HttpContext, claims, or other mechanisms
/// </summary>
public class TenantService : ITenantService
{
    private static readonly AsyncLocal<Guid?> _tenantId = new AsyncLocal<Guid?>();
    
    public Guid? GetCurrentTenantId()
    {
        return _tenantId.Value;
    }
    
    public void SetCurrentTenantId(Guid tenantId)
    {
        _tenantId.Value = tenantId;
    }
}
