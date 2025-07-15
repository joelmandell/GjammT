using GjammT.Models.CustomerRegister;

namespace GjammT.Models.Base;

// This is the explicit join table with extra data (the RoleId)
public class UserCustomerRole : BaseEntity
{
    // Foreign key to the User
    public Guid UserId { get; set; }
    public User User { get; set; }

    // Foreign key to the Customer
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }

    // Foreign key to the Roles
    public Guid RoleId { get; set; }
    public Role Role { get; set; }
}