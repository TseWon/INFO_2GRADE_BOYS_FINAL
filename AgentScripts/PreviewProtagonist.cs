using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PreviewProtagonist
{
    public static string Run()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/USSoldier/USSoldierPlayerVisual.prefab");
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();
            var cameraGo=new GameObject("Preview Camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene);
            var camera=cameraGo.AddComponent<Camera>();
            camera.transform.position=new Vector3(0,1.05f,3.2f);
            camera.transform.LookAt(new Vector3(0,.9f,0));
            camera.fieldOfView=35;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.74f,.79f,.84f,1);
            camera.allowHDR=false;
            var lightGo=new GameObject("Preview Light");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene);
            var light=lightGo.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=2;
            light.transform.rotation=Quaternion.Euler(35,150,0);
            var rt=new RenderTexture(960,960,24);
            camera.targetTexture=rt;
            var before=RenderTexture.active;
            camera.Render(); RenderTexture.active=rt;
            var image=new Texture2D(960,960,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,960,960),0,0); image.Apply();
            var path=Path.GetFullPath("meshy_output/Soldiers/Brazilian_Soldier_US_Flag/USSoldier_UnityPreview.png");
            File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=before;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
            return "PREVIEW "+path+" skin="+skin.name;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
