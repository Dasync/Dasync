using System.Reflection;

namespace Dasync.Serialization
{
    public class AssemblyResolver : IAssemblyResolver
    {
        public Assembly Resolve(AssemblySerializationInfo info)
        {
#warning Cache results

            var assemblyName = new AssemblyName
            {
                Name = info.Name,
                Version = info.Version
            };

            if (!string.IsNullOrEmpty(info.Token))
            {
                var publicKeyToken = info.Token.ParseAsHexByteArray();
                assemblyName.SetPublicKeyToken(publicKeyToken);
            }

            return Assembly.Load(assemblyName);
        }
    }
}
