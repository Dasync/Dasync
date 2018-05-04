using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dasync.Ioc;

[assembly: AssemblyCompany("D-ASYNC LLC")]
[assembly: AssemblyCopyright("Copyright © 2018")]
[assembly: AssemblyTitle("GenerateFunctions")]
[assembly: AssemblyProduct("GenerateFunctions")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]

namespace GenerateFunctions
{
    class Program
    {
        static void Main(string[] args)
        {
#warning need to log into a file, not to the console

            var targetPath = args[0];
            var targetDir = Path.GetDirectoryName(targetPath);

            var targetRelativePath = string.Concat("..", "/",
                Path.GetFileName(targetDir), "/",
                Path.GetFileName(targetPath));

            AppDomain.CurrentDomain.SetupInformation.PrivateBinPath = targetDir;
            AppDomain.CurrentDomain.SetupInformation.PrivateBinPathProbe = targetDir;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                var name = new AssemblyName(eventArgs.Name);
                var filePath = Path.Combine(targetDir, name.Name + ".dll");
                if (File.Exists(filePath))
                {
                    var assembly = LoadAssembly(filePath);
                    return assembly;
                }

                return null;
            };

            var hostAssembly = LoadAssembly(targetPath);

            var dasyncIocContainer = LoadDasyncIocModules(targetDir);
            var converters = dasyncIocContainer.ResolveAll<IIocContainerConverter>();

            var appIocContainer = TryCreateAppIocContainer(hostAssembly, converters);
            if (appIocContainer == null)
            {
                Console.WriteLine("No App IoC container found.");
                return;
            }

            var serviceNames = new List<string>();

            foreach (var serviceBinding in appIocContainer.DiscoverServices())
            {
                if (serviceBinding.IsExternal)
                    continue;

                var serviceName = GetSericeName(serviceBinding.ServiceType);
                Console.WriteLine($"Found service: {serviceName}");

                var functionDir = Path.GetFullPath(Path.Combine(targetDir, "..", serviceName));
                if (!Directory.Exists(functionDir))
                    Directory.CreateDirectory(functionDir);

                var functionJson = GetFunctionBindingsJson(serviceName);
                var functionJsonFilePath = Path.Combine(functionDir, "function.json");
                File.WriteAllText(functionJsonFilePath, functionJson);
                WriteStartupFile(functionDir, targetRelativePath, new[] { serviceName });
                Console.WriteLine($"Added function: {functionJsonFilePath}");

                serviceNames.Add(serviceName);
            }

            var addHttpGatewayFunction = true;
            if (addHttpGatewayFunction)
            {
                var httpGatewayFunctionName = "gateway";

                var functionDir = Path.GetFullPath(Path.Combine(targetDir, "..", httpGatewayFunctionName));
                if (!Directory.Exists(functionDir))
                    Directory.CreateDirectory(functionDir);

                var functionJson = GetHttpGatewayFunctionBindingsJson();
                var functionJsonFilePath = Path.Combine(functionDir, "function.json");
                File.WriteAllText(functionJsonFilePath, functionJson);
                WriteStartupFile(functionDir, targetRelativePath, serviceNames);
                Console.WriteLine($"Added the HTTP '{httpGatewayFunctionName}' function: {functionJsonFilePath}");
            }
        }

#warning Same logic is in ServiceRegistry class
        private static string GetSericeName(Type serviceType)
        {
            if (serviceType.IsInterface &&
                serviceType.Name.Length >= 2 &&
                serviceType.Name[0] == 'I' &&
                char.IsUpper(serviceType.Name[1]))
            {
                return serviceType.Name.Substring(1);
            }
            else
            {
                return serviceType.Name;
            }
        }

        private static void WriteStartupFile(
            string functionDir,
            string startupFileRelativePath,
            IEnumerable<string> serviceNames)
        {
            var startupFilePath = Path.Combine(functionDir, "dasync.startup.json");
            var json =
@"{
  ""startupFile"": """ + startupFileRelativePath + @""",
  ""serviceNames"": [ " + string.Join(", ", serviceNames.Select(n => string.Concat("\"", n, "\""))) + @" ]
}";
            File.WriteAllText(startupFilePath, json);
        }

        private static string GetFunctionBindingsJson(string serviceName)
        {
            return
@"{
  ""bindings"": [
    {
      ""type"": ""queueTrigger"",
      ""queueName"": """ + GetQueueName(serviceName) + @""",
      ""direction"": ""in"",
      ""name"": ""content""
    }
  ],
  ""disabled"": false,
  ""scriptFile"": """ + @"../bin/Dasync.Fabric.AzureFunctions.dll" + @""",
  ""entryPoint"": """ + @"Dasync.Fabric.AzureFunctions.EntryPoints.QueueTrigger.RunAsync" + @"""
}";
        }

        private static string GetQueueName(string serviceName)
        {
#warning make sure that the name is alpha-numeric and does not start with a number
            return serviceName.Replace(".", "").ToLower();
        }

        private static string GetHttpGatewayFunctionBindingsJson()
        {
            return
@"{
  ""bindings"": [
    {
      ""authLevel"": ""anonymous"",
      ""name"": ""request"",
      ""type"": ""httpTrigger"",
      ""direction"": ""in"",
      ""methods"": [
        ""get"",
        ""post"",
        ""delete"",
        ""head"",
        ""patch"",
        ""put"",
        ""options""
      ]
    },
    {
      ""name"": ""$return"",
      ""type"": ""http"",
      ""direction"": ""out""
    }
  ],
  ""disabled"": false,
  ""scriptFile"": """ + @"../bin/Dasync.Fabric.AzureFunctions.dll" + @""",
  ""entryPoint"": """ + @"Dasync.Fabric.AzureFunctions.EntryPoints.HttpTrigger.RunAsync" + @"""
}";
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

        private static BasicIocContainer LoadDasyncIocModules(string directory)
        {
            var iocContainer = new BasicIocContainer();

            var dllFiles = Directory.GetFiles(directory, "*dasync*ioc*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dllFilePath in dllFiles)
            {
                Assembly assembly;
                try
                {
                    assembly = LoadAssembly(dllFilePath);
                }
                catch
                {
                    continue;
                }

                if (TryFindDiBindings(assembly, out var bindings))
                {
                    iocContainer.Load(bindings);
                    Console.WriteLine($"Loaded DI bindings for {assembly.FullName}");
                }
            }

            return iocContainer;
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
