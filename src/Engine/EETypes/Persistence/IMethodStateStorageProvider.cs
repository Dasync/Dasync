namespace Dasync.EETypes.Persistence
{
    public interface IMethodStateStorageProvider
    {
        IMethodStateStorage GetStorage(ServiceId serviceId, MethodId methodId, bool returnNullIfNotFound = false);
    }
}
