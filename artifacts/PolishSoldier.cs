using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class PolishSoldier {
 public static string Run(){
  if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
  var go=GameObject.Find("Snow Bounce - soft environment fill");if(go==null)go=new GameObject("Snow Bounce - soft environment fill",typeof(Light));var l=go.GetComponent<Light>();l.type=LightType.Directional;l.color=new Color(.80f,.87f,1);l.intensity=.52f;l.shadows=LightShadows.None;go.transform.rotation=Quaternion.Euler(32,145,0);
  var helmet=AssetDatabase.LoadAssetAtPath<Material>("Assets/Characters/TacticalSoldier/Soldier_Helmet.mat");helmet.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Blender/TacticalSoldier/Textures/Soldier_Cordura_Normal.png"));helmet.SetFloat("_BumpScale",.18f);helmet.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(helmet);
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();p.thirdPerson=false;AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
  var cam=p.viewCamera;var pos=cam.transform.localPosition;var rot=cam.transform.localRotation;float fov=cam.fieldOfView;var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1200,1200,24);rt.antiAliasing=4;var tex=new Texture2D(1200,1200,TextureFormat.RGB24,false);
  try{cam.fieldOfView=38;cam.transform.position=p.transform.position+p.transform.forward*3.1f+p.transform.right*.8f+Vector3.up*1.15f;cam.transform.LookAt(p.transform.position+Vector3.up*.95f);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();File.WriteAllBytes("artifacts/tactical_soldier_final.png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;RenderTexture.active=active;cam.transform.localPosition=pos;cam.transform.localRotation=rot;cam.fieldOfView=fov;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
  return "Saved material and soft snow bounce fill; final preview captured";
 }
}
