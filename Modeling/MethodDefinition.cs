using System.Reflection;

namespace Dasync.Modeling
{
    internal class MethodDefinition :
        PropertyBag, IMutablePropertyBag, IPropertyBag,
        IMutableMethodDefinition, IMethodDefinition,
        IMutableRoutineDefinition, IRoutineDefinition
    {
        public MethodDefinition(ServiceDefinition serviceDefinition, MethodInfo methodInfo)
        {
            ServiceDefinition = serviceDefinition;
            MethodInfo = methodInfo;
        }

        public ServiceDefinition ServiceDefinition { get; }

        public MethodInfo MethodInfo { get; }

        IServiceDefinition IMethodDefinition.Service => ServiceDefinition;

        IServiceDefinition IRoutineDefinition.Service => ServiceDefinition;

        IMutableServiceDefinition IMutableMethodDefinition.Service => ServiceDefinition;

        IMutableServiceDefinition IMutableRoutineDefinition.Service => ServiceDefinition;
    }
}
