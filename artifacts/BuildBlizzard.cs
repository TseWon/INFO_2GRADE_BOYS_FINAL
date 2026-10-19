using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
public static class BuildBlizzard {
 public static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 public static string Run(){
  var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.path!="Assets/Scene/Scene1.unity")throw new Exception("Scene1 edit mode required");
  Directory.CreateDirectory("artifacts/backups");EditorSceneManager.SaveScene(scene,"artifacts/backups/Scene1_before_blizzard_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
  if(GameObject.Find("Research Facility - Snow Site")!=null)throw new Exception("Already placed");
  var facility=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blender/abandoned_research_facility.blend"));facility.name="Research Facility - Snow Site";facility.transform.position=new Vector3(0,0,35);Undo.RegisterCreatedObjectUndo(facility,"Place research facility");
  foreach(var mf in facility.GetComponentsInChildren<MeshFilter>()){if(mf.name.Contains("glazing")||mf.name.Contains("cracks")||mf.name.Contains("pipework"))continue;var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;}
  var truck=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blender/military_cargo_truck.blend"));truck.name="Military Truck - Forecourt";truck.transform.SetPositionAndRotation(new Vector3(4,0,-66),Quaternion.Euler(0,28,0));truck.transform.position+=Vector3.up*(-BoundsOf(truck).min.y+.005f);Undo.RegisterCreatedObjectUndo(truck,"Place truck");
  var hull=truck.AddComponent<BoxCollider>();hull.center=new Vector3(.05f,1.7f,0);hull.size=new Vector3(8,2.8f,2.45f);
  var ground=GameObject.Find("Snow-covered ground 400 x 400 m");var mesh=ground.GetComponent<MeshFilter>().sharedMesh;var vs=mesh.vertices;
  for(int i=0;i<vs.Length;i++){var p=vs[i];float dx=Mathf.Max(0,Mathf.Abs(p.x)-116);float dz=Mathf.Max(0,Mathf.Abs(p.z-26)-106);float d=Mathf.Max(dx,dz);p.y=Mathf.Lerp(0,p.y,Mathf.SmoothStep(0,1,d/20));vs[i]=p;}mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);ground.GetComponent<MeshCollider>().sharedMesh=null;ground.GetComponent<MeshCollider>().sharedMesh=mesh;
  RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.023f;RenderSettings.fogColor=new Color(.65f,.71f,.77f);RenderSettings.ambientSkyColor=new Color(.43f,.49f,.56f);RenderSettings.ambientEquatorColor=new Color(.35f,.40f,.46f);RenderSettings.ambientGroundColor=new Color(.25f,.29f,.34f);RenderSettings.sun.intensity=.44f;RenderSettings.sun.color=new Color(.93f,.96f,1);RenderSettings.sun.shadowStrength=.45f;
  var cam=Camera.main;cam.transform.position=new Vector3(18,3.6f,-83);cam.transform.LookAt(new Vector3(-2,7,-43));cam.fieldOfView=64;cam.farClipPlane=240;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=RenderSettings.fogColor;
  var ps=cam.GetComponentInChildren<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.name="Heavy Snowfall - near visibility";ps.transform.position=cam.transform.position+new Vector3(-5,6,8);ps.transform.rotation=Quaternion.identity;
  var main=ps.main;main.duration=12;main.startLifetime=new ParticleSystem.MinMaxCurve(8,12);main.startSize=new ParticleSystem.MinMaxCurve(.045f,.125f);main.startColor=new Color(.94f,.97f,1,.85f);main.maxParticles=22000;main.prewarm=true;var shape=ps.shape;shape.scale=new Vector3(65,26,65);var emission=ps.emission;emission.rateOverTime=1800;var velocity=ps.velocityOverLifetime;velocity.x=1.2f;velocity.y=-2.1f;velocity.z=.35f;var noise=ps.noise;noise.strength=.42f;noise.frequency=.25f;noise.scrollSpeed=.4f;
  var collision=ps.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.Planes;var plane=new GameObject("Snowfall ground collision plane");plane.transform.position=new Vector3(0,.015f,0);plane.transform.SetParent(GameObject.Find("Snow Plain - Morning").transform);collision.SetPlane(0,plane.transform);collision.dampen=1;collision.lifetimeLoss=1;
  var groundMat=ground.GetComponent<Renderer>().sharedMaterial;groundMat.SetFloat("_BumpScale",.23f);EditorUtility.SetDirty(groundMat);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);ps.Simulate(12,true,true);Selection.activeGameObject=facility;SceneView.lastActiveSceneView?.LookAt(new Vector3(0,12,-30),Quaternion.Euler(15,-15,0),80);
  var fb=BoundsOf(facility);var tb=BoundsOf(truck);if(fb.Intersects(tb))throw new Exception("Facility and truck bounds intersect");return "Saved. Facility="+fb+" Truck="+tb+"; clear gap Z="+(fb.min.z-tb.max.z).ToString("F2")+"m; truck ground clearance="+tb.min.y.ToString("F3")+"m; fog transmission at 15/50/80m="+Mathf.Exp(-Mathf.Pow(.023f*15,2))+" / "+Mathf.Exp(-Mathf.Pow(.023f*50,2))+" / "+Mathf.Exp(-Mathf.Pow(.023f*80,2));
 }
}
