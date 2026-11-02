using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class TestFittedSolo {
 public static async Task<string> Run(){
  if(!Application.isPlaying)throw new Exception("Play mode required");
  EditorApplication.isPaused=false;Application.runInBackground=true;
  int checkFrame=Time.frameCount;await Task.Delay(1000);
  if(Time.frameCount<=checkFrame)throw new Exception("Player loop is not advancing");
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();var cc=p.GetComponent<CharacterController>();var gate=UnityEngine.Object.FindAnyObjectByType<OutwardResearchGate>();
  var pos=p.transform.position;var rot=p.transform.rotation;bool enabled=p.enabled;bool open=gate.IsOpen;int before=Time.frameCount;
  var skins=p.visual.GetComponentsInChildren<SkinnedMeshRenderer>();var thigh=p.visual.GetComponentsInChildren<Transform>().First(t=>t.name=="Thigh_L");
  var rest=thigh.localRotation;float maxBend=0;
  void Place(Vector3 v){cc.enabled=false;p.transform.position=v;p.transform.rotation=Quaternion.identity;cc.enabled=true;Physics.SyncTransforms();}
  try{
   p.enabled=false;p.SetThirdPerson(true);Place(new Vector3(0,.05f,-70));gate.SetOpen(false);
   for(int i=0;i<100;i++){p.Move(Vector2.up,false,.025f);await Task.Delay(25);maxBend=Mathf.Max(maxBend,Quaternion.Angle(rest,thigh.localRotation));}
   float closed=p.transform.position.z;
   if(closed> -64.15f||closed< -69)throw new Exception("Closed gate collision/movement failed: "+closed);
   Place(new Vector3(0,.05f,-70));gate.SetOpen(true);await Task.Delay(1500);
   for(int i=0;i<110;i++){p.Move(Vector2.up,false,.025f);await Task.Delay(25);}
   float passed=p.transform.position.z;
   if(passed< -63)throw new Exception("Open gate passage blocked: "+passed);
   Place(new Vector3(17,.05f,-75));p.transform.rotation=Quaternion.Euler(0,90,0);
   for(int i=0;i<45;i++){p.Move(Vector2.up,false,.025f);await Task.Delay(25);}
   float wallX=p.transform.position.x;
   if(wallX>19)throw new Exception("Access invisible wall breached");
   if(Time.frameCount<=before+5||maxBend<1)throw new Exception("Frame advancement or gait missing");
   foreach(var sk in skins){var mesh=new Mesh();sk.BakeMesh(mesh);if(mesh.vertices.Any(v=>float.IsNaN(v.x)||float.IsInfinity(v.y)))throw new Exception("Invalid skinned vertex");UnityEngine.Object.Destroy(mesh);}
   string report="Frames advanced="+(Time.frameCount-before)+"; walking thigh bend="+maxBend+"; closed gate stop z="+closed+"; open gate passage z="+passed+"; invisible wall stop x="+wallX+"; skinned meshes="+skins.Length+"; camera-parent="+p.viewCamera.transform.parent.name;
   File.WriteAllText("artifacts/fitted_solo_playtest.txt",report);return report;
  } finally {gate.SetOpen(open);cc.enabled=false;p.transform.position=pos;p.transform.rotation=rot;cc.enabled=true;p.SetThirdPerson(false);p.enabled=enabled;}
 }
}
