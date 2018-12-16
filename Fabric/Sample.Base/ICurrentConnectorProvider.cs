namespace Dasync.Fabric.Sample.Base
{
    // Another temporary hack - this entire 'fabric' thing needs to be thrown away
    public interface ICurrentConnectorProvider
    {
        IFabricConnector Connector { get; }
    }
}
