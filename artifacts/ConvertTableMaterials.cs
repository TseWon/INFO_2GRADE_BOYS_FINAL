using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

public static class ConvertTableMaterials
{
    const string Model="Assets/Blender/oak_steel_table.blend";
    const string Folder="Assets/Blender/OakSteelTable/Materials";
    const string Status="artifacts/table_unity_status.txt";
    static string Key(string s) { return new string(s.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray()); }
    public static string Start()
    {
        File.WriteAllText(Status,"Running");
        EditorApplication.delayCall += () => { try { File.WriteAllText(Status,Run()); } catch(Exception e) { File.WriteAllText(Status,e.ToString()); } };
        return "Table material conversion scheduled";
    }
    static void Texture(string path,bool normal,bool srgb)
    {
        var i=(TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        i.sRGBTexture=srgb;i.mipmapEnabled=true;i.anisoLevel=8;i.maxTextureSize=2048;
        i.filterMode=FilterMode.Trilinear;i.wrapMode=TextureWrapMode.Repeat;
        i.SaveAndReimport();
    }
    public static string Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var entries=JArray.Parse(File.ReadAllText("artifacts/table_material_manifest.json"));
        var materialMap=new Dictionary<string,Material>();
        foreach(JObject entry in entries)
        {
            string name=(string)entry["name"],path=Folder+"/"+Key(name)+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) {material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.name=name;material.SetFloat("_Mode",0);material.renderQueue=-1;
            var maps=entry["maps"] as JObject;
            if(maps!=null)
            {
                string albedo=(string)maps["albedo"],normal=(string)maps["normal"],packed=(string)maps["metallicSmoothness"];
                Texture(albedo,false,true);Texture(normal,true,false);Texture(packed,false,false);
                material.SetColor("_Color",Color.white);
                material.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                material.SetFloat("_BumpScale",.5f);material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
                material.SetFloat("_GlossMapScale",1);material.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                var c=(JArray)entry["color"];
                material.SetColor("_Color",new Color((float)c[0],(float)c[1],(float)c[2],1));
                material.SetFloat("_Metallic",(float)entry["metallic"]);
                material.SetFloat("_Glossiness",1-(float)entry["roughness"]);
            }
            EditorUtility.SetDirty(material);materialMap[Key(name)]=material;
        }
        AssetDatabase.SaveAssets();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        if(model==null)throw new Exception("Table not imported");
        var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
        foreach(var source in model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct())
        {
            Material replacement;
            if(!materialMap.TryGetValue(Key(source.name),out replacement))throw new Exception("Material not matched: "+source.name);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),replacement);
        }
        importer.SaveAndReimport();
        return Verify();
    }
    public static string Verify()
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        var materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
        foreach(var m in materials)
            if(!AssetDatabase.GetAssetPath(m).StartsWith(Folder)||m.shader.name!="Standard")throw new Exception("Material verification failed: "+m.name);
        foreach(var mesh in model.GetComponentsInChildren<MeshFilter>(true))
            if(mesh.sharedMesh.uv.Length!=mesh.sharedMesh.vertexCount)throw new Exception("UV missing: "+mesh.name);
        Selection.activeObject=model;EditorGUIUtility.PingObject(model);
        return "Success: Renderers="+renderers.Length+"; Materials="+materials.Length+"; UVs and external material links verified";
    }
}
