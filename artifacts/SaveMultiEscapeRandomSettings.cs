using System;
using FacilityEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SaveMultiEscapeRandomSettings
{
    private static void EnableRandom(MultiEscapeGame game)
    {
        if (game == null) throw new Exception("MultiEscapeGame missing.");
        var serialized = new SerializedObject(game);
        serialized.FindProperty("randomizeAnswers").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(game);
    }
    public static string Save()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode first.");
        var game = UnityEngine.Object.FindAnyObjectByType<MultiEscapeGame>();
        if (game == null || game.gameObject.scene.path != "Assets/Scene/MultiScene1.unity")
            throw new Exception("Expected MultiScene1.");
        Undo.RecordObject(game, "Randomize escape puzzles");
        EnableRandom(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        string path = "Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            EnableRandom(prefab.GetComponentInChildren<MultiEscapeGame>(true));
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        return "Scene and prefab saved with randomized answers enabled.";
    }
}
