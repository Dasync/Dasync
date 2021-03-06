﻿using System;
using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public static class SerializerExtensions
    {
        public static T Deserialize<T>(this ISerializer serializer, Stream stream)
        {
            return (T)serializer.Deserialize(stream, typeof(T));
        }

        public static T Deserialize<T>(this ISerializer serializer, string data)
        {
            if (serializer is ITextSerializer textSerializer)
            {
                return textSerializer.Deserialize<T>(data);
            }
            else
            {
                using (var stream = new MemoryStream(Convert.FromBase64String(data), writable: false))
                    return serializer.Deserialize<T>(stream);
            }
        }

        public static T Deserialize<T>(this ISerializer serializer, byte[] data)
        {
            using (var stream = new MemoryStream(data, writable: false))
                return serializer.Deserialize<T>(stream);
        }

        public static T Deserialize<T>(this ITextSerializer serializer, string data)
        {
            using (var reader = new StringReader(data))
                return (T)serializer.Deserialize(reader, typeof(T));
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

        public static void Populate(this ISerializer serializer, byte[] data, IValueContainer target)
        {
            using (var stream = new MemoryStream(data, writable: false))
                serializer.Populate(stream, target);
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
                serializedValueContainer.GetFormat() == serializer.Format)
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
                serializedValueContainer.GetFormat() == serializer.Format &&
                serializedValueContainer.GetSerializedForm() is string serializedString)
                return serializedString;

            using (var textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, @object);
                return textWriter.ToString();
            }
        }

        public static byte[] SerializeToBytes(this ISerializer serializer, object @object)
        {
            if (@object is ISerializedValueContainer serializedValueContainer &&
                serializedValueContainer.GetFormat() == serializer.Format)
            {
                var serializedForm = serializedValueContainer.GetSerializedForm();
                if (serializedForm is byte[] serializedData)
                {
                    return serializedData;
                }
            }

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, @object);
                return stream.ToArray();
            }
        }
    }
}
