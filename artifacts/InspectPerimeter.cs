using UnityEngine;
using UnityEditor;
using System.Linq;
public static class InspectPerimeter {
 public static string Run(){
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  AssetDatabase.ImportAsset("Assets/Environment/IronPerimeter/DoubleGate.fbx",ImportAssetOptions.ForceSynchronousImport);
  AssetDatabase.ImportAsset("Assets/Environment/IronPerimeter/FencePanel.fbx",ImportAssetOptions.ForceSynchronousImport);
  var g=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/IronPerimeter/DoubleGate.fbx");
  var f=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/IronPerimeter/FencePanel.fbx");
  if(!g||!f)return "MISSING gate="+(g!=null)+" fence="+(f!=null);
  var gi=UnityEngine.Object.Instantiate(g);var fi=UnityEngine.Object.Instantiate(f);
  var result="GATE="+string.Join(";",gi.GetComponentsInChildren<Transform>().Select(t=>t.name+"@"+t.localPosition+" scale="+t.lossyScale))+
  "\nGATE_RENDERERS="+string.Join(";",gi.GetComponentsInChildren<Renderer>().Select(r=>r.name+":"+r.bounds.center+" size="+r.bounds.size))+
  "\nFENCE="+string.Join(";",fi.GetComponentsInChildren<Renderer>().Select(r=>r.name+":"+r.bounds.center+" size="+r.bounds.size));
  UnityEngine.Object.DestroyImmediate(gi);UnityEngine.Object.DestroyImmediate(fi);return result;
 }
}
