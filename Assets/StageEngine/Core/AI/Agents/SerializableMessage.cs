using AutoGen.Core;
using System;
using System.Collections.Generic;

namespace StageEngine.Core.AI.Agents
{
    [Serializable]
    public class SerializableMessage
    {
        public string Content { get; set; }
        public string From { get; set; }
        public string Role { get; set; }
        public DateTime Timestamp { get; set; }

        public SerializableMessage() { }

        public SerializableMessage(IMessage message)
        {
            Content = message.GetContent();
            From = message.From ?? "Unknown";
            Role = message is TextMessage textMsg ? textMsg.Role.ToString() : "Unknown";
            Timestamp = DateTime.Now;
        }

        public IMessage ToMessage()
        {
            var role = AutoGen.Core.Role.Assistant;
            return new TextMessage(role, Content, from: From);
        }
    }

    [Serializable]
    public class AgentsMessageHistory
    {
        public List<SerializableMessage> Messages { get; set; } = new List<SerializableMessage>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}