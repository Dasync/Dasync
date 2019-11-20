namespace Dasync.Communication.Http
{
    public class HttpCommunicatorSettings
    {
        public bool? Https { get; set; }

        public string Host { get; set; }

        public int? Port { get; set; }

        public string Address { get; set; }

        public string ApiSegment { get; set; }

        public string MethodPath { get; set; }

        public string Path { get; set; }

        public string Url { get; set; }

        public string Serializer { get; set; }

        public bool? Compress { get; set; }
    }
}
