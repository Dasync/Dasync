using System;

namespace Dasync.EETypes.Persistence
{
    public class StateNotFoundException : Exception
    {
        public StateNotFoundException() : base("Could not find persisted method state in the storage.") { }

        public StateNotFoundException(ServiceId service, PersistedMethodId method) : this()
        {
            Service = service;
            Method = method;
        }

        public ServiceId Service { get; }

        public PersistedMethodId Method { get; }
    }
}
