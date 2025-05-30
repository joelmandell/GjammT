using GjammT.Models.Base;

namespace GjammT.Models.CustomerRegister;

public class Customer : BaseEntity
{
    public required string Name { get; set; }
    /// <summary>
    /// Social Security Number
    /// </summary>
    public string? Ssn { get; set; }
    /// <summary>
    /// Employer Identification Number (used for tax purposes, business, non-profit, etc.)
    /// </summary>
    public string? Ein { get; set; }

    public List<Address> Addresses { get; set; } = new();
    public List<User> Users { get; set; } = new();
}