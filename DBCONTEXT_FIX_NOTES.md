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
3. **Keeping** only the 3-parameter constructor: `AppDbContext(DbContextOptions<AppDbContext> options, Guid? tenantId)`

This single constructor:
- Is unambiguous for the DI container
- Works with `IDbContextFactory<AppDbContext>` - EF Core's factory will pass `null` for the `tenantId` parameter
- Supports explicit tenant-aware instantiation when needed
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
- Passes `null` for non-service parameters like `Guid? tenantId` (the default for nullable types)
- Creates `AppDbContext` instances with tenant filtering disabled (null tenant)

For tenant-aware operations, code should use the custom `AppDbContextFactory` which properly handles tenant context.
