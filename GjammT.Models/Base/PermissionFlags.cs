[Flags]
public enum PermissionFlags
{
    // Basic Permissions
    None        = 0,      // 00000000
    CanRead     = 1 << 0, // 00000001 (1)
    CanCreate   = 1 << 1, // 00000010 (2)
    CanWrite    = 1 << 2, // 00000100 (4)
    CanDelete   = 1 << 3, // 00001000 (8)
    All = ~0 // A bitwise trick to select all flags
}