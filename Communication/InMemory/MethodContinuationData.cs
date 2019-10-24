using System;
using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.Serialization;

namespace Dasync.Communication.InMemory
{
    public class MethodContinuationData : IMethodContinuationData
    {
        public MethodContinuationData()
        {
        }

        public MethodContinuationData(Message message)
        {
            IntentId = (string)message.Data["IntentId"];
            Service = (ServiceId)message.Data["Service"];
            Method = (PersistedMethodId)message.Data["Method"];
            TaskId = (string)message.Data["TaskId"];
            Caller = (CallerDescriptor)message.Data["Caller"];
        }

        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public string TaskId { get; set; }

        public string IntentId { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public ISerializer Serializer { get; set; }

        public string SerializedResult { get; set; }

        public TaskResult ReadResult(Type expectedResultValueType)
        {
            // TODO: use 'expectedResultValueType'
            return Serializer.Deserialize<TaskResult>(SerializedResult);
        }
    }
}
