namespace Dasync.AspNetCore.Communication
{
    public static class DasyncHttpCodes
    {
        public static readonly int Succeeded = 200;
        public static readonly int Faulted = 400;
        public static readonly int Canceled = 499; // Client Closed Request (Nginx)

        public static readonly int Scheduled = 202;
    }
}
