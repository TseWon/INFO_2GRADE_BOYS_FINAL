using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using FacilityEscape;

public static class CheckElectricChair
{
    static MultiEscapeGame game;
    static MultiEscapePlayer a;
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static GameObject[] Parts(string key)
    {
        var view = UnityEngine.Object.FindAnyObjectByType<MultiEscapeView>();
        return (GameObject[])typeof(MultiEscapeView).GetField(key, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
    }
    static void Click(string path)
    {
        var target = GameObject.Find("MultiEscapePrototype/04_OperatorConsole/" + path);
        Check(target != null, "Control exists: " + path);
        Vector3 desired = Quaternion.LookRotation(target.transform.position - a.ViewCamera.transform.position).eulerAngles;
        Vector3 current = a.ViewCamera.transform.eulerAngles;
        a.Look(new Vector2(Mathf.DeltaAngle(current.y, desired.y), -Mathf.DeltaAngle(current.x, desired.x)) / 2);
        Physics.SyncTransforms();
        Check(Physics.Raycast(a.InteractionRay, out var hit, 4) && hit.collider.GetComponentInParent<MultiEscapeInteractable>() == target.GetComponent<MultiEscapeInteractable>(), "Chair blocks control: " + path);
        a.TryInteract(a.InteractionRay);
    }
    static void SaveCamera(Camera camera, string path)
    {
        var rt = RenderTexture.GetTemporary(1280, 800, 24);
        var prior = camera.targetTexture; var active = RenderTexture.active;
        var tex = new Texture2D(1280, 800, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(tex); }
    }
    public static string Run()
    {
        Check(Application.isPlaying, "Play Mode required");
        checks = 0;
        game = UnityEngine.Object.FindAnyObjectByType<MultiEscapeGame>();
        // Isolate geometry/view verification; production scene retains the LAN component.
        game.Lan = null;
        a = UnityEngine.Object.FindObjectsByType<MultiEscapePlayer>().Single(p => p.Role == EscapeRole.Blindfolded);
        game.StartRound();
        var wrists = Parts("wristRestraints"); var other = Parts("remainingRestraints");
        Check(wrists.Count(x => x.name.StartsWith("Wrist restraint")) == 4, "Four model wrist straps connected");
        Check(other.Count(x => x.name.StartsWith("Long torso strap")) == 4, "Four torso straps connected");
        Check(wrists.All(x => x.activeSelf) && other.All(x => x.activeSelf), "Restraints visible at start");
        var before = a.ViewCamera.transform.rotation;
        SaveCamera(a.ViewCamera, "artifacts/electric-chair-player-a.png");
        string[] buttons = { "Button_3_Red", "Button_1_Blue", "Button_2_Yellow", "Button_4_Green" };
        for (int i = 0; i < 4; i++) Click("ColorButtons_KeepLogicRoots/" + buttons[(int)game.Rules.GetColorAnswer(i)]);
        Check(game.Rules.Stage == EscapeStage.SymbolLock && wrists.All(x => !x.activeSelf), "Color unlock hides all wrist straps and buckles");
        Check(other.All(x => x.activeSelf), "Torso and ankle restraints remain");
        for (int d = 0; d < 3; d++) for (int i = 0; i < (int)game.Rules.GetSymbolAnswer(d); i++)
            Click("SymbolPanel_UnlocksAfterColors/Dial_" + d + "/TurnRight");
        Click("SymbolPanel_UnlocksAfterColors/ConfirmLever_KeepLogicRoot");
        Check(game.Rules.Stage == EscapeStage.Escaped && other.All(x => !x.activeSelf), "Escape releases model restraints and helmet");
        game.StartRound();
        Check(wrists.All(x => x.activeSelf) && other.All(x => x.activeSelf), "Restart restores all model parts");
        a.ViewCamera.transform.rotation = before;
        var cameraRoot = new GameObject("Temporary_Chair_Preview");
        var camera = cameraRoot.AddComponent<Camera>();
        try
        {
            camera.transform.position = new Vector3(3.6f, 2.8f, .7f);
            camera.transform.LookAt(new Vector3(.3f, 1.1f, -1.65f));
            camera.fieldOfView = 60; camera.nearClipPlane = .05f;
            camera.cullingMask &= ~(1 << 30);
            SaveCamera(camera, "artifacts/electric-chair-applied.png");
        }
        finally { UnityEngine.Object.DestroyImmediate(cameraRoot); }
        return checks + " chair geometry, interactions and restraint lifecycle checks passed.";
    }
}
