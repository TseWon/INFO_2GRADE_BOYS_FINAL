using System;
using System.Linq;
using FacilityEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupMultiEscapeLan
{
    const string Root = "Assets/MultiplayerPrototype/";
    static void Ref(UnityEngine.Object target, string key, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(key).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Transform Group(string name, Transform parent, Vector3 position = default)
    {
        var item = new GameObject(name).transform;
        item.SetParent(parent, false); item.localPosition = position;
        return item;
    }
    static void Mesh(string name, Transform parent, PrimitiveType type, Vector3 p, Vector3 scale, Material material)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = name; item.transform.SetParent(parent, false);
        item.transform.localPosition = p; item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
    }
    static void Configure(GameObject root)
    {
        var game = root.GetComponentInChildren<MultiEscapeGame>(true);
        var players = root.GetComponentsInChildren<MultiEscapePlayer>(true);
        var lan = game.GetComponent<MultiEscapeLanSession>() ?? game.gameObject.AddComponent<MultiEscapeLanSession>();
        Ref(lan, "game", game);
        Ref(lan, "playerA", players.Single(p => p.Role == EscapeRole.Blindfolded));
        Ref(lan, "playerB", players.Single(p => p.Role == EscapeRole.Observer));
        var gray = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/BrushedSteel.mat");
        var dark = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/DarkMetal.mat");
        foreach (var player in players)
        {
            if (player.GetComponent<MultiEscapeAvatar>() != null) continue;
            var avatar = player.gameObject.AddComponent<MultiEscapeAvatar>();
            var visual = Group("Avatar_ReplaceVisuals", player.transform);
            var cloth = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/" +
                (player.Role == EscapeRole.Blindfolded ? "SafetyYellow" : "ButtonBlue") + ".mat");
            Mesh("Torso", visual, PrimitiveType.Capsule, new Vector3(0, 1.21f, 0), new Vector3(.48f, .35f, .3f), cloth);
            var head = Group("Head_LookPivot", visual, new Vector3(0, 1.8f, 0));
            Mesh("Head", head, PrimitiveType.Sphere, Vector3.zero, new Vector3(.34f, .4f, .33f), gray);
            Mesh(player.Role == EscapeRole.Blindfolded ? "Blindfold" : "Visor", head, PrimitiveType.Cube,
                new Vector3(0, .035f, .15f), new Vector3(.31f, .09f, .05f), dark);
            for (int s = -1; s <= 1; s += 2)
            {
                Mesh("UpperArm_" + s, visual, PrimitiveType.Capsule, new Vector3(s * .3f, 1.19f, .04f), new Vector3(.13f, .22f, .13f), cloth);
                Mesh("Forearm_" + s, visual, PrimitiveType.Cube, new Vector3(s * .34f, 1.05f, .3f), new Vector3(.13f, .13f, .47f), gray);
            }
            var seated = Group("SeatedLegs", visual);
            var standing = Group("StandingLegs", visual);
            for (int s = -1; s <= 1; s += 2)
            {
                Mesh("Thigh_" + s, seated, PrimitiveType.Cube, new Vector3(s * .15f, .72f, .24f), new Vector3(.2f, .22f, .55f), cloth);
                Mesh("Shin_" + s, seated, PrimitiveType.Cube, new Vector3(s * .15f, .36f, .48f), new Vector3(.17f, .6f, .19f), dark);
                Mesh("Leg_" + s, standing, PrimitiveType.Capsule, new Vector3(s * .15f, .48f, 0), new Vector3(.19f, .43f, .19f), cloth);
            }
            Ref(avatar, "player", player); Ref(avatar, "visualRoot", visual); Ref(avatar, "head", head);
            Ref(avatar, "seatedLegs", seated); Ref(avatar, "standingLegs", standing);
        }
    }
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Stop play first.");
        var root = GameObject.Find("MultiEscapePrototype");
        if (root == null || root.scene.path != "Assets/Scene/MultiScene1.unity") throw new Exception("Expected MultiScene1.");
        Undo.RegisterFullObjectHierarchyUndo(root, "Configure two-player LAN");
        Configure(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        string path = Root + "Prefabs/FacilityEscapePrototype.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        return "LAN session and two replaceable player avatars saved in MultiScene1 and prefab.";
    }
}
