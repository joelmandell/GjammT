using GjammT.Core.Interfaces;

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
}