using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class VerifySoloStation {
 const string Out="artifacts/surface_station/";
 static void Capture(Camera cam,string name,int w=1280,int h=800) {
  var previous=cam.targetTexture;var active=RenderTexture.active;
  var rt=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Out+name+".png",tex.EncodeToPNG());}
  finally{cam.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
 public static string Inspect() {
  AssetDatabase.ImportAsset("Assets/Environment/SurfaceResearchStation/Models/SurfaceResearchStation.fbx",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
  AssetDatabase.ImportAsset("Assets/Environment/SurfaceResearchStation/Models/SurfaceResearchStation_LOD1.fbx",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
  var root=GameObject.Find("Surface Research Station - Snow Site");if(!root)throw new Exception("Station missing");
  var lines=new List<string>{"Scene="+SceneManager.GetActiveScene().path,"OldExists="+(GameObject.Find("Research Facility - Snow Site")!=null)};
  lines.Add("Shaders="+string.Join(",",root.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>m.name+":"+m.shader.name+":errors="+ShaderUtil.ShaderHasError(m.shader))));
  var player=GameObject.Find("Tactical Soldier Player");var playerRenderers=player.GetComponentsInChildren<Renderer>();var flags=playerRenderers.Select(r=>r.shadowCastingMode).ToArray();
  try{foreach(var r in playerRenderers)r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;Capture(Camera.main,"solo_station_player_view");}
  finally{for(int i=0;i<flags.Length;i++)playerRenderers[i].shadowCastingMode=flags[i];}
  var view=new GameObject("Temporary station verification camera");view.hideFlags=HideFlags.HideAndDontSave;var cam=view.AddComponent<Camera>();cam.CopyFrom(Camera.main);
  try{
   cam.transform.position=new Vector3(18,12,-70);cam.transform.LookAt(new Vector3(0,2,-43));cam.fieldOfView=58;Capture(cam,"solo_station_overview");
   cam.transform.position=new Vector3(5,3,-61);cam.transform.LookAt(new Vector3(0,2,-54));cam.fieldOfView=60;Capture(cam,"solo_station_entrance");
   var comparison=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/TacticalSoldier/TacticalSoldierVisual.prefab"));
   try {
    comparison.hideFlags=HideFlags.HideAndDontSave;comparison.transform.position=new Vector3(1.25f,.98f,-55.2f);comparison.transform.rotation=Quaternion.Euler(0,180,0);
    foreach(var r in comparison.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.On;
    cam.transform.position=new Vector3(3.7f,2.6f,-61);cam.transform.LookAt(new Vector3(.4f,2,-54.3f));cam.fieldOfView=48;Capture(cam,"solo_station_soldier_scale");
   }finally{UnityEngine.Object.DestroyImmediate(comparison);}
  }finally{UnityEngine.Object.DestroyImmediate(view);}
  foreach(float x in new[]{0,-10.8f,10.8f}) {
   var probe=new GameObject("Temporary staircase clearance probe");probe.hideFlags=HideFlags.HideAndDontSave;
   try {
    probe.transform.position=new Vector3(x,.04f,x==0?-60:-58);var c=probe.AddComponent<CharacterController>();c.height=1.88f;c.radius=.3f;c.center=new Vector3(0,.94f,0);c.stepOffset=.25f;c.skinWidth=.025f;c.slopeLimit=45;c.minMoveDistance=0;
    Physics.SyncTransforms();float maxY=0;
    for(int i=0;i<150;i++){c.Move(new Vector3(0,-.08f,.055f));maxY=Mathf.Max(maxY,probe.transform.position.y);}
    lines.Add("STAIR_PROBE x="+x+" final="+probe.transform.position.ToString("F3")+" maxHeight="+maxY.ToString("F3")+" grounded="+c.isGrounded+" reachedLanding="+(maxY>.78f));
   }finally{UnityEngine.Object.DestroyImmediate(probe);}
  }
  var text=string.Join("\n",lines);File.WriteAllText(Out+"solo_verification.txt",text);return text;
 }
}
