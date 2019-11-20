using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.ValueContainer;

namespace Dasync.Proxy
{
    public class MethodInvokerFactory : IMethodInvokerFactory
    {
        private readonly Dictionary<MethodInfo, IMethodInvoker> _invokers;

        public MethodInvokerFactory()
        {
            // TODO: inject IValueContainerFactory?
            _invokers = new Dictionary<MethodInfo, IMethodInvoker>();
        }

        public IMethodInvoker Create(MethodInfo methodInfo, MethodInfo interfaceMethodInfo = null)
        {
            lock (_invokers)
            {
                if (!_invokers.TryGetValue(methodInfo, out var invoker))
                {
                    var parameterContainerFactory = GetParametersContainerFactory(interfaceMethodInfo ?? methodInfo);
                    invoker = new MethodInvoker(methodInfo, parameterContainerFactory);
                    _invokers.Add(methodInfo, invoker);
                }
                return invoker;
            }
        }

        private IValueContainerFactory GetParametersContainerFactory(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            return ValueContainerFactory.GetFactory(
                parameters.Select(p => new KeyValuePair<string, Type>(p.Name, p.ParameterType)));
        }
    }
}
