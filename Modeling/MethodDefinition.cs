using System;
using System.Linq;
using System.Reflection;

namespace Dasync.Modeling
{
    internal class MethodDefinition :
        PropertyBag, IMutablePropertyBag, IPropertyBag,
        IMutableMethodDefinition, IMethodDefinition,
        IMutableRoutineDefinition, IRoutineDefinition
    {
        private string[] _alternateNames = Array.Empty<string>();
        private MethodInfo[] _interfaceMethods = Array.Empty<MethodInfo>();

        public MethodDefinition(ServiceDefinition serviceDefinition, MethodInfo methodInfo)
        {
            ServiceDefinition = serviceDefinition;
            MethodInfo = methodInfo;
        }

        public ServiceDefinition ServiceDefinition { get; }

        public MethodInfo MethodInfo { get; }

        public bool IsRoutine { get; set; }

        IServiceDefinition IMethodDefinition.Service => ServiceDefinition;

        IServiceDefinition IRoutineDefinition.Service => ServiceDefinition;

        IMutableServiceDefinition IMutableMethodDefinition.Service => ServiceDefinition;

        IMutableServiceDefinition IMutableRoutineDefinition.Service => ServiceDefinition;

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
