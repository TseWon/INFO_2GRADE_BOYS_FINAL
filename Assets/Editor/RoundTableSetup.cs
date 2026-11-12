using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RoundTableSetup
{
    private const string ScenePath = "Assets/Scene/SoloScene2.unity";
    private const string TableFbx = "Assets/Environment/WalnutRoundTable/WalnutRoundTable.fbx";
    private const string SoldierFbx = "Assets/Characters/Companions/ModernSoldierGrey/ModernSoldierGrey_Rigged.fbx";
    private const string BaseColorPath = "Assets/Environment/WalnutRoundTable/WalnutRoundTable_BaseColor.png";
    private const string NormalPath = "Assets/Environment/WalnutRoundTable/WalnutRoundTable_Normal.png";
    private const string MaterialPath = "Assets/Environment/WalnutRoundTable/WalnutRoundTable_Walnut.mat";

    [MenuItem("Tools/Environment/Apply 5x1x5 Walnut Tables And Mask Fix")]
    public static void Apply()
    {
        AssetDatabase.ImportAsset(TableFbx, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(SoldierFbx, ImportAssetOptions.ForceUpdate);
        ConfigureNormalTexture();
        var material = CreateWalnutMaterial();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = GameObject.Find("Round Lab Tables");
        if (root == null)
        {
            throw new InvalidOperationException("Round Lab Tables root was not found in SoloScene2.");
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length != 2)
        {
            Debug.LogWarning($"Expected two round tables, found {renderers.Length} renderers.");
        }

        foreach (var renderer in renderers)
        {
            renderer.sharedMaterial = material;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        var sizes = Array.ConvertAll(renderers, renderer => renderer.bounds.size.ToString("F2"));
        Debug.Log($"Applied walnut material and saved SoloScene2. Table bounds: {string.Join(", ", sizes)}");
    }

    private static void ConfigureNormalTexture()
    {
        if (AssetImporter.GetAtPath(NormalPath) is not TextureImporter importer ||
            importer.textureType == TextureImporterType.NormalMap)
        {
            return;
        }

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    private static Material CreateWalnutMaterial()
    {
        var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        if (baseColor == null)
        {
            throw new InvalidOperationException($"Table base-color texture is missing: {BaseColorPath}");
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "WalnutRoundTable_Walnut" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.SetTexture("_BaseMap", baseColor);
        material.SetTexture("_MainTex", baseColor);
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        if (normal != null)
        {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
        }

        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.28f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
