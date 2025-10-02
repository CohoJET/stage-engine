using System;
using UnityEngine;

namespace StageEngine.Core.Conversations
{
    public class ConversationMessage
    {
        public string Sender { get; set; }
        public Color FlairColor { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
