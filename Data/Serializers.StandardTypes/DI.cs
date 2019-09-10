using System;
using System.Collections.Generic;
using Dasync.Serialization;
using Dasync.Serializers.StandardTypes.Runtime;

namespace Dasync.Serializers.StandardTypes
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ITypeNameShortener)] = typeof(StandardTypeNameShortener),
            [typeof(IAssemblyNameShortener)] = typeof(StandardAssemblyNameShortener),
            [typeof(IObjectDecomposerSelector)] = typeof(StandardTypeDecomposerSelector),
            [typeof(IObjectComposerSelector)] = typeof(StandardTypeComposerSelector),
            [typeof(ExceptionSerializer)] = typeof(ExceptionSerializer),
        };
    }
}
