using System.Reflection;

namespace Dasync.Modeling
{
    public interface IMethodDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }

        MethodInfo MethodInfo { get; }

        /// <summary>
        /// Tells is a method is part of a service contract and can be executed in a reliable way.
        /// </summary>
        bool IsRoutine { get; }
    }
}
