namespace Dasync.EETypes
{
    public struct Route
    {
        public string Service;

        public string Region;

        public string InstanceId;

        public string PartitionKey;

        //public string SequenceKey; // causal order - can be used as a partition key for related events 

        public string ApiVersion;

        // TODO: add routing properties for Edge/IoT/Hybrid solutions, e.g.
        // string DestinationType;
        // string DeviceId;
        // etc.
    }
}
