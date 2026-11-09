using System.Collections.Generic;
using UnityEngine;

namespace HollowCreek
{
    public sealed class HollowCreekAtmosphere : MonoBehaviour
    {
        public HollowCreekCampaign campaign;
        public Light[] practicalLights;
        AudioSource ambience, musicBox, footsteps, threat, impact, feedback;
        readonly List<AudioClip> clips = new List<AudioClip>();
        Vector3 previous;
        float walked;
        void Start()
        {
            ambience = Make("Facility ventilation", 0, 4, true);
            musicBox = Make("Detuned winter music box", 1, 12, true);
            footsteps = Make("Metal footstep", 2, .3f, false);
            threat = Make("N breathing", 3, 3, true);
            impact = Make("Metal crash", 4, 1.1f, false);
            feedback = Make("Terminal tone", 5, .15f, false);
            ambience.volume = campaign.sceneNumber == 1 ? .07f : .12f;
            musicBox.volume = .05f; previous = campaign.player.transform.position;
        }
        AudioSource Make(string name, int kind, float seconds, bool loop)
        {
            const int rate = 22050;
            float[] samples = new float[Mathf.CeilToInt(seconds * rate)];
            var random = new System.Random(981 + kind); float filtered = 0;
            // Original winter music-box motif, detuned against a slow sub-harmonic.
            int[] notes = { 64, 67, 71, 67, 62, 65, 69, 65, 60, 64, 67, 63, 59, 62, 65, 58 };
            for (int i = 0; i < samples.Length; i++) {
                float t = i / (float)rate, noise = (float)random.NextDouble() * 2 - 1;
                filtered = Mathf.Lerp(filtered, noise, .045f);
                float wave;
                switch (kind) {
                    case 0: wave = filtered * .5f + Mathf.Sin(t * 2 * Mathf.PI * 47) * .13f; break;
                    case 1:
                        float pitch = 440 * Mathf.Pow(2, (notes[(int)(t / .75f) % notes.Length] - 69) / 12f);
                        float beat = t % .75f;
                        wave = (Mathf.Sin(t * 2 * Mathf.PI * pitch) + .3f * Mathf.Sin(t * 2 * Mathf.PI * pitch * 2.007f)) * Mathf.Exp(-beat * 6) * .32f; break;
                    case 2: wave = (noise * .3f + Mathf.Sin(t * 1200) * .5f) * Mathf.Exp(-t * 23); break;
                    case 3: wave = (filtered + Mathf.Sin(t * 350) * .1f) * (.3f + .7f * Mathf.Pow(Mathf.Sin(t * Mathf.PI / 3), 2)); break;
                    case 4: wave = (noise * .4f + Mathf.Sin(t * 220) * .5f) * Mathf.Exp(-t * 7); break;
                    default: wave = Mathf.Sin(t * 2 * Mathf.PI * 660) * .25f; break;
                }
                samples[i] = Mathf.Clamp(wave, -.8f, .8f) * Mathf.Min(1, t * 80) * Mathf.Min(1, (seconds - t) * 80);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); clips.Add(clip);
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>(); source.playOnAwake = false; source.clip = clip; source.loop = loop;
            source.volume = 0; if (loop) source.Play(); return source;
        }
        void Update()
        {
            if (ambience == null) return;
            bool paused = campaign.hud.Paused;
            if (paused) { ambience.Pause(); musicBox.Pause(); threat.Pause(); return; }
            ambience.UnPause(); musicBox.UnPause(); threat.UnPause();
            bool chasing = campaign.Rules.stage == Stage.Chase || campaign.Rules.stage == Stage.Vent;
            threat.volume = chasing ? Mathf.Lerp(.38f, .025f, Mathf.Clamp01(campaign.PursuerDistance / 28)) : 0;
            if (campaign.monster != null) threat.transform.position = campaign.monster.position + Vector3.up;
            threat.spatialBlend = 1; threat.minDistance = 2; threat.maxDistance = 35;
            musicBox.volume = chasing ? .018f : .05f;
            var position = campaign.player.transform.position;
            float delta = Vector3.Distance(previous, position); previous = position;
            if (delta < 1 && !campaign.player.InputBlocked) walked += delta;
            if (walked > (campaign.player.IsCrawling ? 1.2f : 1.6f)) { walked = 0; footsteps.volume = .16f; footsteps.pitch = chasing ? 1.13f : .93f; footsteps.Play(); }
            if (practicalLights != null) for (int i = 0; i < practicalLights.Length; i++) {
                var light = practicalLights[i]; if (light == null) continue;
                // Low amplitude flicker retains navigable lighting without full-screen strobing.
                light.intensity = 1.7f + Mathf.Sin(Time.time * (chasing ? 7 : 2) + i * 3.2f) * (chasing ? .45f : .12f);
            }
        }
        public void Cue(bool success) { if (feedback != null) { feedback.pitch = success ? 1.2f : .55f; feedback.volume = .22f; feedback.Play(); } }
        public void Impact(float amount) { if (impact != null) { impact.volume = Mathf.Clamp01(amount) * .55f; impact.Play(); } }
        void OnDestroy() { foreach (var clip in clips) if (clip != null) Destroy(clip); }
    }
}
