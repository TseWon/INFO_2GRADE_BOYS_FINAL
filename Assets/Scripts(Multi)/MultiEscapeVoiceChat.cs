using System;
using UnityEngine;

namespace FacilityEscape
{
    [DisallowMultipleComponent]
    public sealed class MultiEscapeVoiceChat : MonoBehaviour
    {
        public const string DefaultDevice = "System default";
        private MultiEscapeLanSession lan;
        private MultiEscapeGame game;
        private bool lastUnlocked;
        private int lastRound = -1;
        private readonly MultiEscapeVoiceBuffer buffer = new MultiEscapeVoiceBuffer();
        private readonly short[] packet = new short[MultiEscapeVoiceBuffer.FrameSamples];
        private AudioClip microphoneClip, playbackClip;
        private AudioSource speaker;
        private float[] capture;
        private string device;
        private int capturePosition, captureFrames;
        private uint sequence;
        private double retryAt, lastCaptureAt, lastPollAt, lastReceivedAt = double.NegativeInfinity;
        private bool recordingStarted, muted;
        public string SelectedDevice { get; private set; } = DefaultDevice;
        public bool Muted => muted;
        public float InputLevel { get; private set; }
        public int SentPackets { get; private set; }
        public int ReceivedPackets { get; private set; }
        public bool IsRecording => microphoneClip != null;
        public string Status { get; private set; } = "VOICE / Connect both players first";

        public void Initialize(MultiEscapeLanSession session, MultiEscapeGame escapeGame)
        {
            lan = session;
            game = escapeGame;
            lan.VoiceReceived += Receive;
            lan.Changed += ConnectionChanged;
            game.StateChanged += RefreshPermissions;
        }

        private void RefreshPermissions()
        {
            if (lastUnlocked == lan.VoiceUnlocked && lastRound == game.RoundNumber) return;
            lastUnlocked = lan.VoiceUnlocked;
            lastRound = game.RoundNumber;
            StopAudio();
            retryAt = 0;
        }

        public void SelectMicrophone(string name)
        {
            SelectedDevice = name;
            StopMicrophone();
            retryAt = 0;
        }

        public void ToggleMute()
        {
            muted = !muted;
            StopMicrophone();
            retryAt = 0;
        }

        private void ConnectionChanged()
        {
            if (lan.Connected) return;
            StopAudio();
            sequence = 0;
            retryAt = 0;
        }

        private void Update()
        {
            if (lan == null || !lan.Connected)
            {
                Status = "VOICE / Connect both players first";
                return;
            }
            if (lan.CanReceiveVoice)
            {
                EnsureSpeaker();
                bool recent = Time.realtimeSinceStartupAsDouble - lastReceivedAt < .3;
                if (!recent) buffer.Clear();
            }
            if (!lan.CanSendVoice)
            {
                StopMicrophone();
                Status = "VOICE / A hears B | A's voice cannot reach B until both puzzles are solved";
                return;
            }
            if (muted)
            {
                Status = (lan.VoiceUnlocked ? "FREE / Two-way voice" : "EARPLUGS / Cannot hear A") + " | Microphone muted";
                return;
            }
            double now = Time.realtimeSinceStartupAsDouble;
            if (microphoneClip == null)
            {
                if (now >= retryAt) StartMicrophone();
                return;
            }
            int position = Microphone.GetPosition(device);
            if (position < 0 || !Microphone.IsRecording(device))
            { MicrophoneFailed("Microphone disconnected. Reconnect or select another device."); return; }
            if (!recordingStarted)
            {
                if (position == 0)
                {
                    if (now - lastCaptureAt > 3) MicrophoneFailed("No microphone input. Check Windows microphone permission.");
                    return;
                }
                recordingStarted = true;
            }
            // Drop old capture after a stall instead of sending delayed speech in a burst.
            if (now - lastPollAt > .25) capturePosition = position;
            lastPollAt = now;
            int available = (position - capturePosition + microphoneClip.samples) % microphoneClip.samples;
            while (available >= captureFrames)
            {
                if (!microphoneClip.GetData(capture, capturePosition))
                { MicrophoneFailed("Cannot read microphone audio. Select another device."); return; }
                float peak = 0;
                for (int i = 0; i < packet.Length; i++)
                {
                    float offset = i * (captureFrames / (float)packet.Length);
                    int a = (int)offset, b = Math.Min(a + 1, captureFrames - 1);
                    float first = 0, second = 0;
                    for (int channel = 0; channel < microphoneClip.channels; channel++)
                    {
                        first += capture[a * microphoneClip.channels + channel];
                        second += capture[b * microphoneClip.channels + channel];
                    }
                    float value = Mathf.Clamp(Mathf.Lerp(first, second, offset - a) / microphoneClip.channels, -1, 1);
                    peak = Mathf.Max(peak, Mathf.Abs(value));
                    packet[i] = (short)Mathf.RoundToInt(value * 32767);
                }
                InputLevel = peak;
                lan.SendVoice(sequence++, packet);
                SentPackets++;
                capturePosition = (capturePosition + captureFrames) % microphoneClip.samples;
                available -= captureFrames;
                lastCaptureAt = now;
            }
            if (now - lastCaptureAt > 3)
            { MicrophoneFailed("Microphone stopped producing audio. Check the headset."); return; }
            Status = (lan.VoiceUnlocked ? "FREE / Two-way voice | Mic: " : "EARPLUGS / Cannot hear A | Mic to A: ")
                + Mathf.RoundToInt(InputLevel * 100) + "%";
        }

