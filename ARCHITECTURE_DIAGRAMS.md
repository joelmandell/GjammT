# Multi-Tenant Architecture Diagram

## Overview Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          GjammT Multi-Tenant System                      │
└─────────────────────────────────────────────────────────────────────────┘

                            ┌──────────────┐
                            │ ASP.NET Core │
                            │ Application  │
                            └──────┬───────┘
                                   │
                    ┌──────────────┼──────────────┐
                    │              │              │
            ┌───────▼────────┐ ┌──▼──────────┐ ┌▼──────────────┐
            │ TenantMiddleware│ │Controllers  │ │Authentication│
            │                 │ │             │ │              │
            │ Sets tenant from│ │             │ │              │
            │ - Subdomain     │ │             │ │              │
            │ - Header        │ │             │ │              │
            │ - Claims        │ │             │ │              │
            └───────┬─────────┘ └──┬──────────┘ └──────────────┘
                    │              │
                    │  ┌───────────▼────────────┐
                    │  │   ITenantService       │
                    │  │   (Current Tenant ID)  │
                    │  └───────────┬────────────┘
                    │              │
                    └──────────────┼────────────────────┐
                                   │                    │
                        ┌──────────▼──────────┐   ┌────▼──────────┐
                        │ AppDbContextFactory │   │ Services      │
                        │                     │   │ - UserTenant  │
                        │ Creates contexts    │   │ - Authorization│
                        │ with tenant filter  │   └────┬──────────┘
                        └──────────┬──────────┘        │
                                   │                   │
                        ┌──────────▼───────────────────▼─────┐
                        │         AppDbContext               │
                        │   (Tenant-Aware Database Context)  │
                        │                                    │
                        │  - Filters IMultiTenant entities   │
                        │  - Auto-sets ClientCustomerId      │
                        └──────────┬─────────────────────────┘
                                   │
                        ┌──────────▼─────────────────────────┐
                        │     PostgreSQL Database            │
                        │                                    │
                        │  ┌──────────────────────────────┐  │
                        │  │  Global Entities             │  │
                        │  │  - Users                     │  │
                        │  │  - Roles                     │  │
                        │  │  - PermissionGroups          │  │
                        │  │  - UserCustomerRoles         │  │
                        │  └──────────────────────────────┘  │
                        │                                    │
                        │  ┌──────────────────────────────┐  │
                        │  │  Tenant-Specific Entities    │  │
                        │  │  - Customers                 │  │
                        │  │  - Addresses                 │  │
                        │  │  (Filtered by ClientCustomer)│  │
                        │  └──────────────────────────────┘  │
                        └────────────────────────────────────┘
```

## Entity Relationship Diagram

```
┌─────────────────────┐
│  ClientCustomer     │
│  (Tenant)           │
│  ─────────────      │
│  Id                 │
│  CompanyName        │
│  Subdomain          │
└──────────┬──────────┘
           │
           │ 1:N
           │
┌──────────▼──────────┐         ┌──────────────────┐
│  Customer           │         │  UserCustomerRole│
│  ─────────────      │◄────────┤  (Join Table)    │
│  Id                 │   N:N   │  ────────────    │
│  Name               │         │  UserId          │
│  ClientCustomerId   │         │  CustomerId      │
│  ...                │         │  RoleId          │
└─────────────────────┘         └────────┬─────────┘
                                         │
                           ┌─────────────┼─────────────┐
                           │             │             │
                    ┌──────▼───────┐ ┌──▼───┐  ┌──────▼─────────┐
                    │     User     │ │ Role │  │   Customer     │
                    │  (Global)    │ │(Glob)│  │(Tenant-Specific)│
                    │  ──────────  │ │──────│  │  ────────────  │
                    │  Id          │ │ Id   │  │  Id            │
                    │  Email       │ │ Name │  │  Name          │
                    │  FirstName   │ │ Desc │  │ClientCustomerId│
                    │  ...         │ └──┬───┘  └────────────────┘
                    └──────────────┘    │
                                        │
                                        │ 1:N
                                        │
                                 ┌──────▼──────────┐
                                 │ RolePermission  │
                                 │ ─────────────   │
                                 │ RoleId          │
                                 │ PermissionGroup │
                                 │ AllowedActions  │
                                 └─────────────────┘
