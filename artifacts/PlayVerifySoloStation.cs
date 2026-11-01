using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class PlayVerifySoloStation {
 const string Out="artifacts/surface_station/";
 public static string Resume() {Application.runInBackground=true;EditorApplication.isPaused=false;return "Resumed verification; frame="+Time.frameCount;}
 public static string Prepare() {
  File.WriteAllText(Out+"run_in_background_before.txt",Application.runInBackground.ToString());Application.runInBackground=true;
  EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return "Saved; runInBackground enabled for verification";
 }
 public static async Task<string> Runtime() {
  if(!Application.isPlaying)throw new Exception("Play mode required");
  int first=Time.frameCount;await Task.Delay(500);int second=Time.frameCount;
  if(second<=first)throw new Exception("Player loop is not advancing");
  var errors=new List<string>();Application.LogCallback listener=(text,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);};Application.logMessageReceived+=listener;
  var player=GameObject.Find("Tactical Soldier Player");var mover=player.GetComponent<SnowFirstPerson>();var motor=player.GetComponent<CharacterController>();
  var oldPos=player.transform.position;var oldRot=player.transform.rotation;bool enabled=mover.enabled;float high=0;Vector3 final=Vector3.zero;
  try {
   mover.enabled=false;motor.enabled=false;player.transform.position=new Vector3(0,.04f,-60);player.transform.rotation=Quaternion.identity;motor.enabled=true;Physics.SyncTransforms();
   for(int i=0;i<140;i++){mover.Move(Vector2.up,false,.02f);high=Mathf.Max(high,player.transform.position.y);await Task.Delay(20);}
   final=player.transform.position;
   var cam=Camera.main;var rt=RenderTexture.GetTemporary(1280,800,24);var active=RenderTexture.active;var target=cam.targetTexture;var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);
   try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes(Out+"solo_station_runtime.png",tex.EncodeToPNG());}
   finally{cam.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);}
  } finally {
   motor.enabled=false;player.transform.position=oldPos;player.transform.rotation=oldRot;motor.enabled=true;mover.enabled=enabled;Application.logMessageReceived-=listener;
  }
  string result="Frames="+first+" -> "+second+"\nActualPlayerStairMax="+high.ToString("F3")+"\nFinal="+final.ToString("F3")+"\nReachedLanding="+(high>.78f)+"\nErrorsDuringWalk="+errors.Count+"\n"+string.Join("\n",errors);
  File.WriteAllText(Out+"solo_playmode_report.txt",result);
  if(high<.78f||errors.Count>0)throw new Exception(result);
  return result;
 }
 public static string Restore() {
  if(Application.isPlaying)throw new Exception("Stop play mode first");
  Application.runInBackground=bool.Parse(File.ReadAllText(Out+"run_in_background_before.txt"));
  var root=GameObject.Find("Surface Research Station - Snow Site");Selection.activeGameObject=root;
  EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return "Edit mode restored; scene saved";
 }
}
