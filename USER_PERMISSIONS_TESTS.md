# User Permission System - Test Scenarios

## Test Setup

These tests verify the user-level permission system works correctly with customer and tenant linking.

### Prerequisites
- Database with UserPermissions table migrated
- Sample data:
  - User: John Doe (ID: user-123)
  - Tenant: City of Springfield (ID: tenant-456)
  - Customer: ACME Corp (ID: customer-789, belongs to tenant-456)
  - Permission Group: Invoices (ID: pg-001)

## Test Cases

### Test 1: User Permission Overrides Role Permission

**Scenario:** User has role-based permission to read invoices, but user-level permission grants write access

**Setup:**
```csharp
// Role permission for user's role
RolePermission: {
    PermissionGroupId: pg-001 (Invoices),
    AllowedActions: 1 (CanRead)
}

// User-level permission
UserPermission: {
    UserId: user-123,
    CustomerId: customer-789,
    PermissionGroupId: pg-001 (Invoices),
    AllowedActions: 5 (CanRead | CanWrite)
}
```

**Test:**
```csharp
var authService = new AuthorizationService(dbContext);
var hasRead = await authService.HasPermissionAsync(
    user-123, customer-789, "Invoices", PermissionFlags.CanRead);
var hasWrite = await authService.HasPermissionAsync(
    user-123, customer-789, "Invoices", PermissionFlags.CanWrite);
```

**Expected Results:**
- hasRead: true (from user permission)
- hasWrite: true (from user permission, overrides role)

### Test 2: No User Permission Falls Back to Role

**Scenario:** User has no user-level permission, should use role permission

**Setup:**
```csharp
// Only role permission exists
RolePermission: {
    PermissionGroupId: pg-001 (Invoices),
    AllowedActions: 7 (CanRead | CanCreate | CanWrite)
}
// No UserPermission
```

**Test:**
```csharp
var hasRead = await authService.HasPermissionAsync(
    user-123, customer-789, "Invoices", PermissionFlags.CanRead);
var hasDelete = await authService.HasPermissionAsync(
    user-123, customer-789, "Invoices", PermissionFlags.CanDelete);
```

**Expected Results:**
- hasRead: true (from role permission)
- hasDelete: false (not in role permission)

### Test 3: Customer-Level Permission Scope

**Scenario:** User has customer-level permission for specific customer

**Setup:**
```csharp
UserPermission: {
    UserId: user-123,
    CustomerId: customer-789,  // Specific customer
    ClientCustomerId: null,
    PermissionGroupId: pg-001,
    AllowedActions: 15 (All permissions)
}
```

**Test:**
```csharp
// Check permission for the specific customer
var hasPermForCustomer = await authService.HasPermissionAsync(
    user-123, customer-789, "Invoices", PermissionFlags.CanDelete);

// Check permission for different customer in same tenant
var hasPermForOtherCustomer = await authService.HasPermissionAsync(
    user-123, other-customer-999, "Invoices", PermissionFlags.CanDelete);
```

**Expected Results:**
- hasPermForCustomer: true (matches customer-level permission)
- hasPermForOtherCustomer: false (different customer)

### Test 4: Tenant-Level Permission Scope

**Scenario:** User has tenant-level permission that applies to all customers in tenant

**Setup:**
```csharp
UserPermission: {
    UserId: user-123,
    CustomerId: null,           // Null = tenant-level
    ClientCustomerId: tenant-456,
    PermissionGroupId: pg-001,
    AllowedActions: 1 (CanRead)
}
```

**Test:**
```csharp
var hasTenantPerm = await authService.HasTenantPermissionAsync(
    user-123, tenant-456, "Invoices", PermissionFlags.CanRead);
```

**Expected Results:**
- hasTenantPerm: true (matches tenant-level permission)

### Test 5: CRUD Operations in Admin UI

**Scenario:** Verify user permission CRUD operations work correctly

**Create Test:**
```csharp
// Navigate to /gjadmin/userpermissions
// Click "Add User Permission"
// Select:
//   - User: John Doe
//   - Scope: Customer Level
//   - Tenant: City of Springfield
//   - Customer: ACME Corp
//   - Permission Group: Invoices
//   - Allowed Actions: Read, Write (checked)
//   - Notes: "Temporary access for audit"
// Click Save

// Verify in database:
var perm = await dbContext.UserPermissions
    .FirstOrDefaultAsync(up => up.UserId == user-123 && up.CustomerId == customer-789);
```

**Expected Results:**
- Permission created in database
- AllowedActions = 5 (CanRead | CanWrite)
- Notes field populated
- CreatedAt timestamp set

**Update Test:**
```csharp
// Click Edit on existing permission
// Change Allowed Actions to: Read, Write, Delete (checked)
// Update Notes: "Extended access"
// Click Save

// Verify:
var updated = await dbContext.UserPermissions.FindAsync(perm.Id);
```

**Expected Results:**
- AllowedActions = 13 (CanRead | CanWrite | CanDelete)
- Notes updated
- UpdatedAt timestamp updated

**Delete Test:**
```csharp
// Click Delete on permission
// Confirm deletion
// Verify:
var deleted = await dbContext.UserPermissions.FindAsync(perm.Id);
```

