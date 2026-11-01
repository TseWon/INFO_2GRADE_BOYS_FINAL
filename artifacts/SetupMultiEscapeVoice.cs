using System;
using System.Linq;
using FacilityEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupMultiEscapeVoice
{
    static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
    }

    static void Configure(GameObject root)
    {
        var player = root.GetComponentsInChildren<MultiEscapePlayer>(true).Single(p => p.Role == EscapeRole.Observer);
        var head = player.transform.Find("Avatar_ReplaceVisuals/Head_LookPivot");
        if (head == null) throw new Exception("B head not found.");
        if (head.Find("EarDefenders") != null) return;
        var cups = AssetDatabase.LoadAssetAtPath<Material>("Assets/MultiplayerPrototype/Materials/SafetyYellow.mat");
        var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/MultiplayerPrototype/Materials/DarkMetal.mat");
        var ear = new GameObject("EarDefenders").transform;
        ear.SetParent(head, false);
        foreach (int side in new[] { -1, 1 })
        {
            Part(ear, "EarSeal_" + side, new Vector3(side * .17f, 0, 0), new Vector3(.04f, .16f, .13f), dark);
            Part(ear, "EarCup_" + side, new Vector3(side * .205f, 0, 0), new Vector3(.06f, .18f, .15f), cups);
            Part(ear, "HeadbandSide_" + side, new Vector3(side * .19f, .13f, 0), new Vector3(.03f, .18f, .055f), dark);
        }
        Part(ear, "HeadbandTop", new Vector3(0, .225f, 0), new Vector3(.41f, .035f, .055f), dark);
    }

    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode first.");
        var root = GameObject.Find("MultiEscapePrototype");
        if (root == null || root.scene.path != "Assets/Scene/MultiScene1.unity") throw new Exception("Open MultiScene1 first.");
        Undo.RegisterFullObjectHierarchyUndo(root, "Add B ear defenders");
        Configure(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        const string path = "Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        return "B ear defenders saved in MultiScene1 and FacilityEscapePrototype.prefab.";
    }

    public static string BuildTest() => Build("Builds/Voice-Test/FacilityEscape.exe", BuildOptions.Development);
    public static string BuildRelease() => Build("Builds/FacilityEscape-Voice/FacilityEscape.exe", BuildOptions.None);
    static string Build(string path, BuildOptions options)
    {
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scene/MultiScene1.unity" }, locationPathName = path,
            target = BuildTarget.StandaloneWindows64, options = options
        });
        System.IO.File.WriteAllText("artifacts/Voice-Build-" + (options == BuildOptions.None ? "release" : "test") + ".txt",
            result.summary.result + " | errors=" + result.summary.totalErrors + " | warnings=" + result.summary.totalWarnings + " | " + path);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("Build failed: " + result.summary.result);
        return path + " | errors=" + result.summary.totalErrors + " | warnings=" + result.summary.totalWarnings;
    }
}
