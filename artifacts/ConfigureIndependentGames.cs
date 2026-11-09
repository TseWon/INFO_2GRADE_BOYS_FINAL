using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Profile;

public static class ConfigureIndependentGames
{
    public static string Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        Directory.CreateDirectory("Assets/BuildProfiles");
        AssetDatabase.Refresh();
        Create(IndependentGameBuilds.SoloProfile, IndependentGameBuilds.SoloScenes);
        Create(IndependentGameBuilds.MultiProfile, IndependentGameBuilds.MultiScenes);
        EditorBuildSettings.scenes = IndependentGameBuilds.SoloScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        AssetDatabase.SaveAssets();
        IndependentGameBuilds.SelectSolo();
        return Verify();
    }

    static void Create(string path, string[] scenes)
    {
        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
        if (profile == null) {
            // Unity 6000.4 exposes profile creation internally; use its platform factory
            // so target and platform settings are initialized exactly as in the Editor.
            var factory = typeof(BuildProfile).GetMethod("CreateInstance", BindingFlags.NonPublic | BindingFlags.Static, null,
                new[] { typeof(BuildTarget), typeof(StandaloneBuildSubtarget) }, null);
            if (factory == null) throw new MissingMethodException("BuildProfile.CreateInstance platform factory");
            profile = (BuildProfile)factory.Invoke(null, new object[] { BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Player });
            profile.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.overrideGlobalScenes = true;
        profile.scenes = scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        EditorUtility.SetDirty(profile);
    }

    public static string Verify()
    {
        var lines = new System.Collections.Generic.List<string>();
        foreach (bool solo in new[] { true, false }) {
            var path = solo ? IndependentGameBuilds.SoloProfile : IndependentGameBuilds.MultiProfile;
            var expected = solo ? IndependentGameBuilds.SoloScenes : IndependentGameBuilds.MultiScenes;
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            if (profile == null || !profile.overrideGlobalScenes) throw new Exception("Missing scene override: " + path);
            var actual = profile.GetScenesForBuild().Where(s => s.enabled).Select(s => s.path).ToArray();
            if (!actual.SequenceEqual(expected)) throw new Exception("Incorrect scene order: " + path);
            var forbidden = solo ? "Assets/Scripts(Multi)/" : "Assets/Scripts(Solo)/";
            var cross = AssetDatabase.GetDependencies(actual, true).Where(p => p.StartsWith(forbidden, StringComparison.Ordinal) && p.EndsWith(".cs")).ToArray();
            if (cross.Length != 0) throw new Exception("Cross-mode scene dependencies: " + string.Join(", ", cross));
            lines.Add("PASS " + profile.name + ": " + string.Join(" -> ", actual) + "; no opposite-mode script dependencies");
        }
        if (EditorBuildSettings.scenes.Any(s => s.enabled && s.path.Contains("MultiScene"))) throw new Exception("Global fallback still combines modes");
        lines.Add("PASS Global fallback contains only solo scenes; multiplayer profile overrides the global list");
        Directory.CreateDirectory("artifacts/hollow-creek");
        var result = string.Join("\n", lines);
        File.WriteAllText("artifacts/hollow-creek/independent-scenes.txt", result);
        return result;
    }
}
