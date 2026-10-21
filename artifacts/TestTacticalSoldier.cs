using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class TestTacticalSoldier {
 static void Capture(Camera cam,string path){var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1200,1200,24);rt.antiAliasing=4;var tex=new Texture2D(1200,1200,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{cam.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 public static async Task<object> Run(){
  if(!EditorApplication.isPlaying)throw new Exception("Play mode required");EditorApplication.isPaused=false;Application.runInBackground=true;
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();var cc=p.GetComponent<CharacterController>();var gait=p.visual.GetComponent<SoldierLocomotion>();if(gait==null)throw new Exception("Gait missing");var cam=p.viewCamera;var saved=p.transform.position;var savedRotation=p.transform.rotation;var eye=cam.transform.localPosition;var camRotation=cam.transform.localRotation;var fov=cam.fieldOfView;
  p.enabled=false;gait.enabled=false;
  try{
   if(Mathf.Abs(eye.y-1.67f)>.01||Mathf.Abs(cc.height-1.88f)>.01)throw new Exception("Wrong soldier scale");
   var skins=p.visual.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length!=9)throw new Exception("Wrong mesh count "+skins.Length);
   foreach(var s in skins){if(s.bones.Length==0||s.bones.Any(b=>b==null))throw new Exception("Bone binding missing");if(s.sharedMesh.uv.Length!=s.sharedMesh.vertexCount)throw new Exception("UV missing");foreach(var m in s.sharedMaterials)if(m==null||!m.shader.isSupported)throw new Exception("Material error");}
   var mats=skins.SelectMany(s=>s.sharedMaterials).Distinct().ToArray();foreach(var m in mats.Where(m=>new[]{"Camouflage","Cordura","Mask","Leather"}.Any(k=>m.name.Contains(k))))if(m.mainTexture==null||m.GetTexture("_BumpMap")==null||m.GetTexture("_MetallicGlossMap")==null)throw new Exception("PBR texture missing: "+m.name);
   var leg=p.visual.GetComponentsInChildren<Transform>().First(t=>t.name=="Thigh_L");var rest=leg.localRotation;var start=p.transform.position;
   for(int i=0;i<30;i++){p.Move(Vector2.up,false,1f/60);gait.SendMessage("LateUpdate");await Task.Delay(16);}
   float moved=Vector3.Distance(new Vector3(start.x,0,start.z),new Vector3(p.transform.position.x,0,p.transform.position.z));float bend=Quaternion.Angle(rest,leg.localRotation);if(moved<1.2f||bend<1)throw new Exception("Movement/gait failed "+moved+" / "+bend);
   cc.enabled=false;p.transform.position=saved;cc.enabled=true;Physics.SyncTransforms();
   p.SetThirdPerson(true);p.SendMessage("LateUpdate");float cameraDistance=Vector3.Distance(cam.transform.position,p.transform.TransformPoint(eye));if(cameraDistance<1||cameraDistance>2.9f)throw new Exception("Third person camera failed");
   p.SetThirdPerson(false);p.SendMessage("LateUpdate");if(Vector3.Distance(cam.transform.localPosition,eye)>.001f)throw new Exception("First person restore failed");
   p.SetThirdPerson(true);cam.fieldOfView=38;cam.transform.position=p.transform.position+p.transform.forward*3.1f+p.transform.right*.8f+Vector3.up*1.15f;cam.transform.LookAt(p.transform.position+Vector3.up*.95f);Capture(cam,"artifacts/tactical_soldier_unity.png");
   cam.fieldOfView=32;cam.transform.position=p.transform.position+p.transform.forward*1.25f+p.transform.right*.28f+Vector3.up*1.66f;cam.transform.LookAt(p.transform.position+Vector3.up*1.56f);Capture(cam,"artifacts/tactical_soldier_detail.png");
   return new{skinnedMeshes=skins.Length,materials=mats.Length,boneCount=skins.SelectMany(s=>s.bones).Distinct().Count(),movedMeters=moved,legBendDegrees=bend,cameraDistance,eyeHeight=eye.y,particles=p.snowfall.particleCount};
  }finally{cc.enabled=false;p.transform.SetPositionAndRotation(saved,savedRotation);cc.enabled=true;cam.fieldOfView=fov;p.SetThirdPerson(false);cam.transform.localPosition=eye;cam.transform.localRotation=camRotation;p.enabled=true;gait.enabled=true;}
 }
}
