using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace HollowCreek
{
    public sealed class SoloCinematicSlot : MonoBehaviour
    {
        public PlayableDirector timeline;
        public Camera cinematicCamera;
        public Camera gameplayCamera;

        public IEnumerator Play()
        {
            if (timeline == null || timeline.playableAsset == null) yield break;

            var documents = FindObjectsByType<UIDocument>(FindObjectsInactive.Include);
            var documentStates = new bool[documents.Length];
            for (int i = 0; i < documents.Length; i++)
            {
                documentStates[i] = documents[i].enabled;
                documents[i].enabled = false;
            }
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            var canvasStates = new bool[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                canvasStates[i] = canvases[i].enabled;
                canvases[i].enabled = false;
            }
            bool oldGameplayCamera = gameplayCamera != null && gameplayCamera.enabled;
            bool oldCinematicCamera = cinematicCamera != null && cinematicCamera.enabled;
            if (gameplayCamera != null && cinematicCamera != null) gameplayCamera.enabled = false;
            if (cinematicCamera != null) cinematicCamera.enabled = true;
            timeline.Play();
            float timeout = Time.realtimeSinceStartup + Mathf.Max(2f, (float)timeline.duration + 2f);
            while (timeline.state == PlayState.Playing && Time.realtimeSinceStartup < timeout) yield return null;
            timeline.Stop();
            if (cinematicCamera != null) cinematicCamera.enabled = oldCinematicCamera;
            if (gameplayCamera != null) gameplayCamera.enabled = oldGameplayCamera;
            for (int i = 0; i < canvases.Length; i++) if (canvases[i] != null) canvases[i].enabled = canvasStates[i];
            for (int i = 0; i < documents.Length; i++) if (documents[i] != null) documents[i].enabled = documentStates[i];
        }
    }
}
