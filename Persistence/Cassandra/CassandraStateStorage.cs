using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Persistence.Cassandra
{
    public class CassandraStateStorage : IMethodStateStorage, IDisposable
    {
        private StateStorageSettings _settings;
        private ICluster _cluster;
        private ISession _session;
        private ISerializer _serializer;
        private ISerializerProvider _serializerProvider;

        public CassandraStateStorage(
            StateStorageSettings settings,
            ICluster cluster,
            ISerializer defaultSerializer,
            ISerializerProvider serializerProvider)
        {
            _settings = settings;
            _cluster = cluster;
            _serializerProvider = serializerProvider;

            _serializer =
                !string.IsNullOrEmpty(settings.Serializer)
                ? serializerProvider.GetSerializer(settings.Serializer)
                : defaultSerializer;
        }

        public async Task<string> WriteStateAsync(ServiceId serviceId, PersistedMethodId methodId, MethodExecutionState state)
        {
            var tableName = GetTableQualifiedName(serviceId, methodId);

            var key = new StorageRecord
            {
                service = serviceId.Name,
                method = methodId.Name,
                intent_id = methodId.IntentId
            };

            var record = new StorageRecord
            {
                etag = DateTimeOffset.UtcNow.Ticks,
                status = (int)Statuses.Paused,
                updated_at = DateTimeOffset.Now,
                caller_service = state.Caller?.Service?.Name,
                caller_proxy = state.Caller?.Service?.Proxy,
                caller_method = state.Caller?.Method?.Name,
                caller_event = state.Caller?.Event?.Name,
                caller_intent_id = state.Caller?.IntentId,
                format = _serializer.Format,
                execution_state = _serializer.SerializeToBytes(state),
            };

            var query = new StringBuilder("UPDATE ").Append(tableName);

            query.Append(" SET ");
            WriteValues(query, record, delimiter: ", ");
            query.Append(" WHERE ");
            WriteValues(query, key, delimiter: " AND ");

            if (!string.IsNullOrEmpty(methodId.ETag))
                query.Append(" IF etag = ").AppendCqlText(methodId.ETag);

            var result = await ExecuteQueryAsync(serviceId, methodId, query.ToString());

            if (result != null)
            {
                var row = result.FirstOrDefault();
                if (row != null && !row.GetValue<bool>("[applied]"))
                {
                    var actualETag = row.GetValueOrDefault<long>("etag");
                    throw new ETagMismatchException(methodId.ETag, actualETag.ToString());
                }
            }

            return record.etag.ToString();
        }

        public async Task<MethodExecutionState> ReadStateAsync(ServiceId serviceId, PersistedMethodId methodId, CancellationToken ct)
        {
            var tableName = GetTableQualifiedName(serviceId, methodId);

            var key = new StorageRecord
            {
                service = serviceId.Name,
                method = methodId.Name,
                intent_id = methodId.IntentId
            };

            var query = new StringBuilder("SELECT * FROM ")
                .Append(tableName).Append(" WHERE ");
            WriteValues(query, key, delimiter: " AND ");

            var result = await ExecuteQueryAsync(serviceId, methodId, query.ToString());
            var row = result.FirstOrDefault();

            if (row == null)
                throw new StateNotFoundException(serviceId, methodId);

            var record = ReadValues(row);

            if (record.status.HasValue && record.status.Value != (int)Statuses.Paused)
                throw new InvalidOperationException("Method is not paused");

            if (!(record.execution_state?.Length > 0))
                throw new InvalidOperationException("Empty state data");

            var serializer = !string.IsNullOrEmpty(record.format)
                ? _serializerProvider.GetSerializer(record.format)
                : _serializer;

            return serializer.Deserialize<MethodExecutionState>(record.execution_state);
        }

        public async Task WriteResultAsync(ServiceId serviceId, MethodId methodId, string intentId, ITaskResult result)
        {
            var tableName = GetTableQualifiedName(serviceId, methodId);

            var key = new StorageRecord
            {
                service = serviceId.Name,
                method = methodId.Name,
                intent_id = intentId
            };

            var record = new StorageRecord
            {
                etag = DateTimeOffset.UtcNow.Ticks,
                status = (int)Statuses.Complete,
                outcome = (int)(result.IsCanceled ? Outcomes.Canceled : (result.IsFaulted() ? Outcomes.Failed : Outcomes.Succeeded)),
                updated_at = DateTimeOffset.Now,
                format = _serializer.Format,
                task_result = _serializer.SerializeToBytes(result)
            };

            var query = new StringBuilder("UPDATE ").Append(tableName);

            if (_settings.ResultTTL.HasValue)
                query.Append(" USING TTL ").Append((int)_settings.ResultTTL.Value.TotalSeconds);

            query.Append(" SET ");
            WriteValues(query, record, delimiter: ", ");
            query.Append(", execution_state = null, method_state = null, flow_context = null, continuation = null, continuation_state = null");
            query.Append(" WHERE ");
            WriteValues(query, key, delimiter: " AND ");

            await ExecuteQueryAsync(serviceId, methodId, query.ToString());
        }

        public async Task<ITaskResult> TryReadResultAsync(ServiceId serviceId, MethodId methodId, string intentId, Type resultValueType, CancellationToken ct)
        {
            var tableName = GetTableQualifiedName(serviceId, methodId);

            var key = new StorageRecord
            {
                service = serviceId.Name,
                method = methodId.Name,
                intent_id = intentId
            };

            var query = new StringBuilder("SELECT status, outcome, format, task_result, result, error FROM ")
                .Append(tableName).Append(" WHERE ");
            WriteValues(query, key, delimiter: " AND ");

            var result = await ExecuteQueryAsync(serviceId, methodId, query.ToString());
            var row = result.FirstOrDefault();

            if (row == null)
                return null;

            var record = ReadValues(row);

            if (!record.outcome.HasValue)
                return null;

            if (record.outcome.Value == (int)Outcomes.Canceled)
                return TaskResult.Create(resultValueType, null, null, isCanceled: true);

            var serializer = !string.IsNullOrEmpty(record.format)
                ? _serializerProvider.GetSerializer(record.format)
                : _serializer;

            var taskResult = TaskResult.CreateEmpty(resultValueType);
            serializer.Populate(record.task_result, (IValueContainer)taskResult);
            return taskResult;
        }

        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }

        private async ValueTask<ISession> Session()
        {
            if (_session != null && !_session.IsDisposed)
                return _session;

            _session = await _cluster.ConnectAsync();
            return _session;
        }

        private string GetKeyspaceName(ServiceId serviceId)
        {
            return _settings.Keyspace
                .Replace("{serviceName}", serviceId.Name)
                .ToLowerInvariant();
        }

        private string GetTableName(ServiceId serviceId, MethodId methodId)
        {
            return _settings.TableName
                .Replace("{serviceName}", serviceId.Name)
                .Replace("{methodName}", methodId.Name)
                .ToLowerInvariant();
        }

        private string GetTableQualifiedName(ServiceId serviceId, MethodId methodId) =>
            string.Concat(GetKeyspaceName(serviceId), ".", GetTableName(serviceId, methodId));

        private async Task<RowSet> ExecuteQueryAsync(ServiceId serviceId, MethodId methodId, string query)
        {
            var session = await Session();
        @TryExecute:
            try
            {
                return await session.ExecuteAsync(new SimpleStatement(query.ToString()));
            }
            catch (InvalidQueryException ex)
            {
                if (ex.Message.StartsWith("Keyspace ")) // asumme "Keyspace {name} does not exist"
                {
                    session.CreateKeyspaceIfNotExists(GetKeyspaceName(serviceId));
                    await CreateTableIfNotExistsAsync(session, GetKeyspaceName(serviceId), GetTableName(serviceId, methodId));
                    goto TryExecute;
                }
                else if (ex.Message.StartsWith("unconfigured table ")) // assume "unconfigured table {name}"
                {
                    await CreateTableIfNotExistsAsync(session, GetKeyspaceName(serviceId), GetTableName(serviceId, methodId));
                    goto TryExecute;
                }
                else
                {
                    throw;
                }
            }
        }

        private Task CreateTableIfNotExistsAsync(ISession session, string keyspace, string name)
        {
            var query = string.Concat(
"CREATE TABLE IF NOT EXISTS ",
keyspace, ".", name,
@"(service text,
method text,
intent_id text,
etag bigint,
status int,
outcome int,
last_intent_id text,
invoked_at timestamp,
started_at timestamp,
updated_at timestamp,
duration duration,
cancel_at timestamp,
cancellation_id text,
transition_count int,
caller_service text,
caller_proxy text,
caller_method text,
caller_event text,
caller_intent_id text,
format text,
execution_state blob,
method_state blob,
flow_context blob,
task_result blob,
result blob,
error blob,
continuation blob,
continuation_state blob,
PRIMARY KEY ((service, method), intent_id)
) WITH CLUSTERING ORDER BY (intent_id DESC);");

            return session.ExecuteAsync(new SimpleStatement(query));
        }

        private static StringBuilder WriteValues(StringBuilder sb, StorageRecord record, string delimiter)
        {
            if (record.service != null)
                sb.Append("service = '").Append(record.service).Append('\'').Append(delimiter);

            if (record.method != null)
                sb.Append("method = '").Append(record.method).Append('\'').Append(delimiter);

            if (record.intent_id != null)
                sb.Append("intent_id = '").Append(record.intent_id).Append('\'').Append(delimiter);

            if (record.etag != null)
                sb.Append("etag = ").Append(record.etag).Append(delimiter);

            if (record.status.HasValue)
                sb.Append("status = ").Append(record.status.Value).Append(delimiter);

            if (record.outcome.HasValue)
                sb.Append("outcome = ").Append(record.outcome.Value).Append(delimiter);

            if (record.last_intent_id != null)
                sb.Append("last_intent_id = '").Append(record.last_intent_id).Append(delimiter);

            if (record.invoked_at.HasValue)
                sb.Append("invoked_at = ").AppendCqlTimestamp(record.invoked_at.Value).Append(delimiter);

            if (record.started_at.HasValue)
                sb.Append("started_at = ").AppendCqlTimestamp(record.started_at.Value).Append(delimiter);

            if (record.updated_at.HasValue)
                sb.Append("updated_at = ").AppendCqlTimestamp(record.updated_at.Value).Append(delimiter);

            if (record.duration.HasValue)
                sb.Append("duration = ").AppendCqlDuration(record.duration.Value).Append(delimiter);

            if (record.cancel_at.HasValue)
                sb.Append("cancel_at = ").AppendCqlTimestamp(record.cancel_at.Value).Append(delimiter);

            if (record.cancellation_id != null)
                sb.Append("cancellation_id = '").Append(record.cancellation_id).Append('\'');

            if (record.transition_count.HasValue)
                sb.Append("transition_count = ").Append(record.transition_count.Value).Append(delimiter);

            if (record.caller_service != null)
                sb.Append("caller_service = '").Append(record.caller_service).Append('\'').Append(delimiter);

            if (record.caller_proxy != null)
                sb.Append("caller_proxy = '").Append(record.caller_proxy).Append('\'').Append(delimiter);

            if (record.caller_method != null)
                sb.Append("caller_method = '").Append(record.caller_method).Append('\'').Append(delimiter);

            if (record.caller_event != null)
                sb.Append("caller_event = '").Append(record.caller_event).Append('\'').Append(delimiter);

            if (record.caller_intent_id != null)
                sb.Append("caller_intent_id = '").Append(record.caller_intent_id).Append('\'').Append(delimiter);

            if (record.format != null)
                sb.Append("format = '").Append(record.format).Append('\'').Append(delimiter);

            if (record.execution_state?.Length > 0)
                sb.Append("execution_state = ").AppendCqlBlob(record.execution_state).Append(delimiter);

            if (record.method_state?.Length > 0)
                sb.Append("method_state = ").AppendCqlBlob(record.method_state).Append(delimiter);

            if (record.flow_context?.Length > 0)
                sb.Append("flow_context = ").AppendCqlBlob(record.flow_context).Append(delimiter);

            if (record.task_result?.Length > 0)
                sb.Append("task_result = ").AppendCqlBlob(record.task_result).Append(delimiter);

            if (record.result?.Length > 0)
                sb.Append("result = ").AppendCqlBlob(record.result).Append(delimiter);

            if (record.error?.Length > 0)
                sb.Append("error = ").AppendCqlBlob(record.error).Append(delimiter);

            if (record.continuation?.Length > 0)
                sb.Append("continuation = ").AppendCqlBlob(record.continuation).Append(delimiter);

            if (record.continuation_state?.Length > 0)
                sb.Append("continuation_state = ").AppendCqlBlob(record.continuation_state).Append(delimiter);

            // Remove last delimiter
            sb.Remove(sb.Length - delimiter.Length, delimiter.Length);

            return sb;
        }

        private static StorageRecord ReadValues(Row row)
        {
            return new StorageRecord
            {
                service = row.GetValueOrDefault<string>("service"),
                method = row.GetValueOrDefault<string>("method"),
                intent_id = row.GetValueOrDefault<string>("intent_id"),
                etag = row.GetValueOrDefault<long?>("etag"),
                status = row.GetValueOrDefault<int?>("status"),
                outcome = row.GetValueOrDefault<int?>("outcome"),
                last_intent_id = row.GetValueOrDefault<string>("last_intent_id"),
                invoked_at = row.GetValueOrDefault<DateTimeOffset?>("invoked_at")?.ToLocalTime(),
                started_at = row.GetValueOrDefault<DateTimeOffset?>("started_at")?.ToLocalTime(),
                updated_at = row.GetValueOrDefault<DateTimeOffset?>("updated_at")?.ToLocalTime(),
                duration = row.GetDurationValue("duration"),
                cancel_at = row.GetValueOrDefault<DateTimeOffset?>("cancel_at")?.ToLocalTime(),
                cancellation_id = row.GetValueOrDefault<string>("cancellation_id"),
                transition_count = row.GetValueOrDefault<int?>("transition_count"),
                caller_service = row.GetValueOrDefault<string>("caller_service"),
                caller_proxy = row.GetValueOrDefault<string>("caller_proxy"),
                caller_method = row.GetValueOrDefault<string>("caller_method"),
                caller_event = row.GetValueOrDefault<string>("caller_event"),
                caller_intent_id = row.GetValueOrDefault<string>("caller_intent_id"),
                format = row.GetValueOrDefault<string>("format"),
                execution_state = row.GetValueOrDefault<byte[]>("execution_state"),
                method_state = row.GetValueOrDefault<byte[]>("method_state"),
                flow_context = row.GetValueOrDefault<byte[]>("flow_context"),
                task_result = row.GetValueOrDefault<byte[]>("task_result"),
                result = row.GetValueOrDefault<byte[]>("result"),
                error = row.GetValueOrDefault<byte[]>("error"),
                continuation = row.GetValueOrDefault<byte[]>("continuation"),
                continuation_state = row.GetValueOrDefault<byte[]>("continuation_state")
            };
        }
    }
}
