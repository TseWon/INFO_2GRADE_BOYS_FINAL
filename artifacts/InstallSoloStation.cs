using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class InstallSoloStation
{
 const string Folder="Assets/Environment/SurfaceResearchStation";
 const string TexFolder="Assets/Blender/SurfaceResearchStation/Textures";
 const string ModelPath=Folder+"/Models/SurfaceResearchStation.fbx";
 const string OldName="Research Facility - Snow Site";
 const string NewName="Surface Research Station - Snow Site";
 [Serializable] class MatEntry { public string name; public float[] base_color; public float metallic; public float roughness; }
 [Serializable] class Manifest { public MatEntry[] items; }
 static Bounds BoundsOf(GameObject go) {
  var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();
  if(rs.Length==0)throw new Exception("No renderers: "+go.name);
  var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
 }
 static void Marker(Transform parent,string name,Vector3 pos) {
  var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;
 }
 static Dictionary<string,Material> Materials() {
  Directory.CreateDirectory(Folder+"/Materials");
  AssetDatabase.ImportAsset(Folder+"/Shaders/StationSnowPBR.shader",ImportAssetOptions.ForceSynchronousImport);
  var snow=Shader.Find("Environment/Surface Station Snow PBR");
  if(snow==null||ShaderUtil.ShaderHasError(snow))throw new Exception("Station shader failed");
  foreach(var path in Directory.GetFiles(TexFolder,"*.png")) {
   var p=path.Replace('\\','/');var ti=AssetImporter.GetAtPath(p) as TextureImporter;
   if(ti==null) {AssetDatabase.ImportAsset(p);ti=AssetImporter.GetAtPath(p) as TextureImporter;}
   ti.textureType=p.Contains("_Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
   ti.sRGBTexture=p.Contains("_BaseColor");ti.mipmapEnabled=true;ti.anisoLevel=4;ti.filterMode=FilterMode.Trilinear;ti.maxTextureSize=1024;ti.SaveAndReimport();
  }
  var entries=Newtonsoft.Json.JsonConvert.DeserializeObject<MatEntry[]>(File.ReadAllText("Assets/Blender/SurfaceResearchStation/MaterialManifest.json"));
  var result=new Dictionary<string,Material>();
  foreach(var item in entries) {
   string key=item.name.Substring(3),path=Folder+"/Materials/"+item.name+".mat";
   var colorTex=AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder+"/"+key+"_BaseColor.png");
   var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null){m=new Material(colorTex?snow:Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
   m.shader=colorTex?snow:Shader.Find("Standard");m.name=item.name;
   m.color=colorTex?Color.white:new Color(item.base_color[0],item.base_color[1],item.base_color[2],1);
   if(colorTex) {
    m.SetTexture("_MainTex",colorTex);m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder+"/"+key+"_Normal.png"));
    m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder+"/"+key+"_MetallicSmoothness.png"));
    m.SetFloat("_BumpScale",.28f);m.SetFloat("_SnowAmount",key=="Graphite"?.45f:.76f);
   } else {
    m.SetFloat("_Metallic",item.metallic);m.SetFloat("_Glossiness",1-item.roughness);
    if(key=="WarmLight"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.66f,.31f)*2);}
   }
   EditorUtility.SetDirty(m);result[item.name]=m;
  }
  return result;
 }
 static GameObject Model(string path,Transform parent,Dictionary<string,Material> mats,bool collisions) {
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Model not imported: "+path);
  var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(parent,false);
  foreach(var r in model.GetComponentsInChildren<Renderer>()) {
   r.sharedMaterials=r.sharedMaterials.Select(m=> {
    var key=mats.Keys.FirstOrDefault(k=>m.name==k||m.name.StartsWith(k+"."));
    if(key==null)throw new Exception("Unmapped material: "+m.name);return mats[key];
   }).ToArray();
   r.gameObject.isStatic=true;
   if(collisions&&(r.name.Contains("__Shell__")||r.name.Contains("__Decks__")||r.name.Contains("__Roofs__"))) {
    var filter=r.GetComponent<MeshFilter>();if(filter&&filter.sharedMesh){var c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=filter.sharedMesh;c.convex=false;}
   }
  }
  return model;
 }
 public static string Run() {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Editor must be ready in edit mode");
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scene/SoloScene1.unity")throw new Exception("Expected SoloScene1; found "+scene.path);
  if(GameObject.Find(NewName))throw new Exception("Replacement already present; inspect before repeating");
  var old=scene.GetRootGameObjects().SingleOrDefault(g=>g.name==OldName);if(old==null)throw new Exception("Original facility not found");
  var player=GameObject.Find("Tactical Soldier Player");var cc=player.GetComponent<CharacterController>();
  var playerBounds=BoundsOf(player);
  if(playerBounds.size.y>2.12f || cc.height*player.transform.lossyScale.y>2.12f)throw new Exception("Door clearance insufficient for current player; resize first");
  Directory.CreateDirectory("artifacts/backups");string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss");
  string backup="artifacts/backups/SoloScene1_before_station_"+stamp+".unity";
  if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Scene backup failed");
  var materials=Materials();
  var parent=new GameObject(NewName);Undo.RegisterCreatedObjectUndo(parent,"Replace surface research station");
  GameObject high=null;
  try {
   high=Model(ModelPath,parent.transform,materials,true);high.name="Station LOD0";
   var low=Model(Folder+"/Models/SurfaceResearchStation_LOD1.fbx",parent.transform,materials,false);low.name="Station LOD1";
   var lod=parent.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.32f,high.GetComponentsInChildren<Renderer>()),new LOD(.008f,low.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
   Marker(parent.transform,"Entrance - threshold 0.87m",new Vector3(0,.87f,12.3f));
   Marker(parent.transform,"Future Vertical Core - planning only",new Vector3(0,.87f,6.5f));
   Marker(parent.transform,"Future B1 Landing - planning only",new Vector3(0,-5.13f,6.5f));
   Directory.CreateDirectory(Folder+"/Prefabs");PrefabUtility.SaveAsPrefabAssetAndConnect(parent,Folder+"/Prefabs/SurfaceResearchStation.prefab",InteractionMode.AutomatedAction);
   parent.transform.position=new Vector3(0,.015f,-42);parent.transform.rotation=Quaternion.Euler(0,180,0);
   Physics.SyncTransforms();
   var bounds=BoundsOf(high);var truck=GameObject.Find("Military Truck - Forecourt");var truckBounds=BoundsOf(truck);
   if(bounds.Intersects(truckBounds))throw new Exception("Station overlaps truck bounds");
   // The current scene is safely backed up; remove only the identified old scene instance.
   Undo.DestroyObjectImmediate(old);
   Selection.activeGameObject=parent;
   if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.Frame(bounds,false);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   var report="Scene="+scene.path+"\nBackup="+backup+"\nPlayerHeight="+playerBounds.size.y.ToString("F3")+"\nController="+cc.height+" x "+(2*cc.radius)+"\nDoorMainClear=1.56 x 2.36m; DomeClear=1.25 x 2.32m; resize not required\nStationPosition="+parent.transform.position+"\nStationBounds="+bounds+"\nTruckBounds="+truckBounds+"\nTruckOverlap=false\nMaterials="+materials.Count+"\nStaticMeshColliders="+high.GetComponentsInChildren<MeshCollider>().Length+"\nOldFacilityRemoved=true\n";
   File.WriteAllText("artifacts/surface_station/solo_install_report.txt",report);return report;
  } catch {if(old&&parent)UnityEngine.Object.DestroyImmediate(parent);throw;}
 }
}
