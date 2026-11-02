using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class InstallFittedSoldier {
 const string Folder="Assets/Characters/FittedSoldier";
 const string Model=Folder+"/FittedSoldier.fbx";
 static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 public static string Run(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scene/SoloScene1.unity")throw new Exception("SoloScene1 edit mode required");
  if(!File.Exists(Folder+"/manifest.json"))throw new Exception("Export has not completed");
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();if(p==null)throw new Exception("Player missing");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);var manifest=JObject.Parse(File.ReadAllText(Folder+"/manifest.json"));
  Directory.CreateDirectory(Folder+"/Materials");var mats=new Dictionary<string,Material>();
  foreach(var data in (JArray)manifest["materials"]){
   string name=(string)data["name"];string path=Folder+"/Materials/"+name+".mat";
   var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
   m.color=Color.white;m.SetFloat("_Metallic",(float)data["metallic"]);m.SetFloat("_Glossiness",1-(float)data["roughness"]);
   foreach(string kind in new[]{"BaseColor","Normal"}){
    string tex=(string)data[kind];if(string.IsNullOrEmpty(tex))continue;
    var ti=AssetImporter.GetAtPath(tex) as TextureImporter;if(ti==null)throw new Exception("Missing texture "+tex);
    ti.textureType=kind=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=kind=="BaseColor";ti.mipmapEnabled=true;ti.anisoLevel=4;ti.maxTextureSize=2048;ti.SaveAndReimport();
    m.SetTexture(kind=="Normal"?"_BumpMap":"_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
    if(kind=="Normal"){m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",1);}
   }
   EditorUtility.SetDirty(m);mats[name]=m;
  }
  var imp=(ModelImporter)AssetImporter.GetAtPath(Model);imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.Generic;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.meshCompression=ModelImporterMeshCompression.Off;
  foreach(var kv in mats)imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kv.Key),kv.Value);imp.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);if(!source)throw new Exception("FBX missing");
  var visual=(GameObject)PrefabUtility.InstantiatePrefab(source);visual.name="Fitted Soldier - Player Visual";visual.transform.SetParent(p.transform,false);
  var b=BoundsOf(visual);visual.transform.localScale*=1.88f/b.size.y;b=BoundsOf(visual);visual.transform.position+=Vector3.up*(p.transform.position.y-b.min.y);
  foreach(var r in visual.GetComponentsInChildren<Renderer>()){
   r.sharedMaterials=r.sharedMaterials.Select(m=>mats.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();
   if(r.sharedMaterials.Any(m=>!m||!m.shader.isSupported))throw new Exception("Material missing or unsupported");
   if(r is SkinnedMeshRenderer skin){skin.updateWhenOffscreen=true;if(skin.bones.Any(t=>!t))throw new Exception("Missing bone");}
  }
  var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length<8)throw new Exception("Skinned export incomplete");
  var cc=p.GetComponent<CharacterController>();var gait=visual.AddComponent<SoldierLocomotion>();gait.motor=cc;
  var old=p.visual;Undo.RecordObject(p,"Replace soldier visual");p.visual=visual.transform;p.thirdPerson=false;
  PrefabUtility.SaveAsPrefabAsset(visual,Folder+"/FittedSoldierVisual.prefab");
  if(old)Undo.DestroyObjectImmediate(old.gameObject);
  EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);Selection.activeGameObject=visual;
  var report="SoloScene1 visual replaced; height="+BoundsOf(visual).size.y+" renderers="+skins.Length+" bones="+skins.SelectMany(s=>s.bones).Distinct().Count()+" vertices="+skins.Sum(s=>s.sharedMesh.vertexCount)+" materials="+mats.Count+" movement/camera preserved; source=artifacts/FittedSoldier_Source.blend";
  File.WriteAllText("artifacts/fitted_soldier_unity_report.txt",report);Capture();return report;
 }
 public static string Capture(){
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();var go=new GameObject("Temporary soldier review");var cam=go.AddComponent<Camera>();cam.CopyFrom(p.viewCamera);cam.cullingMask=-1;cam.fieldOfView=36;var rs=p.visual.GetComponentsInChildren<Renderer>();var modes=rs.Select(r=>r.shadowCastingMode).ToArray();foreach(var r in rs)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
  try{foreach(string view in new[]{"front","side","back"}){var offset=view=="front"?p.transform.forward*3.8f:view=="back"?-p.transform.forward*3.8f:p.transform.right*3.8f;cam.transform.position=p.transform.position+offset+Vector3.up*1.05f;cam.transform.LookAt(p.transform.position+Vector3.up*.98f);var rt=RenderTexture.GetTemporary(900,1100,24);var active=RenderTexture.active;var tex=new Texture2D(900,1100,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,1100),0,0);tex.Apply();File.WriteAllBytes("artifacts/fitted_soldier_unity_"+view+".png",tex.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}}}
  finally{for(int i=0;i<rs.Length;i++)rs[i].shadowCastingMode=modes[i];UnityEngine.Object.DestroyImmediate(go);}return "Captured three views";
 }
}
