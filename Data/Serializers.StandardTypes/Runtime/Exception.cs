using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Runtime
{
    public sealed class ExceptionSerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly ITypeSerializerHelper _typeSerializerHelper;

        public ExceptionSerializer(ITypeSerializerHelper typeSerializerHelper)
        {
            _typeSerializerHelper = typeSerializerHelper;
        }

        public IValueContainer Decompose(object value)
        {
            var ex = (Exception)value;
            return new ExceptionContainer
            {
                Type = _typeSerializerHelper.GetTypeSerializationInfo(ex.GetType()).ToString(),
                Message = ex.Message,
                InnerException = (ex is AggregateException) ? null : ex.InnerException,
                InnerExceptions = (ex is AggregateException aggregateException) ? aggregateException.InnerExceptions.ToArray() : null,
                StackTrace = ex.StackTrace
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var c = (ExceptionContainer)container;

            Type exceptionType = null;
            var exceptionTypeResolved = false;
            try
            {
                exceptionType = _typeSerializerHelper.ResolveType(TypeSerializationInfo.Parse(c.Type));
                exceptionTypeResolved = true;
            }
            catch
            {
                exceptionType = typeof(Exception);
            }

            var result = (Exception)FormatterServices.GetUninitializedObject(exceptionType);
            result.SetClassName(exceptionTypeResolved ? result.GetType().Name : c.Type);
            result.SetMessage(c.Message);
            result.SetStackTrace(c.StackTrace);
            result.SetInnerException(c.InnerException);

            if (result is AggregateException aggregateException)
                aggregateException.SetInnerExceptions(new ReadOnlyCollection<Exception>(c.InnerExceptions ?? Array.Empty<Exception>()));

            return result;
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            return new ExceptionContainer();
        }
    }

    public sealed class ExceptionContainer : ValueContainerBase, IValueContainerWithTypeInfo
    {
        public string Type;
        public string Message;
        public Exception InnerException;
        public Exception[] InnerExceptions;
        public string StackTrace;

        public Type GetObjectType() => typeof(Exception);
    }
}
