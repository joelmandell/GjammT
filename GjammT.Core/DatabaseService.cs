// using System.Reflection;
// using System.Runtime.InteropServices;
// using System.Runtime.Loader;
// using System.Text.RegularExpressions;
// using JasperFx;
// using JasperFx.CodeGeneration;
// using JasperFx.RuntimeCompiler;
// using Marten;
// using Marten.Events;
// using Microsoft.Extensions.Configuration;
//
// namespace GjammT.Core;
//
// public class DatabaseService
// {
//     private static string GetDefaultDatabaseConnection()
//     {
//         var currentDirectory = Directory.GetCurrentDirectory();
//         var sharedSettingsPath = Path.Combine(currentDirectory, "secrets.json");
//
//         var config = new ConfigurationBuilder()
//             .AddJsonFile(sharedSettingsPath, optional: false)
//             .Build();
//
//         return config["ConnectionStrings:Default"];
//     }
//
//     private static string GetShortNetVersion()
//     {
//         string frameworkDescription = RuntimeInformation.FrameworkDescription;
//         // Matches ".NET 9.0.1" → "net9.0" or ".NET Core 3.1.0" → "netcoreapp3.1"
//         var match = Regex.Match(frameworkDescription, @"\.NET\s*(Core\s*)?([0-9]+\.[0-9]+)");
//         if (match.Success)
//         {
//             string version = match.Groups[2].Value;
//             return $"net{version}";
//         }
//
//         return "unknown"; // Fallback
//     }
//
//     public static string GetConfig(string key)
//     {
//         var currentDirectory = Directory.GetCurrentDirectory();
//         var sharedSettingsPath = Path.Combine(currentDirectory, "sharedsettings.json");
//
//         var config = new ConfigurationBuilder()
//             .AddJsonFile(sharedSettingsPath, optional: false)
//             .Build();
//
//         return config[key];
//     }
//
//     public static string GetBinPath(string path)
//     {
//         var buildModePath = "Debug";
// #if RELEASE
//         buildModePath = "Update";
// #endif
//         var binPath =
//             $"{GetConfig("AppSettings:ProjectPath")}{path}/bin/{buildModePath}/{GetShortNetVersion()}/{path}.dll";
//         return binPath;
//     }
//
//     public static Assembly GetAssembly(string project)
//     {
//         var modelAssemblyPath = GetBinPath(project);
//         var loadContext = new AssemblyLoadContext(project, true);
//         var bytes = File.ReadAllBytes(modelAssemblyPath);
//
//         return loadContext.LoadFromStream(new MemoryStream(bytes));
//     }
//
//     public static IDocumentStore Store()
//     {
//         string solutionDir = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
//         var modelsDirectory = $"{solutionDir}/GjammT.Models/";
//         var fileAssembly = GetAssembly("GjammT.Models");
//         var fileGjammT = GetAssembly("GjammT");
//
//         var assemblyGenerator = new AssemblyGenerator();
//         var baseTypes = fileAssembly.GetTypes().Where(t => t.GetInterface("IDoc") != null).ToList();
//
//         assemblyGenerator.ReferenceAssembly(fileAssembly);
//         var rules = new JasperFx.CodeGeneration.GenerationRules()
//         {
//             TypeLoadMode = TypeLoadMode.Dynamic
//         };
//         rules.ApplicationAssembly = fileGjammT;
//         foreach (var baseType in baseTypes)
//         {
//             // opts.StartAssembly(baseType.Assembly);
//             rules.Assemblies.Add(baseType.Assembly);
//         }
//         // rulesConnection(GetDefaultDatabaseConnection());
//
//         rules.GeneratedCodeOutputPath = Path.Combine(
//             modelsDirectory,
//             "Generated");
//         //opts.ApplicationAssembly = fileAssembly;
//         // opts.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
//         // var genAsm = new JasperFx.CodeGeneration.GeneratedAssembly(rules);
//         // foreach (var baseType in baseTypes)
//         // {
//         //     // opts.StartAssembly(baseType.Assembly);
//         //     genAsm.AddType(baseType.Name, baseType);
//         // }
//
//         // genAsm.AttachAssembly(fileAssembly);
//         // genAsm.AttachAssembly(fileGjammT);
//         // assemblyGenerator.Compile(genAsm);
//
//         // return (EventDocumentStorage)Activator.CreateInstance(assemblyGenerator.C.CompiledType!, options)!;
//
//         var store = DocumentStore
//             .For(opts =>
//             {
//                 opts.Connection(GetDefaultDatabaseConnection());
//                 opts.GeneratedCodeMode = TypeLoadMode.Dynamic;
//                 //opts.ApplicationAssembly = fileAssembly;
//                 opts.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
//
//                 opts.GeneratedCodeOutputPath = Path.Combine(
//                     modelsDirectory,
//                     "Generated");
//                 // var baseTypes = fileAssembly.GetTypes().Where(t => t.GetInterface("IDoc") != null).ToList();
//                 opts.SetApplicationProject(fileGjammT);
//
//                 // foreach (var baseType in baseTypes)
//                 // {
//                 //     // opts.StartAssembly(baseType.Assembly);
//                 //     opts.RegisterDocumentType(baseType);
//                 // }
//             });
//
//         store.Rules.ReferenceAssembly(fileAssembly);
//         //store.Rules.Assemblies.Add(fileAssembly);
//         return store;
//     }
// }