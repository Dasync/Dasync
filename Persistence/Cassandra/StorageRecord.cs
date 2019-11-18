using System;

namespace Dasync.Persistence.Cassandra
{
    public class StorageRecord
    {
        public string service { get; set; }

        public string method { get; set; }

        public string intent_id { get; set; }

        public long? etag { get; set; }

        public int? status { get; set; }

        public int? outcome { get; set; }

        public string last_intent_id { get; set; }

        public DateTimeOffset? invoked_at { get; set; }

        public DateTimeOffset? started_at { get; set; }

        public DateTimeOffset? updated_at { get; set; }

        public TimeSpan? duration { get; set; }

        public DateTimeOffset? cancel_at { get; set; }

        public string cancellation_id { get; set; }

        public int? transition_count { get; set; }

        public string caller_service { get; set; }

        public string caller_proxy { get; set; }

        public string caller_method { get; set; }

        public string caller_event { get; set; }

        public string caller_intent_id { get; set; }

        public string format { get; set; }

        public byte[] execution_state { get; set; }

        public byte[] method_state { get; set; }

        public byte[] flow_context { get; set; }

        public byte[] task_result { get; set; }

        public byte[] result { get; set; }

        public byte[] error { get; set; }

        public byte[] continuation { get; set; }

        public byte[] continuation_state { get; set; }
    }
}
