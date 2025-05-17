namespace GjammT.SharedKernel;

public sealed class ServiceLocator
{
    private static IServiceProvider? _serviceProvider { get; set; }

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider ??= serviceProvider;
    }
    
    public static T Resolve<T>()
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);
        return (T)_serviceProvider!.GetService(typeof(T))!;
    }
}