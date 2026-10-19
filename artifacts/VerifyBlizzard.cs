using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class VerifyBlizzard {
 static Bounds B(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static void Capture(Camera cam,string path){var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1100,720,24);var tex=new Texture2D(1100,720,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1100,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{cam.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 public static object Run(){
  var truck=GameObject.Find("Military Truck - Forecourt");var lab=GameObject.Find("Research Facility - Snow Site");var ground=GameObject.Find("Snow-covered ground 400 x 400 m").GetComponent<MeshCollider>();var cam=Camera.main;
  cam.transform.position=new Vector3(14,3.1f,-79);cam.transform.LookAt(new Vector3(-2,6,-43));
  var ps=cam.GetComponentInChildren<ParticleSystem>();ps.transform.position=cam.transform.position+new Vector3(-5,6,8);ps.transform.rotation=Quaternion.identity;
  var wheels=truck.GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("tread blocks")).Select(r=>{var b=r.bounds;RaycastHit hit;if(!ground.Raycast(new Ray(new Vector3(b.center.x,20,b.center.z),Vector3.down),out hit,50))throw new Exception("Wheel outside ground");float gap=b.min.y-hit.point.y;if(gap<-.01f||gap>.04f)throw new Exception("Wheel grounding issue "+r.name+" "+gap);return r.name+": "+gap.ToString("F4")+"m";}).ToArray();
  if(wheels.Length!=6)throw new Exception("Expected six wheels");var tb=B(truck);var lb=B(lab);if(tb.Intersects(lb))throw new Exception("Model overlap");if(tb.Contains(cam.transform.position)||lb.Contains(cam.transform.position))throw new Exception("Camera intersects model bounds");
  foreach(var r in truck.GetComponentsInChildren<Renderer>().Concat(lab.GetComponentsInChildren<Renderer>()))foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||!m.shader.isSupported)throw new Exception("Material missing or unsupported");
  ps.Simulate(12,true,true);var particles=new ParticleSystem.Particle[22000];var count=ps.GetParticles(particles);float t0=particles[0].remainingLifetime;var p0=particles[0].position;ps.Simulate(.25f,true,false);ps.GetParticles(particles);if(particles[0].position==p0)throw new Exception("Snow simulation stalled");
  EditorSceneManager.MarkSceneDirty(truck.scene);EditorSceneManager.SaveScene(truck.scene);Capture(cam,"artifacts/blizzard_final.png");
  var pos=cam.transform.position;var rot=cam.transform.rotation;try{cam.transform.position=new Vector3(-9,2.4f,-71);cam.transform.LookAt(new Vector3(4,1.5f,-66));ps.Simulate(12,true,true);Capture(cam,"artifacts/blizzard_truck_check.png");}finally{cam.transform.SetPositionAndRotation(pos,rot);ps.Simulate(12,true,true);}
  return new{wheels,gapMeters=lb.min.z-tb.max.z,labGround=lb.min.y,particles=count,particleMovedMeters=Vector3.Distance(p0,particles[0].position),lifetimeBefore=t0,lifetimeAfter=particles[0].remainingLifetime,missingMaterials=0,saved=true,notes="Static mesh colliders on solid facility structures, truck box collider, ground mesh collider; no player controller in this scene."};
 }
}
