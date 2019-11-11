using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Communication.InMemory
{
    public class InMemoryCommunicator : ICommunicator, ISynchronousCommunicator
    {
        private readonly ISerializer _serializer;
        private readonly IMessageHub _messageHub;
        private readonly IMethodStateStorageProvider _methodStateStorageProvider;

        public InMemoryCommunicator(
            ISerializer serializer,
            IMessageHub messageHub,
            IMethodStateStorageProvider methodStateStorageProvider)
        {
            _serializer = serializer;
            _messageHub = messageHub;
            _methodStateStorageProvider = methodStateStorageProvider;
        }

        public string Type => InMemoryCommunicationMethod.MethodType;

        public CommunicationTraits Traits =>
            CommunicationTraits.Volatile |
            CommunicationTraits.SyncReplies |
            CommunicationTraits.MessageLockOnPublish;

        public async Task<InvokeRoutineResult> InvokeAsync(
            MethodInvocationData data,
            InvocationPreferences preferences)
        {
            var message = new Message
            {
                Type = MessageType.InvokeMethod,
            };

            MethodInvocationDataTransformer.Write(message, data, _serializer);

            var result = new InvokeRoutineResult
            {
                Outcome = InvocationOutcome.Scheduled
            };

            if (preferences.Synchronous)
            {
                var completionNotification = new TaskCompletionSource<InvokeRoutineResult>();
                message.Data["Notification"] = completionNotification;

                _messageHub.Schedule(message);

                result = await completionNotification.Task;

                if (result.Result != null)
                {
                    result.Result = TaskResult.CreateEmpty(preferences.ResultValueType);
                    _serializer.Populate(_serializer.SerializeToString(result.Result), (IValueContainer)result.Result);
                }
            }
            else if (preferences.LockMessage)
            {
                result.MessageHandle = new MessageHandle(message, _messageHub);
            }
            else
            {
                _messageHub.Schedule(message);
            }

            return result;
        }

        public Task<ContinueRoutineResult> ContinueAsync(
            MethodContinuationData data,
            InvocationPreferences preferences)
        {
            var message = new Message
            {
                Type = MessageType.Response,
                DeliverAt = data.ContinueAt
            };

            MethodContinuationDataTransformer.Write(message, data, _serializer);

            var result = new ContinueRoutineResult();

            if (preferences.LockMessage)
            {
                result.MessageHandle = new MessageHandle(message, _messageHub);
            }
            else
            {
                _messageHub.Schedule(message);
            }

            return Task.FromResult(result);
        }

        public async Task<InvokeRoutineResult> GetInvocationResultAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            Type resultValueType,
            CancellationToken ct)
        {
            var storage = _methodStateStorageProvider.GetStorage(serviceId, methodId, returnNullIfNotFound: true);
            if (storage == null)
            {
                return new InvokeRoutineResult
                {
                    Outcome = InvocationOutcome.Unknown
                };
            }

            var taskResult = await storage.TryReadResultAsync(serviceId, methodId, intentId, resultValueType, ct);

            return new InvokeRoutineResult
            {
                Result = taskResult,
                Outcome = taskResult != null
                    ? InvocationOutcome.Complete
                    : InvocationOutcome.Scheduled
            };
        }
    }
}
