using Dasync.Modeling;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Resolvers
{
    public interface IEventReference
    {
        EventId Id { get; }

        IEventDefinition Definition { get; }

        IValueContainer CreateParametersContainer();
    }
}
