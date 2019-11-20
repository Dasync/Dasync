namespace Dasync.Communication.Http
{
    public static class DasyncHttpCodes
    {
        public static readonly int Succeeded = 200;
        public static readonly int Scheduled = 202;
        public static readonly int Deduplicated = 208;
        public static readonly int Running = 304;
        public static readonly int Faulted = 400;
        public static readonly int Canceled = 499;
    }
}
