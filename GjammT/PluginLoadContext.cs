using System.Reflection;
using System.Runtime.Loader;
namespace GjammT;
sealed class PluginLoadContext : AssemblyLoadContext
{
    public PluginLoadContext(string pluginPath) : base(isCollectible: true) { }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
}