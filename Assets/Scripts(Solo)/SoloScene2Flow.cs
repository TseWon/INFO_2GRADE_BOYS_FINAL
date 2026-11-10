using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowCreek
{
    [RequireComponent(typeof(SnowFirstPerson))]
    public sealed class SoloScene2Flow : MonoBehaviour
    {
        public Transform elevatorInteractionPoint;
        public SoloCinematicSlot transitionCinematic;
        public SoloScene2InteractionHint interactionHint;
        public float interactionRadius = 3f;
        SnowFirstPerson player;
        bool loading;

        void Awake() { player = GetComponent<SnowFirstPerson>(); }
        void Update()
        {
            if (elevatorInteractionPoint == null) { if (interactionHint != null) interactionHint.SetVisible(false); return; }
            var delta = elevatorInteractionPoint.position - transform.position;
            bool inRange = Mathf.Abs(delta.y) <= 2.5f && new Vector2(delta.x, delta.z).magnitude <= interactionRadius;
            if (interactionHint != null) interactionHint.SetVisible(inRange && !loading);
            if (loading || !inRange || !Input.GetKeyDown(KeyCode.E)) return;
            StartCoroutine(BoardElevator());
        }
        IEnumerator BoardElevator()
        {
            int nextIndex = SoloSceneRoutes.BuildIndex(SoloSceneRoutes.ElevatorChapter);
            if (nextIndex < 0)
            {
                Debug.LogError("Missing solo scene: " + SoloSceneRoutes.ElevatorChapter, this);
                yield break;
            }
            loading = true; player.InputBlocked = true;
            if (interactionHint != null) interactionHint.SetVisible(false);
            if (transitionCinematic != null) yield return transitionCinematic.Play();
            HollowCreekSession.Enter(2);
            HollowCreekSession.Save();
            var operation = SceneManager.LoadSceneAsync(nextIndex);
            if (operation == null) { loading = false; player.InputBlocked = false; yield break; }
            while (!operation.isDone) yield return null;
        }
    }
}
