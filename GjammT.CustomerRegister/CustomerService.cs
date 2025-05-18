using GjammT.AccessSystem;
using GjammT.Core.Interfaces;
using GjammT.SharedKernel;
namespace GjammT.CustomerRegister;

public sealed class CustomerService : ICustomerRegister
{
    public static string ModuleInfo => "CustomerRegister";
}