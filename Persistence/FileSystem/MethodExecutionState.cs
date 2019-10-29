using System;
using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Persistence.FileSystem
{
    public class MethodExecutionState : IMethodExecutionState
    {
        public ISerializerProvider SerializerProvider { get; set; }

        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        //public byte[] MethodStateData { get; set; }

        public IValueContainer MethodState { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public ISerializedMethodContinuationState CallerState { get; set; }

        public void ReadMethodState(IValueContainer container)
        {
            if (MethodState is ISerializedValueContainer serializedValueContainer)
            {
                var format = serializedValueContainer.GetFormat();
                var serializer = SerializerProvider.GetSerializer(format);
                var serializedForm = serializedValueContainer.GetSerializedForm();
                if (serializedForm is string stringContent)
                {
                    serializer.Populate(stringContent, container);
                }
                else if (serializedForm is byte[] byteContent)
                {
                    serializer.Populate(byteContent, container);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported runtime type '{serializedForm?.GetType()}' for a serialized form of '{format}'.");
                }
            }
            else
            {
                MethodState.CopyTo(container);
            }
        }
    }
}
