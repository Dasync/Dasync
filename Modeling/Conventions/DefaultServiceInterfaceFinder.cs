using System;

namespace Dasync.Modeling
{
    public class DefaultServiceInterfaceFinder
    {
        public static Type FindDefaultInterface(Type serviceType)
        {
            var generatedServiceNameFromImplementation = DefaultServiceNamer.GetServiceNameFromType(serviceType);

            foreach (var interfaceType in serviceType.GetInterfaces())
            {
                var generatedServiceNameFromInterface = DefaultServiceNamer.GetServiceNameFromType(interfaceType);
                if (generatedServiceNameFromImplementation == generatedServiceNameFromInterface)
                    return interfaceType;
            }

            return null;
        }
    }
}
