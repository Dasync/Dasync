using System;
using System.Collections.Generic;
using System.Linq;
using Cassandra;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;

namespace Dasync.Persistence.Cassandra
{
    public class CassandraPersistenceMethod : IPersistenceMethod
    {
        public const string MethodType = "Cassandra";

        private readonly ISerializer _defaultSerializer;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IConfiguration _configuration;

        public CassandraPersistenceMethod(
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializerProvider,
            IEnumerable<IConfiguration> safeConfiguration)
        {
            _defaultSerializer = defaultSerializerProvider.DefaultSerializer;
            _serializerProvider = serializerProvider;
            _configuration = safeConfiguration.FirstOrDefault();
        }

        public string Type => MethodType;

        public IMethodStateStorage CreateMethodStateStorage(IConfiguration configuration)
        {
            var connectionSettings = new ConnectionSettings();
            configuration.Bind(connectionSettings);

            var storageSettings = CreateDefaultStorageSettings();
            configuration.Bind(storageSettings);

            var builder = Cluster.Builder();
            ApplySettings(builder, connectionSettings);

            ICluster cluster = builder.Build();

            var connection = cluster.Connect();

            return new CassandraStateStorage(
                storageSettings,
                cluster,
                _defaultSerializer,
                _serializerProvider);
        }

        private void ApplySettings(Builder builder, ConnectionSettings settings)
        {
            var connectionString = settings.ConnectionString;
            if (!string.IsNullOrWhiteSpace(settings.Connection))
                connectionString = _configuration?.GetSection(settings.Connection).Value;

            if (!string.IsNullOrEmpty(connectionString))
                builder.WithConnectionString(connectionString);

            if (!string.IsNullOrEmpty(settings.CloudBundlePath))
                builder.WithCloudSecureConnectionBundle(settings.CloudBundlePath);

            if (!string.IsNullOrEmpty(settings.ContactPoint))
                builder.AddContactPoint(settings.ContactPoint);

            if (settings.ContactPoints?.Length > 0)
                builder.AddContactPoints(settings.ContactPoints);

            if (!string.IsNullOrEmpty(settings.UserName) && settings.Password != null)
                builder.WithCredentials(settings.UserName, settings.Password);

            if (settings.Port.HasValue)
                builder.WithPort(settings.Port.Value);

            if (settings.Ssl == true)
                builder.WithSSL();

            if (!string.IsNullOrEmpty(settings.Compression))
            {
                if ("none".Equals(settings.Compression, StringComparison.OrdinalIgnoreCase))
                {
                    builder.WithCompression(CompressionType.NoCompression);
                }
                else if ("Snappy".Equals(settings.Compression, StringComparison.OrdinalIgnoreCase))
                {
                    builder.WithCompression(CompressionType.Snappy);
                }
                else if ("LZ4".Equals(settings.Compression, StringComparison.OrdinalIgnoreCase))
                {
                    builder.WithCompression(CompressionType.LZ4);
                }
            }
        }

        public static StateStorageSettings CreateDefaultStorageSettings()
        {
            return new StateStorageSettings
            {
                Keyspace = "dasync",
                TableName = "state_and_results",
                ResultTTL = TimeSpan.FromDays(7)
            };
        }
    }
}
