using UnityEngine.UIElements;

namespace StageEngine.Core.UI
{
    [UxmlElement]
    public partial class SceneInfoElement : VisualElement
    {
        private Label SceneTitleLabel => this.Q<Label>("scene-title");

        public SceneInfoElement() { }
    }
}
