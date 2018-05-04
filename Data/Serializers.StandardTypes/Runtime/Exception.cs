using System;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Runtime
{
    public sealed class ExceptionSerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer Decompose(object value)
        {
            var ex = (Exception)value;
            return new ExceptionContainer
            {
                Message = ex.Message,
                InnerException = ex.InnerException
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var c = (ExceptionContainer)container;
            return new Exception(c.Message, c.InnerException);
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            return new ExceptionContainer();
        }
    }

    public sealed class ExceptionContainer : ValueContainerBase
    {
        public string Message;
        public Exception InnerException;
    }
}
