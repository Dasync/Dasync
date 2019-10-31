using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;

namespace Dasync.Communication.InMemory
{
    public class MethodContinuationDataTransformer
    {
        public static void Write(Message message, MethodContinuationData data,
            SerializedMethodContinuationState continuationState, ISerializer serializer)
        {
            message.Data["IntentId"] = data.IntentId;
            message.Data["Service"] = data.Service.Clone();
            message.Data["Method"] = data.Method.Clone();
            message.Data["TaskId"] = data.TaskId;
            message.Data["Continuation:Format"] = continuationState?.Format;
            message.Data["Continuation:State"] = continuationState?.State;
            message.Data["Format"] = serializer.Format;
            message.Data["Result"] = serializer.SerializeToString(data.Result);
            message.Data["Caller"] = data.Caller?.Clone();
        }

        public static MethodContinuationData Read(Message message, ISerializerProvider serializerProvider)
        {
            return new MethodContinuationData
            {
                IntentId = (string)message.Data["IntentId"],
                Service = (ServiceId)message.Data["Service"],
                Method = (PersistedMethodId)message.Data["Method"],
                TaskId = (string)message.Data["TaskId"],
                Caller = (CallerDescriptor)message.Data["Caller"],
                Result = new SerializedValueContainer(
                    (string)message.Data["Format"],
                    message.Data["Result"],
                    serializerProvider)
            };
        }
    }
}
