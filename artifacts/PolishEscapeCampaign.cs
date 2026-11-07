using System;
using System.IO;
using System.Linq;
using FacilityEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PolishEscapeCampaign
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        var c=UnityEngine.Object.FindAnyObjectByType<MultiEscapeCampaign>();
        Directory.CreateDirectory("Assets/MultiplayerCampaign/Textures");
        foreach(string name in new[]{"AgedConcrete","BlackSteel","OxidizedSteel"})
        {
            const int size=256;var tex=new Texture2D(size,size,TextureFormat.RGB24,false);
            var random=new System.Random(name.Length*51);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float noise=Mathf.PerlinNoise(x*.043f,y*.043f)*.24f+Mathf.PerlinNoise(x*.013f,y*.019f)*.35f;
                float grain=(float)random.NextDouble()*.12f;
                float value=.40f+noise+grain;
                if(name=="BlackSteel")value-=Mathf.Repeat(y,32)<2?.22f:0;
                if(name=="AgedConcrete")value-=Mathf.Repeat(y,128)<2||Mathf.Repeat(x+(y>128?64:0),128)<1?.2f:0;
                if(name=="OxidizedSteel")value-=Mathf.PerlinNoise(x*.15f,y*.015f)>.6f?.22f:0;
                tex.SetPixel(x,y,new Color(value,value,value));
            }
            tex.Apply();string path="Assets/MultiplayerCampaign/Textures/"+name+".png";
            File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/MultiplayerCampaign/Materials/"+name+".mat");
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);mat.mainTextureScale=new Vector2(3,3);
            mat.SetFloat("_Glossiness",name=="BlackSteel"?.3f:.12f);
            EditorUtility.SetDirty(mat);
        }
        Directory.CreateDirectory("Assets/Resources");AssetDatabase.Refresh();
        const string insectPath="Assets/Resources/MultiEscapeInsects.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(insectPath)==null)
        {
            var m=new Material(Shader.Find("Particles/Standard Unlit"));m.SetFloat("_Mode",2);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);
            m.SetFloat("_ZWrite",0);m.EnableKeyword("_ALPHABLEND_ON");m.renderQueue=3000;AssetDatabase.CreateAsset(m,insectPath);
        }
        var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/MultiplayerCampaign/Materials/BlackSteel.mat");
        var yellow=AssetDatabase.LoadAssetAtPath<Material>("Assets/MultiplayerCampaign/Materials/WornSafetyPaint.mat");
        foreach(var target in c.targets.Where(t=>t.action<3))target.display.characterSize=.047f;
        foreach(var text in c.GetComponentsInChildren<TextMesh>(true))
            if(text.name=="EvidenceCaption")text.characterSize=.022f;
        var hall=c.transform.Find("03_Pursuit");
        if(hall.Find("RescueRope")==null)
        {
            var rope=new GameObject("RescueRope");rope.transform.SetParent(hall,false);
            for(int i=0;i<15;i++)
            {
                var rung=GameObject.CreatePrimitive(PrimitiveType.Cube);rung.name="RopeRung";rung.transform.SetParent(rope.transform,false);
                rung.transform.position=new Vector3(.4f,.15f+i*.18f,51.65f);rung.transform.localScale=new Vector3(.45f,.035f,.035f);
                rung.GetComponent<Renderer>().sharedMaterial=yellow;UnityEngine.Object.DestroyImmediate(rung.GetComponent<Collider>());
            }
            for(int i=-1;i<=1;i+=2)
            {
                var line=GameObject.CreatePrimitive(PrimitiveType.Cylinder);line.transform.SetParent(rope.transform,false);
                line.transform.position=new Vector3(.4f+i*.22f,1.4f,51.65f);line.transform.localScale=new Vector3(.035f,1.4f,.035f);
                line.GetComponent<Renderer>().sharedMaterial=yellow;UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());
            }
            rope.SetActive(false);
        }
        // Route the killer around the evidence table, then through the marked chicanes.
        Vector3[] points={new Vector3(0,0,9),new Vector3(1.8f,0,12),new Vector3(1.8f,0,18),new Vector3(0,0,22),new Vector3(3.7f,0,24),
            new Vector3(3.7f,0,29),new Vector3(-3.7f,0,31.5f),new Vector3(-3.7f,0,36),new Vector3(3.7f,0,37.5f),new Vector3(3.7f,0,41),
            new Vector3(0,0,45),new Vector3(0,0,50),new Vector3(0,2.9f,52),new Vector3(0,0,54),new Vector3(0,0,64)};
        var routes=hall.Find("PursuitWaypoints");
        c.pursuitRoute=points.Select((p,i)=>{var t=routes.Find("Route_"+i)??new GameObject("Route_"+i).transform;t.SetParent(routes,false);t.position=p;return t;}).ToArray();
        if(c.teamGate.Find("GateStripes")==null)
        {
            var stripes=new GameObject("GateStripes").transform;stripes.SetParent(c.teamGate,false);
            // Gate has non-unit scale: authored in world space to retain stripe proportions.
            for(int i=-4;i<=4;i++)
            {
                var bar=GameObject.CreatePrimitive(PrimitiveType.Cube);bar.transform.SetParent(stripes,true);
                bar.transform.position=new Vector3(i,2.5f,42.82f);bar.transform.rotation=Quaternion.Euler(0,0,-30);bar.transform.localScale=new Vector3(.035f,.2f,.1f);
                bar.GetComponent<Renderer>().sharedMaterial=yellow;UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
            }
        }
        EditorUtility.SetDirty(c);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
        PrefabUtility.SaveAsPrefabAsset(c.transform.root.gameObject,"Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab");
        AssetDatabase.SaveAssets();return "Grime textures, visible rescue rope, gate stripes, readable evidence and unobstructed enemy route saved.";
    }
}
