using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HollowCreek;
public static class InstallHollowSpeech
{
 public static string Run()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop play mode first");
  Directory.CreateDirectory("Assets/HollowCreek/Speech");
  var entries=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText("artifacts/hollow-creek/SpeechLines.json"));
  var lines=entries.Select(e=>{
   string id=(string)e["id"],path="Assets/HollowCreek/Speech/"+id+".wav";
   File.Copy("artifacts/hollow-creek/speech/"+id+".wav",path,true);
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;
   var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.6f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
   return new HollowCreekSpeech.Line{startsWith=(string)e["match"],clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path),pitch=id.StartsWith("radio")?.95f:.9f};
  }).ToArray();
  for(int i=1;i<=4;i++) {
   var scene=EditorSceneManager.OpenScene("Assets/Scene/SoloScene"+i+".unity",OpenSceneMode.Single);
   var c=UnityEngine.Object.FindAnyObjectByType<HollowCreekCampaign>();
   var speech=c.GetComponent<HollowCreekSpeech>();if(speech==null)speech=c.gameObject.AddComponent<HollowCreekSpeech>();speech.lines=lines;
   var source=c.GetComponent<AudioSource>();source.playOnAwake=false;source.volume=.65f;
   var low=c.GetComponent<AudioLowPassFilter>();if(low==null)low=c.gameObject.AddComponent<AudioLowPassFilter>();low.cutoffFrequency=3400;
   var high=c.GetComponent<AudioHighPassFilter>();if(high==null)high=c.gameObject.AddComponent<AudioHighPassFilter>();high.cutoffFrequency=180;
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  EditorSceneManager.OpenScene("Assets/Scene/MultiScene1.unity");AssetDatabase.SaveAssets();
  return "Bound 18 temporary Korean synthesized dialogue clips to all four solo scenes.";
 }
}
