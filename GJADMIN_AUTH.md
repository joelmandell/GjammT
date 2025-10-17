# GjAdmin Authentication

## Overview

The GjAdmin authentication system provides a hardcoded admin signin mechanism for accessing the `/gjadmin` routes. This is a temporary solution for testing the admin interfaces and will be refactored to a more secure solution in the future.

## Configuration

The admin credentials are configured in `appsettings.json`:

```json
{
  "GjAdmin": {
    "Username": "gjadmin",
    "Password": ""
  }
}
```

### Security Note

⚠️ **Important**: This repository is public, so the default password is intentionally left empty. In production or private deployments, you should:

1. Set a strong password in your `appsettings.json` or `appsettings.Production.json`
2. Consider using environment variables or Azure Key Vault for sensitive configuration
3. This will be replaced with a more secure authentication mechanism in the future

## Usage

### Signing In

There are two ways to sign in as GjAdmin:

#### 1. Using the Login Component (Recommended)

The standard login component at the root of the application (`/`) automatically detects when the username "gjadmin" is entered and routes the login request to the gjadmin endpoint. Simply:

1. Navigate to the application root
2. Enter username: `gjadmin`
3. Enter the configured password
4. Submit the form

The component will automatically POST to `/Auth/SignInGjAdmin` instead of the regular `/Auth/SignIn` endpoint.

#### 2. Direct API Call

You can also make a direct POST request to:

```
POST /Auth/SignInGjAdmin
Content-Type: application/json

{
  "userName": "gjadmin",
  "password": ""
}
```

On successful authentication:
- A cookie-based authentication session is created
- The user is redirected to `/gjadmin`
- A "GjAdmin" role claim is added to the user's identity

### API Endpoints

- **POST /Auth/SignInGjAdmin** - Sign in with gjadmin credentials
- **GET /Auth/SignOut** - Sign out (shared with regular tenant signin)

### Protected Routes

All routes under `/gjadmin/*` require authentication via the `[Authorize]` attribute:

- `/gjadmin` - Admin dashboard
- `/gjadmin/tenants` - Tenant management
- `/gjadmin/users` - User management
- `/gjadmin/customers` - Customer management
- `/gjadmin/roles` - Role management
- `/gjadmin/permissions` - Permission management

## Implementation Details

### Components

1. **GjAdminSettings.cs** - Configuration model for admin credentials
2. **ILoginService.GjAdminSignIn()** - Interface method for admin authentication
3. **LoginService.GjAdminSignIn()** - Implementation that validates credentials against configuration
4. **AuthController.SignInGjAdmin()** - API endpoint for admin signin
5. **SignIn.razor** - Login component with intelligent routing based on username

### Authentication Flow

```
1. User enters credentials in SignIn.razor component
2. JavaScript detects if username is "gjadmin" (case-insensitive)
3. Form action is automatically changed to /Auth/SignInGjAdmin
4. Form submits credentials to appropriate endpoint
5. AuthController calls ILoginService.GjAdminSignIn()
6. LoginService validates username and password against GjAdminSettings
7. If valid, creates ClaimsPrincipal with Name and Role claims
8. Signs in user with cookie authentication
9. Redirects to /gjadmin
```

### Differences from Regular Signin

- **Regular SignIn** (`/Auth/SignIn`): Requires subdomain, validates against tenant-specific users
- **GjAdmin SignIn** (`/Auth/SignInGjAdmin`): No subdomain required, validates against hardcoded admin credentials, adds "GjAdmin" role claim

## Future Improvements

This authentication mechanism is temporary and will be enhanced with:

- [ ] Proper password hashing
- [ ] Multi-factor authentication
- [ ] Integration with identity providers (Azure AD, OAuth)
- [ ] Audit logging for admin actions
- [ ] Role-based access control for different admin capabilities
- [ ] Session timeout and refresh token support

## Testing

To test the authentication:

1. Update your `appsettings.Development.json` with a password (optional for local testing)
2. Run the application
3. Send a POST request to `/Auth/SignInGjAdmin` with the configured credentials
4. Verify you're redirected to `/gjadmin` and can access admin pages

Example using curl:

```bash
curl -X POST http://localhost:5000/Auth/SignInGjAdmin \
  -H "Content-Type: application/json" \
  -d '{"userName":"gjadmin","password":""}'
```

Example using Postman or similar tools:
- Method: POST
- URL: `http://localhost:5000/Auth/SignInGjAdmin`
- Body (JSON):
  ```json
  {
    "userName": "gjadmin",
    "password": ""
  }
  ```
