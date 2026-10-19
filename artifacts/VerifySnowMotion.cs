using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class VerifySnowMotion {
 public static async Task<string> Run(){
  EditorApplication.isPaused=false;Application.runInBackground=true;
  int f=Time.frameCount;var ps=UnityEngine.Object.FindAnyObjectByType<ParticleSystem>();
  if(!ps.isPlaying)ps.Play();var p=new ParticleSystem.Particle[5000];int n=ps.GetParticles(p);float life=n>0?p[0].remainingLifetime:0;
  await Task.Delay(1800);
  int end=Time.frameCount;int count=ps.GetParticles(p);
  if(end<=f)throw new Exception("Frame counter is stalled at "+f);
  if(count==0)throw new Exception("No snow particles");
  return "Frames advanced "+f+" -> "+end+"; live snow particles="+count+"; initial lifetime="+life+"; final lifetime="+p[0].remainingLifetime;
 }
}
