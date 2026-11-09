using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FacilityEscape;
using UnityEngine;

public static class TestVoiceContinuity
{
    static List<string> results = new List<string>();
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); results.Add("PASS " + message); }
    struct Packet { public double arrival; public uint sequence; public short[] data; }
    static short[] Tone(uint id)
    {
        var data = new short[320];
        for (int i = 0; i < data.Length; i++) data[i] = (short)(Math.Sin((id * 320 + i) * Math.PI * 2 * 440 / 16000) * 8000);
        return data;
    }
    static void Simulate(int rate, int block, bool impaired)
    {
        var buffer = new MultiEscapeVoiceBuffer(); var packets = new List<Packet>(); var random = new System.Random(316);
        for (uint id = 0; id < 550; id++) {
            if (impaired && id > 4 && id % 47 == 0) continue;
            double jitter = impaired ? random.NextDouble() * .012 + (id % 17 == 0 ? .028 : 0) : 0;
            packets.Add(new Packet { arrival = id * .02 + jitter, sequence = id, data = Tone(id) });
        }
        packets.Sort((a,b)=>a.arrival.CompareTo(b.arrival));
        var output = new float[block * 2]; int index = 0, silent = 0, checkedBlocks = 0;
        for (double now = 0; now < 10; now += block / (double)rate) {
            while (index < packets.Count && packets[index].arrival <= now) { var p = packets[index++]; buffer.Push(p.sequence,p.data); }
            buffer.ReadResampled(output, 2, rate);
            if (now < .5) continue;
            checkedBlocks++; double energy = 0;
            for (int i = 0; i < output.Length; i += 2) {
                if (output[i] != output[i+1] || float.IsNaN(output[i]) || Math.Abs(output[i]) > 1) throw new Exception("Invalid stereo PCM");
                energy += output[i] * output[i];
            }
            if (Math.Sqrt(energy / block) < .01) silent++;
        }
        Check(silent == 0, rate + "Hz / " + block + " DSP frames / " + (impaired ? "jitter+reorder+loss" : "clean") + ": " + checkedBlocks + " callbacks, silent=" + silent + "; underruns=" + buffer.Underruns + ", concealed=" + buffer.ConcealedFrames + ", target=" + buffer.TargetMilliseconds + "ms");
    }
    public static string Run()
    {
        results.Clear();
        foreach (EscapeStage stage in Enum.GetValues(typeof(EscapeStage))) {
            Check(MultiEscapeVoiceBuffer.CanHear(EscapeRole.Blindfolded, EscapeRole.Observer, stage), "B to A permitted in " + stage);
            Check(MultiEscapeVoiceBuffer.CanHear(EscapeRole.Observer, EscapeRole.Blindfolded, stage) == (stage == EscapeStage.Escaped), "A to B unlock preserved in " + stage);
        }
        var b = new MultiEscapeVoiceBuffer();
        Check(!b.Push(0,null) && !b.Push(0,new short[12]), "Malformed packets rejected");
        Check(b.Push(0,Tone(0)) && b.Push(2,Tone(2)) && b.Push(1,Tone(1)), "Out-of-order packet recovered before playout");
        Check(!b.Push(1,Tone(1)), "Duplicate rejected");
        b.Push(3,Tone(3)); var data = new float[640]; b.ReadResampled(data, 2, 16000);
        Check(!b.Push(0,Tone(0)), "Already played packet rejected");
        b.Clear(); b.Read(data); Check(data.All(x=>x==0), "Disconnect flushes queued speech and interpolation tail");
        Check(b.Push(uint.MaxValue,Tone(0)) && b.Push(0,Tone(1)) && b.Push(1,Tone(2)), "Sequence wrap accepted");
        b.Clear(); b.Push(0,Tone(0)); b.Push(10000,Tone(1)); Check(b.BufferedMilliseconds == 20, "Long gap discards stale queue and bounds latency");
        foreach (int rate in new[]{16000,44100,48000}) foreach (int block in new[]{256,512,1024,2048}) { Simulate(rate,block,false); Simulate(rate,block,true); }
        Directory.CreateDirectory("artifacts/hollow-creek"); string report=string.Join("\n",results);
        File.WriteAllText("artifacts/hollow-creek/voice-continuity-tests.txt",report); return report;
    }
}
