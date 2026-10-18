using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ConvertFacilityMaterials
{
    const string ModelPath = "Assets/Blender/abandoned_research_facility.blend";
    const string Folder = "Assets/Blender/ResearchFacility/Materials";
    [Serializable] public class Maps { public string albedo, normal, roughness, metallicSmoothness; }
    [Serializable] public class Entry { public string name, family; public float[] tint; public Maps maps; }
    [Serializable] public class Manifest { public Entry[] items; }

    static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
    static void ConfigureTexture(string path, bool srgb, bool normal)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("Missing texture importer: " + path);
        var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        bool changed = importer.textureType != type || importer.sRGBTexture != srgb || !importer.mipmapEnabled
            || importer.wrapMode != TextureWrapMode.Repeat || importer.anisoLevel != 8
            || importer.textureCompression != TextureImporterCompression.CompressedHQ;
        importer.textureType = type;
        importer.sRGBTexture = srgb;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 8;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        if (changed) importer.SaveAndReimport();
    }

    public static string Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var manifest = new Manifest { items = Newtonsoft.Json.JsonConvert.DeserializeObject<Entry[]>(File.ReadAllText("artifacts/facility_material_manifest.json")) };
        if (manifest.items == null || manifest.items.Length == 0) throw new Exception("Material manifest is empty");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var configured = new HashSet<string>();
        foreach (var e in manifest.items)
        {
            if (configured.Add(e.maps.albedo)) ConfigureTexture(e.maps.albedo, true, false);
            if (configured.Add(e.maps.normal)) ConfigureTexture(e.maps.normal, false, true);
            if (configured.Add(e.maps.metallicSmoothness)) ConfigureTexture(e.maps.metallicSmoothness, false, false);
        }
        var shader = Shader.Find("Standard");
        if (shader == null) throw new Exception("Built-in Standard shader not available");
        var materials = new Dictionary<string, Material>();
        foreach (var e in manifest.items)
        {
            string filename = Normalize(e.name) + ".mat";
            string path = Folder + "/" + filename;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader; m.name = e.name;
            m.SetColor("_Color", Color.white);
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(e.maps.albedo));
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(e.maps.normal));
            m.SetFloat("_BumpScale", .65f);
            m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(e.maps.metallicSmoothness));
            m.SetFloat("_GlossMapScale", 1f);
            m.SetFloat("_SmoothnessTextureChannel", 0);
            m.SetFloat("_Mode", 0); // Opaque glass appearance avoids transparent surface sorting.
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            m.SetInt("_ZWrite", 1); m.renderQueue = -1;
            m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_METALLICGLOSSMAP");
            m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            EditorUtility.SetDirty(m);
            materials[Normalize(e.name)] = m;
        }
        AssetDatabase.SaveAssets();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new Exception("Model import has not finished");
        var importerModel = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        var sourceMaterials = model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
            .Where(m => m != null).GroupBy(m => m.name).Select(g => g.First()).ToArray();
        var missing = new List<string>();
        int remaps = 0;
        foreach (var source in sourceMaterials)
        {
            Material replacement;
            if (!materials.TryGetValue(Normalize(source.name), out replacement)) { missing.Add(source.name); continue; }
            importerModel.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), replacement);
            remaps++;
        }
        if (missing.Count > 0) throw new Exception("Unmatched source materials: " + string.Join(", ", missing));
        importerModel.SaveAndReimport();
        AssetDatabase.SaveAssets();
        return Verify();
    }

    public static string Verify()
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model==null) throw new Exception("Model missing");
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        var mats=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
        var failures=new List<string>();
        foreach(var m in mats)
        {
            if(m.shader==null || m.shader.name!="Standard" || m.GetTexture("_MainTex")==null || m.GetTexture("_BumpMap")==null)
                failures.Add(m.name);
            if(!AssetDatabase.GetAssetPath(m).StartsWith(Folder)) failures.Add("embedded:"+m.name);
        }
        var meshes=model.GetComponentsInChildren<MeshFilter>(true);
        foreach(var f in meshes) if(f.sharedMesh==null || f.sharedMesh.uv.Length!=f.sharedMesh.vertexCount) failures.Add("UV:"+f.name);
        if(failures.Count>0) throw new Exception(string.Join(";",failures));
        Selection.activeObject=model;EditorGUIUtility.PingObject(model);
        string report="Renderers="+renderers.Length+"; Materials="+mats.Length+"; UVs=OK; AlbedoNormals=OK; ExternalMaterialRemaps=OK";
        File.WriteAllText("artifacts/facility_unity_material_verification.txt",report);
        return report;
    }

    public static string RenderPreview()
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if(model==null) throw new Exception("Model not loaded");
        var preview=new PreviewRenderUtility();
        try
        {
            var instance=UnityEngine.Object.Instantiate(model);
            preview.AddSingleGO(instance);
            var renderers=instance.GetComponentsInChildren<Renderer>(true);
            Bounds bounds=renderers[0].bounds;
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            var camera=preview.camera;
            camera.nearClipPlane=.5f;camera.farClipPlane=1500;
            camera.fieldOfView=40;
            camera.transform.position=bounds.center+new Vector3(1f,.7f,1.3f).normalized*340;
            camera.transform.LookAt(bounds.center);
            camera.backgroundColor=new Color(.10f,.115f,.13f,1);
            camera.clearFlags=CameraClearFlags.SolidColor;
            preview.lights[0].intensity=1.6f;preview.lights[0].transform.rotation=Quaternion.Euler(45,35,0);
            preview.lights[1].intensity=1.0f;preview.lights[1].transform.rotation=Quaternion.Euler(330,215,0);
            preview.ambientColor=new Color(.35f,.35f,.35f,1);
            preview.BeginStaticPreview(new Rect(0,0,1200,900));
            preview.Render();
            var texture=preview.EndStaticPreview();
            string path="artifacts/research_facility_unity_preview.png";
            File.WriteAllBytes(path,texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return path;
        }
        finally { preview.Cleanup(); }
    }

    public static string FinalReview()
    {
        return Verify()+"; Preview="+RenderPreview();
    }
}
