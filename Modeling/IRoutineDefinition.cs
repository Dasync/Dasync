using System.Reflection;

namespace Dasync.Modeling
{
    public interface IRoutineDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }

        MethodInfo MethodInfo { get; }
    }
}
