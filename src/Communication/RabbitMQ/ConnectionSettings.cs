namespace Dasync.Communication.RabbitMQ
{
    public class ConnectionSettings
    {
        public string Connection { get; set; }

        public string Endpoint { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string HostName { get; set; }

        public int? Port { get; set; }

        public string VirtualHost { get; set; }
    }
}
