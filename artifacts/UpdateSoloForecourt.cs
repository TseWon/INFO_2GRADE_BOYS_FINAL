using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class UpdateSoloForecourt {
 public static string Run(){
  var scene=SceneManager.GetActiveScene();
  if(EditorApplication.isPlaying||scene.path!="Assets/Scene/SoloScene1.unity")throw new Exception("SoloScene1 edit mode required");
  var perimeter=GameObject.Find("Hollow Creek Iron Perimeter");
  var truck=GameObject.Find("Military Truck - Forecourt");
  var ground=GameObject.Find("Snow-covered ground 400 x 400 m");
  if(!perimeter||!truck||!ground)throw new Exception("Required scene objects missing");
  Directory.CreateDirectory("artifacts/backups");
  var backup="artifacts/backups/SoloScene1_before_fitted_soldier_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
  if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
  if(PrefabUtility.IsPartOfPrefabInstance(perimeter))PrefabUtility.UnpackPrefabInstance(perimeter,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  Undo.RegisterFullObjectHierarchyUndo(perimeter,"Shorten front yard only");
  var gate=perimeter.GetComponentInChildren<OutwardResearchGate>();
  float delta=-64f-gate.transform.position.z;
  if(Mathf.Abs(delta)>0.01f){
   foreach(Transform t in perimeter.transform.Cast<Transform>().ToArray()){
    if(t.name.StartsWith("Fence side ")&&!t.name.StartsWith("Fence side end")&&t.position.z< -64)Undo.DestroyObjectImmediate(t.gameObject);
    else if(t.name.StartsWith("Fence front")||t==gate.transform||t.name=="Invisible wall south-west"||t.name=="Invisible wall south-east")t.position+=Vector3.forward*delta;
   }
  }
  foreach(var c in perimeter.GetComponentsInChildren<BoxCollider>()){
   if(c.name=="Invisible wall west"||c.name=="Invisible wall east") {var p=c.transform.position;p.z=-38;c.transform.position=p;c.size=new Vector3(c.size.x,c.size.y,52);}
   if(c.name=="Invisible wall access west"||c.name=="Invisible wall access east") {var p=c.transform.position;p.z=-87;c.transform.position=p;c.size=new Vector3(c.size.x,c.size.y,46);}
  }
  // A scene-specific snow mesh: leave other scenes and the distant snow banks untouched.
  var mf=ground.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name="Solo cleared forecourt snow";
  var verts=mesh.vertices;int changed=0;
  for(int i=0;i<verts.Length;i++){
   var p=ground.transform.TransformPoint(verts[i]);
   float dx=Mathf.Max(0,Mathf.Abs(p.x)-22),dz=Mathf.Max(0,Mathf.Abs(p.z+85)-27);
   float d=Mathf.Sqrt(dx*dx+dz*dz);float blend=1-Mathf.SmoothStep(0,1,d/10);
   if(blend<=0)continue;
   p.y=Mathf.Lerp(p.y,0,blend);verts[i]=ground.transform.InverseTransformPoint(p);changed++;
  }
  mesh.vertices=verts;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
  const string path="Assets/Environment/SnowPlain/SoloClearedForecourt.asset";
  var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
  if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
  Undo.RecordObject(mf,"Clear snow around truck");mf.sharedMesh=mesh;
  var collider=ground.GetComponent<MeshCollider>();Undo.RecordObject(collider,"Update cleared snow collision");collider.sharedMesh=null;collider.sharedMesh=mesh;
  Physics.SyncTransforms();
  var rs=truck.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);
  Undo.RecordObject(truck.transform,"Seat truck on cleared snow");truck.transform.position+=Vector3.up*(.005f-bounds.min.y);
  EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var report="Front fence z=-64 (was -76), rear z=-12 unchanged; depth 52m (was64); width38m height4m unchanged; modified snow vertices="+changed+"; truck="+truck.transform.position+"; backup="+backup;
  File.WriteAllText("artifacts/solo_forecourt_update.txt",report);return report;
 }
}
