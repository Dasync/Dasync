namespace Dasync.Modeling
{
    public interface IMutableMethodDefinition : IMethodDefinition, IMutablePropertyBag
    {
        new IMutableServiceDefinition Service { get; }

        /// <summary>
        /// Tells if the method is 'read-only' and does not modify any data.
        /// </summary>
        new bool IsQuery { get; set; }

        /// <summary>
        /// Tells is a method is not a part of a service contract and cannot be invoked.
        /// </summary>
        new bool IsIgnored { get; set; }

        bool AddAlternateName(string name);
    }
}
