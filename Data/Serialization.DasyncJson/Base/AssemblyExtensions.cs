using System.Reflection;

namespace Dasync.Serialization
{
    public static class AssemblyExtensions
    {
        public static AssemblySerializationInfo ToAssemblySerializationInfo(this Assembly assembly)
        {
#warning pre-cache
            return new AssemblySerializationInfo
            {
                Name = assembly.GetName().Name,
                Version = assembly.GetName().Version,
                Token = assembly.GetName().GetPublicKeyToken()?.ToHexString()
            };
        }
    }
}
