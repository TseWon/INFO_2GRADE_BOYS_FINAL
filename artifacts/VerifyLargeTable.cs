using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class VerifyLargeTable {
 public static string Run() {
  string path="Assets/Blender/oak_steel_table.blend";
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  if(model==null)throw new Exception("Model missing");
  var instance=UnityEngine.Object.Instantiate(model);
  try {
   instance.transform.position=Vector3.zero;
   var rs=instance.GetComponentsInChildren<Renderer>();
   var b=rs[0].bounds; foreach(var r in rs)b.Encapsulate(r.bounds);
   var mats=rs.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
   foreach(var m in mats){
    if(m==null || !AssetDatabase.GetAssetPath(m).StartsWith("Assets/Blender/OakSteelTable/Materials"))throw new Exception("Material link missing");
    if(m.name.Contains("oak")){m.SetFloat("_BumpScale",.65f);EditorUtility.SetDirty(m);if(m.mainTexture==null||m.GetTexture("_BumpMap")==null)throw new Exception("Wood texture missing");}
   }
   foreach(var mf in instance.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh.uv.Length!=mf.sharedMesh.vertexCount)throw new Exception("UV missing");
   var s=b.size;
   if(Mathf.Abs(s.x-2f)>.01f||Mathf.Abs(s.y-1.3f)>.01f||Mathf.Abs(s.z-2.4f)>.01f)throw new Exception("Incorrect bounds "+s);
   AssetDatabase.SaveAssets(); Selection.activeObject=model;EditorGUIUtility.PingObject(model);
   return "Verified dimensions="+s.ToString("F3")+"; renderers="+rs.Length+"; materials="+mats.Length+"; textured wood, normal map and UVs valid; GUID="+AssetDatabase.AssetPathToGUID(path);
  } finally {UnityEngine.Object.DestroyImmediate(instance);}
 }
}
