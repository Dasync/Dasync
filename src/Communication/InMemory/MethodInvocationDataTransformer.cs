using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;

namespace Dasync.Communication.InMemory
{
    public class MethodInvocationDataTransformer
    {
        public static void Write(Message message, MethodInvocationData data, ISerializer serializer)
        {
            message.Data["IntentId"] = data.IntentId;
            message.Data["Service"] = data.Service.Clone();
            message.Data["Method"] = data.Method.Clone();
            message.Data["Continuation"] = data.Continuation;
            message.Data["Continuation:Format"] = data.ContinuationState?.Format;
            message.Data["Continuation:State"] = data.ContinuationState?.State;
            message.Data["Format"] = serializer.Format;
            message.Data["Parameters"] = serializer.SerializeToString(data.Parameters);
            message.Data["Caller"] = data.Caller?.Clone();
            message.Data["FlowContext"] = data.FlowContext;
        }

        public static MethodInvocationData Read(Message message, ISerializerProvider serializerProvider)
        {
            return new MethodInvocationData
            {
                IntentId = (string)message.Data["IntentId"],
                Service = (ServiceId)message.Data["Service"],
                Method = (MethodId)message.Data["Method"],
                Continuation = (ContinuationDescriptor)message.Data["Continuation"],
                ContinuationState = TryGetMethodContinuationState(message),
                Caller = (CallerDescriptor)message.Data["Caller"],
                FlowContext = (Dictionary<string, string>)message.Data["FlowContext"],
                Parameters = new SerializedValueContainer(
                    (string)message.Data["Format"],
                    message.Data["Parameters"],
                    serializerProvider)
            };
        }

        internal static SerializedMethodContinuationState TryGetMethodContinuationState(Message message)
        {
            if (message.Data.TryGetValue("Continuation:State", out var stateObj) && stateObj is byte[] state && state.Length > 0)
            {
                return new SerializedMethodContinuationState
                {
                    State = state,
                    Format =
                        message.Data.TryGetValue("Continuation:Format", out var contentTypeObj) && contentTypeObj is string format
                        ? format
                        : null
                };
            }

            return null;
        }
    }
}
