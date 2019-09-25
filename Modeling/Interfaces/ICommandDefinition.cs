using System.Reflection;

namespace Dasync.Modeling
{
    public interface ICommandDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }

        MethodInfo MethodInfo { get; }
    }
}
