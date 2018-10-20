using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Bootstrap;
using Dasync.Fabric.Sample.Base;
using Dasync.Ioc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FunctionExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Dasync.Fabric.AzureFunctions
{
    public class LazyAppIocContainerProvider : IAppIocContainerProvider
    {
        private readonly Lazy<IAppServiceIocContainer> _lazyContainer;

        public LazyAppIocContainerProvider(Func<IAppServiceIocContainer> provider) =>
            _lazyContainer = new Lazy<IAppServiceIocContainer>(provider);

        public IAppServiceIocContainer GetAppIocContainer() => _lazyContainer.Value;
    }

    public class InitializedRuntime
    {
        public IIocContainer EngineIocContainer { get; set; }

        public IAzureFunctionsFabric Fabric { get; set; }
    }

    public sealed class ContainerConverters
    {
        public ContainerConverters(IIocContainerConverter[] converters)
        {
            Converters = converters;
        }

        public IIocContainerConverter[] Converters { get; }
    }

    public class GlobalStartup
    {
        private static readonly Dictionary<string, Task<InitializedRuntime>> _functionToRuntimeMap =
            new Dictionary<string, Task<InitializedRuntime>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Task<InitializedRuntime>> _startupFileToRuntimeMap =
            new Dictionary<string, Task<InitializedRuntime>>(StringComparer.OrdinalIgnoreCase);

        public static Task<InitializedRuntime> GetRuntimeAsync(
            FunctionExecutionContext context,
            ILogger logger)
        {
            Task<InitializedRuntime> initializeRuntimeTask;
            lock (_functionToRuntimeMap)
            {
                if (!_functionToRuntimeMap.TryGetValue(context.FunctionName, out initializeRuntimeTask))
                {
                    initializeRuntimeTask = Task.Run(() => InitializeRuntimeAsync(context, logger));
                    _functionToRuntimeMap.Add(context.FunctionName, initializeRuntimeTask);
                }
            }
            return initializeRuntimeTask;
        }

        private static Task<InitializedRuntime> InitializeRuntimeAsync(
            FunctionExecutionContext context,
            ILogger logger)
        {
            if (!TryGetStartupSettings(context.FunctionDirectory, out var startupSettings))
                throw new InvalidOperationException($"Missing startup file in '{context.FunctionDirectory}'.");

            var startupFileName = Path.GetFileName(startupSettings.StartupFile);
            Task<InitializedRuntime> initializeRuntimeTask;
            lock (_startupFileToRuntimeMap)
            {
                if (!_startupFileToRuntimeMap.TryGetValue(startupFileName, out initializeRuntimeTask))
                {
                    initializeRuntimeTask = Task.Run(() => InitializeRuntimeAsync(
                        startupSettings, context, logger));
                    _startupFileToRuntimeMap.Add(startupFileName, initializeRuntimeTask);
                }
            }
            return initializeRuntimeTask;
        }

        private static async Task<InitializedRuntime> InitializeRuntimeAsync(
            DasyncFunctionStartupSettings startupSettings,
            FunctionExecutionContext context,
            ILogger logger)
        {
            var startupInfo = LoadStartupType(startupSettings);

            var engineIocContainer = TryCreateEngineIocContainer(startupInfo);
            if (engineIocContainer == null)
            {
                engineIocContainer = CreateDefaultEngineIocContainer(
                    context.FunctionDirectory,
                    Path.Combine(context.FunctionDirectory, "..", "bin"));
            }

            if (startupInfo?.StartupAssembly != null)
            {
                var appIocContainerProviderHolder = engineIocContainer
                    .Resolve<AppIocContainerProviderProxy.Holder>();

                appIocContainerProviderHolder.Provider =
                    new LazyAppIocContainerProvider(() =>
                    {
                        var converters = engineIocContainer.Resolve<ContainerConverters>().Converters;
                        return TryCreateAppIocContainer(startupInfo.StartupAssembly, converters);
                    });
            }

            var settings = engineIocContainer.Resolve<AzureFunctionsFabricSettings>();
            settings.FunctionsDirectory = Path.GetFullPath(Path.Combine(context.FunctionDirectory, ".."));

            var bootstrapper = engineIocContainer.Resolve<Bootstrapper>();
            var bootstrapResult = await bootstrapper.BootstrapAsync(CancellationToken.None);

            return new InitializedRuntime
            {
                EngineIocContainer = engineIocContainer,
                Fabric = (IAzureFunctionsFabric)bootstrapResult.Fabric
            };
        }

        [JsonObject]
        public sealed class DasyncFunctionStartupSettings
        {
            [JsonProperty("startupFile")]
            public string StartupFile { get; set; }

            [JsonProperty("serviceNames")]
            public List<string> ServiceNames { get; set; }
        }

        public static bool TryGetStartupSettings(
            string functionDirectory,
            out DasyncFunctionStartupSettings settings)
        {
            var dasyncStartupFilePath = Path.Combine(functionDirectory, "dasync.startup.json");
            if (!File.Exists(dasyncStartupFilePath))
            {
                settings = null;
                return false;
            }

            var startupJson = File.ReadAllText(dasyncStartupFilePath);
            if (string.IsNullOrEmpty(startupJson))
            {
                settings = null;
                return false;
            }

            settings = JsonConvert.DeserializeObject<DasyncFunctionStartupSettings>(startupJson);

            if (!string.IsNullOrEmpty(settings.StartupFile))
                settings.StartupFile = Path.GetFullPath(Path.Combine(functionDirectory, settings.StartupFile));

            return true;
        }

        [JsonObject]
        public sealed class AzureFunctionSettings
        {
            [JsonProperty("bindings")]
            public List<AzureFunctionBinding> Bindings { get; set; }
        }

        [JsonObject]
        public sealed class AzureFunctionBinding
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("direction")]
            public string Direction { get; set; }

            [JsonProperty("queueName")]
            public string QueueName { get; set; }
        }

        public static bool TryGetFunctionSettings(
            string functionDirectory,
            out AzureFunctionSettings settings)
        {
            var functionFilePath = Path.Combine(functionDirectory, "function.json");
            if (!File.Exists(functionFilePath))
            {
                settings = null;
                return false;
            }

            var functionJson = File.ReadAllText(functionFilePath);
            if (string.IsNullOrEmpty(functionJson))
            {
                settings = null;
                return false;
            }

            settings = JsonConvert.DeserializeObject<AzureFunctionSettings>(functionJson);
            return true;
        }

        private class StartupInfo
        {
            public Assembly StartupAssembly { get; set; }
        }

        private static StartupInfo LoadStartupType(DasyncFunctionStartupSettings settings)
        {
            if (string.IsNullOrEmpty(settings.StartupFile))
                return null;

            var startupAssembly = LoadAssembly(settings.StartupFile);

            var result = new StartupInfo
            {
                StartupAssembly = startupAssembly
            };

            return result;
        }

        private static IIocContainer TryCreateEngineIocContainer(StartupInfo startupInfo)
        {
            if (startupInfo == null)
                return null;

            return null;
        }

        private static readonly Dictionary<Type, Type>[] DefaultBindingsCollection = new[]
        {
            Dasync.Ioc.DI.Bindings,
            Dasync.Serialization.DI.Bindings,
            Dasync.Serialization.Json.DI.Bindings,
            Dasync.ServiceRegistry.DI.Bindings,
            Dasync.Proxy.DI.Bindings,
            Dasync.AsyncStateMachine.DI.Bindings,
            Dasync.ExecutionEngine.DI.Bindings,
            Dasync.Bootstrap.DI.Bindings,
            Dasync.AzureStorage.DI.Bindings,
            Dasync.FabricConnector.AzureStorage.DI.Bindings,
            Dasync.Fabric.AzureFunctions.DI.Bindings
        };

        private static readonly HashSet<string> DefaultBindingsAssemblyFileNames =
            new HashSet<string>(new[]
            {
                "Dasync.Ioc.dll",
                "Dasync.Serialization.dll",
                "Dasync.Serialization.Json.dll",
                "Dasync.Serializers.StandardTypes.dll",
                "Dasync.Serializers.EETypes.dll",
                "Dasync.ServiceRegistry.dll",
                "Dasync.ValueContainer.dll",
                "Dasync.Proxy.dll",
                "Dasync.Accessors.dll",
                "Dasync.AsyncStateMachine.dll",
                "Dasync.EETypes.dll",
                "Dasync.ExecutionEngine.dll",
                "Dasync.Bootstrap.dll",
                "Dasync.AzureStorage.dll",
                "Dasync.FabricConnector.AzureStorage.dll",
                "Dasync.Fabric.AzureFunctions.dll",
                "Dasync.Hints.dll"
            },
            StringComparer.OrdinalIgnoreCase);

        private static IIocContainer CreateDefaultEngineIocContainer(params string[] assemblyDirectories)
        {
            var container = new BasicIocContainer();
            foreach (var bindings in DefaultBindingsCollection)
                container.Load(bindings);

            if (assemblyDirectories != null)
            {
                foreach (var directory in assemblyDirectories)
                {
                    if (!Directory.Exists(directory))
                        continue;

                    var dllFiles = Directory.GetFiles(directory, "*dasync*.dll", SearchOption.TopDirectoryOnly);
                    foreach (var dllFilePath in dllFiles)
                    {
                        var fileName = Path.GetFileName(dllFilePath);
                        if (DefaultBindingsAssemblyFileNames.Contains(fileName))
                            continue;

                        Assembly assembly;
                        try
                        {
                            assembly = LoadAssembly(dllFilePath);
                        }
                        catch
                        {
                            continue;
                        }

                        if (!TryFindDiBindings(assembly, out var bindings))
                            continue;

                        // Ignore other fabrics, if a such assembly was copied over by accident.
                        if (bindings.Keys.Contains(typeof(IFabric)))
                            continue;

                        container.Load(bindings);
                    }
                }
            }

            return container;
        }

        private static Assembly LoadAssembly(string filePath)
        {
            var a = Assembly.ReflectionOnlyLoadFrom("file:///" + filePath);
            return Assembly.Load(a.GetName());
            //var assemblyName = new AssemblyName
            //{
            //    CodeBase = "file:///" + filePath
            //};
            //return Assembly.Load(assemblyName);
        }

        private static bool TryFindDiBindings(Assembly assembly, out Dictionary<Type, Type> bindings)
        {
            try
            {
                var diBindingsType = assembly.GetTypes().SingleOrDefault(t => t.Name == "DI");
                if (diBindingsType != null)
                {
                    var bindingsField = diBindingsType.GetFields(
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .SingleOrDefault(f => f.FieldType == typeof(Dictionary<Type, Type>));

                    if (bindingsField != null)
                    {
                        bindings = (Dictionary<Type, Type>)bindingsField.GetValue(null);
                        return true;
                    }
                }

                bindings = null;
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not load types from assembly '{assembly.FullName}'.", ex);
            }
        }

        private static IAppServiceIocContainer TryCreateAppIocContainer(
            Assembly hostAssembly, IIocContainerConverter[] converters)
        {
            var converterMap = converters.ToDictionary(c => c.ContainerType);

            foreach (var type in hostAssembly.GetTypes())
            {
                var methods = type.GetMethods(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var methodInfo in methods)
                {
                    Type returnType;
                    try
                    {
                        returnType = methodInfo.ReturnType;
                    }
                    catch
                    {
                        continue;
                    }

                    if (returnType == typeof(IAppServiceIocContainer))
                    {
                        return (IAppServiceIocContainer)methodInfo.Invoke(null, new object[0]);
                    }

                    if (converterMap.TryGetValue(returnType, out var converter))
                    {
                        var container = methodInfo.Invoke(null, new object[0]);
                        return converter.Convert(container) as IAppServiceIocContainer;
                    }
                }
            }

            return null;
        }
    }
}
