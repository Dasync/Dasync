using System;
using Dasync.Modeling;

namespace Dasync.AspNetCore.Platform
{
    internal class UnknownExternalServiceDefinition : PropertyBag, IServiceDefinition
    {
        public UnknownExternalServiceDefinition(string name) : this(name, null) { }

        public UnknownExternalServiceDefinition(string name, ICommunicationModel model)
        {
            Name = name;
            Model = model;
        }

        public ICommunicationModel Model { get; }

        public string Name { get; }

        public ServiceType Type => ServiceType.External;

        public Type[] Interfaces => null;

        public Type Implementation => null;
    }
}
