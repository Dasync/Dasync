using System;
using System.Linq;
using System.Reflection;

namespace Dasync.Modeling
{
    internal class MethodDefinition :
        PropertyBag, IMutablePropertyBag, IPropertyBag,
        IMutableMethodDefinition, IMethodDefinition,
        IMutableCommandDefinition, ICommandDefinition
    {
        private string[] _alternateNames = Array.Empty<string>();
        private MethodInfo[] _interfaceMethods = Array.Empty<MethodInfo>();

        public MethodDefinition(ServiceDefinition serviceDefinition, MethodInfo methodInfo)
        {
            ServiceDefinition = serviceDefinition;
            MethodInfo = methodInfo;
            Name = methodInfo.Name;
        }

        public ServiceDefinition ServiceDefinition { get; }

        public string Name { get; }

        public MethodInfo MethodInfo { get; }

        public bool IsQuery { get; set; }

        public bool IsIgnored { get; set; }

        public MethodInfo[] InterfaceMethods => _interfaceMethods;

        IServiceDefinition IMethodDefinition.Service => ServiceDefinition;

        IServiceDefinition ICommandDefinition.Service => ServiceDefinition;

        IMutableServiceDefinition IMutableMethodDefinition.Service => ServiceDefinition;

        IMutableServiceDefinition IMutableCommandDefinition.Service => ServiceDefinition;

        public string[] AlternateNames => _alternateNames;

        public bool AddAlternateName(string name)
        {
            if (_alternateNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                return false;
            ServiceDefinition.OnMethodAlternateNameAdding(this, name);
            _alternateNames = _alternateNames.Concat(new[] { name }).ToArray();
            return true;
        }

        public void AddInterfaceMethod(MethodInfo methodInfo)
        {
            if (_interfaceMethods.Contains(methodInfo))
                return;
            _interfaceMethods = _interfaceMethods.Concat(new[] { methodInfo }).ToArray();
        }
    }
}
