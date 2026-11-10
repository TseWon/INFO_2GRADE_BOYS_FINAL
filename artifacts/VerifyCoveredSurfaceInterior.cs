using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyCoveredSurfaceInterior {
 public static async System.Threading.Tasks.Task<object> Motion(){
  if(!Application.isPlaying)throw new Exception("Play mode required");
  Application.runInBackground=true;
  int start=Time.frameCount;float began=Time.time;
  await System.Threading.Tasks.Task.Delay(16000);
  var state=new{startFrame=start,endFrame=Time.frameCount,startTime=began,endTime=Time.time};
  File.WriteAllText("artifacts/surface_interior/runtime-motion.txt",state.ToString());
  return new{motion=state,inspection=Run()};
 }
 public static object Run(){
  var s=SceneManager.GetActiveScene();if(s.name!="SoloScene2")throw new Exception("SoloScene2 required");
  var b=GameObject.Find("Surface Research Interior - Covered");if(!b)throw new Exception("Building missing");
  var camera=Camera.main;var ps=GameObject.Find("Heavy Snowfall - exterior only").GetComponent<ParticleSystem>();
  Physics.SyncTransforms();
  var p=camera.transform.position;var q=camera.transform.rotation;var fov=camera.fieldOfView;
  try {
   Capture(camera,"interior_reception.png");
   camera.transform.position=new Vector3(-14,1.7f,5);camera.transform.LookAt(new Vector3(0,3.5f,2.5f));Capture(camera,"interior_cage.png");
   camera.transform.position=new Vector3(-14,1.7f,19);camera.transform.LookAt(new Vector3(-23,1.7f,20));Capture(camera,"interior_snow_window.png");
   camera.transform.position=new Vector3(40,28,47);camera.transform.LookAt(new Vector3(0,3,0));Capture(camera,"covered_exterior.png");
  }finally{camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=fov;}
  int roofHits=0;var probes=new[]{new Vector3(0,30,20),new Vector3(-14,30,18),new Vector3(14,30,18),new Vector3(0,30,2),new Vector3(14,30,5),new Vector3(-14,30,5),new Vector3(0,30,-18),new Vector3(14,30,-18),new Vector3(-14,30,-18)};
  foreach(var at in probes)if(Physics.Raycast(at,Vector3.down,out var hit,40)&&hit.collider.name.StartsWith("SI_08"))roofHits++;
  var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);int inside=0;
  for(int i=0;i<count;i++){if(particles[i].remainingLifetime<=0)continue;var v=particles[i].position;if(v.y>.2f&&v.y<3.9f&&((Mathf.Abs(v.x)<9.8f&&v.z>14.2f&&v.z<24.8f)||(Mathf.Abs(v.x)<7.8f&&v.z>-26.8f&&v.z<-9.2f)))inside++;}
  var renderers=b.GetComponentsInChildren<Renderer>();
  var result=new{scene=s.name,objects=renderers.Length,triangles=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/SurfaceResearchInterior/Models/SurfaceResearchInterior.fbx").GetComponentsInChildren<MeshFilter>().Sum(m=>Enumerable.Range(0,m.sharedMesh.subMeshCount).Sum(i=>(long)m.sharedMesh.GetIndexCount(i))/3),missingMaterials=renderers.Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader.isSupported)),roofHits,roofProbes=probes.Length,particles=count,indoorParticles=inside,worldCollision=ps.collision.type.ToString(),fog=RenderSettings.fogDensity,play=Application.isPlaying,frames=Time.frameCount,gameTime=Time.time,camera=camera.transform.position.ToString()};
  File.WriteAllText("artifacts/surface_interior/unity-verification.txt",result.ToString());return result;
 }
 public static void Capture(Camera camera,string file){
  var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,800,24);rt.antiAliasing=2;var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes("artifacts/surface_interior/"+file,tex.EncodeToPNG());}
  finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
 public static object Runtime(){
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();var ps=UnityEngine.Object.FindAnyObjectByType<SurfaceInteriorSnow>();
  return new{playing=Application.isPlaying,frames=Time.frameCount,time=Time.time,player=p?p.transform.position.ToString():"missing",particles=ps?ps.snowfall.particleCount:0};
 }
}
