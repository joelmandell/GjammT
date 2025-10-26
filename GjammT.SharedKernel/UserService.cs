using GjammT.Models.CustomerRegister;
using GjammT.Models.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace GjammT.SharedKernel;

/// <summary>
/// Service for managing users with secure password handling
/// </summary>
public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new user with hashed password
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">Plain text password</param>
    /// <param name="firstName">User's first name</param>
    /// <param name="lastName">User's last name</param>
    /// <param name="phoneNumber">User's phone number (optional)</param>
    /// <param name="dateOfBirth">User's date of birth (optional)</param>
    /// <returns>Created user</returns>
    public async Task<User> CreateUserAsync(
        string email, 
        string password, 
        string firstName, 
        string lastName,
        string? phoneNumber = null,
        DateTime? dateOfBirth = null)
    {
        // Check if user with email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {email} already exists");
        }

        // Hash the password using BCrypt with work factor of 12
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = hashedPassword,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber ?? string.Empty,
            DateOfBirth = dateOfBirth?.ToUniversalTime() ?? DateTime.MinValue.ToUniversalTime(),
            RegistrationDate = DateTime.UtcNow,
            IsEmailVerified = false,
            IsActive = true,
            Role = UserRole.Private,
            PasswordResetToken = null,
            ResetTokenExpiry = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Verifies a password against a user's hashed password
    /// </summary>
    /// <param name="user">The user to verify</param>
    /// <param name="password">Plain text password to verify</param>
    /// <returns>True if password is correct</returns>
    public bool VerifyPassword(User user, string password)
    {
        try
        {
            // Try to verify as a BCrypt hash
            return BCrypt.Net.BCrypt.Verify(password, user.Password);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Password is not a valid BCrypt hash, check if it's a plaintext password
            // This should only happen for legacy users with plaintext passwords
            if (user.Password == password)
            {
                // Password matches but is stored in plaintext
                // Auto-upgrade to hashed password for security
                user.Password = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                user.UpdatedAt = DateTime.UtcNow;
                _context.SaveChangesAsync().Wait(); // Note: Synchronous wait in this context
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Updates a user's password
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <param name="newPassword">New plain text password</param>
    /// <returns>True if password was updated</returns>
    public async Task<bool> UpdatePasswordAsync(Guid userId, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Hash the new password with work factor of 12
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Generates a password reset token for a user
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>Password reset token, or null if user not found</returns>
    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return null;
        }

        // Generate a secure random token
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + 
                   Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        
        user.PasswordResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(24); // Token valid for 24 hours
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return token;
    }

    /// <summary>
    /// Resets a user's password using a reset token
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="token">Password reset token</param>
    /// <param name="newPassword">New plain text password</param>
    /// <returns>True if password was reset</returns>
    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.PasswordResetToken != token)
        {
            return false;
        }

        // Check if token is expired
        if (user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
        {
            return false;
        }

        // Hash the new password with work factor of 12
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.PasswordResetToken = null;
        user.ResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets a user by email
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>User or null if not found</returns>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>User or null if not found</returns>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FindAsync(userId);
    }
}
