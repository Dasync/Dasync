using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using RabbitMQ.Client;

namespace Dasync.Communication.RabbitMQ
{
    public class RabbitMQMessageListener : IMessageListener
    {
        private IModel _channel;
        private string _consumerTag;

        public RabbitMQMessageListener(IModel channel, string consumerTag)
        {
            _channel = channel;
            _consumerTag = consumerTag;
        }

        public Task StopAsync(CancellationToken ct)
        {
            if (_consumerTag != null)
            {
                _channel.BasicCancel(_consumerTag);
                _consumerTag = null;
                _channel = null;
            }
            return Task.CompletedTask;
        }
    }
}
