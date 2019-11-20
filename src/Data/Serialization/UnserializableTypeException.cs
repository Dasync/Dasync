using System;

namespace Dasync.Serialization
{
    public class UnserializableTypeException : Exception
    {
        public UnserializableTypeException(Type type)
            : base($"Cannot serialize type '{type}'.")
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
