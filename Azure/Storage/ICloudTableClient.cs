namespace Dasync.AzureStorage
{
    public interface ICloudTableClient
    {
        ICloudTable GetTableReference(string tableName);
    }
}
