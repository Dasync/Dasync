using Dasync.Modeling;

namespace Dasync.EETypes.Communication
{
    public interface ICommunicationSettingsProvider
    {
        MethodCommunicationSettings GetMethodSettings(IMethodDefinition methodDefinition);

        EventCommunicationSettings GetEventSettings(IEventDefinition eventDefinition);
    }
}
