using System;
using System.Collections.Generic;
using Dasync.Serialization;

namespace Dasync.Bootstrap
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ITypeNameShortener)] = typeof(AggregateTypeNameShortener),
            [typeof(IAssemblyNameShortener)] = typeof(AggregateAssemblyNameShortener),
            [typeof(IObjectDecomposerSelector)] = typeof(AggregateObjectDecomposerSelector),
            [typeof(IObjectComposerSelector)] = typeof(AggregateObjectComposerSelector),
        };
    }
}
