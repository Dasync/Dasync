using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Communication.InMemory
{
    public class MethodInvocationData : IMethodInvocationData, IMethodSerializedParameters
    {
        public MethodInvocationData()
        {
        }

        public MethodInvocationData(Message message, ISerializerProvider serializerProvider)
        {
            IntentId = (string)message.Data["IntentId"];
            Service = (ServiceId)message.Data["Service"];
            Method = (MethodId)message.Data["Method"];
            Continuation = (ContinuationDescriptor)message.Data["Continuation"];
            Caller = (CallerDescriptor)message.Data["Caller"];
            FlowContext = (Dictionary<string, string>)message.Data["FlowContext"];
            Format = (string)message.Data["Format"];
            SerializedForm = message.Data["Parameters"];
            SerializerProvider = serializerProvider;
        }

        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        public string IntentId { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public string Format { get; set; }

        public object SerializedForm { get; set; }

        public ISerializerProvider SerializerProvider { get; set; }

        public Task ReadInputParameters(IValueContainer target)
        {
            if (SerializerProvider == null)
                throw new InvalidOperationException("Serializer provider is not initialized.");
            var serializer = SerializerProvider.GetSerializer(Format);
            serializer.Populate((string)SerializedForm, target);
            return Task.CompletedTask;
        }
    }
}
