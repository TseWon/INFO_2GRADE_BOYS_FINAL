using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class RemoveStationRailings {
 [Serializable] public class Plan { public PartSet[] objects; }
 [Serializable] public class PartSet { public string name; public Part[] parts; }
 [Serializable] public class Part { public int index; public float[] min,max; }
 const string Prefab="Assets/Environment/SurfaceResearchStation/Prefabs/SurfaceResearchStation.prefab";
 const string Folder="Assets/Environment/SurfaceResearchStation/MeshesWithoutRailings";
 static bool Inside(Vector3 v,Part p) {
  const float epsilon=.003f;
  // These FBX meshes retain centimetre-scaled Blender axes with mirrored X.
  v=new Vector3(-v.x*100f,v.y*100f,v.z*100f);
  return v.x>=p.min[0]-epsilon&&v.x<=p.max[0]+epsilon&&v.y>=p.min[1]-epsilon&&v.y<=p.max[1]+epsilon&&v.z>=p.min[2]-epsilon&&v.z<=p.max[2]+epsilon;
 }
 static Mesh Filter(Mesh source,PartSet set,out int removed) {
  var result=UnityEngine.Object.Instantiate(source);result.name=set.name+" without railings";removed=0;
  var vertices=source.vertices;var hitParts=new HashSet<int>();
  for(int sub=0;sub<source.subMeshCount;sub++) {
   var indices=source.GetTriangles(sub);var kept=new List<int>();
   for(int i=0;i<indices.Length;i+=3) {
    var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
    int match=Array.FindIndex(set.parts,p=>Inside(a,p)&&Inside(b,p)&&Inside(c,p));
    if(match>=0){removed++;hitParts.Add(match);}else {kept.Add(indices[i]);kept.Add(indices[i+1]);kept.Add(indices[i+2]);}
   }
   result.SetTriangles(kept,sub,false);
  }
  // Some shoes are exact duplicates, so the first matching bound covers both copies.
  if(removed==0)throw new Exception("No railing triangles matched "+set.name);
  result.RecalculateBounds();return result;
 }
 public static string Inspect(){return Run(false);}
 public static string Apply(){return Run(true);}
 static string Run(bool apply) {
  var scene=SceneManager.GetActiveScene();
  if(EditorApplication.isPlaying||scene.path!="Assets/Scene/SoloScene1.unity")throw new Exception("SoloScene1 edit mode required");
  var document=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("artifacts/station_rails_removal_plan.json"));
  var plan=new Plan { objects=document["objects"].Select(o=>new PartSet { name=(string)o["name"],parts=o["parts"].Select(p=>new Part {index=(int)p["index"],min=p["min"].Select(v=>(float)v).ToArray(),max=p["max"].Select(v=>(float)v).ToArray()}).ToArray()}).ToArray()};
  var prefab=PrefabUtility.LoadPrefabContents(Prefab);var replacements=new Dictionary<string,Mesh>();var lines=new List<string>();
  try {
   var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
   foreach(var set in plan.objects) {
    var mf=filters.Single(f=>f.name==set.name);
    var mesh=Filter(mf.sharedMesh,set,out int removed);replacements.Add(set.name,mesh);
    lines.Add(set.name+": removed="+removed+" triangles; remaining="+mesh.triangles.Length/3);
   }
   if(!apply)return string.Join("\n",lines);
   var backup="artifacts/backups/Station_before_railing_removal_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
   File.Copy(Prefab,backup+"/SurfaceResearchStation.prefab");if(!EditorSceneManager.SaveScene(scene,backup+"/SoloScene1.unity",true))throw new Exception("Scene backup failed");
   if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Environment/SurfaceResearchStation","MeshesWithoutRailings");
   foreach(var entry in replacements) {
    var path=Folder+"/"+entry.Key+".asset";
    if(AssetDatabase.LoadAssetAtPath<Mesh>(path))throw new Exception("Refusing duplicate run: "+path);
    AssetDatabase.CreateAsset(entry.Value,path);
    var mf=filters.Single(f=>f.name==entry.Key);mf.sharedMesh=entry.Value;
    foreach(var collider in mf.GetComponents<MeshCollider>())collider.sharedMesh=entry.Value;
   }
   PrefabUtility.SaveAsPrefabAsset(prefab,Prefab);
   var station=GameObject.Find("Surface Research Station - Snow Site");
   foreach(var mf in station.GetComponentsInChildren<MeshFilter>(true)) {
    if(!replacements.TryGetValue(mf.name,out var mesh)||mf.sharedMesh==mesh)continue;
    Undo.RecordObject(mf,"Remove station railings");mf.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
    foreach(var collider in mf.GetComponents<MeshCollider>()){Undo.RecordObject(collider,"Remove railing collision");collider.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
   }
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Physics.SyncTransforms();
   lines.Add("Backup="+backup);File.WriteAllText("artifacts/station_railing_removal.txt",string.Join("\n",lines));return string.Join("\n",lines);
  } finally {
   PrefabUtility.UnloadPrefabContents(prefab);
   foreach(var mesh in replacements.Values)if(mesh&&!AssetDatabase.Contains(mesh))UnityEngine.Object.DestroyImmediate(mesh);
  }
 }
}
