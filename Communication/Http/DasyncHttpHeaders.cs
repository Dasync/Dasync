namespace Dasync.Communication.Http
{
    public static class DasyncHttpHeaders
    {
        public static readonly string PoweredBy = "X-Powered-By";

        public static readonly string RouteService = "X-Route-Service";
        public static readonly string RouteRegion = "X-Route-Region";
        public static readonly string RouteInstanceId = "X-Route-Instance-Id";
        public static readonly string RoutePartitionKey = "X-Route-Partition-Key";
        public static readonly string RouteSequenceKey = "X-Route-Sequence-Key";
        public static readonly string RouteApiVersion = "X-Route-Api-Version";

        public static readonly string ReplyService = "X-Reply-Service";
        public static readonly string ReplyRegion = "X-Reply-Region";
        public static readonly string ReplyInstanceId = "X-Reply-Instance-Id";
        public static readonly string ReplyPartitionKey = "X-Reply-Partition-Key";
        public static readonly string ReplySequenceKey = "X-Reply-Sequence-Key";
        public static readonly string ReplyApiVersion = "X-Reply-Api-Version";

        public static readonly string CallerIntentId = "X-Caller-Intent-ID";
        public static readonly string CallerServiceName = "X-Caller-Service-Name";
        public static readonly string CallerServiceProxy = "X-Caller-Service-Proxy";
        public static readonly string CallerMethodName = "X-Caller-Method-Name";
        public static readonly string CallerEventName = "X-Caller-Event-Name";

        public static readonly string RequestId = "X-Request-ID";
        public static readonly string CorrelationId = "X-Correlation-ID";

        public static readonly string Context = "X-Context";

        public static readonly string IntentId = "X-Intent-ID";
        public static readonly string IntentType = "X-Intent-Type";
        public static readonly string Retry = "X-Retry";

        public static readonly string Envelope = "X-Envelope";

        public static readonly string TaskResult = "X-Task-Result";
    }
}
