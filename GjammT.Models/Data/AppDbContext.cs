using System.Linq.Expressions;
using GjammT.Models.Base;
using GjammT.Models.CustomerRegister;
using Microsoft.EntityFrameworkCore;

namespace GjammT.Models.Data;

public class AppDbContext : DbContext
{
    private readonly Guid? _tenantId = Guid.NewGuid();

    public AppDbContext()
    {
    }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=postgres;User Id=joelmandell;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Call base first

        // 1. Define the composite primary key for the RolePermission join entity.
        // A role can have only one set of permissions per group.
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionGroupId });

        // 2. Configure the relationship between Role and the join entity
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.Permissions) // Points to the collection in the Role class
            .HasForeignKey(rp => rp.RoleId);

        // 3. Configure the relationship between PermissionGroup and the join entity
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.PermissionGroup)
            .WithMany() // A PermissionGroup can be in many RolePermissions, but we don't need a navigation property on PermissionGroup.
            .HasForeignKey(rp => rp.PermissionGroupId);
        
        // This generic loop applies tenancy rules.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IMultiTenant.ClientCustomerId));
                var tenantIdConstant = Expression.Constant(_tenantId);
                var body = Expression.Equal(property, tenantIdConstant);
                var lambda = Expression.Lambda(body, parameter);
                entityType.SetQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // This logic also only looks for `IMultiTenant` implementers.
        // New `Customer` entities will be ignored by this block and saved normally.
        foreach (var entry in ChangeTracker.Entries<IMultiTenant>().Where(e => e.State == EntityState.Added))
        {
            if (_tenantId.HasValue)
            {
                entry.Entity.ClientCustomerId = _tenantId.Value;
            }
            else
            {
                throw new InvalidOperationException("Cannot save a tenant-specific entity without a valid Tenant ID.");
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
    
    public DbSet<PermissionGroup> PermissionGroups { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserCustomerRole> UserCustomerRoles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }
}