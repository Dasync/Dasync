namespace Dasync.Modeling
{
    public interface IMutableMethodDefinition : IMethodDefinition, IMutablePropertyBag
    {
        new IMutableServiceDefinition Service { get; }

        /// <summary>
        /// Tells is a method is part of a service contract and can be executed in a reliable way.
        /// </summary>
        new bool IsRoutine { get; set; }

        /// <summary>
        /// Tells if the method is 'read-only' and does not modify any data.
        /// </summary>
        new bool IsQuery { get; set; }
    }
}
