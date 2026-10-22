using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FacilityEscape;

public static class ApplyElectricChair
{
    const string ModelPath = "Assets/Blender/electric_chair.blend";
    static bool Wrist(string name) => name.StartsWith("Wrist restraint") || name.StartsWith("Buckle ") || name.StartsWith("Strap punched hole");
    static bool Helmet(string name) => name.StartsWith("Head electrode") || name.StartsWith("Helmet ") || name == "Terminal contact" || name.StartsWith("Arched leather helmet");
    static bool Remaining(string name) => name.StartsWith("Long torso strap") || name.StartsWith("Hanging ankle restraint") || name.StartsWith("Ankle ") || Helmet(name);
    static void SetArray(SerializedProperty property, List<GameObject> objects)
    {
        property.arraySize = objects.Count;
        for (int i = 0; i < objects.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }
    static List<GameObject> Existing(SerializedProperty property)
    {
        var result = new List<GameObject>();
        for (int i = 0; i < property.arraySize; i++)
            if (property.GetArrayElementAtIndex(i).objectReferenceValue is GameObject obj) result.Add(obj);
        return result;
    }
    static void Box(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var box = go.AddComponent<BoxCollider>(); box.center = center; box.size = size;
    }
    static void FitHelmetMount(Transform visual)
    {
        var bracket = visual.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "Suspension bracket");
        bracket.transform.localPosition = PrefabUtility.GetCorrespondingObjectFromSource(bracket.transform).localPosition;
        bracket.transform.position += Vector3.up * .24f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(bracket.transform);
        if (visual.parent.Find("HelmetMount_Extension") != null) return;
        var extension = GameObject.CreatePrimitive(PrimitiveType.Cube);
        extension.name = "HelmetMount_Extension";
        extension.transform.SetParent(visual.parent, false);
        extension.transform.localPosition = visual.localPosition + new Vector3(0, 1.95f, -.38f);
        extension.transform.localScale = new Vector3(.045f, .34f, .045f);
        extension.GetComponent<Renderer>().sharedMaterial = bracket.sharedMaterial;
        UnityEngine.Object.DestroyImmediate(extension.GetComponent<Collider>());
    }
    static string Configure(GameObject root)
    {
        var view = root.GetComponentInChildren<MultiEscapeView>(true);
        var viewData = new SerializedObject(view);
        var wrists = Existing(viewData.FindProperty("wristRestraints"));
        var remaining = Existing(viewData.FindProperty("remainingRestraints"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new Exception("Imported electric_chair model missing.");
        int parts = 0;
        foreach (string suffix in new[] { "A", "B" })
        {
            var chair = root.transform.Find("02_Chairs/Chair_" + suffix);
            if (chair.Find("ElectricChair_Asset") != null) throw new Exception("Chair already applied; edit existing instance.");
            chair.Find("Visual_ReplaceWithChairAsset").gameObject.SetActive(false);
            // Keep old logical references available, but hide the placeholder cubes and colliders.
            foreach (var t in chair.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("WristRestraint") || t.name.StartsWith("AnkleRestraint") || t.name == "WaistRestraint")
                {
                    foreach (var renderer in t.GetComponents<Renderer>()) renderer.enabled = false;
                    foreach (var collider in t.GetComponents<Collider>()) collider.enabled = false;
                }
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, chair);
            visual.name = "ElectricChair_Asset";
            visual.transform.localPosition = new Vector3(suffix == "A" ? -1.45f : 1.45f, 0, -1.65f);
            visual.transform.localRotation = model.transform.localRotation;
            visual.transform.localScale = model.transform.localScale;
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (Wrist(renderer.name)) wrists.Add(renderer.gameObject);
                if (Remaining(renderer.name)) remaining.Add(renderer.gameObject);
                // Fit the dome above the existing 1.8 m eye point and placeholder head.
                if (Helmet(renderer.name))
                {
                    renderer.transform.position += Vector3.up * .24f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
                }
            }
            parts += renderers.Length;
            FitHelmetMount(visual.transform);
            var collision = new GameObject("ElectricChair_Collision").transform;
            collision.SetParent(chair, false); collision.localPosition = visual.transform.localPosition;
            Box(collision, "Seat", new Vector3(0, .67f, 0), new Vector3(.93f, .12f, .72f));
            Box(collision, "Back", new Vector3(0, 1.26f, -.32f), new Vector3(.93f, 1.05f, .13f));
            foreach (int side in new[] { -1, 1 })
            {
                Box(collision, "Armrest_" + side, new Vector3(side * .405f, 1.015f, .035f), new Vector3(.16f, .075f, .84f));
                Box(collision, "FrontLeg_" + side, new Vector3(side * .405f, .49f, .34f), new Vector3(.12f, .94f, .12f));
                Box(collision, "RearLeg_" + side, new Vector3(side * .405f, .37f, -.32f), new Vector3(.12f, .74f, .12f));
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
        }
        SetArray(viewData.FindProperty("wristRestraints"), wrists);
        SetArray(viewData.FindProperty("remainingRestraints"), remaining);
        viewData.ApplyModifiedPropertiesWithoutUndo();
        return $"{parts} model renderers, {wrists.Count} wrist parts, {remaining.Count} remaining restraint parts.";
    }
    public static string Apply()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode first.");
        var root = GameObject.Find("MultiEscapePrototype");
        if (root == null || root.scene.path != "Assets/Scene/MultiScene1.unity") throw new Exception("Expected MultiScene1.");
        Undo.RegisterFullObjectHierarchyUndo(root, "Apply Blender electric chairs");
        string result = Configure(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        const string prefabPath = "Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try { Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        return "Scene + prefab saved. " + result;
    }
    static void FixOrientation(GameObject root)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        foreach (string suffix in new[] { "A", "B" })
        {
            var visual = root.transform.Find("02_Chairs/Chair_" + suffix + "/ElectricChair_Asset");
            visual.localRotation = model.transform.localRotation;
            visual.localScale = model.transform.localScale;
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                if (Helmet(r.name))
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(r.transform);
                    r.transform.localPosition = source.localPosition;
                    r.transform.position += Vector3.up * .24f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r.transform);
                }
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
            FitHelmetMount(visual);
        }
    }
    public static string RepairOrientation()
    {
        if (Application.isPlaying) throw new Exception("Stop play first.");
        var root = GameObject.Find("MultiEscapePrototype");
        FixOrientation(root);
        EditorSceneManager.MarkSceneDirty(root.scene); EditorSceneManager.SaveScene(root.scene);
        const string path = "Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { FixOrientation(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        return "Preserved Blender import axis conversion in scene and prefab.";
    }
    public static string Inspect()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var s = new StringBuilder("Scene: " + EditorSceneManager.GetActiveScene().path + "\n");
        if (model == null) return s + "Model not imported.";
        var instance = UnityEngine.Object.Instantiate(model);
        try
        {
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
                s.AppendLine(r.name + " | center=" + r.bounds.center.ToString("F3") + " size=" + r.bounds.size.ToString("F3") + " | " + string.Join(",", r.sharedMaterials.Select(m => m == null ? "NULL" : m.name + ":" + m.shader.name + ":" + m.color)));
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        var chairs = GameObject.Find("MultiEscapePrototype/02_Chairs");
        if (chairs != null) foreach (var t in chairs.GetComponentsInChildren<Transform>(true))
            s.AppendLine("CHAIR " + t.name + " pos=" + t.position + " scale=" + t.localScale + " renderer=" + (t.GetComponent<Renderer>() != null) + " collider=" + (t.GetComponent<Collider>() != null));
        return s.ToString();
    }
}
