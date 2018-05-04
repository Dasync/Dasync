using System;

namespace Dasync.ExecutionEngine.StateMetadata.Service
{
    public class ServiceStateMetadata
    {
        public Type ServiceType { get; private set; }

        public ServiceStateVariable[] Variables { get; private set; }

        public ServiceStateMetadata(Type serviceType, ServiceStateVariable[] variables)
        {
            ServiceType = serviceType;
            Variables = variables;
        }
    }
}
