using UnityEngine;
using UnityEngine.UIElements;

namespace StageEngine.Core.UI
{
    [UxmlElement]
    public partial class GameInfoElement : VisualElement
    {
        private Label GameTitleLablel => this.Q<Label>("game-title");
        private Label SceneTitleLabel => this.Q<Label>("scene-title");

        public GameInfoElement() { }
    }
}
