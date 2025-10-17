# GjammT Admin Interface

## Overview
The GjammT Admin Interface provides a comprehensive administrative dashboard for managing the multi-tenant booking system. All admin pages are accessible under the `/gjadmin` route.

## Access
- **Base URL**: `/gjadmin`
- **Authentication**: Required (uses `[Authorize]` attribute)
- **Admin Sign In**: Use `/Auth/SignInGjAdmin` endpoint with credentials from appsettings.json
- **Configuration**: See [GJADMIN_AUTH.md](../../../../../GJADMIN_AUTH.md) for authentication details
- **Layout**: Uses `AdminLayout.razor` for consistent navigation and styling

## Available Pages

### 1. Dashboard (`/gjadmin`)
The main admin dashboard providing an overview and quick navigation to all admin sections.

**Features:**
- Quick access cards to all management areas
- Information about multi-tenant architecture
- Permission system overview

### 2. Tenant Management (`/gjadmin/tenants`)
Manage companies and organizations (ClientCustomer entities).

**Features:**
- View all tenants in a data table
- Create new tenants with company name and subdomain
- Edit existing tenant information
- Delete tenants (with confirmation)

**Entity Fields:**
- Company Name (required)
- Subdomain (required) - used for tenant identification
- ID (auto-generated GUID)

### 3. User Management (`/gjadmin/users`)
Manage global user accounts that can work across multiple tenants.

**Features:**
- View all users with their status and roles
- Create new users with full profile information
- Edit user details and status
- Delete users (with warning about associated customer roles)
- Filter by active/inactive status

**Entity Fields:**
- First Name (required)
- Last Name (required)
- Email (required)
- Phone Number
- Password (required for new users)
- Date of Birth
- User Role (Private/Business/Admin)
- Active Status (checkbox)

### 4. Customer Management (`/gjadmin/customers`)
Manage tenant-specific customers with multi-tenant isolation.

**Features:**
- Filter customers by tenant
- View customers with tenant association
- Create new customers assigned to a specific tenant
- Edit customer information
- Delete customers

**Entity Fields:**
- Name (required)
- Tenant (required) - dropdown selection
- Legacy Code (optional)
- SSN (Social Security Number)
- EIN (Employer Identification Number)

### 5. Role Management (`/gjadmin/roles`)
Define and manage roles with granular permissions.

**Features:**
- View all roles with permission counts
- Create new roles with name and description
- Edit role information
- Manage role permissions (bitwise flags)
- Delete roles (with warning about affected users)
- Permission assignment interface

**Entity Fields:**
- Role Name (required)
- Description
- Permissions (managed separately)

**Permission Flags:**
- CanRead (1) - View/read access
- CanCreate (2) - Create new items
- CanWrite (4) - Update existing items
- CanDelete (8) - Delete items

### 6. Permission Management (`/gjadmin/permissions`)
Create and manage permission groups representing protected resources.

**Features:**
- View all permission groups
- See which roles use each permission group
- Create new permission groups
- Edit permission group names
- Delete permission groups (with warning about role usage)

**Entity Fields:**
- Group Name (required) - e.g., "Products", "Invoices", "Bookings"
- ID (auto-generated GUID)

## Architecture

### Multi-Tenant Support
The admin interface fully supports the multi-tenant architecture:
- **Global Entities**: Users, Roles, PermissionGroups (shared across all tenants)
- **Tenant-Specific Entities**: Customers (isolated per tenant via IMultiTenant)
- **Cross-Tenant Bridge**: UserCustomerRole links users to customers with specific roles

### Database Access
All pages use `IDbContextFactory<AppDbContext>` for proper async database operations:
```csharp
@inject IDbContextFactory<AppDbContext> ContextFactory

// Usage
await using var context = await ContextFactory.CreateDbContextAsync();
var items = await context.Items.ToListAsync();
```

