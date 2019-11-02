using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.EETypes;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Modeling
{
    public interface IExternalServiceDefinition : IServiceDefinition
    {
        ServiceId Id { get; }

        new IExternalCommunicationModel Model { get; }

        IExternalMethodDefinition GetOrAddMethod(MethodId methodId);
    }

    public class ExternalServiceDefinition : PropertyBag, IExternalServiceDefinition, IMutableServiceDefinition
    {
        private readonly ICommunicationModelEnricher _communicationModelEnricher;
        private readonly List<ExternalMethodDefinition> _methods = new List<ExternalMethodDefinition>();
        private readonly List<IEventDefinition> _events = new List<IEventDefinition>();

        public ExternalServiceDefinition(
            ICommunicationModelEnricher communicationModelEnricher,
            IExternalCommunicationModel model,
            ServiceId serviceId)
        {
            _communicationModelEnricher = communicationModelEnricher;
            Model = model;
            Id = serviceId.Clone();
        }

        public ServiceId Id { get; }

        public IExternalCommunicationModel Model { get; }

        public string Name
        {
            get
            {
                return Id.Proxy ?? Id.Name;
            }
            set
            {
            }
        }

        public string[] AlternateNames { get; } = Array.Empty<string>();

        public bool AddAlternateName(string name) => false;

        public ServiceType Type { get; set; } = ServiceType.External;

        public Type[] Interfaces => Array.Empty<Type>();

        public bool AddInterface(Type interfaceType) => false;

        public bool RemoveInterface(Type interfaceType) => false;

        public Type Implementation { get; set; }

        ICommunicationModel IServiceDefinition.Model => Model;

        IMutableCommunicationModel IMutableServiceDefinition.Model => Model as IMutableCommunicationModel;

        public IEnumerable<IMethodDefinition> Methods => _methods;

        IEnumerable<IMutableMethodDefinition> IMutableServiceDefinition.Methods => _methods;

        public IEnumerable<IEventDefinition> Events => _events;

        IEnumerable<IMutableEventDefinition> IMutableServiceDefinition.Events => _events.Cast<IMutableEventDefinition>();

        public IMutableMethodDefinition GetMethod(string name) => FindMethod(name) as IMutableMethodDefinition;

        public IMethodDefinition FindMethod(string methodName)
        {
            lock (_methods)
            {
                return _methods.FirstOrDefault(s => string.Equals(s.Name, methodName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public IMethodDefinition FindMethod(MethodInfo methodInfo) => null;

        public IMutableEventDefinition GetEvent(string name) => FindEvent(name) as IMutableEventDefinition;

        public IEventDefinition FindEvent(string eventName)
        {
            lock (_events)
            {
                return _events.FirstOrDefault(s => string.Equals(s.Name, eventName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public IEventDefinition FindEvent(EventInfo eventInfo) => null;

        public IExternalMethodDefinition GetOrAddMethod(MethodId methodId)
        {
            lock (_methods)
            {
                var existingDefinition = FindMethod(methodId.Name);
                if (existingDefinition != null)
                    return (IExternalMethodDefinition)existingDefinition;

                var newDefinition = new ExternalMethodDefinition(this, methodId);
                _communicationModelEnricher.Enrich(newDefinition);
                _methods.Add(newDefinition);
                return newDefinition;
            }
        }
    }
}
