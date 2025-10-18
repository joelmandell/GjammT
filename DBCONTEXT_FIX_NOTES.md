# AppDbContext Constructor Fix

## Problem
The original `AppDbContext` had three constructors:
1. Parameterless constructor
2. Constructor with `DbContextOptions<AppDbContext>`
3. Constructor with `DbContextOptions<AppDbContext>` and `Guid? tenantId`

This caused an `InvalidOperationException` when the DI container tried to resolve `AppDbContext` because constructors 2 and 3 were ambiguous - both could accept the same arguments since the `tenantId` parameter is nullable.

## Solution
The fix removes the ambiguity by:
1. **Removing** the parameterless constructor (used only in legacy code)
2. **Removing** the 2-parameter constructor with only `DbContextOptions`
3. **Keeping** only the constructor with both parameters and an explicit default value: `AppDbContext(DbContextOptions<AppDbContext> options, Guid? tenantId = null)`

This single constructor:
- Is unambiguous for the DI container (only one constructor)
- Has an explicit default value for `tenantId`, allowing `IDbContextFactory<AppDbContext>` to create instances with just `DbContextOptions`
- Works with `IDbContextFactory<AppDbContext>` - EF Core's factory can call it with just the options parameter
- Supports explicit tenant-aware instantiation when needed: `new AppDbContext(options, specificTenantId)`
- Works with the custom `AppDbContextFactory` for tenant-specific contexts

## Changes Made

### 1. GjammT.Models/Data/AppDbContext.cs
- Removed parameterless constructor
- Removed 2-parameter constructor
- Kept only the constructor with explicit `tenantId` parameter

### 2. GjammT.Booking/Plugins.cs
- Updated to inject `DbContextOptions<AppDbContext>` via constructor
- Changed all `new AppDbContext()` calls to `new AppDbContext(dbOptions, tenantId: null)`

### 3. GjammT.Models/Data/AppDbContextDesignTimeFactory.cs (NEW)
- Created `IDesignTimeDbContextFactory<AppDbContext>` for EF Core migrations
- Ensures `dotnet ef` commands work properly without the parameterless constructor

## Testing
To verify the fix works:
1. Access the gjadmin interface at `/gjadmin`
2. Navigate to any admin page (Users, Roles, Permissions, etc.)
3. The pages should load without `InvalidOperationException`
4. The `IDbContextFactory<AppDbContext>` should successfully create contexts

## Technical Details
The `AddDbContextFactory<AppDbContext>()` registration in Program.cs creates a factory that:
- Resolves `DbContextOptions<AppDbContext>` from DI (configured by `AddDbContextFactory`)
- Can omit the `tenantId` parameter since it has a default value of `null`
- Creates `AppDbContext` instances with tenant filtering disabled (null tenant)

The constructor signature `AppDbContext(DbContextOptions<AppDbContext> options, Guid? tenantId = null)` allows:
1. Factory creation: `factory.CreateDbContext()` → calls constructor with just options
2. Explicit tenant: `new AppDbContext(options, tenantId)` → calls constructor with specific tenant
3. No ambiguity: Only one constructor exists

For tenant-aware operations, code should use the custom `AppDbContextFactory` which properly handles tenant context.
