public class PermissionGroup
{
    public Guid Id { get; set; }

    // The name of the resource or feature being protected.
    // e.g., "Products", "Invoices", "Settings"
    public string Name { get; set; }
}