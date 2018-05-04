using System;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface IObjectComposer
    {
#warning Should this method take in the value type?
        IValueContainer CreatePropertySet(Type valueType);

#warning Why does this method takes in value type?
        object Compose(IValueContainer container, Type valueType);
    }

    public interface IStronglyTypedObjectComposer<TObject, TPropertySet>
    {
        TPropertySet CreatePropertySet();

        TObject Compose(ref TPropertySet propertySet, Type valueType);
    }
}
