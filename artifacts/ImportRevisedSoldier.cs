using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ImportRevisedSoldier {
 public static object Run(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  string model="Assets/Blender/TacticalSoldier/tactical_soldier.blend";
  AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();
  if(p==null||p.gameObject.scene.path!="Assets/Scene/Scene1.unity")throw new Exception("Scene1 player required");
  var skins=p.visual.GetComponentsInChildren<SkinnedMeshRenderer>();
  var camo=skins.First(s=>s.name=="Mesh_Soldier_Camouflage");
  if(camo.sharedMesh.vertexCount<40000)throw new Exception("Old anatomy is still loaded");
  if(skins.Length!=9||skins.Any(s=>s.bones.Any(b=>b==null)))throw new Exception("Missing skinned renderer/bone");
  foreach(var s in skins)foreach(var m in s.sharedMaterials)if(m==null||!m.shader.isSupported)throw new Exception("Material error");
  int blends=camo.sharedMesh.boneWeights.Count(w=>w.weight1>.01f);
  if(blends<1000)throw new Exception("Blended joint skinning lost");
  PrefabUtility.SaveAsPrefabAsset(p.visual.gameObject,"Assets/Characters/TacticalSoldier/TacticalSoldierVisual.prefab");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
  var result=new{meshes=skins.Length,vertices=skins.Sum(s=>s.sharedMesh.vertexCount),blendedVertices=blends,materials=skins.SelectMany(s=>s.sharedMaterials).Distinct().Count(),bones=skins.SelectMany(s=>s.bones).Distinct().Count()};
  File.WriteAllText("artifacts/soldier_anatomy_import.txt",result.ToString());
  Capture(p);
  EditorSceneManager.SaveScene(p.gameObject.scene);
  return result;
 }
 static void Capture(SnowFirstPerson p){
  var cam=p.viewCamera;var pos=cam.transform.localPosition;var rot=cam.transform.localRotation;float fov=cam.fieldOfView;var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1200,1200,24);rt.antiAliasing=4;var tex=new Texture2D(1200,1200,TextureFormat.RGB24,false);
  try{cam.fieldOfView=38;cam.transform.position=p.transform.position+p.transform.forward*3.1f+p.transform.right*.8f+Vector3.up*1.15f;cam.transform.LookAt(p.transform.position+Vector3.up*.95f);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();File.WriteAllBytes("artifacts/tactical_soldier_final.png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;RenderTexture.active=active;cam.transform.localPosition=pos;cam.transform.localRotation=rot;cam.fieldOfView=fov;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
