using UnityEngine;
using UnityEngine.UIElements;

namespace StageEngine.Core.UI
{
    public class UIInitializer : MonoBehaviour
    {
        void Start()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                InitializeAllElements(uiDocument.rootVisualElement);
            }
        }

        private void InitializeAllElements(VisualElement root)
        {
            var initializableElements = root.Query<VisualElement>()
                .Where(element => element is IInitializableElement)
                .ToList();

            foreach (var element in initializableElements)
            {
                if (element is IInitializableElement initializableElement)
                {
                    initializableElement.Init();
                }
            }
        }
    }
}
