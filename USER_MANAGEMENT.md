# User Management and Password Security

## Overview

This document describes the secure user management system implemented in GjammT, including password hashing, user creation, and authentication.

## Security Features

### Password Hashing
All user passwords are securely hashed using **BCrypt** with a salt factor of 12. Passwords are never stored in plain text.

- **Hash Algorithm**: BCrypt
- **Salt Factor**: 12 (provides good security with acceptable performance)
- **Auto-salting**: BCrypt automatically generates a unique salt for each password

### Password Reset Tokens
The `PasswordResetToken` field is nullable and only populated when a user requests a password reset. Tokens:
- Are randomly generated using GUIDs
- Expire after 24 hours
- Are cleared after successful password reset

## UserService API

The `UserService` class (in `GjammT.SharedKernel`) provides all user management functionality:

### Creating a User

```csharp
var userService = new UserService(dbContext);

var user = await userService.CreateUserAsync(
    email: "user@example.com",
    password: "SecurePassword123!",
    firstName: "John",
    lastName: "Doe",
    phoneNumber: "+1234567890", // optional
    dateOfBirth: new DateTime(1990, 1, 1) // optional
);
```

**Important**: The password parameter should be the plain text password. The service will automatically hash it.

### Verifying a Password

```csharp
var user = await userService.GetUserByEmailAsync("user@example.com");
bool isValid = userService.VerifyPassword(user, "password-to-check");
```

### Updating a Password

```csharp
bool success = await userService.UpdatePasswordAsync(userId, "NewSecurePassword123!");
```

### Password Reset Flow

#### 1. Generate Reset Token
```csharp
string? token = await userService.GeneratePasswordResetTokenAsync("user@example.com");
// Send token to user via email
```

#### 2. Reset Password with Token
```csharp
bool success = await userService.ResetPasswordAsync(
    email: "user@example.com",
    token: receivedToken,
    newPassword: "NewPassword123!"
);
```

### Getting Users

```csharp
// By email
var user = await userService.GetUserByEmailAsync("user@example.com");

// By ID
var user = await userService.GetUserByIdAsync(userId);
```

## Migration from Existing Code

### Before (Insecure)
```csharp
var user = new User
{
    Email = email,
    Password = "plain-text-password", // ❌ Security risk!
    FirstName = firstName,
    LastName = lastName,
    PasswordResetToken = "" // ❌ Caused database constraint error
};
db.Users.Add(user);
await db.SaveChangesAsync();
```

### After (Secure)
```csharp
var userService = new UserService(db);
var user = await userService.CreateUserAsync(
    email, 
    password, // ✅ Will be hashed automatically
    firstName, 
    lastName
);
// ✅ User saved with hashed password and null reset token
```

## Database Migration

A migration has been created to make the `PasswordResetToken` column nullable:
- File: `20251018195000_MakePasswordResetTokenNullable.cs`
- This fixes the NOT NULL constraint violation when creating users

To apply the migration:
```bash
cd GjammT.Models
dotnet ef database update
```

## LoginService Integration

The `LoginService` has been updated to verify passwords using BCrypt:

```csharp
public async Task<bool> UserNameSignIn(UserNameSigninRequest request)
{
    var user = await _userService.GetUserByEmailAsync(request.UserName);
    
    if (user == null || !user.IsActive)
    {
        return false;
    }

    return _userService.VerifyPassword(user, request.Password);
}
```

## Best Practices

1. **Never store plain text passwords**: Always use `UserService.CreateUserAsync()` or `UserService.UpdatePasswordAsync()`

2. **Password requirements**: Consider implementing password strength requirements:
   - Minimum length (e.g., 8 characters)
   - Mix of uppercase, lowercase, numbers, and special characters
   - Not in common password lists

3. **Rate limiting**: Implement rate limiting on login attempts to prevent brute force attacks

4. **Secure token delivery**: Password reset tokens should be sent via secure channels (email) and never logged

5. **Token expiry**: The current implementation expires tokens after 24 hours. Adjust in `UserService.GeneratePasswordResetTokenAsync()` if needed

6. **User activation**: New users have `IsActive = true` by default. Consider implementing email verification before activation

## Dependencies

- **BCrypt.Net-Next** (v4.0.3): Modern BCrypt implementation for .NET
  - Added to `GjammT.SharedKernel.csproj`

## Security Considerations

1. **BCrypt Resistance**: BCrypt is designed to be slow, making brute force attacks computationally expensive
2. **Salt Factor**: Set to 12 (can be increased for more security at the cost of performance)
3. **Timing Attacks**: BCrypt's verify function is resistant to timing attacks
4. **Rainbow Tables**: Automatic salting makes rainbow table attacks ineffective

## Future Enhancements

Consider implementing:
- Two-factor authentication (2FA)
- Password history to prevent password reuse
- Account lockout after multiple failed login attempts
- Password strength meter for user feedback
- Email verification on registration
- OAuth/OpenID Connect for third-party authentication
