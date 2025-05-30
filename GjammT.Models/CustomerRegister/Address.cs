using GjammT.Models.Base;

namespace GjammT.Models.CustomerRegister;

public class Address : BaseEntity
{
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string StateProvince { get; set; }
    public required string PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsPrimary { get; set; }
    public AddressType AddressType { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}