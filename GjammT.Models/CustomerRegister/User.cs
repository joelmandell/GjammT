using GjammT.Models.Base;

namespace GjammT.Models.CustomerRegister;

public class User : BaseEntity
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public  string PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }= DateTime.MinValue.ToUniversalTime();
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.Private;
    public List<Address> Addresses { get; set; } = new();
    public string PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }= DateTime.MinValue.ToUniversalTime();
    public ICollection<UserCustomerRole> CustomerRoles { get; set; } = new List<UserCustomerRole>();
}