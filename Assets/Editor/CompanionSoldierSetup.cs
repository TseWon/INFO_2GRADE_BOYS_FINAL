using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CompanionSoldierSetup
{
    private const string ScenePath = "Assets/Scene/SoloScene2.unity";
    private const string RootName = "Companion Soldiers";
    private const string MaterialFolder = "Assets/Characters/Companions/Materials";
    private const string WrongGreyCloneFolder = "Assets/Characters/Companions/TacticalSoldierGrey";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scene/SoloScene1.unity",
        "Assets/Scene/SoloScene2.unity",
        "Assets/Scene/SoloScene3.unity",
        "Assets/Scene/SoloScene4.unity",
        "Assets/Scene/SoloScene5.unity",
    };

    private static readonly SoldierAsset[] Soldiers =
    {
        new(
            "Companion - Tactical Soldier",
            "Assets/Characters/Companions/TacticalSoldier/TacticalSoldier_Rigged.fbx",
            "Assets/Characters/Companions/TacticalSoldier/TacticalSoldier_Source_base_color.png",
            new Vector3(-1.6f, 0f, -2f)),
        new(
            "Companion - Call of Duty Soldier",
            "Assets/Characters/Companions/CallOfDutySoldier/CallOfDutySoldier_Rigged.fbx",
            "Assets/Characters/Companions/CallOfDutySoldier/CallOfDutySoldier_Source_base_color.png",
            new Vector3(1.6f, 0f, -2f)),
        new(
            "Companion - Modern Soldier",
            "Assets/Characters/Companions/ModernSoldier/ModernSoldier_Rigged.fbx",
            "Assets/Characters/Companions/ModernSoldier/ModernSoldier_Source_base_color.png",
            new Vector3(-1.6f, 0f, 2f)),
        new(
            "Companion - Grey Mask Soldier",
            "Assets/Characters/Companions/ModernSoldierGrey/ModernSoldierGrey_Rigged.fbx",
            "Assets/Characters/Companions/ModernSoldier/ModernSoldier_Source_base_color.png",
            new Vector3(1.6f, 0f, 2f)),
    };

    [MenuItem("Tools/Companion Soldiers/Configure Rig And Place In SoloScene2")]
    public static void ConfigureRigAndPlace()
    {
        ConfigureAllSoloScenes();
    }

    [MenuItem("Tools/Companion Soldiers/Configure All Solo Scenes")]
    public static void ConfigureAllSoloScenes()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        ConfigureHumanoidImports();
        var soldierMaterials = CreateSoldierMaterials();

        foreach (var path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (path == ScenePath)
            {
                ConfigureSoloScene2(scene, soldierMaterials);
            }
            else
            {
                ReplaceCampaignSoldiers(scene, soldierMaterials);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (AssetDatabase.IsValidFolder(WrongGreyCloneFolder))
        {
            AssetDatabase.DeleteAsset(WrongGreyCloneFolder);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("Replaced campaign soldiers in SoloScene1-5 and configured the corrected Modern grey-mask clone.");
    }

    private static void ConfigureSoloScene2(
        Scene scene,
        IReadOnlyDictionary<string, Material> soldierMaterials)
    {
        var existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(existingRoot);
        }

        var root = new GameObject(RootName);
        root.transform.SetPositionAndRotation(
            new Vector3(0f, 0.1f, 21.5f),
            Quaternion.Euler(0f, 180f, 0f));

        foreach (var soldier in Soldiers)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(soldier.ModelPath);
            if (model == null)
            {
                throw new InvalidOperationException($"Could not load soldier model: {soldier.ModelPath}");
            }

            var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not instantiate soldier model: {soldier.ModelPath}");
            }

            instance.name = soldier.Name;
            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = soldier.LocalPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ApplyMaterials(
                instance,
                soldierMaterials[soldier.Name],
                soldierMaterials["Mask Grey"],
                soldier.Name.Contains("Grey Mask"));
        }

        Selection.activeGameObject = root;
    }

    private static void ReplaceCampaignSoldiers(
        Scene scene,
        IReadOnlyDictionary<string, Material> soldierMaterials)
    {
        var targets = new List<SoldierLocomotion>();
        foreach (var locomotion in UnityEngine.Object.FindObjectsByType<SoldierLocomotion>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (locomotion.gameObject.scene == scene && IsCampaignSoldier(locomotion.transform))
            {
                targets.Add(locomotion);
            }
        }

        targets.Sort((a, b) => CampaignOrder(a.name).CompareTo(CampaignOrder(b.name)));
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var soldier = Soldiers[Mathf.Min(i, Soldiers.Length - 1)];
            RemoveOldVisuals(target.transform);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(soldier.ModelPath);
            if (model == null)
            {
                throw new InvalidOperationException($"Could not load soldier model: {soldier.ModelPath}");
            }

            var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not instantiate soldier model: {soldier.ModelPath}");
            }

            instance.name = $"Visual - {soldier.Name.Replace("Companion - ", string.Empty)}";
            instance.transform.SetParent(target.transform, false);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            ApplyMaterials(
                instance,
                soldierMaterials[soldier.Name],
                soldierMaterials["Mask Grey"],
                soldier.Name.Contains("Grey Mask"));
        }
    }

    private static bool IsCampaignSoldier(Transform target)
    {
        for (var current = target.parent; current != null; current = current.parent)
        {
            if (current.name == "Hollow Creek Campaign")
            {
                return true;
            }
        }

        return false;
    }

    private static int CampaignOrder(string objectName)
    {
        if (objectName.StartsWith("Miller", StringComparison.OrdinalIgnoreCase)) return 0;
        if (objectName.StartsWith("Reed", StringComparison.OrdinalIgnoreCase)) return 1;
        if (objectName.StartsWith("Cole", StringComparison.OrdinalIgnoreCase)) return 2;
        if (objectName.StartsWith("Hayes", StringComparison.OrdinalIgnoreCase)) return 3;
        return 100;
    }

    private static void RemoveOldVisuals(Transform target)
    {
        for (var i = target.childCount - 1; i >= 0; i--)
        {
            var child = target.GetChild(i);
            if (child.name == "FittedSoldierRig" ||
                child.name.StartsWith("Visual - ", StringComparison.Ordinal) ||
                child.GetComponentInChildren<Renderer>(true) != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static System.Collections.Generic.Dictionary<string, Material> CreateSoldierMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Characters/Companions", "Materials");
        }

        var result = new System.Collections.Generic.Dictionary<string, Material>();
        foreach (var soldier in Soldiers)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(soldier.BaseColorPath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Could not load base color texture: {soldier.BaseColorPath}");
            }

            var materialName = soldier.Name.Replace("Companion - ", string.Empty).Replace(' ', '_');
            var materialPath = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Smoothness", 0.22f);
            EditorUtility.SetDirty(material);
            result[soldier.Name] = material;
        }

        var greyPath = $"{MaterialFolder}/Mask_Grey.mat";
        var grey = AssetDatabase.LoadAssetAtPath<Material>(greyPath);
        if (grey == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            grey = new Material(shader) { name = "Mask_Grey" };
            AssetDatabase.CreateAsset(grey, greyPath);
        }

        var maskColor = new Color(0.32f, 0.36f, 0.39f, 1f);
        grey.SetColor("_BaseColor", maskColor);
        grey.SetColor("_Color", maskColor);
        grey.SetFloat("_Smoothness", 0.08f);
        EditorUtility.SetDirty(grey);
        result["Mask Grey"] = grey;
        AssetDatabase.SaveAssets();
        return result;
    }

    private static void ApplyMaterials(
        GameObject instance,
        Material baseMaterial,
        Material greyMaskMaterial,
        bool greyMask)
    {
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (greyMask && renderer.sharedMaterials.Length > 1)
            {
                renderer.sharedMaterials = new[] { baseMaterial, greyMaskMaterial };
            }
            else
            {
                renderer.sharedMaterial = baseMaterial;
            }
        }
    }

    private static void ConfigureHumanoidImports()
    {
        foreach (var soldier in Soldiers)
        {
            ConfigureModelImporter(soldier.ModelPath, importAnimation: true);

            var directory = Path.GetDirectoryName(soldier.ModelPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
            {
                continue;
            }

            var stem = Path.GetFileNameWithoutExtension(soldier.ModelPath)
                .Replace("_Rigged", string.Empty);
            ConfigureModelImporter($"{directory}/{stem}_Walk.fbx", importAnimation: true);
            ConfigureModelImporter($"{directory}/{stem}_Run.fbx", importAnimation: true);
        }
    }

    private static void ConfigureModelImporter(string assetPath, bool importAnimation)
    {
        if (!File.Exists(Path.GetFullPath(assetPath)))
        {
            return;
        }

        if (AssetImporter.GetAtPath(assetPath) is not ModelImporter importer)
        {
            throw new InvalidOperationException($"ModelImporter was not found for {assetPath}");
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = importAnimation;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

        if (AssetDatabase.WriteImportSettingsIfDirty(assetPath))
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
    }

    private readonly struct SoldierAsset
    {
        public readonly string Name;
        public readonly string ModelPath;
        public readonly string BaseColorPath;
        public readonly Vector3 LocalPosition;

        public SoldierAsset(string name, string modelPath, string baseColorPath, Vector3 localPosition)
        {
            Name = name;
            ModelPath = modelPath;
            BaseColorPath = baseColorPath;
            LocalPosition = localPosition;
        }
    }
}
