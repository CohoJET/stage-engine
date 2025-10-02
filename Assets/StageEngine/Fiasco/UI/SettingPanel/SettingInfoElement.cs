using UnityEngine;
using UnityEngine.UIElements;

namespace StageEngine.Fiasco.UI
{
    [UxmlElement]
    public partial class SettingInfoElement : VisualElement
    {
        private Label SettingHeader => this.Q<Label>("setting-header");
        private Label SettingDescription => this.Q<Label>("setting-description");

        public SettingInfoElement() { }
    }
}
