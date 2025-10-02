using UnityEngine.UIElements;

namespace StageEngine.Fiasco.UI
{
    [UxmlElement]
    public partial class DicePoolElement : VisualElement
    {
        private Label DicePoolHeader => this.Q<Label>("dice-pool-header");
        private VisualElement WhiteDiceRow => this.Q<VisualElement>("white-dice-row");
        private VisualElement BlackDiceRow => this.Q<VisualElement>("black-dice-row");

        public DicePoolElement() { }
    }
}