using System;
using Dasync.EETypes;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class MethodResolver : IMethodResolver
    {
        public bool TryResolve(IServiceDefinition serviceDefinition, RoutineMethodId methodId, out IMethodReference methodReference)
        {
            var methodDefinition = serviceDefinition.FindMethod(methodId.Name);
            if (methodDefinition == null)
            {
                methodReference = null;
                return false;
            }

            methodReference = new MethodReference(methodId, methodDefinition);
            return true;
        }
    }
}
