using UnityEngine;
using UnityEngine.Rendering;

namespace FacilityEscape
{
    /// <summary>현재 프로젝트의 Built-in Render Pipeline용 카메라별 흑백 효과.</summary>
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    public sealed class MultiGrayscaleCamera : MonoBehaviour
    {
        private Material grayscaleMaterial;

        private void OnEnable()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                Debug.LogError("MultiGrayscaleCamera는 Built-in 전용입니다. SRP에서는 카메라별 후처리를 연결하세요.", this);
                enabled = false;
                return;
            }
            var shader = Resources.Load<Shader>("MultiEscapeGrayscale");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("Resources/MultiEscapeGrayscale.shader를 불러올 수 없습니다.", this);
                enabled = false;
                return;
            }
            grayscaleMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (grayscaleMaterial != null) Graphics.Blit(source, destination, grayscaleMaterial);
            else Graphics.Blit(source, destination);
        }

        private void OnDisable()
        {
            if (grayscaleMaterial != null) Destroy(grayscaleMaterial);
            grayscaleMaterial = null;
        }
    }
}
