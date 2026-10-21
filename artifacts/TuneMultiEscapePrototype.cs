using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneMultiEscapePrototype
{
    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        var root = GameObject.Find("MultiEscapePrototype");
        foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
        {
            Undo.RecordObject(text, "Fit prototype text");
            switch (text.name)
            {
                case "LockOneTitle": text.characterSize = .022f; break;
                case "PositionNumber": text.characterSize = .026f; break;
                case "SelectedSymbol": text.characterSize = .029f; break;
                case "DialNumber": text.characterSize = .017f; break;
                case "Direction": text.characterSize = .023f; break;
                case "ConfirmLabel": text.characterSize = .018f; break;
                case "PanelLockedIndicator": text.characterSize = .025f; break;
                case "BoardHeading": text.characterSize = .043f; break;
                case "ObserverOnlyText": text.characterSize = .028f; break;
                case "ObserverOnlyButtonMap": text.characterSize = .023f; break;
                case "ExitSign": text.characterSize = .07f; break;
            }
            if (text.GetComponent<FacilityEscape.MultiEscapeWorldText>() == null)
                text.gameObject.AddComponent<FacilityEscape.MultiEscapeWorldText>();
        }
        var a = root.transform.Find("03_PlayerRigs/Player_A_Operator/ViewCamera");
        var b = root.transform.Find("03_PlayerRigs/Player_B_Observer/ViewCamera");
        a.rotation = Quaternion.LookRotation(new Vector3(-1.45f, 1.15f, .15f) - a.position);
        b.rotation = Quaternion.LookRotation(new Vector3(-.3f, 1.65f, 2.4f) - b.position);
        foreach (var part in root.GetComponentsInChildren<Transform>(true))
        {
            if (part.name.StartsWith("Armrest") && part.name != "ArmrestConsoleSupport")
            {
                var position = part.position; position.z = -1.3f; part.position = position;
                var scale = part.localScale; scale.z = .9f; part.localScale = scale;
            }
            if (part.name.StartsWith("WristRestraint"))
            {
                var position = part.position; position.z = -1.1f; part.position = position;
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab");
        return "Updated text sizes; scene and prefab saved.";
    }
}
