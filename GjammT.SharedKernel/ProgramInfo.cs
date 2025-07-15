using System.Collections.Concurrent;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace GjammT.SharedKernel;

/// <summary>
/// This is a class that will have the responsibility to broker for accessing modules
/// and caching and also recieving connections for deployments from perhaps a CLI-tool on the server
/// or remotely on safe channel.
/// </summary>
public class ProgramInfo
{
    private static readonly List<Assembly> LoadedAssemblies = new();
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1); // Binary semaphore (1 indicates only one thread/task can enter)
    private static ResourceManager? _resourceManager;
    private static RazorComponentsEndpointConventionBuilder? _razorComponentsEndpointConventionBuilder;
    private static bool RefreshResources { get; set; }
    private static readonly ContainerBuilder ContainerBuilder = new();
    private static readonly Autofac.IContainer Container = ContainerBuilder.Build();
    private static ConcurrentDictionary<string, AssemblyLoadContext> _assemblyLoadContexts;

    public async Task LoadComponentAssembly(string module)
    {
        await Semaphore.WaitAsync();
        try
        {
            _assemblyLoadContexts ??= new ConcurrentDictionary<string, AssemblyLoadContext>();
            var assemblyPath = GetBinPath(module);
            var bytes = await File.ReadAllBytesAsync(assemblyPath);

            if (!_assemblyLoadContexts.TryGetValue("GjammT", out var loadContext))
            {
                loadContext = new AssemblyLoadContext("GjammT", true);
                _assemblyLoadContexts.TryAdd("GjammT", loadContext);
            }
            else
            {
                loadContext.Unload();
                loadContext = new AssemblyLoadContext("GjammT", true);
                _assemblyLoadContexts.TryAdd("GjammT", loadContext);
            }

            var assembly = loadContext.LoadFromStream(new MemoryStream(bytes));

            loadContext.Resolving += (context, name) =>
            {
                if (name.Name.AsSpan().StartsWith("GjammT"))
                {
                    var assemblyRefPath = GetBinPath(name?.Name);
                    if (File.Exists(assemblyRefPath))
                    {
                        var bytesRef = File.ReadAllBytes(assemblyRefPath);
                        var fileAssembly = loadContext.LoadFromStream(new MemoryStream(bytesRef));
                        return fileAssembly;
                    }
                } else
                {
                    var assemblyRefPath = GetUIDependableModule(name.Name);
                    if (File.Exists(assemblyRefPath))
                    {
                        var bytesRef = File.ReadAllBytes(assemblyRefPath);
                        var fileAssembly = loadContext.LoadFromStream(new MemoryStream(bytesRef));
                        return fileAssembly;
                    }
                }
                return context.Assemblies.FirstOrDefault(a => a.FullName == name.FullName);
            };

            foreach (var assemblyRef in assembly.GetReferencedAssemblies())
            {
                var assemblyRefPath = GetBinPath(assemblyRef?.Name);
                if (File.Exists(assemblyRefPath))
                {
                    var bytesRef = File.ReadAllBytes(assemblyRefPath);
                    var fileAssembly = loadContext.LoadFromStream(new MemoryStream(bytesRef));
                }
            }

            var razorBuilder = _razorComponentsEndpointConventionBuilder;

            var appbuilder = razorBuilder.GetType().GetProperty("ApplicationBuilder",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var endpoint = razorBuilder.GetType().GetProperty("EndpointRouteBuilder",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var webApp = (WebApplication)endpoint.GetValue(razorBuilder);

            var dataSources = webApp.GetType()
                .GetProperty("DataSources", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .GetValue(webApp) as List<Microsoft.AspNetCore.Routing.EndpointDataSource>;

            var assemblyMethods = razorBuilder.GetType()?
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?.FirstOrDefault()
                ?.GetValue(razorBuilder).GetType()
                ?.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var addAssembly = assemblyMethods?.FirstOrDefault(m => m.Name == "AddAssembly");
            var removeLibrary = assemblyMethods?.FirstOrDefault(m => m.Name == "RemoveLibrary");

            var existingAssembly = LoadedAssemblies.FirstOrDefault(a => a.FullName == assembly.FullName);

            if (existingAssembly != null)
            {
                //remove old version
                LoadedAssemblies.Remove(existingAssembly);
                removeLibrary?.Invoke(appbuilder?.GetValue(razorBuilder), [existingAssembly?.FullName]);

                //re-add
                try
                {
                    addAssembly?.Invoke(appbuilder?.GetValue(razorBuilder), [assembly]);
                }
                catch (Exception e)
                {
                    //Logging?
                }

                LoadedAssemblies.Add(assembly);
            }
            else
            {
                addAssembly?.Invoke(appbuilder?.GetValue(razorBuilder), [assembly]);
                LoadedAssemblies.Add(assembly);
            }

            RefreshResources = true;

            try
            {
                var blazorEndpointSource = dataSources?.LastOrDefault();

                //Get the updateendpoints method that does the magic for us and adds the new page component by its endpoint.
                blazorEndpointSource?.GetType()
                    ?.GetMethod("UpdateEndpoints", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?.Invoke(blazorEndpointSource, null);
            }
            catch
            {
                //Logging
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    //Implement some caching strategy for reloading the DLL if it really has changed?
    public static async Task<T> GetInstance<T,TConcrete>()
    {
        await Semaphore.WaitAsync();
        try
        {
            using (var scope = Container.BeginLifetimeScope(child =>
                   {
                       child.RegisterType<TConcrete>().As<T>().InstancePerDependency();
                   }))
            {
                return scope.Resolve<T>();
            }
        }
        catch (Exception e)
        {
            //TODO: Logger
        }
        finally
        {
            Semaphore.Release();
        }
        
        return default(T)!;
    }
    
    public IEnumerable<Assembly> GetLoadedAssemblies() => LoadedAssemblies;

    public static void SetRazorBuilder(RazorComponentsEndpointConventionBuilder builder)
    {
        _razorComponentsEndpointConventionBuilder ??= builder;
    }
    
    public static string Version => $"2025.05-alpha";
    public static string Name => "GjammT";
    
    public static string Resource(string key)
    {
        //TODO: Implement some caching strategy for reloading the DLL if an 
        //event is sent for redeploying. And modules is downloaded or streamed from secure remote repo.
        //Or CLI-tool that is deploying on the managing server and has an open connection.
        if(_resourceManager is null || RefreshResources) {
            var assemblyPath = GetBinPath("GjammT.Models");

            var loadContext = new AssemblyLoadContext(nameof(Resource));
            var bytes = File.ReadAllBytes(assemblyPath);

            var fileAssembly = loadContext.LoadFromStream(new MemoryStream(bytes));
            
            var resource = fileAssembly.DefinedTypes.FirstOrDefault(t => t.Name == "Resources");
            
            PropertyInfo resourceManagerProp = resource?.GetProperty("ResourceManager", 
                                                   BindingFlags.Public | BindingFlags.Static)
                                               ?? throw new ArgumentException("ResourceManager property not found");
        
            // Get the ResourceManager instance
            _resourceManager = (ResourceManager?)resourceManagerProp.GetValue(null)
                                              ?? throw new InvalidOperationException("Could not get ResourceManager instance");
            RefreshResources = false;
        }
        
        // Get the resource value
        return _resourceManager?.GetString(key) ?? "RESOURCE_MANAGER_NOT_FOUND";
    }

    public static string GetConfig(string key)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var sharedSettingsPath = Path.Combine(currentDirectory, "sharedsettings.json");

        var config = new ConfigurationBuilder()
            .AddJsonFile(sharedSettingsPath, optional: false)
            .Build();

        return config[key]; 
    }
    
    public static string GetBinPath(string path)
    {
        var buildModePath = "Debug";
#if RELEASE
        buildModePath = "Update";
#endif
        var binPath = $"{GetConfig("AppSettings:ProjectPath")}{path}/bin/{buildModePath}/{GetShortNetVersion()}/{path}.dll";
        return binPath;
    }
    
    public static string GetUIDependableModule(string path)
    {
        var buildModePath = "Debug";
#if RELEASE
        buildModePath = "Update";
#endif
        var binPath = $"{GetConfig("AppSettings:ProjectPath")}GjammT.UI/bin/{buildModePath}/{GetShortNetVersion()}/{path}.dll";
        return binPath;
    }
    
    private static string GetShortNetVersion()
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