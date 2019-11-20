using System.Reflection;

namespace Dasync.Modeling
{
    public interface IEventDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }

        string Name { get; }

        EventInfo EventInfo { get; }

        /// <summary>
        /// Mapping of the <see cref="EventInfo"/> to events defined by interface(s) of the service.
        /// Not applicable to <see cref="ServiceType.External"/>.
        /// </summary>
        EventInfo[] InterfaceEvents { get; }

        /// <summary>
        /// Tells if the event is not a part of a service contract and cannot be subscribed or published.
        /// </summary>
        bool IsIgnored { get; }
    }
}
