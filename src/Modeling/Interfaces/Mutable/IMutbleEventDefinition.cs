namespace Dasync.Modeling
{
    public interface IMutableEventDefinition : IEventDefinition, IMutablePropertyBag
    {
        new IMutableServiceDefinition Service { get; }

        /// <summary>
        /// Tells if the event is not a part of a service contract and cannot be subscribed or published.
        /// </summary>
        new bool IsIgnored { get; set; }
    }
}
