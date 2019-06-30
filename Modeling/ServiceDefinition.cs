﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.Modeling
{
    internal class ServiceDefinition : PropertyBag, IMutableServiceDefinition, IServiceDefinition, IMutablePropertyBag, IPropertyBag
    {
        private string _name;
        private ServiceType _type;
        private Type _implementation;
        private Type[] _interfaces = new Type[0];
        private string[] _alternativeNames = new string[0];

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

        public string[] AlternativeNames => _alternativeNames;

        public bool AddAlternativeName(string name)
        {
            if (_alternativeNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                return false;
            Model.OnServiceAlternativeNameAdding(this, name);
            _alternativeNames = _alternativeNames.Concat(new[] { name }).ToArray();
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
    }
}
