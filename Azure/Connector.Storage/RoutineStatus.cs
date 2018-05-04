namespace Dasync.FabricConnector.AzureStorage
{
    public enum RoutineStatus
    {
        Allocated = 0,
        Scheduled = 1,
        Transitioning = 2,
        Awaiting = 3,
        Complete = 4
    }
}
