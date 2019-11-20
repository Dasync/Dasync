using System;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes
{
    public abstract class SerializerBase<TObject, TContainer> : IObjectDecomposer, IObjectComposer
        where TContainer : IValueContainer, new()
    {
        IValueContainer IObjectDecomposer.Decompose(object value)
        {
            return Decompose((TObject)value);
        }

        IValueContainer IObjectComposer.CreatePropertySet(Type type)
        {
            return new TContainer();
        }

        object IObjectComposer.Compose(IValueContainer decomposition, Type valueType)
        {
            return Compose((TContainer)decomposition);
        }

        protected abstract TContainer Decompose(TObject value);

        protected abstract TObject Compose(TContainer container);
    }
}
