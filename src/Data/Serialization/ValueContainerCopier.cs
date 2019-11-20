using System;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface IValueContainerCopier
    {
        void CopyValues(IValueContainer source, IValueContainer destination);
    }

    public class ValueContainerCopier : IValueContainerCopier
    {
        private readonly ISerializerProvider _serializerProvider;

        public ValueContainerCopier(ISerializerProvider serializerProvider)
        {
            _serializerProvider = serializerProvider;
        }

        public void CopyValues(IValueContainer source, IValueContainer destination)
        {
            if (source is ISerializedValueContainer serializedValueContainer)
            {
                var format = serializedValueContainer.GetFormat();
                var serializer = _serializerProvider.GetSerializer(format);
                var serializedForm = serializedValueContainer.GetSerializedForm();
                if (serializedForm is string stringContent)
                {
                    serializer.Populate(stringContent, destination);
                }
                else if (serializedForm is byte[] byteContent)
                {
                    serializer.Populate(byteContent, destination);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported runtime type '{serializedForm?.GetType()}' for a serialized form of '{format}'.");
                }
            }
            else
            {
                // TODO: type conversion
                source.CopyTo(destination);
            }
        }
    }
}