        private void StartMicrophone()
        {
            retryAt = Time.realtimeSinceStartupAsDouble + 3;
            var devices = Microphone.devices;
            if (devices.Length == 0)
            { Status = "MIC / No microphone found. Connect your headset."; return; }
            device = SelectedDevice == DefaultDevice ? null : SelectedDevice;
            if (device != null && Array.IndexOf(devices, device) < 0)
            { Status = "MIC / Selected headset missing. Select a connected microphone."; return; }
            try
            {
                Microphone.GetDeviceCaps(device, out int min, out int max);
                int rate = max > 0 ? Mathf.Clamp(MultiEscapeVoiceBuffer.SampleRate, min, max) : MultiEscapeVoiceBuffer.SampleRate;
                microphoneClip = Microphone.Start(device, true, 1, rate);
                if (microphoneClip == null) { MicrophoneFailed("Microphone unavailable. Check Windows microphone permission."); return; }
                captureFrames = microphoneClip.frequency / 50;
                capture = new float[captureFrames * microphoneClip.channels];
                capturePosition = 0;
                lastCaptureAt = Time.realtimeSinceStartupAsDouble;
                lastPollAt = lastCaptureAt;
                Status = "MIC / Starting headset microphone...";
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Voice microphone: " + exception.Message, this);
                MicrophoneFailed("Cannot open microphone. Check headset and Windows permission.");
            }
        }

        private void MicrophoneFailed(string message)
        {
            StopMicrophone();
            Status = "MIC / " + message;
            retryAt = Time.realtimeSinceStartupAsDouble + 3;
        }

        private void EnsureSpeaker()
        {
            if (speaker == null)
            {
                var output = new GameObject("Partner_Voice");
                output.transform.SetParent(transform, false);
                speaker = output.AddComponent<AudioSource>();
                speaker.playOnAwake = false;
                speaker.loop = true;
                speaker.spatialBlend = 0;
                speaker.volume = 1;
                speaker.priority = 0;
                playbackClip = AudioClip.Create("LAN voice", MultiEscapeVoiceBuffer.SampleRate, 1,
                    MultiEscapeVoiceBuffer.SampleRate, true, buffer.Read);
                speaker.clip = playbackClip;
            }
            if (!speaker.isPlaying) speaker.Play();
        }

        private void Receive(uint id, short[] frame)
        {
            if (!isActiveAndEnabled || lan == null || !lan.CanReceiveVoice) return;
            if (Time.realtimeSinceStartupAsDouble - lastReceivedAt > .3) buffer.Clear();
            if (!buffer.Push(id, frame)) return;
            lastReceivedAt = Time.realtimeSinceStartupAsDouble;
            ReceivedPackets++;
        }

        private void StopMicrophone()
        {
            if (microphoneClip != null)
            {
                Microphone.End(device);
                Destroy(microphoneClip);
                microphoneClip = null;
            }
            recordingStarted = false;
            capture = null;
            InputLevel = 0;
        }

        private void StopAudio()
        {
            StopMicrophone();
            if (speaker != null) speaker.Stop();
            buffer.Clear();
            lastReceivedAt = double.NegativeInfinity;
        }

        private void OnDisable() => StopAudio();
        private void OnDestroy()
        {
            StopAudio();
            if (lan != null) { lan.VoiceReceived -= Receive; lan.Changed -= ConnectionChanged; }
            if (game != null) game.StateChanged -= RefreshPermissions;
            if (speaker != null) Destroy(speaker.gameObject);
            if (playbackClip != null) Destroy(playbackClip);
        }
    }
}
