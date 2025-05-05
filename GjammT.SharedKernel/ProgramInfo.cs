using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace GjammT.SharedKernel;

/// <summary>
/// This is a class that will have the responsibility to broker for accessing modules
/// and caching and also recieving connections for deployments from perhaps a CLI-tool on the server
/// or remotely on safe channel.
/// </summary>
public class ProgramInfo
{
    private static AppSettings _appSettings { get; set; }
    private static ResourceManager _resourceManager { get; set; }
    public static void SetAppSettings(AppSettings appSettings)
    {
        _appSettings ??= appSettings;
    }
    
    public static AppSettings GetAppSettings => _appSettings;
    
    public static string Version => $"2025.05-alpha";
    public static string Name => "GjammT";

    public static MethodInfo GetModule(string name,string className, string methodName)
    {
        //Load module and also load a type definition that can be used to access props and invoke parameters
        //dynamically.
        
        //Create an static analyzer perhaps that can be used to report
        //compile errors for missing methods when doing calls - if it is possible to do.
        
        var moduleKey = $"GjammT.{name}";
        var assemblyPath = GetBinPath(moduleKey);
        var _loadContext = new AssemblyLoadContext(moduleKey);
        var bytes = File.ReadAllBytes(assemblyPath);
        var fileAssembly = _loadContext.LoadFromStream(new MemoryStream(bytes));
        var type = fileAssembly.GetTypes().FirstOrDefault(t => t.Name == className);
        var instance = Activator.CreateInstance(type);

        var pi = instance.GetType().GetMethod(methodName);
        return pi;
    }
    
    public static string Resource(string key)
    {
        //TODO: Implement some caching strategy for reloading the DLL if an 
        //event is sent for redeploying. And modules is downloaded or streamed from secure remote repo.
        //Or CLI-tool that is deploying on the managing server and has an open connection.
        if(_resourceManager is null) {
            var assemblyPath = GetBinPath("GjammT.Models");

            var _loadContext = new AssemblyLoadContext(nameof(Resource));
            var bytes = File.ReadAllBytes(assemblyPath);

            var fileAssembly = _loadContext.LoadFromStream(new MemoryStream(bytes));
            
            var resource = fileAssembly.DefinedTypes.FirstOrDefault(t => t.Name == "Resources");
            
            PropertyInfo resourceManagerProp = resource.GetProperty("ResourceManager", 
                                                   BindingFlags.Public | BindingFlags.Static)
                                               ?? throw new ArgumentException("ResourceManager property not found");
        
            // Get the ResourceManager instance
            _resourceManager = (ResourceManager)resourceManagerProp.GetValue(null)
                                              ?? throw new InvalidOperationException("Could not get ResourceManager instance");
        }
        
        // Get the resource value
        return _resourceManager.GetString(key);
    }

    public static string GetBinPath(string path)
    {
        var buildModePath = "Debug";
#if RELEASE
        buildModePath = "Release";
#endif
        var binPath = $"{GetAppSettings?.ProjectPath}{path}/bin/{buildModePath}/{GetShortNetVersion()}/{path}.dll";
        return binPath;
    }
    
    public static string GetShortNetVersion()
    {
        string frameworkDescription = RuntimeInformation.FrameworkDescription;
        // Matches ".NET 9.0.1" → "net9.0" or ".NET Core 3.1.0" → "netcoreapp3.1"
        var match = Regex.Match(frameworkDescription, @"\.NET\s*(Core\s*)?([0-9]+\.[0-9]+)");
        if (match.Success)
        {
            string version = match.Groups[2].Value; 
            return $"net{version}";
        }
        return "unknown"; // Fallback
    }
}