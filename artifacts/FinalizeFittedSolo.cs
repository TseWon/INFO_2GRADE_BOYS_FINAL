using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class FinalizeFittedSolo {
 public static string Run(){
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();
  if(!p||p.gameObject.scene.path!="Assets/Scene/SoloScene1.unity"||p.visual.name!="Fitted Soldier - Player Visual")throw new Exception("Saved soldier reference missing");
  var skins=p.visual.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length!=8||skins.Any(s=>s.bones.Any(b=>!b)||s.sharedMaterials.Any(m=>!m)))throw new Exception("Missing render dependency");
  var mesh=GameObject.Find("Snow-covered ground 400 x 400 m").GetComponent<MeshFilter>().sharedMesh;
  if(AssetDatabase.GetAssetPath(mesh)!="Assets/Environment/SnowPlain/SoloClearedForecourt.asset")throw new Exception("Cleared snow mesh missing");
  EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);AssetDatabase.SaveAssets();
  Selection.activeGameObject=p.gameObject;
  SceneView.lastActiveSceneView?.LookAt(p.transform.position+Vector3.up,Quaternion.Euler(8,160,0),2.5f);
  var text="Saved SoloScene1; fitted soldier dependencies verified; cleared snow referenced; edit mode restored.\n"+File.ReadAllText("artifacts/fitted_solo_playtest.txt");
  File.WriteAllText("artifacts/fitted_solo_final_verification.txt",text);return text;
 }
}
