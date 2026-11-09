using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HollowCreek;
public static class FixSoloEntrance {
 public static string Run() {
  var scene=SceneManager.GetActiveScene();
  if(EditorApplication.isPlaying || scene.path!="Assets/Scene/SoloScene1.unity") throw new Exception("SoloScene1 edit mode required");
  var target=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HollowCreekTarget>(true)).Single(t=>t.interaction==Interaction.Entrance);
  var campaign=target.campaign;
  var sign=campaign.GetComponentsInChildren<TextMesh>(true).Single(t=>t.text=="HOLLOW CREEK\nACCESS CONTROL");
  string backup="artifacts/backups/SoloScene1_before_entrance_cleanup_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
  if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
  Undo.RecordObject(target.transform,"Expose entrance access panel");
  target.transform.position=new Vector3(1.5f,2.05f,-61.45f);
  Undo.RecordObject(sign.transform,"Align entrance sign");sign.transform.position=new Vector3(1.5f,2.8f,-61.4f);
  var lamp=campaign.GetComponentsInChildren<Light>(true).SingleOrDefault(l=>Vector3.Distance(l.transform.position,new Vector3(1.2f,3,-56))<.1f);
  if(lamp) {Undo.RecordObject(lamp.transform,"Align entrance light");lamp.transform.position=new Vector3(1.2f,3,-62);}
  Physics.SyncTransforms();
  var eye=new Vector3(.65f,2.5f,-62.5f);var direction=target.transform.position-eye;
  if(!Physics.Raycast(eye,direction.normalized,out var hit,3.5f,~0,QueryTriggerInteraction.Ignore)||hit.collider.GetComponentInParent<HollowCreekTarget>()!=target)
   throw new Exception("Access panel ray blocked by "+(hit.collider?hit.collider.name:"nothing"));
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
  var report="Entrance panel="+target.transform.position+" raycast distance="+hit.distance+" backup="+backup;
  File.WriteAllText("artifacts/solo_entrance_fix.txt",report);return report;
 }
}
