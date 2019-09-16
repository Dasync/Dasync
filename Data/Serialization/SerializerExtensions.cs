using System;
using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public static class SerializerExtensions
    {
        public static T Deserialize<T>(this ISerializer serializer, Stream stream) where T : new()
        {
            // Need to be 'object', because T can be a value type.
            object target = new T();
            var propertySet =
                typeof(IValueContainer).IsAssignableFrom(typeof(T))
                ? (IValueContainer)target
                : ValueContainerFactory.CreateProxy(target);
            using (stream)
                serializer.Populate(stream, propertySet);
            return (T)target;
        }

        public static T Deserialize<T>(this ISerializer serializer, string data) where T : new()
        {
            if (serializer is ITextSerializer textSerializer)
            {
                return textSerializer.Deserialize<T>(data);
            }
            else
            {
                using (var stream = new MemoryStream(Convert.FromBase64String(data)))
                    return serializer.Deserialize<T>(stream);
            }
        }

        public static T Deserialize<T>(this ITextSerializer serializer, string data) where T : new()
        {
            // Need to be 'object', because T can be a value type.
            object target = new T();
            var propertySet =
                typeof(IValueContainer).IsAssignableFrom(typeof(T))
                ? (IValueContainer)target
                : ValueContainerFactory.CreateProxy(target);
            using (var reader = new StringReader(data))
                serializer.Populate(reader, propertySet);
            return (T)target;
        }

        public static void Populate(this ISerializer serializer, string data, IValueContainer target)
        {
            if (serializer is ITextSerializer textSerializer)
            {
                textSerializer.Populate(data, target);
            }
            else
            {
                using (var stream = new MemoryStream(Convert.FromBase64String(data)))
                    serializer.Populate(stream, target);
            }
        }

        public static void Populate(this ITextSerializer serializer, string data, IValueContainer target)
        {
            using (var reader = new StringReader(data))
                serializer.Populate(reader, target);
        }

        public static string SerializeToString(this ISerializer serializer, object @object)
        {
            if (serializer is ITextSerializer textSerializer)
            {
                return textSerializer.SerializeToString(@object);
            }

            if (@object is ISerializedValueContainer serializedValueContainer &&
                serializedValueContainer.GetContentType() == serializer.ContentType)
            {
                var serializedForm = serializedValueContainer.GetSerializedForm();
                if (serializedForm is string serializedString)
                {
                    return serializedString;
                }

                if (serializedForm is byte[] serializedData)
                {
                    using (var stream = new MemoryStream())
                    {
                        return Convert.ToBase64String(serializedData);
                    }
                }
            }

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, @object);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static string SerializeToString(this ITextSerializer serializer, object @object)
        {
            if (@object is ISerializedValueContainer serializedValueContainer &&
                serializedValueContainer.GetContentType() == serializer.ContentType &&
                serializedValueContainer.GetSerializedForm() is string serializedString)
                return serializedString;

            using (var textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, @object);
                return textWriter.ToString();
            }
        }
    }
}
