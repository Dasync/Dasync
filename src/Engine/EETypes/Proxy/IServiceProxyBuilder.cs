namespace Dasync.EETypes.Proxy
{
    public interface IServiceProxyBuilder
    {
        object Build(ServiceId serviceId);
    }

    public interface ISerializedServiceProxyBuilder
    {
        object Build(ServiceId serviceId, string[] additionalInterfaces);
    }
}
