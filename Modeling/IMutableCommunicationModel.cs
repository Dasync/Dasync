using System.Collections.Generic;

namespace Dasync.Modeling
{
    public interface IMutableCommunicationModel : ICommunicationModel, IMutablePropertyBag
    {
        new IReadOnlyCollection<IMutableServiceDefinition> Services { get; }
    }
}
