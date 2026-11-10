using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class InspectSurfacePlacement {
 public static object Run(){
  var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
  var info=loaded.Select(s=>new {s.name,s.path,s.isDirty,roots=s.GetRootGameObjects().Select(g=>new{g.name,pos=g.transform.position.ToString(),components=g.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name).ToArray()}).ToArray()}).ToArray();
  var src=SceneManager.GetSceneByPath("Assets/Scene/SoloScene1.unity"); bool opened=!src.isLoaded;
  if(opened)src=EditorSceneManager.OpenScene("Assets/Scene/SoloScene1.unity",OpenSceneMode.Additive);
  var weather=src.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.Contains("Snow")||t.GetComponent<Light>()||t.GetComponent<ParticleSystem>()).Select(t=>new{t.name,root=t.root.name,pos=t.position.ToString(),components=t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name).ToArray()}).ToArray();
  if(opened)EditorSceneManager.CloseScene(src,true);
  return new{loaded=info,weather, pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name:"Built-in"};
 }
}
