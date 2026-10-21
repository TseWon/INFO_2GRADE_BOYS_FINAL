using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ApplyTacticalSoldier {
 const string Model="Assets/Blender/TacticalSoldier/tactical_soldier.blend";
 const string Folder="Assets/Characters/TacticalSoldier";
 static void Texture(string path,bool normal,bool srgb){var ti=(TextureImporter)AssetImporter.GetAtPath(path);if(ti==null)throw new Exception("Texture missing "+path);ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=srgb;ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.anisoLevel=8;ti.filterMode=FilterMode.Trilinear;ti.SaveAndReimport();}
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scene/Scene1.unity")throw new Exception("Scene1 required");
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);if(source==null)throw new Exception("Model still importing");
  EditorSceneManager.SaveScene(scene,"artifacts/backups/Scene1_before_tactical_soldier_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
  var map=new Dictionary<string,Material>();foreach(var e in JArray.Parse(File.ReadAllText("artifacts/soldier_material_manifest.json"))){string name=(string)e["name"];string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}var c=(JArray)e["color"];m.color=new Color((float)c[0],(float)c[1],(float)c[2]);m.SetFloat("_Glossiness",1-(float)e["roughness"]);m.SetFloat("_Metallic",(float)e["metallic"]);
   if(e["maps"] is JObject maps){string a=(string)maps["albedo"],n=(string)maps["normal"],p=(string)maps["metallicSmoothness"];Texture(a,false,true);Texture(n,true,false);Texture(p,false,false);m.color=Color.white;m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(a);m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(n));m.SetFloat("_BumpScale",.35f);m.EnableKeyword("_NORMALMAP");m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(p));m.SetFloat("_GlossMapScale",1);m.EnableKeyword("_METALLICGLOSSMAP");}
   EditorUtility.SetDirty(m);map[name]=m;
  }
  var importer=(ModelImporter)AssetImporter.GetAtPath(Model);foreach(var kv in map)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kv.Key),kv.Value);importer.importAnimation=false;importer.SaveAndReimport();
  source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var player=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();if(player==null)throw new Exception("Existing player missing");
  var old=player.visual;var visual=(GameObject)PrefabUtility.InstantiatePrefab(source);visual.name="Tactical Soldier - Player Visual";visual.transform.SetParent(player.transform,false);visual.transform.localPosition=Vector3.zero;
  foreach(var r in visual.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>map.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();if(r is SkinnedMeshRenderer sk)sk.updateWhenOffscreen=true;}
  var cc=player.GetComponent<CharacterController>();cc.height=1.88f;cc.center=new Vector3(0,.94f,0);cc.radius=.30f;cc.stepOffset=.25f;player.viewCamera.transform.localPosition=new Vector3(0,1.67f,.025f);player.viewCamera.transform.localRotation=Quaternion.identity;player.visual=visual.transform;player.thirdPerson=false;player.gameObject.name="Tactical Soldier Player";
  var gait=visual.AddComponent<SoldierLocomotion>();gait.motor=cc;
  PrefabUtility.SaveAsPrefabAsset(visual,Folder+"/TacticalSoldierVisual.prefab");
  if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=visual;
  var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length==0)throw new Exception("No skinned meshes imported");
  return "Saved Scene1; height=1.88m eye=1.67m; skinned meshes="+skins.Length+"; bones="+skins.SelectMany(s=>s.bones).Distinct().Count()+"; external materials="+map.Count;
 }
}
