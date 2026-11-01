using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildSoloPerimeter {
 const string Folder="Assets/Environment/IronPerimeter";
 const string FencePath=Folder+"/FencePanel.fbx";
 const string GatePath=Folder+"/DoubleGate.fbx";
 const string Name="Hollow Creek Iron Perimeter";
 static Dictionary<string,Material> mats;
 static Material Mat(string name,Color color,float metallic,float smooth) {
  string path=Folder+"/Materials/"+name+".mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
  m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);EditorUtility.SetDirty(m);return m;
 }
 static void Materials(){
  Directory.CreateDirectory(Folder+"/Materials");
  mats=new Dictionary<string,Material>{
   ["IP_GalvanizedSteel"]=Mat("IP_GalvanizedSteel",new Color(.38f,.43f,.45f),.78f,.57f),
   ["IP_WornIron"]=Mat("IP_WornIron",new Color(.12f,.15f,.17f),.72f,.42f),
   ["IP_Concrete"]=Mat("IP_Concrete",new Color(.34f,.36f,.35f),.02f,.11f),
   ["IP_GateIron"]=Mat("IP_GateIron",new Color(.17f,.20f,.21f),.8f,.51f),
   ["IP_GateTrim"]=Mat("IP_GateTrim",new Color(.36f,.37f,.34f),.7f,.53f)
  };
 }
 static GameObject Instance(GameObject source,Transform parent,string name,Vector3 localPosition,Quaternion localRotation,float widthScale=1f,float heightScale=1f){
  var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=localPosition;go.transform.localRotation=localRotation*source.transform.localRotation;
  // The Blender FBX root rotates local Z into Unity world Y.
  go.transform.localScale=Vector3.Scale(source.transform.localScale,new Vector3(widthScale,1f,heightScale));
  foreach(var r in go.GetComponentsInChildren<Renderer>()){
   r.sharedMaterials=r.sharedMaterials.Select(m=>{
    var key=mats.Keys.FirstOrDefault(k=>m.name==k||m.name.StartsWith(k+"."));
    if(key==null)throw new Exception("Unknown perimeter material "+m.name);return mats[key];
   }).ToArray();
  }
  return go;
 }
 static void Barrier(Transform parent,string name,Vector3 center,Vector3 size){
  var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=center;
  var c=go.AddComponent<BoxCollider>();c.size=size;go.layer=0;
 }
 public static string Run(){
  if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Edit mode required");
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scene/SoloScene1.unity")throw new Exception("SoloScene1 required: "+scene.path);
  var previousPerimeter=GameObject.Find(Name);
  var station=GameObject.Find("Surface Research Station - Snow Site");var truck=GameObject.Find("Military Truck - Forecourt");var player=GameObject.Find("Tactical Soldier Player");
  if(!station||!truck||!player)throw new Exception("Station/truck/player missing");
  var fenceAsset=AssetDatabase.LoadAssetAtPath<GameObject>(FencePath);var gateAsset=AssetDatabase.LoadAssetAtPath<GameObject>(GatePath);
  if(!fenceAsset||!gateAsset)throw new Exception("FBX import pending");
  var script=AssetDatabase.LoadAssetAtPath<MonoScript>(Folder+"/OutwardResearchGate.cs");
  if(!script||script.GetClass()!=typeof(OutwardResearchGate))throw new Exception("Gate script compilation pending");
  string backup="artifacts/backups/SoloScene1_before_perimeter_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
  Directory.CreateDirectory("artifacts/backups");if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
  if(previousPerimeter)Undo.DestroyObjectImmediate(previousPerimeter);
  Materials();var perimeter=new GameObject(Name);Undo.RegisterCreatedObjectUndo(perimeter,"Build perimeter");
  // Fence: x ±19, z -76..-12. Front gate centered at x=0, z=-76.
  const float fenceHeight=4f/2.52f;
  const float gateHeight=4f/2.68f;
  int count=0;
  for(float z=-74.5f;z< -12f;z+=3f){
   foreach(int side in new[]{-1,1}){
    Instance(fenceAsset,perimeter.transform,"Fence side "+side+" / "+count,new Vector3(side*19f,0,z),Quaternion.Euler(0,90,0),1f,fenceHeight);count++;
   }
  }
  foreach(int side in new[]{-1,1})Instance(fenceAsset,perimeter.transform,"Fence side end "+side+" / "+count++,new Vector3(side*19f,0,-12.5f),Quaternion.Euler(0,90,0),1f/3f,fenceHeight);
  for(int i=0;i<12;i++)Instance(fenceAsset,perimeter.transform,"Fence rear / "+count++,new Vector3(-17.5f+3f*i,0,-12),Quaternion.identity,1f,fenceHeight);
  Instance(fenceAsset,perimeter.transform,"Fence rear end / "+count++,new Vector3(18f,0,-12),Quaternion.identity,2f/3f,fenceHeight);
  for(int i=0;i<5;i++){
   Instance(fenceAsset,perimeter.transform,"Fence front left / "+count++,new Vector3(-17.5f+3f*i,0,-76),Quaternion.identity,1f,fenceHeight);
   Instance(fenceAsset,perimeter.transform,"Fence front right / "+count++,new Vector3(17.5f-3f*i,0,-76),Quaternion.identity,1f,fenceHeight);
  }
  Instance(fenceAsset,perimeter.transform,"Fence front left gate return / "+count++,new Vector3(-3.25f,0,-76),Quaternion.identity,.5f,fenceHeight);
  Instance(fenceAsset,perimeter.transform,"Fence front right gate return / "+count++,new Vector3(3.25f,0,-76),Quaternion.identity,.5f,fenceHeight);
  var gateController=new GameObject("Outward double gate");gateController.transform.SetParent(perimeter.transform,false);gateController.transform.localPosition=new Vector3(0,0,-76);
  var gate=Instance(gateAsset,gateController.transform,"Forged double-leaf gate model",Vector3.zero,Quaternion.identity,1f,gateHeight);
  PrefabUtility.UnpackPrefabInstance(gate,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  // FBX converts the model's handedness. Its named left leaf lands at world +X.
  var importedLeft=gate.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Gate_RightLeaf_Pivot");
  var importedRight=gate.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Gate_LeftLeaf_Pivot");
  if(!importedLeft||!importedRight)throw new Exception("Imported gate hinge transforms missing");
  Transform Hinge(string name,Transform imported){
   var hinge=new GameObject(name).transform;hinge.SetParent(gateController.transform,false);hinge.position=imported.position;hinge.rotation=Quaternion.identity;
   imported.SetParent(hinge,true);return hinge;
  }
  var left=Hinge("West leaf outward hinge",importedLeft);var right=Hinge("East leaf outward hinge",importedRight);
  // A solid invisible collision line follows the visible fence; the gate is the only passage.
  Barrier(perimeter.transform,"Invisible wall west",new Vector3(-19,3,-44),new Vector3(.35f,6,64));
  Barrier(perimeter.transform,"Invisible wall east",new Vector3(19,3,-44),new Vector3(.35f,6,64));
  Barrier(perimeter.transform,"Invisible wall north",new Vector3(0,3,-12),new Vector3(38,6,.35f));
  Barrier(perimeter.transform,"Invisible wall south-west",new Vector3(-10.75f,3,-76),new Vector3(16.5f,6,.35f));
  Barrier(perimeter.transform,"Invisible wall south-east",new Vector3(10.75f,3,-76),new Vector3(16.5f,6,.35f));
  Barrier(perimeter.transform,"Invisible wall access west",new Vector3(-19,3,-93),new Vector3(.35f,6,34));
  Barrier(perimeter.transform,"Invisible wall access east",new Vector3(19,3,-93),new Vector3(.35f,6,34));
  Barrier(perimeter.transform,"Invisible wall beyond truck",new Vector3(0,3,-110),new Vector3(38,6,.35f));
  foreach(var pair in new[]{(left,1f),(right,-1f)}){
   var c=pair.Item1.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(pair.Item2*1.235f,1.75f,0);c.size=new Vector3(2.47f,3.5f,.14f);
  }
  var door=gateController.AddComponent<OutwardResearchGate>();door.Configure(left,right);
  var before=truck.transform.position;truck.transform.position=new Vector3(before.x,before.y,-96);
  // Preserve the player's existing spawn just outside the gate and the truck 20 m farther out.
  Directory.CreateDirectory(Folder+"/Prefabs");
  PrefabUtility.SaveAsPrefabAssetAndConnect(perimeter,Folder+"/Prefabs/IronPerimeter.prefab",InteractionMode.AutomatedAction);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=gateController;
  var report="scene="+scene.path+" backup="+backup+" fencePanels="+count+" fenceHeight=4m gateHeight=4m gateClearWidth=5m perimeterWidth=38m truck="+before+" -> "+truck.transform.position+" player="+player.transform.position+" invisibleWalls=8";
  File.WriteAllText("artifacts/surface_station/perimeter_install.txt",report);return report;
 }
}
