using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public static class InspectSoloStation
{
    public static string Placement()
    {
        var lines=new List<string>();
        var ground=GameObject.Find("Snow-covered ground 400 x 400 m");
        lines.Add("GROUND components="+string.Join(",",ground.GetComponents<Component>().Select(c=>c.GetType().Name)));
        foreach(float x in new[]{-16f,0f,16f})foreach(float z in new[]{-58f,-42f,-26f}) {
            var hits=new List<string>();
            foreach(var c in ground.GetComponentsInChildren<Collider>())if(c.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var h,60))hits.Add(h.point.y.ToString("F4"));
            lines.Add("GROUND "+x+","+z+"="+string.Join(",",hits));
        }
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/SurfaceResearchStation/Models/SurfaceResearchStation.fbx");
        var model=UnityEngine.Object.Instantiate(source);
        foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Command_Access__Shell"))lines.Add("MODEL PART "+r.name+" center="+r.bounds.center+" size="+r.bounds.size+" rot="+r.transform.eulerAngles);
        UnityEngine.Object.DestroyImmediate(model);
        var text=string.Join("\n",lines);File.WriteAllText("artifacts/surface_station/placement_inspection.txt",text);return text;
    }
    public static string All()
    {
        var lines = new List<string>();
        lines.Add("STATE playing=" + EditorApplication.isPlaying + " compiling=" + EditorApplication.isCompiling);
        for (int i=0;i<SceneManager.sceneCount;i++) {
            var s=SceneManager.GetSceneAt(i);
            lines.Add("SCENE " + s.path + " dirty=" + s.isDirty);
            foreach(var go in s.GetRootGameObjects()) Describe(go,lines);
        }
        foreach(var path in new[]{"Assets/Characters/DesertOperator/Models/DesertOperator.fbx", "Assets/Environment/SurfaceResearchStation/Models/SurfaceResearchStation.fbx"}) {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!model){lines.Add("MODEL MISSING " + path);continue;}
            var instance=UnityEngine.Object.Instantiate(model);
            instance.hideFlags=HideFlags.HideAndDontSave;
            lines.Add("MODEL " + path + " " + BoundsInfo(instance));
            UnityEngine.Object.DestroyImmediate(instance);
        }
        var text=string.Join("\n",lines);
        Directory.CreateDirectory("artifacts/surface_station");
        File.WriteAllText("artifacts/surface_station/solo_inspection.txt",text);
        return text;
    }
    static void Describe(GameObject root,List<string> lines) {
        lines.Add("ROOT " + root.name + " pos=" + root.transform.position.ToString("F3") + " scale=" + root.transform.lossyScale.ToString("F3") + " rot=" + root.transform.eulerAngles.ToString("F1") + " " + BoundsInfo(root));
        foreach(var c in root.GetComponentsInChildren<CharacterController>(true))
            lines.Add("CHARACTER " + c.name + " height=" + c.height + " radius=" + c.radius + " center=" + c.center + " step=" + c.stepOffset + " scale=" + c.transform.lossyScale + " pos=" + c.transform.position);
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
            if(t.name.IndexOf("operator",StringComparison.OrdinalIgnoreCase)>=0 || t.name.IndexOf("soldier",StringComparison.OrdinalIgnoreCase)>=0 || t.name.IndexOf("research",StringComparison.OrdinalIgnoreCase)>=0)
                lines.Add("MATCH " + t.name + " pos=" + t.position + " scale=" + t.lossyScale + " " + BoundsInfo(t.gameObject));
    }
    public static string BoundsInfo(GameObject go) {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)&&!(r is TrailRenderer)).ToArray();
        if(rs.Length==0)return "no renderers";
        var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
        return "bounds center="+b.center.ToString("F3")+" size="+b.size.ToString("F3")+" min="+b.min.ToString("F3")+" renderers="+rs.Length;
    }
}
