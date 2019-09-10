using System;
using System.Collections.Generic;
using Dasync.Serialization;
using Dasync.Serializers.StandardTypes.Collections;
using Dasync.Serializers.StandardTypes.Metadata;
using Dasync.Serializers.StandardTypes.Runtime;
using Dasync.Serializers.StandardTypes.Time;

namespace Dasync.Serializers.StandardTypes
{
    public sealed class StandardTypeDecomposerSelector : IObjectDecomposerSelector
    {
        internal static readonly Dictionary<Type, IObjectDecomposer> _standardDecomposers
            = new Dictionary<Type, IObjectDecomposer>()
            {
                { typeof(TimeSpan), new TimeSpanSerializer() },
                { typeof(DateTime), new DateTimeSerializer() },
                { typeof(DateTimeOffset), new DateTimeOffsetSerializer() },
                { typeof(Version), new VersionSerializer() },
                { typeof(List<>), new ListSerializer() },
                { typeof(HashSet<>), new HashsetSerializer() },
                { typeof(KeyValuePair<,>), new KeyValuePairSerializer() },
                { typeof(Dictionary<,>), new DictionarySerializer() }
            };

        private readonly ExceptionSerializer _exceptionSerializer;

        public StandardTypeDecomposerSelector(ExceptionSerializer exceptionSerializer)
        {
            _exceptionSerializer = exceptionSerializer;
        }

        public IObjectDecomposer SelectDecomposer(Type type)
        {
            if (type.IsGenericType() && !type.IsGenericTypeDefinition())
                type = type.GetGenericTypeDefinition();

            if (_standardDecomposers.TryGetValue(type, out IObjectDecomposer decomposer))
                return decomposer;

            if (typeof(Exception).IsAssignableFrom(type))
                return _exceptionSerializer;

            return null;
        }
    }
}
