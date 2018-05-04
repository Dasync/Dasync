using System;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;

namespace Dasync.ExecutionEngine.Transitions
{
    public interface ITransitionScope
    {
        bool IsActive { get; }

        ITransitionMonitor CurrentMonitor { get; }

        IDisposable Enter(TransitionDescriptor transitionDescriptor);
    }

    public class TransitionScope : ITransitionScope
    {
        private struct ScopeData
        {
            public TransitionContext Context;
            public ITransitionMonitor Monitor;
        }

        private static AsyncLocal<ScopeData> _currentScope = new AsyncLocal<ScopeData>();

        private readonly ITransitionMonitorFactory _transitionMonitorFactory;

        public TransitionScope(ITransitionMonitorFactory transitionMonitorFactory)
        {
            _transitionMonitorFactory = transitionMonitorFactory;
        }

        public bool IsActive => _currentScope.Value.Monitor != null;

        public ITransitionMonitor CurrentMonitor =>
            _currentScope.Value.Monitor ?? throw new InvalidOperationException(
                "Transition context has been requested outside of a scope of a transition.");

        public IDisposable Enter(TransitionDescriptor transitionDescriptor)
        {
            if (IsActive)
                throw new InvalidOperationException(
                    "Cannot start transition of while another one is still running.");

            var context = new TransitionContext
            {
                TransitionDescriptor = transitionDescriptor
            };

            var monitor = _transitionMonitorFactory.Create(context);

            var scopeData = new ScopeData
            {
                Context = context,
                Monitor = monitor
            };

            _currentScope.Value = scopeData;

            return new DisposeTarget(this, scopeData);
        }

        private void Exit(ScopeData scopeData)
        {
            _currentScope.Value = default(ScopeData);
        }

        private sealed class DisposeTarget : IDisposable
        {
            private TransitionScope _owner;
            private ScopeData _scopeData;

            public DisposeTarget(TransitionScope owner, ScopeData scopeData)
            {
                _owner = owner;
                _scopeData = scopeData;
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    _owner.Exit(_scopeData);
                    _owner = null;
                }
            }
        }
    }
}