**Expected Results:**
- deleted is null (or SoftDeleted = true if soft delete is used)

### Test 6: Filter Functionality

**Scenario:** Verify filtering works correctly in admin UI

**Setup:**
```csharp
// Create multiple permissions:
UserPermission 1: { UserId: user-123, CustomerId: customer-789 }
UserPermission 2: { UserId: user-456, CustomerId: customer-789 }
UserPermission 3: { UserId: user-123, CustomerId: customer-999 }
```

**Test:**
```csharp
// Filter by User: John Doe (user-123)
// Expected to show: Permissions 1 and 3

// Filter by Tenant: City of Springfield (tenant-456)
// (Assuming customer-789 belongs to tenant-456)
// Expected to show: Permissions 1 and 2

// Clear all filters
// Expected to show: All 3 permissions
```

### Test 7: Bitwise Permission Flags

**Scenario:** Verify bitwise flags work correctly

**Setup:**
```csharp
// Permission with multiple flags
UserPermission: {
    AllowedActions: 7  // Binary: 0111 = CanRead | CanCreate | CanWrite
}
```

**Test:**
```csharp
var flags = (PermissionFlags)7;
var hasRead = flags.HasFlag(PermissionFlags.CanRead);     // 1
var hasCreate = flags.HasFlag(PermissionFlags.CanCreate); // 2
var hasWrite = flags.HasFlag(PermissionFlags.CanWrite);   // 4
var hasDelete = flags.HasFlag(PermissionFlags.CanDelete); // 8
```

**Expected Results:**
- hasRead: true
- hasCreate: true
- hasWrite: true
- hasDelete: false

## Integration Tests

### Test 8: End-to-End Permission Check

**Scenario:** Complete flow from UI to authorization check

**Steps:**
1. Admin creates user permission via UI
2. User logs in to tenant application
3. User attempts to perform action
4. System checks permission via AuthorizationService
5. Action allowed/denied based on permission

**Expected Flow:**
```
UI (/gjadmin/userpermissions) 
  → Creates UserPermission in database
  → User performs action in app
  → Controller/Service calls AuthorizationService.HasPermissionAsync()
  → AuthorizationService checks UserPermissions table first
  → If found, uses user permission
  → If not found, falls back to RolePermission
  → Returns true/false
  → Action allowed/denied
```

## Performance Tests

### Test 9: Query Performance

**Scenario:** Verify permission checks are efficient with indexes

**Test:**
```sql
-- Should use indexes on UserId, CustomerId, PermissionGroupId
EXPLAIN ANALYZE
SELECT * FROM "UserPermissions" 
WHERE "UserId" = 'user-123' 
  AND "CustomerId" = 'customer-789' 
  AND "PermissionGroupId" = 'pg-001';
```

**Expected:**
- Query uses indexes (Index Scan, not Seq Scan)
- Execution time < 10ms for table with 10,000+ rows

## Security Tests

### Test 10: Authorization Required

**Scenario:** Verify admin pages require authentication

**Test:**
```csharp
// Attempt to access /gjadmin/userpermissions without authentication
```

**Expected:**
- Redirect to login page
- HTTP 401/302 status

### Test 11: Cross-Tenant Isolation

**Scenario:** Verify user can't create permissions for tenants they don't have access to

**Setup:**
```csharp
// User has access to tenant-456 only
// Attempt to create permission for customer in tenant-789
```

**Expected:**
- Permission created (admin interface allows this)
- Note: Admin interface has full access; tenant isolation is for end-user application

## Manual Testing Checklist

- [ ] Can create user permission with customer scope
- [ ] Can create user permission with tenant scope
- [ ] Can create user permission with global scope
- [ ] Can edit existing user permission
- [ ] Can delete user permission with confirmation
- [ ] Filter by user shows correct results
- [ ] Filter by tenant shows correct results
- [ ] Scope dropdown changes form fields correctly
- [ ] Selecting tenant loads customers for that tenant
- [ ] Permission flags checkboxes work correctly
- [ ] Notes field saves and displays
- [ ] User permission overrides role permission in auth check
- [ ] Falls back to role permission when no user permission exists
- [ ] Dashboard card links to user permissions page
- [ ] Navigation menu includes user permissions link
- [ ] User permissions page loads without errors

## Database Verification

```sql
-- Verify table exists
SELECT table_name FROM information_schema.tables 
WHERE table_name = 'UserPermissions';

-- Verify columns
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'UserPermissions';

-- Verify foreign keys
SELECT constraint_name, table_name, column_name
FROM information_schema.key_column_usage
WHERE table_name = 'UserPermissions';

-- Verify indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'UserPermissions';
```

## Success Criteria

All tests pass when:
1. ✅ User permissions override role permissions correctly
2. ✅ System falls back to role permissions when no user permission exists
3. ✅ Customer-level, tenant-level, and global scopes work as expected
4. ✅ CRUD operations in admin UI function correctly
5. ✅ Filters work and show correct results
6. ✅ Bitwise permission flags operate correctly
7. ✅ Database migration applies without errors
8. ✅ Authorization checks are performant
9. ✅ Security measures are in place (authentication required)
10. ✅ Documentation is complete and accurate
