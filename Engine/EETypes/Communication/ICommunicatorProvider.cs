namespace Dasync.EETypes.Communication
{
    public interface ICommunicatorProvider
    {
        ICommunicator GetCommunicator(ServiceId serviceId, MethodId methodId);

        ICommunicator GetCommunicator(ServiceId serviceId, EventId methodId);
    }
}
