using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InspectHollowCreek {
 public static string Run() {
  var lines=new System.Collections.Generic.List<string>();
  foreach(var path in new[]{"Assets/Scene/SoloScene1.unity","Assets/Scene/SoloScene2.unity"}) {
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   try { foreach(var root in scene.GetRootGameObjects()) {
    var rs=root.GetComponentsInChildren<Renderer>(); var b=new Bounds(root.transform.position,Vector3.zero);
    if(rs.Length>0){b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);}
    lines.Add(path+" / "+root.name+" pos="+root.transform.position+" bounds="+b+" colliders="+root.GetComponentsInChildren<Collider>().Length);
    if(root.name.Contains("Underground"))foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.childCount>0||t.name.ToLower().Contains("floor")))lines.Add("  "+t.name+" "+t.position+" scale="+t.lossyScale);
   }}finally{EditorSceneManager.CloseScene(scene,true);}
  }
  return string.Join("\n",lines);
 }
}
