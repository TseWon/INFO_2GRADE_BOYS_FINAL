using UnityEngine;

namespace FacilityEscape
{
    // Replace the child meshes with your own character. Keep the head/pose references.
    public sealed class MultiEscapeAvatar : MonoBehaviour
    {
        [SerializeField] private MultiEscapePlayer player;
        [SerializeField] private Transform visualRoot, head, seatedLegs, standingLegs;

        private void LateUpdate()
        {
            if (player == null || visualRoot == null) return;
            var lan = player.Game.Lan;
            visualRoot.gameObject.SetActive(!player.LocalInput && (lan == null || lan.Connected));
            if (!visualRoot.gameObject.activeSelf) return;
            bool free = player.Game.Rules.Stage == EscapeStage.Escaped;
            visualRoot.localRotation = free
                ? Quaternion.Euler(0, player.ViewCamera.transform.eulerAngles.y, 0) : Quaternion.identity;
            if (head != null) head.rotation = player.ViewCamera.transform.rotation;
            if (seatedLegs != null) seatedLegs.gameObject.SetActive(!free);
            if (standingLegs != null) standingLegs.gameObject.SetActive(free);
        }
    }
}
