using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    public static class WebJobsHostedServicesExtensions
    {
        public static IServiceCollection AddHostedService<TService>(this IServiceCollection services) where TService : class, IHostedService
        {
            services.AddSingleton<IHostedService, TService>();
            services.EnableRegisteredHostedServices();
            return services;
        }

        /// <summary>
        /// Adds custom implementations of IHostedService to the whitelist of Azure Functions host.
        /// </summary>
        public static void EnableRegisteredHostedServices(this IServiceCollection services)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(
                a => a.GetName().Name == "Microsoft.Azure.WebJobs.Script.WebHost");
            if (assembly == null)
                return;

            var dependencyValidatorType = assembly.GetType("Microsoft.Azure.WebJobs.Script.WebHost.DependencyInjection.DependencyValidator", throwOnError: false, ignoreCase: true);
            if (dependencyValidatorType == null)
                return;

            var expectedDependenciesField = dependencyValidatorType.GetField("_expectedDependencies", BindingFlags.NonPublic | BindingFlags.Static);
            if (expectedDependenciesField == null)
                return;

            var expectedDependencies = expectedDependenciesField.GetValue(null);
            if (expectedDependencies == null)
                return;

            var serviceMatchesField = expectedDependenciesField.FieldType.GetField("_serviceMatches", BindingFlags.NonPublic | BindingFlags.Instance);
            if (serviceMatchesField == null)
                return;

            var serviceMatches = serviceMatchesField.GetValue(expectedDependencies) as IDictionary;
            if (serviceMatches == null)
                return;

            // Microsoft.Azure.WebJobs.Script.WebHost.DependencyInjection.ServiceMatch
            var serviceMatch = serviceMatches[typeof(IHostedService)];
            if (serviceMatch == null)
                return;

            var optionalDescriptorsField = serviceMatch.GetType().GetField("_optionalDescriptors", BindingFlags.NonPublic | BindingFlags.Instance);
            if (optionalDescriptorsField == null)
                return;

            var optionalDescriptors = optionalDescriptorsField.GetValue(serviceMatch) as ICollection<ServiceDescriptor>;
            if (optionalDescriptors == null)
                return;

            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(IHostedService))
                {
                    if (!optionalDescriptors.Contains(descriptor))
                        optionalDescriptors.Add(descriptor);
                }
            }
        }
    }
}
