using System;

namespace FacilityEscape
{
    // The audio callback runs off the main thread. Keep Unity APIs outside this buffer.
    public sealed class MultiEscapeVoiceBuffer
    {
        public const int SampleRate = 16000;
        public const int FrameSamples = 320;
        const int Slots = 32;
        readonly object gate = new object();
        readonly short[][] frames = new short[Slots][];
        readonly uint[] ids = new uint[Slots];
        readonly bool[] ready = new bool[Slots];
        readonly float[] previousFrame = new float[FrameSamples];
        uint expected, newest;
        int offset, targetFrames = 4, lossRun, underruns, concealed;
        bool hasSequence, playing, frameValid, primed;
        double fraction;
        float a, b, lastOutput;
        public MultiEscapeVoiceBuffer() { for (int i = 0; i < Slots; i++) frames[i] = new short[FrameSamples]; }
        public int Underruns { get { lock (gate) return underruns; } }
        public int ConcealedFrames { get { lock (gate) return concealed; } }
        public int BufferedMilliseconds { get { lock (gate) return QueuedSamples() * 1000 / SampleRate; } }
        public int TargetMilliseconds { get { lock (gate) return targetFrames * 20; } }

        public static bool CanHear(EscapeRole listener, EscapeRole speaker, EscapeStage stage = EscapeStage.Ready) =>
            (listener == EscapeRole.Blindfolded && speaker == EscapeRole.Observer) ||
            (stage == EscapeStage.Escaped && listener == EscapeRole.Observer && speaker == EscapeRole.Blindfolded);

        public void Clear()
        {
            lock (gate) {
                Array.Clear(ready, 0, ready.Length); Array.Clear(previousFrame, 0, previousFrame.Length);
                hasSequence = playing = primed = false; offset = lossRun = 0;
                fraction = 0; a = b = lastOutput = 0;
            }
        }

        int QueuedSamples() => hasSequence ? Math.Max(0, unchecked((int)(newest - expected)) + 1) * FrameSamples - offset : 0;

        public bool Push(uint sequence, short[] frame)
        {
            if (frame == null || frame.Length != FrameSamples) return false;
            lock (gate)
            {
                if (!hasSequence) { expected = newest = sequence; hasSequence = true; }
                int ahead = unchecked((int)(sequence - expected));
                if (ahead < 0 || ahead == 0 && offset > 0) return false;
                if (ahead >= Slots) {
                    // Bound latency after a stall instead of replaying stale speech.
                    Array.Clear(ready, 0, ready.Length); expected = newest = sequence;
                    offset = lossRun = 0; playing = primed = false; fraction = 0;
                    Array.Clear(previousFrame, 0, previousFrame.Length);
                }
                int slot = (int)(sequence % Slots);
                if (ready[slot] && ids[slot] == sequence) return false;
                Array.Copy(frame, frames[slot], FrameSamples); ids[slot] = sequence; ready[slot] = true;
                if (unchecked((int)(sequence - newest)) > 0) newest = sequence;
                return true;
            }
        }

        float NextSample()
        {
            if (!playing) return 0;
            int slot = (int)(expected % Slots);
            if (offset == 0) {
                frameValid = ready[slot] && ids[slot] == expected;
                if (!frameValid && unchecked((int)(newest - expected)) < 0) {
                    playing = primed = false; underruns++;
                    targetFrames = Math.Min(30, targetFrames + (targetFrames < 6 ? 1 : 0));
                    return 0;
                }
                if (frameValid) lossRun = 0;
                else { lossRun++; concealed++; }
            }
            float sample;
            if (frameValid) { sample = frames[slot][offset] / 32768f; previousFrame[offset] = sample; }
            else sample = lossRun <= 3 ? previousFrame[offset] * (float)Math.Pow(.55, lossRun) : 0;
            if (++offset == FrameSamples) { ready[slot] = false; offset = 0; expected++; }
            return sample;
        }

        public void Read(float[] output) => ReadResampled(output, 1, SampleRate);
        public void ReadResampled(float[] output, int channels, int outputRate)
        {
            if (output == null) return;
            Array.Clear(output, 0, output.Length);
            if (channels < 1 || outputRate < 8000 || output.Length % channels != 0) return;
            lock (gate)
            {
                int deviceFrames = output.Length / channels;
                int required = (int)Math.Ceiling(deviceFrames * (double)SampleRate / outputRate);
                // Include a full device callback and two packets in the startup reserve.
                targetFrames = Math.Max(targetFrames, Math.Min(Slots - 2, (required + FrameSamples - 1) / FrameSamples + 2));
                if (!playing && QueuedSamples() >= targetFrames * FrameSamples) {
                    playing = true; primed = false; fraction = 0;
                }
                if (playing && !primed) { a = NextSample(); b = NextSample(); primed = playing; }
                double step = SampleRate / (double)outputRate;
                for (int i = 0; i < deviceFrames; i++) {
                    float value;
                    if (playing) {
                        value = a + (b - a) * (float)fraction;
                        fraction += step;
                        while (fraction >= 1 && playing) { a = b; b = NextSample(); fraction -= 1; }
                    } else value = lastOutput * .97f;
                    if (Math.Abs(value) < .000001f) value = 0;
                    lastOutput = value;
                    for (int ch = 0; ch < channels; ch++) output[i * channels + ch] = value;
                }
            }
        }
    }
}
