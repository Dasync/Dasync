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
        private string[] _alternativeNames = Array.Empty<string>();

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

        public string[] AlternativeNames => _alternativeNames;

        public bool AddAlternativeName(string name)
        {
            if (_alternativeNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                return false;
            ServiceDefinition.OnMethodAlternativeNameAdding(this, name);
            _alternativeNames = _alternativeNames.Concat(new[] { name }).ToArray();
            return true;
        }
    }
}
