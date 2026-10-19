using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class InspectBlizzard {
 public static object Run(){return new{playing=EditorApplication.isPlaying,scene=SceneManager.GetActiveScene().path,roots=SceneManager.GetActiveScene().GetRootGameObjects().Select(g=>g.name).ToArray(),models=new[]{"abandoned_research_facility","military_cargo_truck"}.Select(n=>{var a=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blender/"+n+".blend");var g=Object.Instantiate(a);try{var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return new{name=n,size=b.size.ToString(),center=b.center.ToString(),min=b.min.ToString(),parts=rs.Select(r=>new{name=r.name,size=r.bounds.size.ToString(),center=r.bounds.center.ToString()}).ToArray(),materials=rs.SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>m==null?"NULL":m.name+" : "+m.shader.name).ToArray()};}finally{Object.DestroyImmediate(g);}}).ToArray()};}
}
