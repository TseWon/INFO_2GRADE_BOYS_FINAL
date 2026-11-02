using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SeatFittedSoldier {
 static Bounds Geometry(Transform visual){bool first=true;var b=new Bounds();foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>()){
  var mesh=r.sharedMesh;var vs=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;var matrices=new Matrix4x4[bind.Length];for(int i=0;i<bind.Length;i++)matrices[i]=r.bones[i].localToWorldMatrix*bind[i];
  for(int i=0;i<vs.Length;i++){var w=weights[i];var v=vs[i];var world=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;if(first){b=new Bounds(world,Vector3.zero);first=false;}else b.Encapsulate(world);}
 }return b;}
 public static string Run(){
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();var visual=p.visual;
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/FittedSoldier/FittedSoldier.fbx");visual.localPosition=Vector3.zero;visual.localRotation=source.transform.localRotation;visual.localScale=source.transform.localScale;
  var before=Geometry(visual);
  visual.localScale*=1.88f/before.size.y;var scaled=Geometry(visual);visual.position+=Vector3.up*(p.transform.position.y-scaled.min.y);
  var b=Geometry(visual);var cc=p.GetComponent<CharacterController>();cc.skinWidth=.025f;
  if(Mathf.Abs(b.size.y-1.88f)>.005f)throw new Exception("Skinned height verification failed: "+b.size.y);
  // The visual origin is the boot sole; the controller foot is at the same origin.
  cc.center=new Vector3(0,.94f,0);cc.height=1.88f;
  PrefabUtility.SaveAsPrefabAsset(visual.gameObject,"Assets/Characters/FittedSoldier/FittedSoldierVisual.prefab");EditorUtility.SetDirty(p);EditorUtility.SetDirty(cc);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
  string text="Actual skinned geometry before="+before+" after="+b+" footRelative="+(b.min.y-p.transform.position.y)+" skinWidth="+cc.skinWidth;
  File.WriteAllText("artifacts/fitted_soldier_grounding.txt",text);return text;
 }
}
