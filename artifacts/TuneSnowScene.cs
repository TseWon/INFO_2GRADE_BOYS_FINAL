using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class TuneSnowScene {
 public static string Run(){
  RenderSettings.sun.intensity=.62f;RenderSettings.sun.color=new Color(1,.97f,.92f);
  RenderSettings.ambientSkyColor=new Color(.36f,.42f,.51f);RenderSettings.ambientEquatorColor=new Color(.27f,.32f,.39f);RenderSettings.ambientGroundColor=new Color(.21f,.25f,.30f);
  var sky=RenderSettings.skybox;sky.SetColor("_SkyTint",new Color(.5f,.5f,.5f));sky.SetFloat("_Exposure",.8f);sky.SetFloat("_AtmosphereThickness",1);EditorUtility.SetDirty(sky);
  var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/SnowPlain/SnowGround.mat");m.color=new Color(.86f,.88f,.9f);EditorUtility.SetDirty(m);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
  var ps=Object.FindFirstObjectByType<ParticleSystem>();ps.Simulate(24,true,true);return "Snow lighting tuned and scene saved";
 }
}
