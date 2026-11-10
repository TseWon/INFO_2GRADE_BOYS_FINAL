using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class OptimizeInteriorSnow {
 public static object Run(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  var scene=SceneManager.GetSceneByPath("Assets/Scene/SoloScene2.unity");
  if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scene/SoloScene2.unity",OpenSceneMode.Single);
  SceneManager.SetActiveScene(scene);
  var building=GameObject.Find("Surface Research Interior - Covered");
  var snow=GameObject.Find("Heavy Snowfall - exterior only").GetComponent<ParticleSystem>();
  snow.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var old=GameObject.Find("Snow collision - roofs and ground only");if(old)UnityEngine.Object.DestroyImmediate(old);
  var proxies=new GameObject("Snow collision - roofs and ground only");proxies.layer=2;proxies.isStatic=true;
  foreach(var mf in building.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.StartsWith("SI_08")&&!m.name.Contains("Graphite"))){
   var g=new GameObject("Snow blocker - "+mf.name);g.layer=2;g.isStatic=true;g.transform.SetParent(proxies.transform,false);g.transform.SetPositionAndRotation(mf.transform.position,mf.transform.rotation);g.transform.localScale=mf.transform.lossyScale;
   g.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
  }
  var ground=new GameObject("Snow ground collision");ground.layer=2;ground.isStatic=true;ground.transform.SetParent(proxies.transform);ground.transform.position=new Vector3(0,-.2f,0);ground.AddComponent<BoxCollider>().size=new Vector3(400,.1f,400);
  var collision=snow.collision;collision.collidesWith=1<<2;collision.quality=ParticleSystemCollisionQuality.Medium;collision.voxelSize=.4f;collision.enableDynamicColliders=false;
  var main=snow.main;main.prewarm=false;main.maxParticles=22000;main.startLifetime=new ParticleSystem.MinMaxCurve(12,16);
  var emission=snow.emission;emission.rateOverTime=1800;
  var weather=snow.GetComponent<SurfaceInteriorSnow>();weather.building=building.transform;
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var result=new{scene=scene.path,proxies=proxies.GetComponentsInChildren<Collider>().Length,collisionLayers=collision.collidesWith.value,prewarm=main.prewarm,maximum=main.maxParticles};File.WriteAllText("artifacts/surface_interior/snow-optimization.txt",result.ToString());return result;
 }
}
