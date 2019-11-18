using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Dasync.Serialization;

namespace Dasync.Communication.InMemory
{
    public interface IMessageHandler
    {
        Task Run(CancellationToken stopToken);
    }

    public class InMemoryMessageHandler : IMessageHandler
    {
        private readonly IMessageHub _messageHub;
        private readonly ILocalMethodRunner _localTransitionRunner;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IServiceResolver _serviceResolver;

        public InMemoryMessageHandler(
            IMessageHub messageHub,
            ILocalMethodRunner localTransitionRunner,
            ISerializerProvider serializerProvider,
            IServiceResolver serviceResolver)
        {
            _messageHub = messageHub;
            _localTransitionRunner = localTransitionRunner;
            _serializerProvider = serializerProvider;
            _serviceResolver = serviceResolver;
        }

        public async Task Run(CancellationToken stopToken)
        {
            try
            {
                await StreamMessages(stopToken).ParallelForEachAsync(HandleMessage);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private IAsyncEnumerable<Message> StreamMessages(CancellationToken stopToken) =>
            new AsyncEnumerable<Message>(async yield =>
            {
                while (!stopToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = await _messageHub.GetMessage(stopToken)
                            .ContinueWith(task => task.IsCanceled ? null : task.Result);
                        if (message == null)
                            break;
                        await yield.ReturnAsync(message);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });

        private async Task HandleMessage(Message message)
        {
            switch (message.Type)
            {
                case MessageType.InvokeMethod:
                    await HandleCommandOrQuery(message);
                    break;

                case MessageType.Response:
                    await HandleResponse(message);
                    break;

                case MessageType.Event:
                    await HandleEvent(message);
                    break;

                default:
                    throw new ArgumentException($"Unknown message type '{message.Type}'.");
            }
        }

        private async Task HandleCommandOrQuery(Message message)
        {
            var invocationData = MethodInvocationDataTransformer.Read(message, _serializerProvider);
            var communicationMessage = new CommunicationMessage(message);

            TaskCompletionSource<InvokeRoutineResult> tcs = null;
            if (message.Data.TryGetValue("Notification", out var sink))
            {
                tcs = (TaskCompletionSource<InvokeRoutineResult>)sink;
                communicationMessage.WaitForResult = true;
            }

            var result = await _localTransitionRunner.RunAsync(invocationData, communicationMessage);

            tcs?.TrySetResult(result);
        }

        private async Task HandleResponse(Message message)
        {
            var continuationData = MethodContinuationDataTransformer.Read(message, _serializerProvider);
            var communicationMessage = new CommunicationMessage(message);
            await _localTransitionRunner.ContinueAsync(continuationData, communicationMessage);
        }

        private async Task HandleEvent(Message message)
        {
            var eventPublishData = EventPublishDataTransformer.Read(message, _serializerProvider, out var skipLocalEvents);

            if (skipLocalEvents && _serviceResolver.Resolve(eventPublishData.Service).Definition.Type == ServiceType.Local)
                return;

            var communicationMessage = new CommunicationMessage(message);
            await _localTransitionRunner.ReactAsync(eventPublishData, communicationMessage);
        }
    }
}
