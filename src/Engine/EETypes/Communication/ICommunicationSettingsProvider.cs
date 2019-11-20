using Dasync.Modeling;

namespace Dasync.EETypes.Communication
{
    public interface ICommunicationSettingsProvider
    {
        MethodCommunicationSettings GetServiceMethodSettings(IServiceDefinition serviceDefinition);

        MethodCommunicationSettings GetMethodSettings(IMethodDefinition methodDefinition);

        EventCommunicationSettings GetEventSettings(IEventDefinition eventDefinition, bool external);
    }
}
