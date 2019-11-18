namespace Dasync.Persistence.Cassandra
{
    public class ConnectionSettings
    {
        public string Connection { get; set; }

        public string ConnectionString { get; set; }

        public string ContactPoint { get; set; }

        public string[] ContactPoints { get; set; }

        public int? Port { get; set; }

        public bool? Ssl { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string CloudBundlePath { get; set; }

        public string Compression { get; set; }
    }
}
