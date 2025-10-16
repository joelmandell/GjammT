using GjammT.Models.CustomerRegister;

namespace GjammT.Models.Base;

/// <summary>
/// Join table to enable customers to exist in multiple tenants.
/// A customer can book activities in different municipalities (tenants).
/// </summary>
public class CustomerTenant : BaseEntity
{
    // Foreign key to the Customer
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Foreign key to the Tenant (ClientCustomer)
    public Guid ClientCustomerId { get; set; }
    public ClientCustomer? ClientCustomer { get; set; }
}
