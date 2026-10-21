using UnityEngine;

namespace FacilityEscape
{
    public sealed class MultiEscapeFeedback : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField] private Transform exitDoor;
        [SerializeField] private Vector3 openOffset = new Vector3(0, 3.4f, 0);
        [SerializeField] private Transform leverHandle;
        [SerializeField] private Light[] chairShockLights = new Light[0];
        private Vector3 closedDoor;
        private Quaternion leverRest;
        private double flashUntil;
        private bool escaped;
        public int ShockCount { get; private set; }

        private void Awake()
        {
            if (exitDoor != null) closedDoor = exitDoor.localPosition;
            if (leverHandle != null) leverRest = leverHandle.localRotation;
        }
        private void OnEnable()
        {
            if (game == null) return;
            game.onRoundStarted.AddListener(ResetFeedback);
            game.onElectricShock.AddListener(Shock);
            game.onEscaped.AddListener(Escape);
        }
        private void OnDisable()
        {
            if (game != null)
            {
                game.onRoundStarted.RemoveListener(ResetFeedback);
                game.onElectricShock.RemoveListener(Shock);
                game.onEscaped.RemoveListener(Escape);
            }
            foreach (var light in chairShockLights) if (light != null) light.enabled = false;
        }
        private void ResetFeedback()
        {
            escaped = false;
            ShockCount = 0;
            flashUntil = 0;
            if (exitDoor != null) exitDoor.localPosition = closedDoor;
            if (leverHandle != null) leverHandle.localRotation = leverRest;
        }
        private void Shock() { ShockCount++; flashUntil = Time.realtimeSinceStartupAsDouble + 0.4; }
        private void Escape()
        {
            escaped = true;
            if (leverHandle != null) leverHandle.localRotation = leverRest * Quaternion.Euler(40, 0, 0);
        }
        private void Update()
        {
            if (exitDoor != null)
                exitDoor.localPosition = Vector3.MoveTowards(exitDoor.localPosition,
                    escaped ? closedDoor + openOffset : closedDoor, 2.8f * Time.unscaledDeltaTime);
            foreach (var light in chairShockLights)
                if (light != null) light.enabled = Time.realtimeSinceStartupAsDouble < flashUntil;
        }
    }
}
