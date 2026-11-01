using System;

namespace FacilityEscape
{
    // The audio callback runs off the main thread. Keep Unity APIs outside this buffer.
    public sealed class MultiEscapeVoiceBuffer
    {
        public const int SampleRate = 16000;
        public const int FrameSamples = 320;
        private const int Capacity = FrameSamples * 10;
        private readonly object gate = new object();
        private readonly float[] samples = new float[Capacity];
        private int read, count;
        private uint lastSequence;
        private bool hasSequence, playing;

        public static bool CanHear(EscapeRole listener, EscapeRole speaker, EscapeStage stage = EscapeStage.Ready) =>
            (listener == EscapeRole.Blindfolded && speaker == EscapeRole.Observer) ||
            (stage == EscapeStage.Escaped && listener == EscapeRole.Observer && speaker == EscapeRole.Blindfolded);

        public void Clear()
        {
            lock (gate) { read = count = 0; playing = hasSequence = false; }
        }

        public bool Push(uint sequence, short[] frame)
        {
            if (frame == null || frame.Length != FrameSamples) return false;
            lock (gate)
            {
                int delta = unchecked((int)(sequence - lastSequence));
                if (hasSequence && delta <= 0) return false;
                int missing = hasSequence ? Math.Min(delta - 1, 10) : 0;
                if (count + (missing + 1) * FrameSamples > Capacity)
                {
                    read = count = missing = 0;
                    playing = false;
                }
                for (int i = 0; i < missing * FrameSamples; i++) Append(0);
                for (int i = 0; i < FrameSamples; i++) Append(frame[i] / 32768f);
                lastSequence = sequence;
                hasSequence = true;
                return true;
            }
        }

        private void Append(float value)
        {
            samples[(read + count) % Capacity] = value;
            count++;
        }

        public void Read(float[] output)
        {
            Array.Clear(output, 0, output.Length);
            lock (gate)
            {
                if (!playing && count < FrameSamples * 3) return;
                playing = true;
                int length = Math.Min(output.Length, count);
                for (int i = 0; i < length; i++) output[i] = samples[(read + i) % Capacity];
                read = (read + length) % Capacity;
                count -= length;
                if (length < output.Length) playing = false;
            }
        }
    }
}
