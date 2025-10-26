# GjammT Booking System

Website for this project is now available on [GjammT.se](https://gjammt.se).

## Overview
GjammT is a modern booking system designed to comply with **ADDA** requirements.
You can read about the requirements (in Swedish) here: https://contracts.tendsign.com/ContractArea/Details/2700268
## Some Highlighted Goals
- ### 🤖 AI Chatbots (In prototype/planning)
  - **For customers**
    - Possibility to make bookings.
    - Find suitable objects for their activity
  - **For admins**
    - Ask for statistics and numbers.
      - Ask to send them to an email or other workflows
      - Generate excels and other docs
  

- ### 🤸 Adoptable and customizable.
  - Easier development experience makes
    it easier to deploy and update new features for the user.
  - For specific customers.


## Name Origin
The name **GjammT** honors the FRI development team at Idavall Data AB between 2013-2025, formed from initials of key contributors:

| Initial | Full Name   | Role                                             |
|---------|-------------|--------------------------------------------------|
| G       | Gustaf T    | CEO, Customer Success Manager                    |
| J       | Johan M & Joel Mandell | Fullstack Developers                             |
| A       | Adam G      | Fullstack Developer                              |
| M       | Madeleine L | Fullstack Developer, Economy Integrations Expert |
| M       | Mari Skinnar T | All things invoicing and customer support        |
| T       | Torbjörn S  | Senior Lead Architect, Fullstack Developer, CTO  |

## Technical Specifications
| Aspect         | Implementation Detail                     |
|----------------|-------------------------------------------|
| Framework      | ASP.NET Core                              |
| Database       | PostgreSQL with EF Core                   |
| Architecture   | Multi-Tenant with Shared Authorization    |
| Authentication | User-based with Hierarchical Permissions  |

## Multi-Tenant Architecture

GjammT implements a sophisticated multi-tenant architecture that allows:
- ✅ Multiple tenants (ClientCustomers) to share the same database
- ✅ Centralized authorization and user role logic across all tenants
- ✅ Users to work for multiple tenants simultaneously with different roles
- ✅ Automatic data isolation per tenant with global user management
- ✅ Fine-grained user-level permissions linked to customers or tenants

### Documentation

- 📖 **[Quick Reference](MULTI_TENANT_QUICK_REFERENCE.md)** - Quick start guide and common operations
- 📖 **[Architecture Guide](MULTI_TENANT_ARCHITECTURE.md)** - Detailed architecture explanation
- 📖 **[Usage Examples](MULTI_TENANT_USAGE.md)** - Code examples and best practices
- 📖 **[Implementation Guide](IMPLEMENTATION_GUIDE.md)** - Setup and migration guide
- 📊 **[Architecture Diagrams](ARCHITECTURE_DIAGRAMS.md)** - Visual diagrams and flows
- 🔐 **[User Permissions Guide](USER_PERMISSIONS.md)** - User-level permission system documentation

### Key Features

**Global Entities (Shared across tenants):**
- Users - Single identity across all tenants
- Roles - Defined once, used by all tenants
- Permissions - Centralized permission management
- User Permissions - Direct permission assignments to users for specific customers/tenants

**Tenant-Specific Entities:**
- Customers - Isolated per tenant
- Addresses - Belong to specific tenant

**Cross-Tenant Bridge:**
- UserCustomerRole - Links users to customers with specific roles across tenants

**Permission Hierarchy:**
- User-Level Permissions (highest priority) - Direct assignments that override role permissions
- Role-Based Permissions (default) - Permissions inherited from user roles

For implementation details, see [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md).

