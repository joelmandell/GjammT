namespace GjammT.Models.Base;

public interface IMultiTenant
{
    public Guid ClientCustomerId { get; set; }
    public ClientCustomer ClientCustomer { get; set; }
}