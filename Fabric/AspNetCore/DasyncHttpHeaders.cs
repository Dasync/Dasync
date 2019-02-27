namespace DasyncAspNetCore
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
    }
}
