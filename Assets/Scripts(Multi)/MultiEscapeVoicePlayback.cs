using System;
using UnityEngine;

namespace FacilityEscape
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MultiEscapeVoicePlayback : MonoBehaviour
    {
        MultiEscapeVoiceBuffer buffer;
        volatile int outputRate = 48000;
        public volatile float OutputPeak;
        public volatile float Gain = 1;
        public int AudioCallbacks { get; private set; }
        public void Initialize(MultiEscapeVoiceBuffer source) { buffer = source; outputRate = AudioSettings.outputSampleRate; }
        void OnEnable() { AudioSettings.OnAudioConfigurationChanged += AudioChanged; outputRate = AudioSettings.outputSampleRate; }
        void OnDisable() { AudioSettings.OnAudioConfigurationChanged -= AudioChanged; }
        void AudioChanged(bool changed) { outputRate = AudioSettings.outputSampleRate; buffer?.Clear(); }
        // Paced by the output device DSP, instead of a streaming decoder's read-ahead.
        void OnAudioFilterRead(float[] data, int channels)
        {
            var source = buffer;
            if (source == null) { Array.Clear(data, 0, data.Length); return; }
            source.ReadResampled(data, channels, outputRate);
            float peak = 0; float gain = Gain;
            for (int i = 0; i < data.Length; i++) { data[i] *= gain; peak = Math.Max(peak, Math.Abs(data[i])); }
            OutputPeak = peak; AudioCallbacks++;
        }
    }
}
