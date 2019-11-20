using System.Reflection;

namespace Dasync.Modeling
{
    public interface IQueryDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }

        MethodInfo MethodInfo { get; }
    }
}
