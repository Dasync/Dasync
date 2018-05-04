using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.ExecutionEngine.StateMetadata.Service
{
    public interface IServiceStateMetadataProvider
    {
        ServiceStateMetadata GetMetadata(Type serviceType);
    }

    public class ServiceStateMetadataProvider : IServiceStateMetadataProvider
    {
        private readonly Dictionary<Type, ServiceStateMetadata> _metadataMap =
            new Dictionary<Type, ServiceStateMetadata>();

        public ServiceStateMetadata GetMetadata(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            lock (_metadataMap)
            {
                if (!_metadataMap.TryGetValue(serviceType, out var metadata))
                {
                    metadata = Build(serviceType);
                    _metadataMap.Add(serviceType, metadata);
                }
                return metadata;
            }
        }

        private ServiceStateMetadata Build(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            var variables = new List<ServiceStateVariable>();

            var allFields = serviceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var injectedAsDependency = GetDependencyInjectedFields(serviceType, allFields);

            foreach (var fi in allFields)
            {
                if (injectedAsDependency.Contains(fi))
                    continue;

                var variableName = fi.Name;

                if (IsPropertyBackingField(fi))
                    variableName = GetAssociatedPropertyName(fi);

                variables.Add(new ServiceStateVariable(variableName, fi));
            }

            return new ServiceStateMetadata(serviceType, variables.ToArray());
        }

        private static bool IsPropertyBackingField(FieldInfo fieldInfo)
            => fieldInfo.GetCustomAttribute<CompilerGeneratedAttribute>() != null;

        private static string GetAssociatedPropertyName(FieldInfo fieldInfo)
        {
            int endIndex;

            if (fieldInfo.Name[0] != '<' || (endIndex = fieldInfo.Name.IndexOf('>')) <= 1)
                throw new InvalidOperationException(
                    $"The given field '{fieldInfo.Name}' of type '{fieldInfo.DeclaringType.FullName}' does not have a name indicating that it backs a property");

            var propertyName = fieldInfo.Name.Substring(startIndex: 1, length: endIndex - 1);
            return propertyName.Intern();
        }

        private HashSet<FieldInfo> GetDependencyInjectedFields(Type declaringType, FieldInfo[] allFields)
        {
#warning From constructor(s) get all interface types? and find mathing fields?

            var allCtorInterfaces = new HashSet<Type>(
                declaringType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(ctor =>
                    ctor
                    .GetParameters()
                    .Where(pi => pi.ParameterType.IsInterface())
                    .Select(pi => pi.ParameterType)));

            return new HashSet<FieldInfo>(allFields.Where(fi => allCtorInterfaces.Contains(fi.FieldType)));
        }
    }
}
