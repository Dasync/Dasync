using System;
using System.Reflection;
using System.Threading.Tasks;
using Dasync.ValueContainer;

namespace Dasync.Proxy
{
    public interface IProxyMethodExecutor
    {
        Task Execute<TParameters>(
            IProxy proxy,
            MethodInfo methodInfo,
            ref TParameters parameters)
            where TParameters : IValueContainer;

        void Subscribe(
            IProxy proxy,
            EventInfo @event,
            Delegate @delegate);

        void Unsubscribe(
            IProxy proxy,
            EventInfo @event,
            Delegate @delegate);

        void RaiseEvent<TParameters>(
            IProxy proxy,
            EventInfo @event,
            ref TParameters parameters)
            where TParameters : IValueContainer;
    }
}
