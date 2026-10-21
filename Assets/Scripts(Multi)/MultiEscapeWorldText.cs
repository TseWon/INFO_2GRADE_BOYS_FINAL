using UnityEngine;

namespace FacilityEscape
{
    // 기본 TextMesh 폰트 셰이더의 벽 관통을 방지하고 동적 폰트 아틀라스를 갱신합니다.
    [ExecuteAlways, RequireComponent(typeof(TextMesh))]
    public sealed class MultiEscapeWorldText : MonoBehaviour
    {
        private TextMesh textMesh;
        private Renderer textRenderer;
        private Material material, original;
        private void OnEnable()
        {
            textMesh = GetComponent<TextMesh>();
            textRenderer = GetComponent<Renderer>();
            original = textRenderer.sharedMaterial;
            var shader = Resources.Load<Shader>("MultiEscapeWorldText");
            if (shader == null) return;
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            UpdateAtlas(textMesh.font);
            textRenderer.sharedMaterial = material;
            Font.textureRebuilt += UpdateAtlas;
        }
        private void UpdateAtlas(Font font)
        {
            if (material != null && font != null && font == textMesh.font)
                material.mainTexture = font.material.mainTexture;
        }
        private void OnDisable()
        {
            Font.textureRebuilt -= UpdateAtlas;
            if (textRenderer != null && textRenderer.sharedMaterial == material) textRenderer.sharedMaterial = original;
            if (material != null)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }
    }
}
