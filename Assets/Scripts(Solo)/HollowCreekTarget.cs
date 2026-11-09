using UnityEngine;
namespace HollowCreek
{
    public sealed class HollowCreekTarget : MonoBehaviour
    {
        public Interaction interaction;
        public string label;
        public Route route;
        public HollowCreekCampaign campaign;
        public Transform marker;
        public bool Available => campaign != null && campaign.IsAvailable(this);
        void Update()
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(Available);
                marker.localScale = Vector3.one * (.07f + Mathf.Sin(Time.time * 3) * .012f);
            }
        }
        public void Use() { if (Available) campaign.Interact(this); }
        void OnDisable() { if (marker != null) marker.gameObject.SetActive(false); }
    }
}
