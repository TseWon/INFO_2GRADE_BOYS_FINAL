using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class VerifyFittedScene {
 static Bounds B(GameObject go){var rs=go.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void Shot(Camera c,string name){var rt=RenderTexture.GetTemporary(1100,800,24);var active=RenderTexture.active;var prev=c.targetTexture;var tex=new Texture2D(1100,800,TextureFormat.RGB24,false);try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1100,800),0,0);tex.Apply();File.WriteAllBytes("artifacts/"+name+".png",tex.EncodeToPNG());}finally{c.targetTexture=prev;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 public static string Run(){
  var perimeter=GameObject.Find("Hollow Creek Iron Perimeter");var gate=perimeter.GetComponentInChildren<OutwardResearchGate>();var truck=GameObject.Find("Military Truck - Forecourt");var station=GameObject.Find("Surface Research Station - Snow Site");
  var ground=GameObject.Find("Snow-covered ground 400 x 400 m").GetComponent<MeshCollider>();
  var tb=B(truck);var sb=B(station);float maximum=-100;
  foreach(float x in new[]{tb.min.x,tb.center.x,tb.max.x})foreach(float z in new[]{tb.min.z,tb.center.z,tb.max.z})if(ground.Raycast(new Ray(new Vector3(x,20,z),Vector3.down),out var h,50))maximum=Mathf.Max(maximum,h.point.y);
  var west=perimeter.GetComponentsInChildren<Transform>().First(t=>t.name=="West leaf outward hinge");var east=perimeter.GetComponentsInChildren<Transform>().First(t=>t.name=="East leaf outward hinge");
  float rear=perimeter.GetComponentsInChildren<Transform>().First(t=>t.name=="Invisible wall north").position.z;
  if(Mathf.Abs(gate.transform.position.z+64)>.01||Mathf.Abs(rear+12)>.01||maximum>tb.min.y+.02)throw new Exception("Placement validation failed");
  var go=new GameObject("Temporary review camera");var c=go.AddComponent<Camera>();c.CopyFrom(Camera.main);c.cullingMask=-1;var wr=west.localRotation;var er=east.localRotation;
  try{c.transform.position=new Vector3(28,31,-101);c.transform.LookAt(new Vector3(0,0,-50));c.fieldOfView=62;Shot(c,"solo_shortened_forecourt");c.transform.position=new Vector3(11,4.5f,-104);c.transform.LookAt(tb.center);c.fieldOfView=55;Shot(c,"solo_truck_cleared");west.localRotation=Quaternion.Euler(0,90,0);east.localRotation=Quaternion.Euler(0,-90,0);c.transform.position=new Vector3(7,4,-73);c.transform.LookAt(new Vector3(0,1.5f,-64));Shot(c,"solo_gate_open_shortened");}
  finally{west.localRotation=wr;east.localRotation=er;UnityEngine.Object.DestroyImmediate(go);Physics.SyncTransforms();}
  string report="rear="+rear+" front="+gate.transform.position.z+" station-front="+sb.min.z+" clearance="+(sb.min.z-gate.transform.position.z)+" truck-bottom="+tb.min.y+" snow-under-truck-max="+maximum+" wall-count="+perimeter.GetComponentsInChildren<BoxCollider>().Count(x=>x.name.StartsWith("Invisible wall"));
  File.WriteAllText("artifacts/solo_forecourt_verified.txt",report);return report;
 }
}
