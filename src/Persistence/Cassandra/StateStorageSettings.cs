using System;

namespace Dasync.Persistence.Cassandra
{
    public class StateStorageSettings
    {
        public string Serializer { get; set; }

        public string Keyspace { get; set; }

        public string TableName { get; set; }

        public TimeSpan? ResultTTL { get; set; }
    }
}
