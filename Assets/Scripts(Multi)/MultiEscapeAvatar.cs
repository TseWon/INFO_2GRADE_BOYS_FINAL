using UnityEngine;

namespace FacilityEscape
{
    // Replace the child meshes with your own character. Keep the head/pose references.
    public sealed class MultiEscapeAvatar : MonoBehaviour
    {
        [SerializeField] private MultiEscapePlayer player;
        [SerializeField] private Transform visualRoot, head, seatedLegs, standingLegs;
        private Transform blindfold, earDefenders;

        private void Awake()
        {
            if (head == null) return;
            blindfold = head.Find("Blindfold");
            earDefenders = head.Find("EarDefenders");
        }

        private void LateUpdate()
        {
            if (player == null || visualRoot == null) return;
            bool free = player.SensoryRestrictionsReleased;
            if (blindfold != null) blindfold.gameObject.SetActive(!free);
            if (earDefenders != null) earDefenders.gameObject.SetActive(!free);
            var lan = player.Game.Lan;
            visualRoot.gameObject.SetActive(!player.LocalInput && (lan == null || lan.Connected));
            if (!visualRoot.gameObject.activeSelf) return;
            visualRoot.localRotation = free
                ? Quaternion.Euler(0, player.ViewCamera.transform.eulerAngles.y, 0) : Quaternion.identity;
            if (head != null) head.rotation = player.ViewCamera.transform.rotation;
            if (seatedLegs != null) seatedLegs.gameObject.SetActive(!free);
            if (standingLegs != null) standingLegs.gameObject.SetActive(free);
        }
    }
}
