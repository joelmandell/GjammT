namespace GjammT.Models.Base;

public class ClientCustomer
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; }
    public string Subdomain { get; set; }
    
    public ICollection<CustomerTenant> Customers { get; set; } = new List<CustomerTenant>();
}