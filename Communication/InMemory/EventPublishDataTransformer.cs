using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.Serialization;

namespace Dasync.Communication.InMemory
{
    public class EventPublishDataTransformer
    {
        public static void Write(Message message, EventPublishData data, PublishPreferences preferences, ISerializer serializer)
        {
            message.Data["IntentId"] = data.IntentId;
            message.Data["Service"] = data.Service.Clone();
            message.Data["Event"] = data.Event.Clone();
            message.Data["Format"] = serializer.Format;
            message.Data["Parameters"] = serializer.SerializeToString(data.Parameters);
            message.Data["Caller"] = data.Caller?.Clone();
            message.Data["SkipLocal"] = preferences.SkipLocalSubscribers;
        }

        public static EventPublishData Read(Message message, ISerializerProvider serializerProvider, out bool skipLocalEvents)
        {
            skipLocalEvents = (bool)message.Data["SkipLocal"];
            return new EventPublishData
            {
                IntentId = (string)message.Data["IntentId"],
                Service = (ServiceId)message.Data["Service"],
                Event = (EventId)message.Data["Event"],
                Caller = (CallerDescriptor)message.Data["Caller"],
                Parameters = new SerializedValueContainer(
                    (string)message.Data["Format"],
                    message.Data["Parameters"],
                    serializerProvider)
            };
        }
    }
}
