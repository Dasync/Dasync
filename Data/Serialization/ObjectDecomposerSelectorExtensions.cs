using System;

namespace Dasync.Serialization
{
    public static class ObjectDecomposerSelectorExtensions
    {
        public static IObjectDecomposer SelectDecomposerOrPoco(this IObjectDecomposerSelector selector, Type type)
        {
            var decomposer = selector.SelectDecomposer(type);
            if (decomposer == null && type.IsPoco())
                decomposer = PocoSerializer.Instance;
            return decomposer;
        }

        public static IObjectComposer SelectComposerOrPoco(this IObjectComposerSelector selector, Type type)
        {
            var decomposer = selector.SelectComposer(type);
            if (decomposer == null && type.IsPoco())
                decomposer = PocoSerializer.Instance;
            return decomposer;
        }
    }
}
