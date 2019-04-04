using System;
using Microsoft.Extensions.Configuration;

namespace Dasync.AspNetCore
{
    public static class ConfigurationExtensions
    {
        private static string GetEnvironmentName(this IConfiguration configuration)
            => configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");

        public static bool IsEnvironment(this IConfiguration configuration, string environmentName)
            => string.Equals(configuration.GetEnvironmentName(), environmentName, StringComparison.OrdinalIgnoreCase);

        public static bool IsProduction(this IConfiguration configuration) => configuration.IsEnvironment("Production");

        public static bool IsStaging(this IConfiguration configuration) => configuration.IsEnvironment("Staging");

        public static bool IsDevelopment(this IConfiguration configuration) => configuration.IsEnvironment("Development");
    }
}
