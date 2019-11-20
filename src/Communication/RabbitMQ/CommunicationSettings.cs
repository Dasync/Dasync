namespace Dasync.Communication.RabbitMQ
{
    public class CommunicationSettings
    {
        public string QueueName { get; set; }

        public string ExchangeName { get; set; }

        public string Serializer { get; set; }

        public bool Compress { get; set; }
    }
}
