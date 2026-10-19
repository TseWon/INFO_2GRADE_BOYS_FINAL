using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
public static class SetupFirstPerson {
 const string Folder="Assets/Environment/SnowPlain/RefinedMaterials";
 static Material CharacterMaterial(string name,Color color,float smooth){var path="Assets/Characters/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Glossiness",smooth);return m;}
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scene/Scene1.unity")throw new Exception("Scene1 required");
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blender/snow_wanderer.blend");if(asset==null)throw new Exception("Character import pending");
  var shader=Shader.Find("Environment/Soft Snow Surface");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Snow shader failed");
  if(GameObject.Find("Snow Wanderer Player")!=null)throw new Exception("Player already exists");
  EditorSceneManager.SaveScene(scene,"artifacts/backups/Scene1_before_firstperson_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
  Directory.CreateDirectory(Folder);Directory.CreateDirectory("Assets/Characters");AssetDatabase.Refresh();
  var materials=new Dictionary<Material,Material>();int count=0;
  foreach(var name in new[]{"Research Facility - Snow Site","Military Truck - Forecourt"})foreach(var r in GameObject.Find(name).GetComponentsInChildren<Renderer>()){
   var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var source=mats[i];if(!materials.TryGetValue(source,out var m)){
    string key=source.name.ToLowerInvariant();m=new Material(source);m.name=source.name+" - snow softened";
    bool glass=key.Contains("glass"),lens=key.Contains("lens")||key.Contains("lamp"),rubber=key.Contains("rubber")||key.Contains("gasket");
    if(!glass&&!lens){m.shader=shader;m.SetFloat("_SnowAmount",rubber?.12f:.80f);m.SetFloat("_BumpScale",key.Contains("concrete")?.13f:.20f);m.SetFloat("_WeatherSoftness",key.Contains("moss")?.45f:.18f);m.SetFloat("_Glossiness",rubber?.10f:key.Contains("metal")||key.Contains("steel")?.30f:.16f);m.SetFloat("_Metallic",key.Contains("steel")?.35f:0);m.SetFloat("_Camouflage",key.Contains("camouflage")?1:0);}
    else {m.SetFloat("_Glossiness",glass?.43f:.55f);m.SetFloat("_Metallic",glass?.05f:0);if(m.HasProperty("_BumpScale"))m.SetFloat("_BumpScale",.08f);}
    var clean=new string(source.name.Where(char.IsLetterOrDigit).ToArray());AssetDatabase.CreateAsset(m,Folder+"/"+clean+".mat");materials[source]=m;count++;
   }mats[i]=m;}r.sharedMaterials=mats;
  }
  QualitySettings.antiAliasing=4;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=65;RenderSettings.sun.shadowBias=.02f;RenderSettings.sun.shadowNormalBias=.12f;RenderSettings.sun.intensity=.52f;
  var cam=Camera.main;var ps=cam.GetComponentInChildren<ParticleSystem>();ps.transform.SetParent(null,true);ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.startSize=new ParticleSystem.MinMaxCurve(.035f,.09f);main.startColor=new Color(.94f,.97f,1,.66f);var em=ps.emission;em.rateOverTime=2200;var noise=ps.noise;noise.strength=.28f;var pr=ps.GetComponent<ParticleSystemRenderer>();pr.maxParticleSize=.035f;
  var player=new GameObject("Snow Wanderer Player");player.transform.position=new Vector3(14,.04f,-79);player.transform.rotation=Quaternion.Euler(0,-24,0);var cc=player.AddComponent<CharacterController>();cc.height=1.0f;cc.radius=.32f;cc.center=new Vector3(0,.5f,0);cc.skinWidth=.025f;cc.stepOffset=.18f;cc.slopeLimit=45;cc.minMoveDistance=0;
  var visual=(GameObject)PrefabUtility.InstantiatePrefab(asset);visual.name="Snow Wanderer - visible model";visual.transform.SetParent(player.transform,false);visual.transform.localPosition=Vector3.zero;
  // Preserve the Blender-to-Unity axis transform; model faces Unity forward.
  var body=CharacterMaterial("Wanderer Ivory",new Color(.84f,.82f,.78f),.26f);var eyes=CharacterMaterial("Wanderer Eyes",new Color(.008f,.009f,.011f),.78f);
  foreach(var r in visual.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.ToLowerInvariant().Contains("eye")?eyes:body).ToArray();}
  PrefabUtility.SaveAsPrefabAsset(visual,"Assets/Characters/SnowWandererVisual.prefab");
  cam.transform.SetParent(player.transform,false);cam.transform.localPosition=new Vector3(0,.77f,.02f);cam.transform.localRotation=Quaternion.identity;cam.nearClipPlane=.04f;cam.fieldOfView=70;cam.allowMSAA=true;
  var controller=player.AddComponent<SnowFirstPerson>();controller.viewCamera=cam;controller.visual=visual.transform;controller.snowfall=ps;
  var edge=new GameObject("World Boundary - 188m limit");for(int i=0;i<4;i++){var wall=new GameObject("Invisible perimeter wall "+i);wall.transform.SetParent(edge.transform);bool x=i<2;float sign=i%2==0?1:-1;wall.transform.position=x?new Vector3(sign*190,10,0):new Vector3(0,10,sign*190);var box=wall.AddComponent<BoxCollider>();box.size=x?new Vector3(2,40,382):new Vector3(382,40,2);}
  ps.transform.position=player.transform.position+new Vector3(-5,9,8);ps.transform.rotation=Quaternion.identity;ps.Simulate(12,true,true);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=player;return "Player configured: WASD/arrows + mouse; eye=.77m; 4 boundary walls; refined materials="+count;
 }
}
