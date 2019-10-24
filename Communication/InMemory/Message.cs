using System;
using System.Collections.Generic;

namespace Dasync.Communication.InMemory
{
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public MessageType Type { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public DateTimeOffset? DeliverAt { get; set; }

        public int DeliveryCount { get; set; }

        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}
