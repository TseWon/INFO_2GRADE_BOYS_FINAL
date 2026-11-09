using System;
using System.Linq;
using FacilityEscape;

public static class TestMultiEscapeVoiceBuffer
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    public static string Run()
    {
        checks = 0;
        Check(MultiEscapeVoiceBuffer.CanHear(EscapeRole.Blindfolded, EscapeRole.Observer), "A hears B");
        Check(!MultiEscapeVoiceBuffer.CanHear(EscapeRole.Observer, EscapeRole.Blindfolded), "B cannot hear A");
        Check(!MultiEscapeVoiceBuffer.CanHear(EscapeRole.Observer, EscapeRole.Observer), "No B echo");
        Check(!MultiEscapeVoiceBuffer.CanHear(EscapeRole.Blindfolded, EscapeRole.Blindfolded), "No A echo");
        foreach (EscapeStage stage in Enum.GetValues(typeof(EscapeStage)))
        {
            Check(MultiEscapeVoiceBuffer.CanHear(EscapeRole.Blindfolded, EscapeRole.Observer, stage), "B to A in " + stage);
            Check(MultiEscapeVoiceBuffer.CanHear(EscapeRole.Observer, EscapeRole.Blindfolded, stage) == (stage == EscapeStage.Escaped), "A to B only after escape: " + stage);
            Check(!MultiEscapeVoiceBuffer.CanHear(EscapeRole.Observer, EscapeRole.Observer, stage) &&
                !MultiEscapeVoiceBuffer.CanHear(EscapeRole.Blindfolded, EscapeRole.Blindfolded, stage), "No echo in " + stage);
        }
        var buffer = new MultiEscapeVoiceBuffer();
        var frame = Enumerable.Repeat((short)16384, 320).ToArray();
        var audio = new float[320];
        Check(!buffer.Push(0, null) && !buffer.Push(0, new short[321]), "Malformed frames rejected");
        buffer.Push(1, frame); buffer.Read(audio);
        Check(audio.All(s => s == 0), "Jitter prebuffer emits silence");
        buffer.Push(2, frame); buffer.Push(3, frame); buffer.Push(4, frame); buffer.Read(audio);
        Check(audio.All(s => s == .5f), "PCM amplitude survives transport buffer");
        Check(!buffer.Push(3, frame) && !buffer.Push(2, frame), "Duplicate and reordered packets rejected");
        buffer.Read(audio); buffer.Read(audio); buffer.Read(audio); buffer.Read(audio); buffer.Read(audio); buffer.Read(audio);
        Check(audio.All(s => s == 0), "Underrun emits silence");
        buffer = new MultiEscapeVoiceBuffer(); buffer.Push(8, frame); buffer.Push(10, frame); buffer.Push(11, frame); buffer.Read(audio);
        Check(audio.All(s => s == .5f), "First packet before gap");
        buffer.Read(audio); Check(audio.All(s => Math.Abs(s - .275f) < .00001f), "Lost packet concealed with attenuated preceding frame");
        buffer.Read(audio); Check(audio.All(s => s == .5f), "Recovery after packet loss");
        buffer.Clear(); Check(buffer.Push(uint.MaxValue, frame) && buffer.Push(0, frame), "Sequence wraps safely");
        buffer.Clear(); buffer.Push(0, frame); buffer.Push(10000, frame); buffer.Read(audio);
        Check(audio.All(s => s == 0), "Huge gap resets bounded queue");
        for (uint i = 10001; i < 10050; i++) buffer.Push(i, frame);
        buffer.Clear(); buffer.Read(audio);
        Check(audio.All(s => s == 0), "Disconnect flushes all queued audio");
        return checks + " voice buffer and role checks passed.";
    }
}
