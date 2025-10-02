using StageEngine.Core.Conversations;
using UnityEngine.UIElements;

namespace StageEngine.Core.UI
{
    [UxmlElement]
    public partial class ConversationMessageElement : VisualElement
    {
        private Label SenderLabel => this.Q<Label>("sender-label");
        private VisualElement Flair => this.Q<VisualElement>("flair");
        private Label ContentLabel => this.Q<Label>("content-label");
        private Label TimestampLabel => this.Q<Label>("timestamp-label");

        public void Init(ConversationMessage message)
        {
            SenderLabel.text = message.Sender;
            Flair.style.backgroundColor = message.FlairColor;
            ContentLabel.text = message.Content;
            TimestampLabel.text = message.Timestamp.ToString("HH:mm:ss");
        }

        public void UpdateContent(string content)
        {
            ContentLabel.text = content;
        }

        public ConversationMessageElement() { }
    }
}
