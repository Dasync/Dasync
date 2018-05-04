using System;

namespace Dasync.Proxy
{
    /// <summary>
    /// A dynamically built proxy type (see <see cref="IProxyTypeBuilder"/>).
    /// </summary>
    public interface IProxy
    {
        /// <summary>
        /// The base class the proxy is built on top of, or NULL
        /// if a proxy type does not derive from any base class
        /// (see <see cref="IProxyTypeBuilder.Build(Type)"/>).
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// The executor of interface or overriden virtual methods.
        /// Must not be set to NULL - there is no safety check.
        /// </summary>
        IProxyMethodExecutor Executor { get; set; }

        /// <summary>
        /// Any user-data associated with this proxy instance.
        /// </summary>
        object Context { get; set; }
    }
}