```

## Data Flow Diagram

### Creating Cross-Tenant User

```
User Request: Create user John who works for Tenant A and Tenant B
│
├──► Step 1: Create User (Global)
│    ┌────────────────────────────┐
│    │ using var context =        │
│    │   new AppDbContext(        │
│    │     options,               │
│    │     tenantId: null         │  ← No tenant filter
│    │   )                        │
│    │                            │
│    │ var user = new User {...}  │
│    │ context.Users.Add(user)    │
│    └────────────────────────────┘
│
├──► Step 2: Set Tenant A Context
│    ┌────────────────────────────┐
│    │ tenantService.SetCurrent   │
│    │   TenantId(tenantA.Id)     │
│    └────────────────────────────┘
│
├──► Step 3: Create Customer in Tenant A
│    ┌────────────────────────────┐
│    │ using var context =        │
│    │   contextFactory           │
│    │     .CreateDbContext()     │  ← Uses Tenant A
│    │                            │
│    │ var customer = new         │
│    │   Customer {               │
│    │     Name = "Customer A"    │
│    │   }                        │
│    │                            │
│    │ ClientCustomerId is set    │
│    │   automatically to Tenant A│
│    └────────────────────────────┘
│
├──► Step 4: Assign User to Customer A
│    ┌────────────────────────────┐
│    │ var ucr = new              │
│    │   UserCustomerRole {       │
│    │     UserId = john.Id,      │
│    │     CustomerId =           │
│    │       customerA.Id,        │
│    │     RoleId = adminRole.Id  │
│    │   }                        │
│    └────────────────────────────┘
│
├──► Step 5: Repeat for Tenant B
│    ┌────────────────────────────┐
│    │ tenantService.SetCurrent   │
│    │   TenantId(tenantB.Id)     │
│    │                            │
│    │ Create Customer B          │
│    │ Assign John to Customer B  │
│    └────────────────────────────┘
│
└──► Result: John can now access both tenants
     ┌────────────────────────────┐
     │ John's Access:             │
     │                            │
     │ Tenant A:                  │
     │   - Customer A (Admin)     │
     │                            │
     │ Tenant B:                  │
     │   - Customer B (Admin)     │
     └────────────────────────────┘
```

## Permission Check Flow

```
Request: Can user John edit Products in Customer A?
│
├──► Step 1: Get User-Customer-Role
│    ┌────────────────────────────────────────┐
│    │ var userRole =                         │
│    │   context.UserCustomerRoles            │
│    │     .Include(ucr => ucr.Role)          │
│    │     .ThenInclude(r => r.Permissions)   │
│    │     .FirstOrDefault(                   │
│    │       ucr => ucr.UserId == johnId &&   │
│    │              ucr.CustomerId ==         │
│    │                customerA.Id)           │
│    └────────────────────────────────────────┘
│
├──► Step 2: Get Role Permissions
│    ┌────────────────────────────────────────┐
│    │ var rolePermission =                   │
│    │   userRole.Role.Permissions            │
│    │     .FirstOrDefault(                   │
│    │       p => p.PermissionGroup.Name      │
│    │            == "Products")              │
│    └────────────────────────────────────────┘
│
├──► Step 3: Check Permission Flags
│    ┌────────────────────────────────────────┐
│    │ var allowed = (PermissionFlags)        │
│    │   rolePermission.AllowedActions        │
│    │                                        │
│    │ return allowed.HasFlag(                │
│    │   PermissionFlags.CanWrite)            │
│    └────────────────────────────────────────┘
│
└──► Result: true/false
```

## Tenant Context Flow

```
HTTP Request to API
│
├──► TenantMiddleware
│    │
│    ├──► Check X-Tenant-Id header
│    │    ┌────────────────────────┐
│    │    │ if (header exists)     │
│    │    │   extract tenant ID    │
│    │    └────────────────────────┘
│    │
│    ├──► Check Subdomain
│    │    ┌────────────────────────┐
│    │    │ if (tenant1.example.com)│
│    │    │   lookup tenant by     │
│    │    │   subdomain            │
│    │    └────────────────────────┘
│    │
│    └──► Check User Claims
│         ┌────────────────────────┐
│         │ if (authenticated)     │
│         │   get tenant from      │
│         │   user claims          │
│         └────────────────────────┘
│
├──► ITenantService.SetCurrentTenantId(tenantId)
│    ┌────────────────────────────────────┐
│    │ _tenantId.Value = tenantId         │
│    │ (Stored in AsyncLocal)             │
│    └────────────────────────────────────┘
│
├──► Controller Action
│    ┌────────────────────────────────────┐
│    │ var context =                      │
│    │   contextFactory.CreateDbContext() │
│    │                                    │
│    │ // Context uses current tenant ID  │
│    │ // All queries auto-filtered       │
│    └────────────────────────────────────┘
│
└──► Response
```

## Key Principles

1. **Separation of Concerns**
   - Global entities (User, Role) - Shared across all tenants
   - Tenant entities (Customer, Address) - Isolated per tenant
   - Bridge entities (UserCustomerRole) - Enable cross-tenant access

2. **Automatic Filtering**
   - When tenant ID is set: only tenant-specific data is visible
   - When tenant ID is null: all data is visible (admin mode)

3. **Single User Identity**
   - Users exist once in the system
   - UserCustomerRole links users to multiple tenants
   - Each link can have a different role

4. **Security by Default**
   - Tenant filtering happens automatically in AppDbContext
   - No risk of accidentally querying cross-tenant data
   - Explicit opt-in required to bypass filtering (tenantId: null)
