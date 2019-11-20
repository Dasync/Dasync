using System;
using System.Reflection;
using Dasync.EETypes;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Modeling
{
    public interface IExternalMethodDefinition : IMethodDefinition
    {
        MethodId Id { get; }

        new IExternalServiceDefinition Service { get; }
    }

    public class ExternalMethodDefinition : PropertyBag, IExternalMethodDefinition, IMutableMethodDefinition
    {
        public ExternalMethodDefinition(IExternalServiceDefinition service, MethodId methodId)
        {
            Id = methodId.CopyTo(new MethodId());
            Service = service;
        }

        public MethodId Id { get; }

        public IExternalServiceDefinition Service { get; }

        IServiceDefinition IMethodDefinition.Service => Service;

        IMutableServiceDefinition IMutableMethodDefinition.Service => Service as IMutableServiceDefinition;

        public string Name => Id.Name;

        public bool AddAlternateName(string name) => false;

        public MethodInfo MethodInfo => null;

        public MethodInfo[] InterfaceMethods { get; } = Array.Empty<MethodInfo>();

        public bool IsQuery { get; set; }

        public bool IsIgnored { get; set; }
    }
}
