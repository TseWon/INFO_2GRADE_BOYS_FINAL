using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using FacilityEscape;
public static class TestVoiceDSP
{
 public static async Task<string> Run()
 {
  var go=new GameObject("Voice DSP verification");var source=go.AddComponent<AudioSource>();
  var buffer=new MultiEscapeVoiceBuffer();var playback=go.AddComponent<MultiEscapeVoicePlayback>();playback.Initialize(buffer);
  var clip=AudioClip.Create("Verification carrier",AudioSettings.outputSampleRate,1,AudioSettings.outputSampleRate,false);
  source.clip=null;playback.Gain=.2f;source.loop=true;source.volume=.2f;source.spatialBlend=0;source.priority=0;source.Play();
  var pcm=new short[320];var output=new float[512];float sourcePeak=0,filterPeak=0,listenerPeak=0;
  double began=Time.realtimeSinceStartupAsDouble;
  try {
   for(uint seq=0;seq<120;seq++) {
    for(int i=0;i<320;i++)pcm[i]=(short)(Math.Sin((seq*320+i)*Math.PI*2*440/16000)*4000);
    buffer.Push(seq,pcm);
    while(Time.realtimeSinceStartupAsDouble-began<(seq+1)*.02)await Task.Delay(3);
    source.GetOutputData(output,0);sourcePeak=Math.Max(sourcePeak,output.Max(x=>Math.Abs(x)));filterPeak=Math.Max(filterPeak,playback.OutputPeak);
    AudioListener.GetOutputData(output,0);listenerPeak=Math.Max(listenerPeak,output.Max(x=>Math.Abs(x)));
   }
   var result="Source pre-filter peak="+sourcePeak+"; DSP peak="+filterPeak+"; listener peak="+listenerPeak+"; callbacks="+playback.AudioCallbacks+"; underruns="+buffer.Underruns+"; queued="+buffer.BufferedMilliseconds;
   System.IO.File.WriteAllText("artifacts/hollow-creek/voice-dsp-test.txt",result);
   if(filterPeak<.01f||listenerPeak<.001f)throw new Exception(result);
   return result;
  } finally {UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(clip);}
 }
}