### Permission System
The permission system uses bitwise flags for efficient storage:
```csharp
// Check if role has read permission
if ((rolePermission.AllowedActions & (int)PermissionFlags.CanRead) != 0)
{
    // User can read
}

// Add permission flag
rolePermission.AllowedActions |= (int)PermissionFlags.CanCreate;

// Remove permission flag
rolePermission.AllowedActions &= ~(int)PermissionFlags.CanDelete;
```

## UI Components

### Layout
- **AdminLayout.razor**: Provides consistent sidebar navigation and header
- Sidebar includes navigation to all admin pages
- Header displays authenticated user information
- "Back to Main Site" link for returning to the public site

### Styling
- Modern, clean design with card-based layouts
- Consistent color scheme (blue primary, gray secondary, red danger)
- Responsive tables with hover effects
- Modal dialogs for create/edit operations
- Form validation and error handling
- Loading states and error messages

### Modal Dialogs
- Create/Edit modals with form inputs
- Delete confirmation dialogs with warnings
- Permission management interface for roles
- Click outside to close functionality
- Save/Cancel actions with loading states

## Development Guidelines

### Adding New Admin Pages
1. Create new `.razor` file in `Components/Pages/Admin/`
2. Add `@page "/gjadmin/yourpage"` directive
3. Add `@layout AdminLayout` directive
4. Add `@attribute [Authorize]` for authentication
5. Inject `IDbContextFactory<AppDbContext>` for database access
6. Update `AdminLayout.razor` navigation menu

### Styling Conventions
- Use existing CSS classes from other admin pages
- Follow the color scheme: #2563eb (blue), #6b7280 (gray), #dc2626 (red)
- Use `.page-container`, `.page-header`, `.table-container` classes
- Modal dialogs use `.modal-overlay`, `.modal-content`, `.modal-header`, `.modal-body`, `.modal-footer`

### Error Handling
- Always use try-catch blocks for database operations
- Display error messages in `.error-message` div
- Show loading states during async operations
- Provide user-friendly error messages

## Future Enhancements

### Planned Features
1. **Search and Filtering**: Add search boxes to data tables
2. **Pagination**: Implement pagination for large datasets
3. **Sorting**: Add column sorting to tables
4. **Bulk Operations**: Select multiple items for bulk actions
5. **Audit Logging**: Track all admin changes
6. **Export**: Export data to CSV/Excel
7. **User-Customer-Role Assignment**: Dedicated page for assigning users to customers with roles

### Booking System Integration
When implementing the booking system, the admin interface should include:
- **Object Management**: Define bookable objects with types
- **Booking Rules**: Configure rate limits and restrictions
- **Calendar View**: Visualize bookings and availability
- **Booking Permissions**: Extend permission model for booking-specific rules

## Testing

### Manual Testing
Since .NET 10.0 SDK is not available in the standard environment:
1. Ensure database connection is configured
2. Run migrations to create database schema
3. Navigate to `/gjadmin` in browser
4. Test CRUD operations for each entity type
5. Verify authorization is enforced
6. Test permission flag combinations

### Database Setup
Required for testing:
```bash
# Run migrations
dotnet ef database update --project GjammT.Models --startup-project GjammT

# Or use the connection string in appsettings.json
```

## Troubleshooting

### Common Issues
1. **Database Connection Errors**: Verify connection string in Program.cs or appsettings.json
2. **Navigation Not Working**: Ensure all routes are correctly defined with `@page` directive
3. **Authorization Failures**: Check that user is authenticated
4. **Save Failures**: Check database constraints and required fields
5. **Foreign Key Violations**: Ensure related entities exist before creating references

## References
- [AGENT_INSTRUCTIONS.md](../../../../AGENT_INSTRUCTIONS.md) - Development guidelines
- [MULTI_TENANT_ARCHITECTURE.md](../../../../MULTI_TENANT_ARCHITECTURE.md) - Architecture details
- [MULTI_TENANT_QUICK_REFERENCE.md](../../../../MULTI_TENANT_QUICK_REFERENCE.md) - Quick reference guide
