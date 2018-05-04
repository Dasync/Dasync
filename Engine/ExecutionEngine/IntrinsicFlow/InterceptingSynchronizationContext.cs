using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.ExecutionEngine.Transitions;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    public class InterceptingSynchronizationContext : SynchronizationContext
    {
        private readonly IIntrinsicFlowController _controller;
        private readonly ITransitionMonitor _monitor;

        public InterceptingSynchronizationContext(
            SynchronizationContext innerSyncContext,
            IIntrinsicFlowController controller,
            ITransitionMonitor monitor)
        {
            InnerContext = innerSyncContext;
            _controller = controller;
            _monitor = monitor;
        }

        public SynchronizationContext InnerContext { get; }

        public override SynchronizationContext CreateCopy() =>
            new InterceptingSynchronizationContext(InnerContext, _controller, _monitor);

        public override void OperationStarted() =>
            InnerContext?.OperationStarted();

        public override void OperationCompleted() =>
            InnerContext?.OperationCompleted();

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_controller.TryHandlePreInvoke(d, state, _monitor))
                return;

            if (InnerContext != null)
            {
                InnerContext.Post(d, state);
            }
            else
            {
#warning Is this a right way of scheduling?
                Task.Run(() =>
                {
                    d(state);
                    _controller.TryHandlePostInvoke(d, state, _monitor);
                });
            }
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (_controller.TryHandlePreInvoke(d, state, _monitor))
                return;

            if (InnerContext != null)
            {
                InnerContext.Send(d, state);
            }
            else
            {
#warning Is this a right way of scheduling?
                Task.Run(() =>
                {
                    d(state);
                    _controller.TryHandlePostInvoke(d, state, _monitor);
                });
            }
        }

#if NETFX
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (InnerContext != null)
            {
                return InnerContext.Wait(waitHandles, waitAll, millisecondsTimeout);
            }
            else
            {
                return base.Wait(waitHandles, waitAll, millisecondsTimeout);
            }
        }
#endif
    }
}
