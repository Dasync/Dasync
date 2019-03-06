using System;
using System.Collections.Generic;
using Dasync.Serializers.StandardTypes.Runtime;

namespace Dasync.Serializers.StandardTypes
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(StandardTypeNameShortener)] = typeof(StandardTypeNameShortener),
            [typeof(StandardAssemblyNameShortener)] = typeof(StandardAssemblyNameShortener),
            [typeof(StandardTypeDecomposerSelector)] = typeof(StandardTypeDecomposerSelector),
            [typeof(StandardTypeComposerSelector)] = typeof(StandardTypeComposerSelector),
            [typeof(ExceptionSerializer)] = typeof(ExceptionSerializer),
        };
    }
}
