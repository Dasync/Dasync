using System;
using Dasync.Serialization;
using Dasync.Serializers.StandardTypes.Runtime;

namespace Dasync.Serializers.StandardTypes
{
    public sealed class StandardTypeComposerSelector : IObjectComposerSelector
    {
        private readonly ExceptionSerializer _exceptionSerializer;

        public StandardTypeComposerSelector(ExceptionSerializer exceptionSerializer)
        {
            _exceptionSerializer = exceptionSerializer;
        }

        public IObjectComposer SelectComposer(Type targetType)
        {
            if (targetType.IsGenericType() && !targetType.IsGenericTypeDefinition())
                targetType = targetType.GetGenericTypeDefinition();

            if (StandardTypeDecomposerSelector._standardDecomposers.TryGetValue(targetType, out IObjectDecomposer decomposer))
                return decomposer as IObjectComposer;

            if (typeof(Exception).IsAssignableFrom(targetType))
                return _exceptionSerializer;

            if (targetType.IsPoco())
                return PocoSerializer.Instance;

            return null;
        }
    }
}
