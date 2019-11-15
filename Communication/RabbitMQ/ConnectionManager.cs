using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Dasync.Communication.RabbitMQ
{
    public interface IConnectionManager
    {
        IConnection GetConnection(ConnectionSettings settings);
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<ConnectionSettings, IConnection> _connections =
            new Dictionary<ConnectionSettings, IConnection>(ConnectionSettingsComparer.Instance);

        public ConnectionManager(IEnumerable<IConfiguration> safeConfiguration)
        {
            _configuration = safeConfiguration.FirstOrDefault();
        }

        public IConnection GetConnection(ConnectionSettings settings)
        {
            IConnection connection;

            lock (_connections)
            {
                if (_connections.TryGetValue(settings, out connection))
                    return connection;
            }

            var connectionFactory = new ConnectionFactory();
            ApplySettings(connectionFactory, settings);
            connection = connectionFactory.CreateConnection();

            lock (_connections)
            {
                if (_connections.TryGetValue(settings, out var cachedConnection))
                {
                    connection.Close();
                    connection.Dispose();
                    return cachedConnection;
                }
                _connections[settings] = connection;
                return connection;
            }
        }

        private void ApplySettings(ConnectionFactory connectionFactory, ConnectionSettings settings)
        {
            var endpoint = settings.Endpoint;
            if (!string.IsNullOrWhiteSpace(settings.Connection))
                endpoint = _configuration?.GetSection(settings.Connection).Value;

            if (!string.IsNullOrWhiteSpace(endpoint))
                connectionFactory.Endpoint = AmqpTcpEndpoint.Parse(endpoint);

            if (settings.UserName != null)
                connectionFactory.UserName = settings.UserName;

            if (settings.Password != null)
                connectionFactory.Password = settings.Password;

            if (settings.HostName != null)
                connectionFactory.HostName = settings.HostName;

            if (settings.Port.HasValue)
                connectionFactory.Port = settings.Port.Value;

            if (settings.VirtualHost != null)
                connectionFactory.VirtualHost = settings.VirtualHost;
        }
    }
}
