using System.IO;
using UnityEditor;

public static class CleanupVoiceQA
{
    public static string Run()
    {
        AssetDatabase.DeleteAsset("Assets/artifacts/voice-hud.png");
        AssetDatabase.DeleteAsset("Assets/Resources/PerformanceTestRunInfo.json");
        AssetDatabase.DeleteAsset("Assets/Resources/PerformanceTestRunSettings.json");
        if (Directory.Exists("Assets/Resources") && Directory.GetFileSystemEntries("Assets/Resources").Length == 0)
            AssetDatabase.DeleteAsset("Assets/Resources");
        AssetDatabase.SaveAssets();
        return "Removed generated QA assets; screenshot retained under artifacts/.";
    }
}
