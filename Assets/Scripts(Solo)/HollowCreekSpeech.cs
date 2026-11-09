using System;
using UnityEngine;
namespace HollowCreek
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class HollowCreekSpeech : MonoBehaviour
    {
        [Serializable] public sealed class Line { public string startsWith; public AudioClip clip; public float pitch = .9f; }
        public Line[] lines;
        AudioSource speaker;
        HollowCreekCampaign campaign;
        bool wasPaused;
        public float Duration { get; private set; }
        void Awake() { speaker = GetComponent<AudioSource>(); speaker.playOnAwake = false; speaker.loop = false; speaker.spatialBlend = 0; speaker.volume = .65f; speaker.priority = 16; campaign = GetComponent<HollowCreekCampaign>(); }
        public float Speak(string text)
        {
            Stop();
            if (lines == null || string.IsNullOrEmpty(text)) return 0;
            foreach (var line in lines) if (line.clip != null && !string.IsNullOrEmpty(line.startsWith) && text.StartsWith(line.startsWith, StringComparison.Ordinal)) {
                speaker.clip = line.clip; speaker.pitch = Mathf.Clamp(line.pitch, .75f, 1.1f);
                Duration = line.clip.length / speaker.pitch; speaker.Play(); return Duration;
            }
            return 0;
        }
        public void Stop() { if (speaker != null) speaker.Stop(); Duration = 0; }
        void Update()
        {
            bool paused = campaign != null && campaign.hud != null && campaign.hud.Paused;
            if (paused && !wasPaused) speaker.Pause(); else if (!paused && wasPaused) speaker.UnPause();
            wasPaused = paused;
        }
    }
}
