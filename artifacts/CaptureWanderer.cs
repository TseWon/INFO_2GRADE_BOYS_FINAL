using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
public static class CaptureWanderer {
 public static string Run(){var player=Object.FindAnyObjectByType<SnowFirstPerson>();var cam=player.viewCamera;var pos=cam.transform.localPosition;var rot=cam.transform.localRotation;var rs=player.visual.GetComponentsInChildren<Renderer>();var modes=rs.Select(r=>r.shadowCastingMode).ToArray();var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(900,900,24);rt.antiAliasing=4;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);player.enabled=false;
  try{foreach(var r in rs)r.shadowCastingMode=ShadowCastingMode.On;cam.transform.position=player.transform.position+player.transform.forward*2.1f+player.transform.right*.7f+Vector3.up*.8f;cam.transform.LookAt(player.transform.position+Vector3.up*.5f);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes("artifacts/wanderer_unity.png",tex.EncodeToPNG());var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return "Character meshes="+rs.Length+"; bounds="+b.size;}
  finally{cam.targetTexture=old;RenderTexture.active=active;cam.transform.localPosition=pos;cam.transform.localRotation=rot;for(int i=0;i<rs.Length;i++)rs[i].shadowCastingMode=modes[i];rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);player.enabled=true;}
 }
}
