using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PlaceCoveredSurfaceInterior
{
 const string Folder="Assets/Environment/SurfaceResearchInterior";
 const string Model=Folder+"/Models/SurfaceResearchInterior.fbx";
 static Material Mat(string name, Color color,float metal,float rough){
  var path=Folder+"/Materials/"+name+".mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
  m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",1-rough);return m;
 }
 static Material Textured(string name,string texture){
  var m=Mat(name,Color.white,.12f,.65f);
  var basePath="Assets/Blender/SurfaceResearchStation/Textures/"+texture;
  m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(basePath+"_BaseColor.png");
  m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(basePath+"_Normal.png"));m.SetFloat("_BumpScale",.3f);m.EnableKeyword("_NORMALMAP");
  var packed=AssetDatabase.LoadAssetAtPath<Texture2D>(basePath+"_MetallicSmoothness.png");
  if(packed){m.SetTexture("_MetallicGlossMap",packed);m.EnableKeyword("_METALLICGLOSSMAP");}
  return m;
 }
 static GameObject Clone(GameObject source,Scene dest,string name){
  var g=UnityEngine.Object.Instantiate(source);g.name=name;g.transform.SetParent(null);SceneManager.MoveGameObjectToScene(g,dest);return g;
 }
 static GameObject Find(Scene s,string name)=>s.GetRootGameObjects().FirstOrDefault(g=>g.name==name);
 static void LightAt(Transform parent,string name,Vector3 at,float range,float intensity){
  var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=at;
  var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.79f,.87f,1);l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;
 }
 public static object Run(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Prefabs");Directory.CreateDirectory("artifacts/backups");
  AssetDatabase.ImportAsset(Model,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
  var importer=(ModelImporter)AssetImporter.GetAtPath(Model);importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.SaveAndReimport();
  var source=SceneManager.GetSceneByPath("Assets/Scene/SoloScene1.unity");
  if(!source.isLoaded)source=EditorSceneManager.OpenScene("Assets/Scene/SoloScene1.unity",OpenSceneMode.Additive);
  SceneManager.SetActiveScene(source);
  var fog=RenderSettings.fog;var fogMode=RenderSettings.fogMode;var fogDensity=RenderSettings.fogDensity;var fogColor=RenderSettings.fogColor;
  var sky=RenderSettings.skybox;var ambMode=RenderSettings.ambientMode;var ambSky=RenderSettings.ambientSkyColor;var ambEq=RenderSettings.ambientEquatorColor;var ambGround=RenderSettings.ambientGroundColor;var ambIntensity=RenderSettings.ambientIntensity;var reflection=RenderSettings.reflectionIntensity;
  var target=SceneManager.GetSceneByPath("Assets/Scene/SoloScene2.unity");
  if(!target.isLoaded)target=EditorSceneManager.OpenScene("Assets/Scene/SoloScene2.unity",OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(target,"artifacts/backups/SoloScene2_before_covered_interior_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
  if(Find(target,"Surface Research Interior - Covered"))throw new Exception("Already installed; inspect before editing.");
  SceneManager.SetActiveScene(target);
  var building=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model),target);
  building.name="Surface Research Interior - Covered";building.transform.position=new Vector3(0,.05f,0);building.transform.rotation=Quaternion.identity;building.transform.localScale=Vector3.one;
  Undo.RegisterCreatedObjectUndo(building,"Place covered surface research interior");
  var wall=Textured("IvoryPaint","IvoryPaint");var frame=Textured("Graphite","Graphite");frame.SetFloat("_Metallic",.65f);
  var floor=Mat("ResinFloor",new Color(.24f,.29f,.30f),.12f,.78f);
  var yellow=Mat("SafetyOchre",new Color(.7f,.43f,.09f),.15f,.55f);
  var glass=Mat("ClearObservationGlass",new Color(.72f,.85f,.9f,.13f),.0f,.16f);
  glass.SetFloat("_Mode",3);glass.SetOverrideTag("RenderType","Transparent");glass.SetInt("_SrcBlend",(int)BlendMode.One);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;
  var cage=Mat("CageSteel",new Color(.105f,.135f,.15f),.78f,.29f);var alloy=Mat("BrushedAlloy",new Color(.32f,.42f,.46f),.72f,.25f);var plinth=Mat("CeramicPlinth",new Color(.15f,.19f,.20f),.45f,.34f);var violet=Mat("WarningViolet",new Color(.27f,.035f,.48f),.25f,.24f);
  foreach(var r in building.GetComponentsInChildren<MeshRenderer>()){
   r.sharedMaterials=r.sharedMaterials.Select(m=>{string n=m?m.name:"";return n.Contains("Ivory")?wall:n.Contains("Graphite_Frame")?frame:n.Contains("Concrete")?floor:n.Contains("Ochre")?yellow:n.Contains("Glass")?glass:n.Contains("ceramic")?plinth:n.Contains("brushed")?alloy:n.Contains("violet")?violet:cage;}).ToArray();
   bool transparent=r.sharedMaterials.Contains(glass);r.shadowCastingMode=transparent?ShadowCastingMode.Off:ShadowCastingMode.On;
   if(!r.name.StartsWith("SI_07") && !r.name.Contains("specimen warning")){
    var mc=r.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
   }
   r.gameObject.isStatic=true;
  }
  var lights=new GameObject("Interior practical lighting");lights.transform.SetParent(building.transform,false);
  LightAt(lights.transform,"Reception ceiling light",new Vector3(0,3.6f,20),15,1.5f);
  LightAt(lights.transform,"West round lab light",new Vector3(-14,3.6f,18.5f),12,1.2f);
  LightAt(lights.transform,"East round lab light",new Vector3(14,3.6f,18.5f),12,1.2f);
  foreach(float x in new[]{-14f,14f})foreach(float z in new[]{-3f,7f})LightAt(lights.transform,"Passage light",new Vector3(x,3.6f,z),10,1);
  foreach(float x in new[]{-14f,0f,14f})LightAt(lights.transform,"Rear laboratory light",new Vector3(x,3.6f,-18),14,1.35f);
  LightAt(lights.transform,"Cage skylight fill",new Vector3(0,9,2.5f),24,2.4f);
  PrefabUtility.SaveAsPrefabAsset(building,Folder+"/Prefabs/SurfaceResearchInterior.prefab");
  var snowGround=Clone(Find(source,"Snow Plain - Morning"),target,"Snow Plain - Morning");snowGround.transform.position+=Vector3.down*.15f;
  foreach(var root in target.GetRootGameObjects().Where(g=>g.name=="Directional Light").ToArray())UnityEngine.Object.DestroyImmediate(root);
  var sun=Clone(Find(source,"Directional Light"),target,"Directional Light");
  var srcFill=Find(source,"Snow Bounce - soft environment fill");if(srcFill)Clone(srcFill,target,srcFill.name);
  var ps=Clone(Find(source,"Heavy Snowfall - near visibility"),target,"Heavy Snowfall - exterior only").GetComponent<ParticleSystem>();
  ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.position=new Vector3(-5,23,8);ps.transform.rotation=Quaternion.identity;
  var main=ps.main;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=35000;main.startLifetime=new ParticleSystem.MinMaxCurve(12,16);
  var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(85,.1f,85);shape.position=Vector3.zero;
  var emission=ps.emission;emission.rateOverTime=2200;
  var collision=ps.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.quality=ParticleSystemCollisionQuality.High;collision.collidesWith=~0;collision.enableDynamicColliders=false;collision.lifetimeLoss=1;collision.radiusScale=.5f;
  for(int i=collision.planeCount-1;i>=0;i--)collision.RemovePlane(i);
  RenderSettings.fog=fog;RenderSettings.fogMode=fogMode;RenderSettings.fogDensity=fogDensity;RenderSettings.fogColor=fogColor;RenderSettings.skybox=sky;RenderSettings.ambientMode=ambMode;RenderSettings.ambientSkyColor=ambSky;RenderSettings.ambientEquatorColor=ambEq;RenderSettings.ambientGroundColor=ambGround;RenderSettings.ambientIntensity=ambIntensity;RenderSettings.reflectionIntensity=reflection;RenderSettings.sun=sun.GetComponent<Light>();
  var camera=target.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
  if(!camera){camera=new GameObject("Main Camera").AddComponent<Camera>();camera.gameObject.AddComponent<AudioListener>();}
  camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=fogColor;camera.fieldOfView=65;camera.nearClipPlane=.08f;camera.farClipPlane=240;
  var player=new GameObject("Interior Explorer");player.transform.position=new Vector3(0,.10f,21.5f);player.transform.rotation=Quaternion.Euler(0,180,0);
  var cc=player.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.3f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.3f;
  camera.transform.SetParent(player.transform,false);camera.transform.localPosition=new Vector3(0,1.65f,0);camera.transform.localRotation=Quaternion.identity;
  var fps=player.AddComponent<SnowFirstPerson>();fps.viewCamera=camera;fps.snowfall=null;fps.boundary=188;
  var weather=ps.gameObject.AddComponent<SurfaceInteriorSnow>();weather.observer=player.transform;weather.snowfall=ps;weather.emissionHeight=23;
  var boundary=Find(source,"World Boundary - 188m limit");if(boundary)Clone(boundary,target,boundary.name);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(target);EditorSceneManager.SaveScene(target);
  // Only the destination scene should run; do not alter or save SoloScene1.
  EditorSceneManager.CloseScene(source,true);
  Selection.activeGameObject=building;SceneView.lastActiveSceneView?.LookAt(new Vector3(0,4,0),Quaternion.Euler(35,-145,0),50);
  var rs=building.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
  var result=new{scene=target.path,bounds=bounds.ToString(),renderers=rs.Length,colliders=building.GetComponentsInChildren<Collider>().Length,ceilings=rs.Count(r=>r.name.StartsWith("SI_08")),fogDensity,groundY=snowGround.transform.position.y,snow=ps.main.maxParticles,spawn=player.transform.position.ToString()};
  File.WriteAllText("artifacts/surface_interior/unity-placement.txt",result.ToString());return result;
 }
}
