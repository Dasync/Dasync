using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.Communication.InMemory
{
    public interface IMessageHub
    {
        void Schedule(Message message);

        Task<Message> GetMessage(CancellationToken ct);
    }

    public class MessageHub : IMessageHub
    {
        private static readonly IComparer<Message> _messageDeliveryTimeComparer = new MessageDeliveryTimeComparer();

        private const int TimerTickPeriodMs = 100;

        private readonly Queue<Message> _messages = new Queue<Message>();
        private readonly List<Message> _delayedMessages = new List<Message>();
        private readonly Queue<TaskCompletionSource<Message>> _listeners = new Queue<TaskCompletionSource<Message>>();
        private readonly Action<object> _removeListenerCallback;
        private readonly Timer _deliveryTimer;

        public MessageHub()
        {
            _removeListenerCallback = RemoveListener;
            _deliveryTimer = new Timer(OnDeliveryTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Schedule(Message message)
        {
            if (message.DeliverAt.HasValue && message.DeliverAt.Value > DateTimeOffset.Now)
            {
                lock (_delayedMessages)
                {
                    _delayedMessages.Add(message);
                    _delayedMessages.Sort(_messageDeliveryTimeComparer);

                    if (_delayedMessages.Count == 1)
                        _deliveryTimer.Change(0, TimerTickPeriodMs);
                }
            }
            else if (_messages.Count > 0 || !TryDeliverMessage(message))
            {
                lock (_messages)
                {
                    _messages.Enqueue(message);
                }
            }
        }

        public Task<Message> GetMessage(CancellationToken ct)
        {
            lock (_messages)
            {
                if (_messages.Count > 0)
                {
                    var message = _messages.Dequeue();
                    message.DeliveryCount++;
                    return Task.FromResult(message);
                }
                else
                {
                    lock (_listeners)
                    {
                        var tcs = new TaskCompletionSource<Message>();
                        _listeners.Enqueue(tcs);
                        if (ct.CanBeCanceled)
                            ct.Register(_removeListenerCallback, tcs);
                        return tcs.Task;
                    }
                }
            }
        }

        private void RemoveListener(object listener)
        {
            lock (_listeners)
            {
                var allListeners = _listeners.ToList();
                _listeners.Clear();
                foreach (var l in allListeners)
                    if (!ReferenceEquals(l, listener))
                        _listeners.Enqueue(l);
            }
            ((TaskCompletionSource<Message>)listener).TrySetCanceled();
        }

        private bool TryDeliverMessage(Message message)
        {
            TaskCompletionSource<Message> listener = null;

            if (_listeners.Count > 0)
            {
                lock (_listeners)
                {
                    if (_listeners.Count == 0)
                        return false;

                    listener = _listeners.Dequeue();
                }
            }
            if (listener == null)
                return false;

            message.DeliveryCount++;
            listener.TrySetResult(message);
            return true;
        }

        private void OnDeliveryTimerTick(object state)
        {
            if (_delayedMessages.Count == 0)
                return;

            lock (_delayedMessages)
            {
                int messageIndex = 0;
                for (; messageIndex < _delayedMessages.Count; messageIndex++)
                {
                    var message = _delayedMessages[messageIndex];
                    if (message.DeliverAt > DateTimeOffset.Now)
                        break;
                    Schedule(message);
                }

                if (messageIndex > 0)
                    _delayedMessages.RemoveRange(0, messageIndex);

                if (_delayedMessages.Count == 0)
                    _deliveryTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private class MessageDeliveryTimeComparer : IComparer<Message>
        {
            public int Compare(Message x, Message y)
            {
                if (x.DeliverAt.HasValue && y.DeliverAt.HasValue)
                    return DateTimeOffset.Compare(x.DeliverAt.Value, y.DeliverAt.Value);
                if (x.DeliverAt.HasValue)
                    return 1;
                if (y.DeliverAt.HasValue)
                    return -1;
                return 0;
            }
        }
    }
}
