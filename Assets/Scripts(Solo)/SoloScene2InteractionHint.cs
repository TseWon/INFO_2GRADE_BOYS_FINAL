using UnityEngine;
using UnityEngine.UIElements;

namespace HollowCreek
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SoloScene2InteractionHint : MonoBehaviour
    {
        public StyleSheet stylesheet;
        Label prompt;
        bool visible;

        void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            if (stylesheet != null) root.styleSheets.Add(stylesheet);
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 22);
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            prompt = new Label("E  승강기 탑승") { pickingMode = PickingMode.Ignore };
            prompt.AddToClassList("prompt");
            prompt.style.display = DisplayStyle.None;
            root.Add(prompt);
        }

        public void SetVisible(bool value)
        {
            if (prompt == null || visible == value) return;
            visible = value;
            prompt.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
