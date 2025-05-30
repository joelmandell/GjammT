namespace GjammT.Models.CustomerRegister;

public enum AddressType
{
    // Most common address types
    Home,
    Work,
    Billing,
    Shipping,
    
    // Additional specialized types
    Mailing,
    Physical,
    Branch,
    HeadOffice,
    
    // For temporary addresses
    Temporary,
    
    // Default/unknown type
    Other
}