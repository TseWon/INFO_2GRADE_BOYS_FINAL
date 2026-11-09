using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

public static class IndependentGameBuilds
{
    public const string SoloProfile = "Assets/BuildProfiles/SoloWindows.asset";
    public const string MultiProfile = "Assets/BuildProfiles/MultiplayerWindows.asset";
    public static readonly string[] SoloScenes = Enumerable.Range(1, 4).Select(i => "Assets/Scene/SoloScene" + i + ".unity").ToArray();
    public static readonly string[] MultiScenes = { "Assets/Scene/MultiScene1.unity" };

    [MenuItem("Game Builds/Solo/Select Scene Group")]
    public static void SelectSolo() => Select(SoloProfile, SoloScenes);

    [MenuItem("Game Builds/Multiplayer/Select Scene Group")]
    public static void SelectMultiplayer() => Select(MultiProfile, MultiScenes);

    [MenuItem("Game Builds/Solo/Build Windows")]
    public static void BuildSolo() => Build(SoloProfile, SoloScenes, "Builds/HollowCreek/HollowCreek.exe");

    [MenuItem("Game Builds/Multiplayer/Build Windows")]
    public static void BuildMultiplayer() => Build(MultiProfile, MultiScenes, "Builds/MultiEscape/MultiEscape.exe");

    static void Select(string path, string[] scenes)
    {
        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
        if (profile == null) throw new BuildFailedException("Missing build profile: " + path);
        if (!profile.overrideGlobalScenes || !profile.GetScenesForBuild().Where(s => s.enabled).Select(s => s.path).SequenceEqual(scenes))
            throw new BuildFailedException("Scene group differs from the independent game configuration: " + path);
        BuildProfile.SetActiveBuildProfile(profile);
    }

    static void Build(string profile, string[] scenes, string output)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building.");
        Select(profile, scenes);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.DetailedBuildReport
        });
        if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException(report.summary.result.ToString());
        UnityEngine.Debug.Log("Independent game build succeeded: " + output);
    }
}
