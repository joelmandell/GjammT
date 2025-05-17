using GjammT.Core.Interfaces;
using GjammT.SharedKernel;

namespace GjammT.AccessSystem;

public sealed class AccessSystemBroker : IAccessSystemBroker
{
    public static void CustomerRegister()
    {
        
    }
    
    public string Heartbeat()
    {
        return "Heartbeat test is running and kinda noice but with some caveatfffs";
    }

    public string MASK()
    {
        return "DOES THIS WORK? YEP";
    }

    public string KO()
    {
        return "SOVA!";
    }
}