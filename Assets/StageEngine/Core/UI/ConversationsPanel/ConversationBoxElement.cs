
using UnityEngine.UIElements;
using System.Collections.Generic;
using StageEngine.Core.Conversations;

namespace StageEngine.Core.UI
{
    [UxmlElement]
    public partial class ConversationBoxElement : VisualElement, IInitializableElement
    {
        private VisualElement ConversationContent => this.Q<VisualElement>("conversation-content");
        private List<ConversationMessageElement> messageElements = new List<ConversationMessageElement>();

        public VisualTreeAsset ConversationMessageTemplate { get; set; }

        public void Init()
        {
            if (ConversationsManager.Instance != null)
            {
                ConversationsManager.Instance.OnMessageAdded += OnMessageAdded;
                ConversationsManager.Instance.OnMessageSet += OnMessageSet;
                ConversationsManager.Instance.OnMessageRemoved += OnMessageRemoved;
                ConversationsManager.Instance.OnMessagesCleared += OnMessagesCleared;
                ConversationsManager.Instance.OnMessageContentUpdated += OnMessageContentUpdated;

                RefreshAllMessages();
            }
        }

        private void OnMessageAdded(ConversationMessage message)
        {
            AddMessageElement(message);
        }

        private void OnMessageSet(int index, ConversationMessage message)
        {
            if (index >= 0 && index < messageElements.Count)
            {
                messageElements[index].Init(message);
            }
        }

        private void OnMessageRemoved(int index)
        {
            if (index >= 0 && index < messageElements.Count)
            {
                var element = messageElements[index];
                ConversationContent.Remove(element);
                messageElements.RemoveAt(index);
            }
        }

        private void OnMessagesCleared()
        {
            ConversationContent.Clear();
            messageElements.Clear();
        }

        private void OnMessageContentUpdated(int index, string content)
        {
            if (index >= 0 && index < messageElements.Count)
            {
                messageElements[index].UpdateContent(content);
            }
        }

        private void RefreshAllMessages()
        {
            ConversationContent.Clear();
            messageElements.Clear();

            if (ConversationsManager.Instance != null)
            {
                foreach (var message in ConversationsManager.Instance.Messages)
                {
                    AddMessageElement(message);
                }
            }
        }

        private void AddMessageElement(ConversationMessage message)
        {
            if (ConversationMessageTemplate == null) return;

            var container = ConversationMessageTemplate.Instantiate();
            var element = container.Q<ConversationMessageElement>();
            if (element != null)
            {
                ConversationContent.Add(element);
                element.Init(message);
                messageElements.Add(element);
            }
        }
    }
}
