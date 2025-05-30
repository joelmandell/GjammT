using GjammT.Models.Base;

namespace GjammT.Models.CustomerRegister;

public class User : BaseEntity
{
    public Guid CustomerId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.Private;
    public List<Address> Addresses { get; set; } = new();
    public string PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
}