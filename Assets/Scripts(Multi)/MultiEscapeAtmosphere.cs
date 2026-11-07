using System.Collections.Generic;
using UnityEngine;

namespace FacilityEscape
{
    [DefaultExecutionOrder(300)]
    public sealed class MultiEscapeAtmosphere : MonoBehaviour
    {
        public MultiEscapeCampaign campaign;
        private AudioSource breath, footsteps, saw, siren, swarm, impact, drone;
        private ParticleSystem insects;
        private Material insectMaterial;
        private Texture2D insectTexture;
        private readonly List<AudioClip> clips = new List<AudioClip>();
        private readonly Vector3[] cameraRest = new Vector3[2];
        private Vector3 previousPosition;
        private float footDistance;
        private bool hitPlayed;

        private void Start()
        {
            breath = Source("Breathing", 0, 3, true);
            footsteps = Source("MetalFootstep", 1, .3f, false);
            saw = Source("Chainsaw", 2, 2, true);
            siren = Source("Alarm", 3, 4, true);
            swarm = Source("InsectSwarm", 4, 2, true);
            impact = Source("TableImpact", 5, .6f, false);
            drone = Source("BasementDrone", 6, 4, true);
            cameraRest[0] = campaign.playerA.ViewCamera.transform.localPosition;
            cameraRest[1] = campaign.playerB.ViewCamera.transform.localPosition;
            var obj = new GameObject("InsectSwarm_PooledParticles");
            obj.transform.SetParent(transform, false);
            insects = obj.AddComponent<ParticleSystem>();
            insects.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = insects.main;
            main.playOnAwake = false; main.loop = true; main.duration = 4;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.3f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(.025f, .065f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.maxParticles = 4000; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new Color(.12f, .065f, .025f, 1);
            var emission = insects.emission; emission.rateOverTime = 3200;
            var shape = insects.shape; shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.8f, 1.2f, .4f);
            var noise = insects.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = 7;
            insectTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = x - 16, dy = y - 16;
                bool body = dx * dx / 20 + dy * dy / 90 < 1;
                bool legs = Mathf.Abs(dx) < 12 && (Mathf.Abs(dy - dx * .4f) < 1.2f || Mathf.Abs(dy + dx * .4f) < 1.2f || Mathf.Abs(dy) < 1);
                insectTexture.SetPixel(x, y, body || legs ? Color.white : Color.clear);
            }
            insectTexture.Apply();
            var insectTemplate = Resources.Load<Material>("MultiEscapeInsects");
            insectMaterial = insectTemplate != null ? new Material(insectTemplate) : new Material(Shader.Find("Particles/Standard Unlit"));
            insectMaterial.SetFloat("_Mode", 2); insectMaterial.SetFloat("_SrcBlend", 5); insectMaterial.SetFloat("_DstBlend", 10);
            insectMaterial.SetFloat("_ZWrite", 0); insectMaterial.EnableKeyword("_ALPHABLEND_ON"); insectMaterial.renderQueue = 3000;
            insectMaterial.mainTexture = insectTexture;
            insects.GetComponent<ParticleSystemRenderer>().sharedMaterial = insectMaterial;
        }
        private AudioSource Source(string name, int kind, float length, bool loop)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(length * rate)];
            var random = new System.Random(kind * 47 + 11);
            float filtered = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float n = (float)random.NextDouble() * 2 - 1;
                filtered = Mathf.Lerp(filtered, n, .1f);
                float wave;
                switch (kind)
                {
                    case 0: wave = filtered * Mathf.Pow(Mathf.Max(0, Mathf.Sin(t / length * Mathf.PI * 2)), 1.3f); break;
                    case 1: wave = (n * .35f + Mathf.Sin(t * 1800) * .35f) * Mathf.Exp(-t * 22); break;
                    case 2: wave = (Mathf.PingPong(t * 87, 1) * 2 - 1) * .38f + n * .17f; break;
                    case 3: wave = Mathf.Sin(2 * Mathf.PI * 460 * t - 170 * Mathf.Cos(Mathf.PI * t)) * .35f; break;
                    case 4: wave = n * .35f + Mathf.Sin(t * 1450) * Mathf.Sin(t * 91) * .24f; break;
                    case 5: wave = (Mathf.Sin(t * 260) * .65f + n * .25f) * Mathf.Exp(-t * 10); break;
                    default: wave = Mathf.Sin(2 * Mathf.PI * 48 * t) * .25f + filtered * .15f; break;
                }
                float envelope = Mathf.Min(1, t * 60) * Mathf.Min(1, (length - t) * 60);
                samples[i] = wave * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); clips.Add(clip);
            var audioObject = new GameObject(name + "_Audio"); audioObject.transform.SetParent(transform, false);
            var source = audioObject.AddComponent<AudioSource>(); source.clip = clip; source.loop = loop;
            source.playOnAwake = false; source.volume = 0;
            if (loop) source.Play();
            return source;
        }
        private void LateUpdate()
        {
            if (breath == null) return;
            var state = campaign.State;
            bool search = state.chapter == EscapeChapter.Search;
            bool chase = state.chapter == EscapeChapter.Chase;
            float tension = search ? Mathf.Clamp01((60 - (float)campaign.Rules.Remaining) / 20) : 0;
            bool bugs = campaign.Rules.Swarming;
            breath.volume = search && !campaign.Rules.Silent && !bugs && !state.swarmHit ? tension * .65f : 0;
            drone.volume = (search || chase) && !campaign.Rules.Silent && !bugs ? .17f : 0;
            siren.volume = chase ? .16f : 0;
            float distance = Vector3.Distance(campaign.LocalPlayer.transform.position, campaign.killer.position);
            saw.volume = chase ? Mathf.Lerp(.55f, .05f, Mathf.Clamp01(distance / 25)) : 0;
            saw.transform.position = campaign.killer.position + Vector3.up;
            saw.spatialBlend = 1; saw.minDistance = 2; saw.maxDistance = 35;
            saw.pitch = chase ? 1 + Mathf.Sin(Time.unscaledTime * 2) * .08f : 1;
            swarm.volume = bugs ? .7f : 0;
            if (bugs)
            {
                var camera = campaign.LocalPlayer.ViewCamera.transform;
                insects.transform.SetPositionAndRotation(camera.position + camera.forward * .8f, camera.rotation);
                if (!insects.isPlaying) insects.Play();
            }
            else if (insects.isPlaying) insects.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (state.swarmHit && !hitPlayed) { impact.volume = .7f; impact.Play(); hitPlayed = true; }
            if (state.chapter == EscapeChapter.Chair) { hitPlayed = false; footDistance = 0; }
            var position = campaign.LocalPlayer.transform.position;
            float delta = Vector3.Distance(position, previousPosition);
            if ((search || chase) && delta < 1) footDistance += delta;
            previousPosition = position;
            // Approaching footsteps continue while the player is still during the final minute.
            if (footDistance > 1.6f || search && tension > 0 && !campaign.Rules.Silent && !bugs &&
                Mathf.Repeat(Time.unscaledTime, Mathf.Lerp(1.1f, .4f, tension)) < Time.unscaledDeltaTime)
            {
                footsteps.volume = .22f + tension * .18f; footsteps.pitch = chase ? 1.15f : .85f;
                footsteps.Play(); footDistance = 0;
            }
            for (int i = 0; i < 2; i++)
            {
                var player = i == 0 ? campaign.playerA : campaign.playerB;
                float trauma = bugs ? .055f : search && state.swarmHit && state.elapsed < 140 ? .1f * (float)(140 - state.elapsed) : 0;
                player.ViewCamera.transform.localPosition = cameraRest[i] + new Vector3(
                    Mathf.Sin(Time.unscaledTime * 47) * trauma, Mathf.Cos(Time.unscaledTime * 39) * trauma, 0);
            }
        }
        private void OnDestroy()
        {
            foreach (var clip in clips) if (clip != null) Destroy(clip);
            if (insectMaterial != null) Destroy(insectMaterial);
            if (insectTexture != null) Destroy(insectTexture);
        }
    }
}
