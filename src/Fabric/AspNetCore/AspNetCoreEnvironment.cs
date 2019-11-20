using System;

namespace Dasync.Fabric.AspNetCore
{
    public static class AspNetCoreEnvironment
    {
        public static string Name => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";

        public static bool IsDevelopment => IsEnvironment("Development");

        private static bool IsEnvironment(string name) => string.Equals(name, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
