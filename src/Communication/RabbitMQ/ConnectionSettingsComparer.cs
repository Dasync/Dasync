using System;
using System.Collections.Generic;

namespace Dasync.Communication.RabbitMQ
{
    public class ConnectionSettingsComparer : IEqualityComparer<ConnectionSettings>
    {
        public static IEqualityComparer<ConnectionSettings> Instance = new ConnectionSettingsComparer();

        public bool Equals(ConnectionSettings x, ConnectionSettings y) =>
            string.Equals(x.Connection, y.Connection, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Endpoint, y.Endpoint, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.UserName, y.UserName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Password, y.Password, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.HostName, y.HostName, StringComparison.OrdinalIgnoreCase) &&
            x.Port == y.Port &&
            string.Equals(x.VirtualHost, y.VirtualHost, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(ConnectionSettings x)
        {
            unchecked
            {
                int code = 0;

                if (x.Connection != null)
                    code = code * 104651 + StringComparer.OrdinalIgnoreCase.GetHashCode(x.Connection);

                if (x.Endpoint != null)
                    code = code * 104651 + StringComparer.OrdinalIgnoreCase.GetHashCode(x.Endpoint);

                if (x.UserName != null)
                    code = code * 104651 + x.UserName.GetHashCode();

                if (x.Password != null)
                    code = code * 104651 + x.Password.GetHashCode();

                if (x.HostName != null)
                    code = code * 104651 + StringComparer.OrdinalIgnoreCase.GetHashCode(x.HostName);

                if (x.Port != null)
                    code = code * 104651 + x.Port.Value;

                if (x.VirtualHost != null)
                    code = code * 104651 + StringComparer.OrdinalIgnoreCase.GetHashCode(x.VirtualHost);

                return code;
            }
        }
    }
}
