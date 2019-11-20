using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Transitions
{
    public class RoutineCompletionNotificationHub : IRoutineCompletionNotifier, IRoutineCompletionSink
    {
        private readonly ICommunicatorProvider _communicatorProvider;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly IMethodStateStorageProvider _methodStateStorageProvider;
        private readonly LinkedList<TrackedInvocation> _trackedInvocations = new LinkedList<TrackedInvocation>();
        private readonly TimerCallback _onTimerTick;
        private long _tokenCounter = 1;

        public RoutineCompletionNotificationHub(
            ICommunicatorProvider communicatorProvider,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            IMethodStateStorageProvider methodStateStorageProvider)
        {
            _communicatorProvider = communicatorProvider;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
            _methodStateStorageProvider = methodStateStorageProvider;
            _onTimerTick = OnTimerTick;
        }

        public long NotifyOnCompletion(ServiceId serviceId, MethodId methodId, string intentId, TaskCompletionSource<ITaskResult> completionSink, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return -1;

            var trackedInvocation = new TrackedInvocation
            {
                Token = Interlocked.Increment(ref _tokenCounter),
                ServiceId = serviceId,
                MethodId = methodId,
                IntentId = intentId,
                CompletionSink = completionSink,
                CancellationToken = ct
            };

            SetPollingMethod(trackedInvocation);

            LinkedListNode<TrackedInvocation> listNode;
            lock (_trackedInvocations)
            {
                listNode = _trackedInvocations.AddLast(trackedInvocation);
            }

            trackedInvocation.PollTimer = new Timer(_onTimerTick, listNode, Timeout.Infinite, Timeout.Infinite);
            ScheduleNextPoll(trackedInvocation);

            return trackedInvocation.Token;
        }

        public bool StopTracking(long token)
        {
            lock (_trackedInvocations)
            {
                for (var node = _trackedInvocations.First; node != null; node = node.Next)
                {
                    if (node.Value.Token == token)
                    {
                        StopTracking(node);
                        return true;
                    }
                }
            }
            return false;
        }

        public void OnRoutineCompleted(ServiceId serviceId, MethodId methodId, string intentId, ITaskResult taskResult)
        {
            List<TrackedInvocation> listeners = null;
            lock (_trackedInvocations)
            {
                for (var node = _trackedInvocations.First; node != null;)
                {
                    var nextNode = node.Next;

                    var trackedInvocation = node.Value;
                    if (trackedInvocation.IntentId == intentId &&
                        trackedInvocation.ServiceId == serviceId &&
                        trackedInvocation.MethodId == methodId)
                    {
                        StopTracking(node);

                        if (listeners == null)
                            listeners = new List<TrackedInvocation>(capacity: 2);
                        listeners.Add(trackedInvocation);
                    }

                    node = nextNode;
                }
            }

            if (listeners == null)
                return;

            foreach (var trackedInvocation in listeners)
            {
                if (trackedInvocation.CancellationToken.IsCancellationRequested)
                    continue;
                var sink = trackedInvocation.CompletionSink;
                Task.Run(() => sink.TrySetResult(taskResult));
            }
        }

        public async Task<ITaskResult> TryPollCompletionAsync(ServiceId serviceId, MethodId methodId, string intentId, CancellationToken ct)
        {
            LinkedListNode<TrackedInvocation> existingListNode = null;

            lock (_trackedInvocations)
            {
                for (var node = _trackedInvocations.First; node != null; node = node.Next)
                {
                    if (node.Value.IntentId == intentId &&
                        node.Value.ServiceId == serviceId &&
                        node.Value.MethodId == methodId)
                    {
                        existingListNode = node;
                        break;
                    }
                }
            }

            TrackedInvocation trackedInvocation;

            if (existingListNode != null)
            {
                trackedInvocation = existingListNode.Value;
            }
            else
            {
                trackedInvocation = new TrackedInvocation
                {
                    Token = -1,
                    ServiceId = serviceId,
                    MethodId = methodId,
                    IntentId = intentId,
                    CompletionSink = new TaskCompletionSource<ITaskResult>(),
                    CancellationToken = ct
                };

                SetPollingMethod(trackedInvocation);
            }

            if (!await PollAsync(trackedInvocation))
                return null;

            if (existingListNode != null)
                StopTracking(existingListNode);

            return trackedInvocation.CompletionSink.Task.Result;
        }

        private void SetPollingMethod(TrackedInvocation trackedInvocation)
        {
            var methodDefinition = _methodResolver.Resolve(
                _serviceResolver.Resolve(trackedInvocation.ServiceId).Definition,
                trackedInvocation.MethodId).Definition;

            // TODO: add a preference for polling if has access to the storage of the external service
            if (methodDefinition.Service.Type == ServiceType.External)
            {
                var communicator = _communicatorProvider.GetCommunicator(trackedInvocation.ServiceId, trackedInvocation.MethodId);
                if (communicator.Traits.HasFlag(CommunicationTraits.SyncReplies) && communicator is ISynchronousCommunicator syncCommunicator)
                {
                    trackedInvocation.Communicator = syncCommunicator;
                }
            }

            if (trackedInvocation.Communicator == null)
            {
                // TODO: somehow make sure that an external service shares the same result storage.
                trackedInvocation.StateStorage = _methodStateStorageProvider.GetStorage(trackedInvocation.ServiceId, trackedInvocation.MethodId);
            }

            // TODO: helper method
            Type taskResultType =
                methodDefinition.MethodInfo.ReturnType == typeof(void)
                ? TaskAccessor.VoidTaskResultType
                : TaskAccessor.GetTaskResultType(methodDefinition.MethodInfo.ReturnType);
            trackedInvocation.ResultValueType =
                taskResultType == TaskAccessor.VoidTaskResultType
                ? typeof(object)
                : taskResultType;
        }

        private async Task<bool> PollAsync(TrackedInvocation trackedInvocation)
        {
            trackedInvocation.PollCount++;
            trackedInvocation.LastPoll = DateTime.Now;

            if (trackedInvocation.Communicator != null)
            {
                var result = await trackedInvocation.Communicator.GetInvocationResultAsync(
                    trackedInvocation.ServiceId,
                    trackedInvocation.MethodId,
                    trackedInvocation.IntentId,
                    trackedInvocation.ResultValueType,
                    trackedInvocation.CancellationToken);

                if (result.Outcome != InvocationOutcome.Complete)
                    return false;

                trackedInvocation.CompletionSink.TrySetResult(result.Result);
                return true;
            }
            else
            {
                var result = await trackedInvocation.StateStorage.TryReadResultAsync(
                    trackedInvocation.ServiceId,
                    trackedInvocation.MethodId,
                    trackedInvocation.IntentId,
                    trackedInvocation.ResultValueType,
                    trackedInvocation.CancellationToken);

                if (result == null)
                    return false;

                trackedInvocation.CompletionSink.TrySetResult(result);
                return true;
            }
        }

        private async void OnTimerTick(object state)
        {
            var listNode = (LinkedListNode<TrackedInvocation>)state;

            if (listNode.Value.CancellationToken.IsCancellationRequested)
            {
                StopTracking(listNode);
                return;
            }

            try
            {
                if (await PollAsync(listNode.Value))
                {
                    StopTracking(listNode);
                    return;
                }
            }
            catch (Exception ex)
            {
                // TODO: log
            }

            if (listNode.Value.CancellationToken.IsCancellationRequested)
            {
                StopTracking(listNode);
                return;
            }

            ScheduleNextPoll(listNode.Value);
        }

        private void ScheduleNextPoll(TrackedInvocation trackedInvocation)
        {
            trackedInvocation.NextPoll = GetNextPollTime(trackedInvocation.LastPoll, trackedInvocation.PollCount);
            var delay = trackedInvocation.NextPoll - DateTime.Now;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;
            trackedInvocation.PollTimer?.Change(delay, Timeout.InfiniteTimeSpan);
        }

        private DateTime GetNextPollTime(DateTime lastPollTime, int pollCount)
        {
            if (pollCount == 0)
                return DateTime.Now + TimeSpan.FromMilliseconds(50);
            return lastPollTime + TimeSpan.FromMilliseconds(Math.Pow(pollCount, 1.6) * 100);
        }

        private void StopTracking(LinkedListNode<TrackedInvocation> listNode)
        {
            lock (_trackedInvocations)
            {
                listNode.Value.PollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                listNode.Value.PollTimer?.Dispose();
                listNode.Value.PollTimer = null;

                if (listNode.List != null)
                    _trackedInvocations.Remove(listNode);
            }
        }

        private class TrackedInvocation
        {
            public long Token { get; set; }

            public ServiceId ServiceId { get; set; }

            public MethodId MethodId { get; set; }

            public string IntentId { get; set; }

            public DateTime LastPoll { get; set; }

            public DateTime NextPoll { get; set; }

            public int PollCount { get; set; }

            public Timer PollTimer { get; set; }

            public Type ResultValueType { get; set; }

            public CancellationToken CancellationToken { get; set; }

            public TaskCompletionSource<ITaskResult> CompletionSink { get; set; }

            public ISynchronousCommunicator Communicator { get; set; }

            public IMethodStateStorage StateStorage { get; set; }
        }
    }
}
