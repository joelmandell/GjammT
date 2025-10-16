using GjammT.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace GjammT.Models.Middleware;

/// <summary>
/// Middleware to automatically set the tenant context based on subdomain or custom header
/// Example usage: app.UseMiddleware<TenantMiddleware>();
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Try to extract tenant ID from custom header first
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader) 
            && Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            tenantService.SetCurrentTenantId(tenantId);
        }
        // Or extract from subdomain (e.g., tenant1.example.com)
        else if (TryGetTenantFromSubdomain(context.Request.Host.Host, out tenantId))
        {
            tenantService.SetCurrentTenantId(tenantId);
        }
        // Or extract from user claims if authenticated
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenantId");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out tenantId))
            {
                tenantService.SetCurrentTenantId(tenantId);
            }
        }
        
        await _next(context);
    }
    
    private bool TryGetTenantFromSubdomain(string host, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        
        // Example: Extract subdomain from "tenant1.example.com"
        // You would need to map subdomain to tenant ID using a lookup service
        var parts = host.Split('.');
        if (parts.Length >= 3)
        {
            var subdomain = parts[0];
            // TODO: Implement subdomain to tenant ID mapping
            // For now, return false
            return false;
        }
        
        return false;
    }
}
