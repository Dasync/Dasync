using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Hosting;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal class MiddlewareSupportService : IHostedService
    {
        public MiddlewareSupportService(
            IServiceProvider serviceProvider,
            RequestDelegateHolder holder)
        {
            holder.RequestDelegate = BuildRequestDelegate(serviceProvider);
        }

        private RequestDelegate BuildRequestDelegate(IServiceProvider serviceProvider)
        {
            var applicationBuilder = new ApplicationBuilder(serviceProvider);
            Configure(applicationBuilder, serviceProvider);
            return applicationBuilder.Build();
        }

        private void Configure(IApplicationBuilder applicationBuilder, IServiceProvider serviceProvider)
        {
            var configureMethods = new List<(MethodInfo method, object instance, int position)>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var attr in assembly.GetCustomAttributes(typeof(WebJobsStartupAttribute), inherit: false))
                {
                    var startupType = ((WebJobsStartupAttribute)attr).WebJobsStartupType;

                    var configureMethod = startupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                        .SingleOrDefault(mi =>
                            mi.ReturnType == typeof(void) &&
                            mi.GetParameters().Any(p => p.ParameterType == typeof(IApplicationBuilder)));

                    if (configureMethod == null)
                        continue;

                    object instance = null;
                    if (!configureMethod.IsStatic)
                        instance = Activator.CreateInstance(startupType);

                    configureMethods.Add((configureMethod, instance, GetInvocationPosition(configureMethod)));
                }
            }

            configureMethods.Sort((x, y) => x.position.CompareTo(y.position));

            foreach (var (configureMethod, instance, _) in configureMethods)
            {
                var parameterDefinitions = configureMethod.GetParameters();
                var parameterValues = new object[parameterDefinitions.Length];
                for (var i = 0; i < parameterDefinitions.Length; i++)
                {
                    var p = parameterDefinitions[i];
                    parameterValues[i] =
                        (p.ParameterType == typeof(IApplicationBuilder))
                        ? applicationBuilder
                        : serviceProvider.GetService(p.ParameterType);
                }

                configureMethod.Invoke(instance, parameterValues);
            }
        }

        private static int GetInvocationPosition(MethodInfo methodInfo)
        {
            var ambientValueAttribute = methodInfo.GetCustomAttribute<AmbientValueAttribute>();
            if (ambientValueAttribute != null)
                return Convert.ToInt32(ambientValueAttribute.Value);

            var defaultValueAttribute = methodInfo.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValueAttribute != null)
                return Convert.ToInt32(defaultValueAttribute.Value);

            return int.MaxValue;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
