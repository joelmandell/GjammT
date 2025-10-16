using GjammT.Models.Base;

namespace GjammT.Models.CustomerRegister;

public class Customer : BaseEntity, IMultiTenant
{
    public required string Name { get; set; }
    public string LegacyCode { get; set; }
    /// <summary>
    /// Social Security Number
    /// </summary>
    public string? Ssn { get; set; }
    /// <summary>
    /// Employer Identification Number (used for tax purposes, business, non-profit, etc.)
    /// </summary>
    public string? Ein { get; set; }

    public List<Address> Addresses { get; set; } = new();
    
    public ICollection<UserCustomerRole> UserRoles { get; set; } = new List<UserCustomerRole>();
    
    // Multi-tenant support - links Customer to a specific ClientCustomer (tenant)
    public Guid ClientCustomerId { get; set; }
    public ClientCustomer ClientCustomer { get; set; }
}