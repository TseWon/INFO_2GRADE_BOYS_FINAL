using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
public static class InspectSoloEntrance {
 public static string Capture() {
  var go=new GameObject("Temporary entrance inspection camera"); go.hideFlags=HideFlags.HideAndDontSave;
  var cam=go.AddComponent<Camera>(); cam.CopyFrom(Camera.main); cam.enabled=false;
  var rt=RenderTexture.GetTemporary(1200,800,24); var tex=new Texture2D(1200,800,TextureFormat.RGB24,false); var old=RenderTexture.active;
  try {
   cam.transform.position=new Vector3(5,4,-71); cam.transform.LookAt(new Vector3(0,2.4f,-61));cam.fieldOfView=58;
   cam.targetTexture=rt; cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,800),0,0);tex.Apply();File.WriteAllBytes("artifacts/solo_building_entrance.png",tex.EncodeToPNG());
  } finally {cam.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);}
  return "artifacts/solo_building_entrance.png";
 }
 public static string Run() {
  var b=new StringBuilder();
  for(int i=0;i<SceneManager.sceneCount;i++) {
   var s=SceneManager.GetSceneAt(i); b.AppendLine("SCENE "+s.path+" dirty="+s.isDirty);
   foreach(var root in s.GetRootGameObjects()) {
    b.AppendLine("ROOT "+root.name+" "+root.transform.position);
    foreach(var t in root.GetComponentsInChildren<Transform>(true)) {
     var n=t.name.ToLowerInvariant();
     if(n.Contains("station")||n.Contains("rail")||n.Contains("fence")||n.Contains("gate")||n.Contains("entrance")||n.Contains("인증")||n.Contains("deck")||n.Contains("access")) {
      var r=t.GetComponent<Renderer>(); b.AppendLine("PART "+t.name+" parent="+(t.parent?t.parent.name:"")+" active="+t.gameObject.activeInHierarchy+" pos="+t.position.ToString("F2")+(r?" bounds="+r.bounds.ToString():""));
     }
    }
   }
  }
  File.WriteAllText("artifacts/solo_entrance_inspection.txt",b.ToString()); return b.ToString();
 }
}
