using System.Linq.Expressions;
using GjammT.Models.Base;
using GjammT.Models.CustomerRegister;
using Microsoft.EntityFrameworkCore;

namespace GjammT.Models.Data;

public class AppDbContext : DbContext
{
    private readonly Guid? _tenantId;
    
    // Constructor with explicit default for tenantId to work with IDbContextFactory
    public AppDbContext(DbContextOptions<AppDbContext> options, Guid? tenantId = null) : base(options)
    {
        _tenantId = tenantId;
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
        
        // 3a. Configure UserPermission relationships
        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.User)
            .WithMany() // A User can have many UserPermissions
            .HasForeignKey(up => up.UserId);

        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.PermissionGroup)
            .WithMany() // A PermissionGroup can be in many UserPermissions
            .HasForeignKey(up => up.PermissionGroupId);

        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.Customer)
            .WithMany() // A Customer can have many UserPermissions
            .HasForeignKey(up => up.CustomerId)
            .IsRequired(false); // Optional relationship

        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.ClientCustomer)
            .WithMany() // A ClientCustomer can have many UserPermissions
            .HasForeignKey(up => up.ClientCustomerId)
            .IsRequired(false); // Optional relationship
        
        // 4. Configure the many-to-many relationship between Customer and ClientCustomer (tenant)
        // A customer can exist in multiple tenants, and a tenant can have multiple customers
        // Define composite primary key to prevent duplicate customer-tenant relationships
        modelBuilder.Entity<CustomerTenant>()
            .HasKey(ct => new { ct.CustomerId, ct.ClientCustomerId });
        
        modelBuilder.Entity<CustomerTenant>()
            .HasOne(ct => ct.Customer)
            .WithMany(c => c.Tenants)
            .HasForeignKey(ct => ct.CustomerId);

        modelBuilder.Entity<CustomerTenant>()
            .HasOne(ct => ct.ClientCustomer)
            .WithMany(cc => cc.Customers)
            .HasForeignKey(ct => ct.ClientCustomerId);
        // 4. Configure the relationship between Customer and ClientCustomer for multi-tenancy
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.ClientCustomer)
            .WithMany() // A ClientCustomer can have many Customers
            .HasForeignKey(c => c.ClientCustomerId);
        
        // This generic loop applies tenancy rules.
        // Only apply query filter if a tenant ID is provided
        if (_tenantId.HasValue)
        {
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
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // This logic only looks for `IMultiTenant` implementers.
        // Global entities like User, Role, and PermissionGroup will be ignored by this block.
        foreach (var entry in ChangeTracker.Entries<IMultiTenant>().Where(e => e.State == EntityState.Added))
        {
            if (_tenantId.HasValue)
            {
                entry.Entity.ClientCustomerId = _tenantId.Value;
            }
            else
            {
                // Allow saving without tenant ID if explicitly set (for migration scenarios)
                if (entry.Entity.ClientCustomerId == Guid.Empty)
                {
                    throw new InvalidOperationException($"Cannot save a tenant-specific entity ({entry.Entity.GetType().Name}) without a valid Tenant ID.");
                }
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
    
    public DbSet<PermissionGroup> PermissionGroups { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserCustomerRole> UserCustomerRoles { get; set; }
    public DbSet<CustomerTenant> CustomerTenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<ClientCustomer> ClientCustomers { get; set; }
}