using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GjammT.Models.Data;

/// <summary>
/// Design-time factory for EF Core migrations and tooling.
/// This is used by the dotnet ef commands.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Use the default connection string for migrations
        // This connection string is also hardcoded in OnConfiguring as a fallback
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=postgres;User Id=joelmandell;");
        
        // For migrations, we don't need a specific tenant ID
        return new AppDbContext(optionsBuilder.Options, tenantId: null);
    }
}
