using Dasync.Modeling;

namespace Dasync.EETypes.Resolvers
{
    public interface IServiceReference
    {
        ServiceId Id { get; }

        IServiceDefinition Definition { get; }

        object GetInstance();
    }
}
