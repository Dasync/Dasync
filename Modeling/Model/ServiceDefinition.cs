using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dasync.Modeling
{
    internal class ServiceDefinition : PropertyBag, IMutableServiceDefinition, IServiceDefinition, IMutablePropertyBag, IPropertyBag
    {
        private string _name;
        private ServiceType _type;
        private Type _implementation;
        private Type[] _interfaces = Array.Empty<Type>();
        private string[] _alternateNames = Array.Empty<string>();

        private readonly Dictionary<string, MethodDefinition> _methodsByName =
            new Dictionary<string, MethodDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, EventDefinition> _eventsByName =
            new Dictionary<string, EventDefinition>(StringComparer.OrdinalIgnoreCase);

        public ServiceDefinition(CommunicationModel model)
        {
            Model = model;
        }

        public CommunicationModel Model { get; }

        ICommunicationModel IServiceDefinition.Model => Model;

        IMutableCommunicationModel IMutableServiceDefinition.Model => Model;

        public string Name
        {
            get => _name;
            set
            {
                Model.OnServiceNameChanging(this, value);
                _name = value;
            }
        }

        public string[] AlternateNames => _alternateNames;

        public bool AddAlternateName(string name)
        {
            if (_alternateNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                return false;
            Model.OnServiceAlternateNameAdding(this, name);
            _alternateNames = _alternateNames.Concat(new[] { name }).ToArray();
            return true;
        }

        public ServiceType Type
        {
            get => _type;
            set => _type = value;
        }

        public Type[] Interfaces
        {
            get => _interfaces;
        }

        IEnumerable<IMethodDefinition> IServiceDefinition.Methods => _methodsByName.Values;

        IEnumerable<IMutableMethodDefinition> IMutableServiceDefinition.Methods => _methodsByName.Values;

        IEnumerable<IEventDefinition> IServiceDefinition.Events => _eventsByName.Values;

        IEnumerable<IMutableEventDefinition> IMutableServiceDefinition.Events => _eventsByName.Values;

        public bool AddInterface(Type interfaceType)
        {
            if (_interfaces.Contains(interfaceType))
                return false;
            Model.OnServiceInterfaceAdding(this, interfaceType);
            _interfaces = _interfaces.Concat(new[] { interfaceType }).ToArray();
            return true;
        }

        public bool RemoveInterface(Type interfaceType)
        {
            if (!_interfaces.Contains(interfaceType))
                return false;
            _interfaces = _interfaces.Except(new[] { interfaceType }).ToArray();
            Model.OnServiceInterfaceRemoved(this, interfaceType);
            return true;
        }

        public Type Implementation
        {
            get => _implementation;
            set
            {
                Model.OnServiceImplementaionChanging(this, value);
                _implementation = value;
            }
        }

        public IMutableMethodDefinition GetMethod(string name)
        {
            if (_methodsByName.TryGetValue(name, out var methodDefinition))
                return methodDefinition;

            if (this.Implementation != null)
            {
                var methods = this.Implementation
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(mi => mi.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (methods.Count > 1)
                    throw new InvalidOperationException($"The service '{Name}' has overloaded methods with the same name of '{name}'.");

                if (methods.Count == 1)
                {
                    var methodInfo = methods[0];
                    methodDefinition = new MethodDefinition(this, methodInfo);
                    foreach (var interfaceType in Interfaces)
                    {
                        var map = this.Implementation.GetInterfaceMap(interfaceType);
                        for (var i = 0; i < map.TargetMethods.Length; i++)
                        {
                            if (ReferenceEquals(methodInfo, map.TargetMethods[i]))
                            {
                                methodDefinition.AddInterfaceMethod(map.InterfaceMethods[i]);
                                break;
                            }
                        }
                    }
                    methodDefinition.IsQuery = methodInfo.HasQueryImplyingName() && methodInfo.IsQueryCandidate();
                    _methodsByName.Add(methodInfo.Name, methodDefinition);
                    return methodDefinition;
                }
            }

            foreach (var interfaceType in Interfaces)
            {
                var methods = interfaceType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .ToList();

                if (methods.Count > 1)
                    throw new InvalidOperationException($"The service '{Name}' has overloaded methods with the same name of '{name}'.");

                if (methods.Count == 1)
                {
                    var methodInfo = methods[0];
                    methodDefinition = new MethodDefinition(this, methodInfo);
                    methodDefinition.IsQuery = methodInfo.HasQueryImplyingName();
                    _methodsByName.Add(methodInfo.Name, methodDefinition);
                    return methodDefinition;
                }
            }

            throw new ArgumentException($"Could not find method '{name}' in service '{Name}'.");
        }

        public IMethodDefinition FindMethod(string methodName)
        {
            _methodsByName.TryGetValue(methodName, out var methodDefinition);
            return methodDefinition;
        }

        internal void OnMethodAlternateNameAdding(MethodDefinition methodDefinition, string newAltName)
        {
            var existingMethod = FindMethod(newAltName);
            if (existingMethod != null && !ReferenceEquals(existingMethod, methodDefinition))
                throw new InvalidOperationException($"Cannot use the alternate name '{newAltName}' for method '{methodDefinition.MethodInfo.Name}' of service '{methodDefinition.ServiceDefinition.Name}', because the name is already used by another method.");

            _methodsByName.Add(newAltName, methodDefinition);
        }

        public IMutableEventDefinition GetEvent(string name)
        {
            if (_eventsByName.TryGetValue(name, out var eventDefinition))
                return eventDefinition;

            if (this.Implementation != null)
            {
                var events = this.Implementation
                    .GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(mi => mi.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (events.Count > 1)
                    throw new InvalidOperationException($"The service '{Name}' has overloaded events with the same name of '{name}'.");

                if (events.Count == 1)
                {
                    var eventInfo = events[0];
                    eventDefinition = new EventDefinition(this, eventInfo);
                    foreach (var interfaceType in Interfaces)
                    {
                        var interfaceEvent = interfaceType.GetEvent(eventInfo.Name,
                            BindingFlags.Instance | BindingFlags.Public);
                        if (interfaceEvent.EventHandlerType == eventInfo.EventHandlerType)
                            eventDefinition.AddInterfaceEvent(interfaceEvent);
                    }
                    _eventsByName.Add(eventInfo.Name, eventDefinition);
                    return eventDefinition;
                }
            }

            foreach (var interfaceType in Interfaces)
            {
                var events = interfaceType
                    .GetEvents(BindingFlags.Instance | BindingFlags.Public)
                    .ToList();

                if (events.Count > 1)
                    throw new InvalidOperationException($"The service '{Name}' has overloaded events with the same name of '{name}'.");

                if (events.Count == 1)
                {
                    var eventInfo = events[0];
                    eventDefinition = new EventDefinition(this, eventInfo);
                    _eventsByName.Add(eventInfo.Name, eventDefinition);
                    return eventDefinition;
                }
            }

            throw new ArgumentException($"Could not find event '{name}' in service '{Name}'.");
        }

        public IEventDefinition FindEvent(string eventName)
        {
            _eventsByName.TryGetValue(eventName, out var eventDefinition);
            return eventDefinition;
        }
    }
}
