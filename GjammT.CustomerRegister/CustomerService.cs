using GjammT.AccessSystem;
using GjammT.Core.Interfaces;
using GjammT.SharedKernel;
namespace GjammT.CustomerRegister;

public sealed class CustomerService : ICustomerRegister
{
    public static string ModuleInfo => "CustomerRegister";

    public static string Foo()
    {
        return "Foo";
    }

    public static async Task<string> Bar()
    {
        try
        {
            var access = await ProgramInfo.GetInstance<IAccessSystemBroker, AccessSystemBroker>();
            var data = access.KO();
            return access.KO().ToString();

        }
        catch (Exception e)
        {
            var ko = 1;
        }

        return null;
    }
}