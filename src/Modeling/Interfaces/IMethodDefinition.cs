using System.Collections.Generic;
using System.Reflection;

namespace Dasync.Modeling
{
    public interface IMethodDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }

        string Name { get; }

        MethodInfo MethodInfo { get; }

        /// <summary>
        /// Mapping of the <see cref="MethodInfo"/> to methods defined by interface(s) of the service.
        /// Not applicable to <see cref="ServiceType.External"/>.
        /// </summary>
        MethodInfo[] InterfaceMethods { get; }

        /// <summary>
        /// Tells if the method is 'read-only' and does not modify any data.
        /// </summary>
        bool IsQuery { get; }

        /// <summary>
        /// Tells if the method is not a part of a service contract and cannot be invoked.
        /// </summary>
        bool IsIgnored { get; }
    }
}
