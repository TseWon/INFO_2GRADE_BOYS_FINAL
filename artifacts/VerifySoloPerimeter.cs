using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class VerifySoloPerimeter {
 const string Out="artifacts/surface_station/";
 static void Capture(Camera cam,string filename){
  int w=1280,h=800;var rt=RenderTexture.GetTemporary(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);var old=cam.targetTexture;var active=RenderTexture.active;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Out+filename+".png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
 public static string Run(){
  var fence=GameObject.Find("Hollow Creek Iron Perimeter");var gate=fence?.GetComponentInChildren<OutwardResearchGate>();
  var truck=GameObject.Find("Military Truck - Forecourt");var station=GameObject.Find("Surface Research Station - Snow Site");
  if(!fence||!gate||!truck||!station)throw new Exception("Required scene objects missing");
  var west=fence.GetComponentsInChildren<Transform>().First(t=>t.name=="West leaf outward hinge");var east=fence.GetComponentsInChildren<Transform>().First(t=>t.name=="East leaf outward hinge");
  var camObject=new GameObject("Temporary perimeter camera");camObject.hideFlags=HideFlags.HideAndDontSave;var cam=camObject.AddComponent<Camera>();cam.CopyFrom(Camera.main);
  try{
   cam.transform.position=new Vector3(0,15,-108);cam.transform.LookAt(new Vector3(0,1,-45));cam.fieldOfView=60;Capture(cam,"perimeter_overview");
   cam.transform.position=new Vector3(7,4.3f,-86);cam.transform.LookAt(new Vector3(0,1.6f,-76));cam.fieldOfView=55;Capture(cam,"perimeter_gate_closed");
   var before=west.GetComponentInChildren<Renderer>().bounds.center;var beforeEast=east.GetComponentInChildren<Renderer>().bounds.center;
   west.localRotation=Quaternion.Euler(0,90,0);east.localRotation=Quaternion.Euler(0,-90,0);Physics.SyncTransforms();
   File.WriteAllText(Out+"gate_rotation_probe.txt","west children="+west.childCount+" before="+before+" after="+west.GetComponentInChildren<Renderer>().bounds.center+" east before="+beforeEast+" after="+east.GetComponentInChildren<Renderer>().bounds.center);
   Capture(cam,"perimeter_gate_open");
  }finally{west.localRotation=Quaternion.identity;east.localRotation=Quaternion.identity;UnityEngine.Object.DestroyImmediate(camObject);}
  var wall=fence.GetComponentsInChildren<BoxCollider>().Count(c=>c.name.StartsWith("Invisible wall"));
  var fencePanel=fence.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Fence side -1 /"));
  Bounds BoundsOf(GameObject go)=>go.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
  var fenceHeight=BoundsOf(fencePanel.gameObject).size.y;
  var gateHeight=BoundsOf(gate.gameObject).size.y;
  var stationSideClearance=19f-BoundsOf(station).extents.x;
  if(fenceHeight<3.9f||fenceHeight>4.15f||gateHeight<3.9f||gateHeight>4.15f||stationSideClearance<3f)throw new Exception("Perimeter size or station clearance invalid: "+fenceHeight+","+gateHeight+","+stationSideClearance);
  var report="scene="+SceneManager.GetActiveScene().path+" panels="+fence.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Fence "))+" invisibleWalls="+wall+" fenceHeight="+fenceHeight+" gateHeight="+gateHeight+" stationSideClearance="+stationSideClearance+" gateScript="+gate.GetType().Name+" westHinge="+west.position+" eastHinge="+east.position+" truckZ="+truck.transform.position.z+" stationZ="+station.transform.position.z;
  File.WriteAllText(Out+"perimeter_verification.txt",report);return report;
 }
}
