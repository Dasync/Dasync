using System;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Dasync.Proxy;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class MethodResolver : IMethodResolver
    {
        private readonly IMethodInvokerFactory _methodInvokerFactory;

        public MethodResolver(IMethodInvokerFactory methodInvokerFactory)
        {
            _methodInvokerFactory = methodInvokerFactory;
        }

        public bool TryResolve(IServiceDefinition serviceDefinition, RoutineMethodId methodId, out IMethodReference methodReference)
        {
            var methodDefinition = serviceDefinition.FindMethod(methodId.Name);
            if (methodDefinition == null || methodDefinition.IsIgnored)
            {
                methodReference = null;
                return false;
            }

            // WARNING: Need to know exact interface used by caller to select the right MethodInfo.
            // This can be an issue only if two or more interfaces have a method with exactly the same signature.
            var interfaceMappedMethodInfo = methodDefinition.InterfaceMethods?.FirstOrDefault();

            var invoker = _methodInvokerFactory.Create(methodDefinition.MethodInfo, interfaceMappedMethodInfo);
            methodReference = new MethodReference(methodId, methodDefinition, invoker);
            return true;
        }
    }
}
