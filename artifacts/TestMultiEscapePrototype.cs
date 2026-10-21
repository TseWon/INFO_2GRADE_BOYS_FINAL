using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FacilityEscape;
using UnityEngine;
using UnityEngine.UIElements;

public static class TestMultiEscapePrototype
{
    static MultiEscapeGame game;
    static MultiEscapeLocalSession session;
    static MultiEscapePlayer a, b;
    static int checks;
    const string ConsolePath = "MultiEscapePrototype/04_OperatorConsole/";
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Prototype check failed: " + message);
        checks++;
    }
    static void Setup()
    {
        Check(Application.isPlaying, "requires Play Mode");
        game = UnityEngine.Object.FindAnyObjectByType<MultiEscapeGame>();
        session = UnityEngine.Object.FindAnyObjectByType<MultiEscapeLocalSession>();
        a = UnityEngine.Object.FindObjectsByType<MultiEscapePlayer>().Single(p => p.Role == EscapeRole.Blindfolded);
        b = UnityEngine.Object.FindObjectsByType<MultiEscapePlayer>().Single(p => p.Role == EscapeRole.Observer);
        session.StartRound();
        Physics.SyncTransforms();
    }
    static GameObject Target(string relativePath) => GameObject.Find(ConsolePath + relativePath);
    static void Click(string relativePath)
    {
        var target = Target(relativePath);
        Check(target != null && target.activeInHierarchy, relativePath + " active");
        Vector3 desired = Quaternion.LookRotation(a.ViewCamera.transform.parent.InverseTransformDirection(
            target.transform.position - a.ViewCamera.transform.position)).eulerAngles;
        Vector3 current = a.ViewCamera.transform.localEulerAngles;
        float sensitivity = (float)typeof(MultiEscapePlayer).GetField("lookSensitivity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(a);
        a.Look(new Vector2(Mathf.DeltaAngle(current.y, desired.y), -Mathf.DeltaAngle(current.x, desired.x)) / sensitivity);
        Vector3 screen = a.ViewCamera.WorldToScreenPoint(target.transform.position);
        Check(screen.z > 0 && screen.x > 0 && screen.x < Screen.width && screen.y > 0 && screen.y < Screen.height,
            relativePath + " in camera view");
        var ui = session.GetComponent<UIDocument>().rootVisualElement;
        var picked = ui.panel.Pick(RuntimePanelUtils.ScreenToPanel(ui.panel, new Vector2(screen.x, Screen.height - screen.y)));
        for (var element = picked; element != null; element = element.parent)
            Check(!element.ClassListContains("blocks-world"), relativePath + " not covered by HUD");
        Ray ray = a.InteractionRay;
        Physics.SyncTransforms();
        bool hit = Physics.Raycast(ray, out RaycastHit info, 4, ~0, QueryTriggerInteraction.Ignore);
        Check(hit && info.collider.GetComponentInParent<MultiEscapeInteractable>() == target.GetComponent<MultiEscapeInteractable>(),
            relativePath + " ray reaches correct collider (hit " + (hit ? info.collider.name : "nothing") + ")");
        a.TryInteract(ray);
    }
    static void Colors()
    {
        for (int i = 0; i < game.Rules.ColorCount; i++) ClickColor(game.Rules.GetColorAnswer(i));
        Check(game.Rules.Stage == EscapeStage.SymbolLock, "colors unlock symbol panel");
    }
    static void ForwardClock(double seconds)
    {
        typeof(MultiEscapeGame).GetField("lastTick", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(game, Time.realtimeSinceStartupAsDouble - seconds);
        typeof(MultiEscapeGame).GetMethod("AdvanceClock", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(game, null);
    }
    static void ClickColor(LockColor color)
    {
        string[] buttons = { "Button_3_Red", "Button_1_Blue", "Button_2_Yellow", "Button_4_Green" };
        Click("ColorButtons_KeepLogicRoots/" + buttons[(int)color]);
    }
    public static string ShowDials()
    {
        checks = 0; Setup(); Colors();
        return "PASS: " + checks + " scene checks, dials visible.";
    }
    public static string ShowEscape()
    {
        checks = 0; Setup(); Colors();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < (int)game.Rules.GetSymbolAnswer(i); j++)
                Click("SymbolPanel_UnlocksAfterColors/Dial_" + i + "/TurnRight");
        Click("SymbolPanel_UnlocksAfterColors/ConfirmLever_KeepLogicRoot");
        a.ResetView();
        return game.Rules.Stage.ToString();
    }
    public static async Task<string> Run()
    {
        checks = 0; Setup();
        var root = GameObject.Find("MultiEscapePrototype");
        Check(!root.transform.Find("04_OperatorConsole/SymbolPanel_UnlocksAfterColors").gameObject.activeSelf, "dials hidden before lock 1");
        Check(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("WristRestraint") && t.gameObject.activeSelf) == 4, "both chairs start bound");
        Check((a.ViewCamera.cullingMask & (1 << 30)) == 0 && (b.ViewCamera.cullingMask & (1 << 30)) != 0, "observer-only clue layer");
        Check(a.ViewCamera.GetComponent<MultiGrayscaleCamera>().enabled && b.ViewCamera.GetComponent<MultiGrayscaleCamera>() == null, "A-only grayscale");
        Check(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(l => l.enabled) == 1, "one audio listener");
        Check(!a.GetComponent<CharacterController>().enabled && !b.GetComponent<CharacterController>().enabled, "both players cannot walk while restrained");
        session.SelectRole(EscapeRole.Observer);
        Check(b.ViewCamera.enabled && !a.ViewCamera.enabled && b.LocalInput && !a.LocalInput, "role switch controls cameras and input");
        Target("ColorButtons_KeepLogicRoots/Button_3_Red").GetComponent<MultiEscapeInteractable>().Interact(b);
        Check(game.Rules.ColorProgress == 0, "observer blocked at real interactable");
        session.SelectRole(EscapeRole.Blindfolded);
        int frame = Time.frameCount;
        double before = game.Rules.RemainingSeconds;
        ClickColor((LockColor)(((int)game.Rules.GetColorAnswer(0) + 1) % 4));
        Check(game.Rules.ColorLockoutSeconds > 2.8 && game.Rules.HeartsA == 3, "wrong color locks without damage");
        Check(Target("PanelLockedIndicator").activeSelf, "lockout indicator visible");
        ClickColor(game.Rules.GetColorAnswer(0));
        Check(game.Rules.ColorProgress == 0, "input ignored during lockout");
        await Task.Delay(3200);
        Check(Application.isPlaying && Time.frameCount > frame + 2 && game.Rules.RemainingSeconds < before - 3, "live frames and real timer advance");
        Check(game.Rules.ColorLockoutSeconds == 0, "real three-second lockout expires");
        Colors();
        Check(root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("WristRestraint")).All(t => !t.gameObject.activeSelf), "both wrist pairs released");
        var symbolRoot = root.transform.Find("04_OperatorConsole/SymbolPanel_UnlocksAfterColors");
        Check(symbolRoot.gameObject.activeSelf, "symbol controls revealed");
        foreach (var control in symbolRoot.GetComponentsInChildren<MultiEscapeInteractable>()) control.Interact(b);
        Check(game.Rules.GetDial(0) == DialSymbol.Circle && game.Rules.Stage == EscapeStage.SymbolLock, "observer cannot dial or confirm");
        Click("SymbolPanel_UnlocksAfterColors/Dial_0/TurnLeft");
        Check(game.Rules.GetDial(0) == DialSymbol.Cross, "physical counterclockwise control wraps");
        Click("SymbolPanel_UnlocksAfterColors/Dial_0/TurnRight");
        Click("SymbolPanel_UnlocksAfterColors/ConfirmLever_KeepLogicRoot");
        Check(game.Rules.GetDial(0) == DialSymbol.Circle && game.Rules.HeartsA == 3, "wrong lever resets without damage");
        var label = symbolRoot.Find("Dial_0/SelectedSymbol").GetComponent<TextMesh>();
        Check(label.text == "O", "dial label reset");

        session.StartRound();
        ForwardClock(20);
        Check(game.Rules.HeartsA == 2 && game.Rules.HeartsB == 2, "20-second simultaneous damage");
        var feedback = UnityEngine.Object.FindAnyObjectByType<MultiEscapeFeedback>();
        Check(feedback.ShockCount == 1, "shock feedback event");
        ForwardClock(20);
        Check(game.Rules.HeartsA == 1 && game.Rules.HeartsB == 1, "40-second simultaneous damage");
        ForwardClock(20);
        Check(game.Rules.Stage == EscapeStage.Failed && game.Rules.HeartsA == 0 && feedback.ShockCount == 3, "60-second team failure");
        var uiRoot = session.GetComponent<UIDocument>().rootVisualElement;
        Check(!uiRoot.Q("modal").ClassListContains("hidden"), "failure overlay visible");
        Target("ColorButtons_KeepLogicRoots/Button_3_Red").GetComponent<MultiEscapeInteractable>().Interact(a);
        Check(game.Rules.ColorProgress == 0, "controls disabled after failure");

        session.StartRound();
        Check(game.Rules.HeartsA == 3 && feedback.ShockCount == 0 && uiRoot.Q("modal").ClassListContains("hidden"), "restart restores HUD and health");
        Colors();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < (int)game.Rules.GetSymbolAnswer(i); j++) Click("SymbolPanel_UnlocksAfterColors/Dial_" + i + "/TurnRight");
        Check(game.Rules.Stage == EscapeStage.SymbolLock, "correct dials still require lever");
        Click("SymbolPanel_UnlocksAfterColors/ConfirmLever_KeepLogicRoot");
        Check(game.Rules.Stage == EscapeStage.Escaped, "physical lever wins");
        Check(root.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("Restraint")).All(t => !t.gameObject.activeSelf), "all restraints released");
        Check(a.GetComponent<CharacterController>().enabled && b.GetComponent<CharacterController>().enabled, "walking unlocked for both players");
        await Task.Delay(1500);
        Check(root.transform.Find("06_Exit/ExitDoor_ReplaceVisual").localPosition.y > 4.8f, "exit door opens");
        Check(uiRoot.Q<Label>("statusLabel").text.StartsWith("ESCAPED"), "success HUD");
        Check(uiRoot.Q<Label>("timerLabel").ClassListContains("hidden"), "timer hidden after escape");
        WalkPath(a, new[] { new Vector3(-2.85f, 0, 4.3f), new Vector3(0, 0, 4.3f), new Vector3(0, 0, 7.5f) });
        WalkPath(b, new[] { new Vector3(2.85f, 0, 4.3f), new Vector3(.6f, 0, 4.3f), new Vector3(.6f, 0, 7.5f) });
        session.StartRound();
        Check(!a.GetComponent<CharacterController>().enabled && !b.GetComponent<CharacterController>().enabled, "restart after success restrains both players");
        Check(!uiRoot.Q<Label>("timerLabel").ClassListContains("hidden"), "timer restored on restart");
        Check(Vector3.Distance(a.transform.position, new Vector3(-1.45f, 0, -1.65f)) < .01f, "restart restores seated position");
        Check(root.transform.Find("06_Exit/ExitDoor_ReplaceVisual").localPosition.y < 1.6f, "restart closes exit");
        Check(root.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("Restraint")).All(t => t.gameObject.activeSelf), "restart restores all restraints");
        return "PASS: " + checks + " scene/raycast/HUD/lifecycle/escape-route checks. Restarted to lock 1.";
    }

    public static string CheckRandomRoundsAndLook()
    {
        checks = 0; Setup();
        string previousColors = null, previousSymbols = null;
        var board = UnityEngine.Object.FindAnyObjectByType<MultiEscapeClueBoard>().GetComponent<TextMesh>();
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int round = 0; round < 32; round++)
        {
            session.StartRound();
            var colors = Enumerable.Range(0, 4).Select(i => game.Rules.GetColorAnswer(i)).ToArray();
            var symbols = Enumerable.Range(0, 3).Select(i => game.Rules.GetSymbolAnswer(i)).ToArray();
            string colorText = string.Join(" > ", colors.Select(c => c.ToString().ToUpperInvariant()));
            string symbolText = string.Join(" > ", symbols.Select(MultiEscapeClueBoard.SymbolText));
            Check(colors.Distinct().Count() == 4, "all four colors exactly once");
            Check(colorText != previousColors && symbolText != previousSymbols, "both puzzles change on restart");
            Check(symbols.Any(s => s != DialSymbol.Circle), "initial dial state is not a solution");
            Check(board.text.Contains(colorText) && board.text.Contains(symbolText), "clue board matches new answers");
            previousColors = colorText; previousSymbols = symbolText; seen.Add(colorText + symbolText);
        }
        Check(seen.Count > 10, "varied generated rounds");
        a.ResetView();
        Quaternion initial = a.ViewCamera.transform.localRotation;
        a.Look(new Vector2(50, 10));
        Check(Quaternion.Angle(initial, a.ViewCamera.transform.localRotation) > 70, "mouse delta rotates beyond old horizontal limit");
        Check(Mathf.Abs(Mathf.DeltaAngle(0, a.ViewCamera.transform.localEulerAngles.z)) < .01f, "no camera roll");
        a.Look(new Vector2(0, 10000));
        Check(Mathf.Abs(Mathf.DeltaAngle(0, a.ViewCamera.transform.localEulerAngles.x)) <= 80.01f, "vertical look clamped");
        a.ResetView();
        session.SetMouseCaptured(false);
        Check(!session.MouseCaptured && UnityEngine.Cursor.visible, "cursor released for UI");
        session.SetMouseCaptured(true);
        Check(session.MouseCaptured && !UnityEngine.Cursor.visible, "cursor captured for mouse look");
        var ui = session.GetComponent<UIDocument>().rootVisualElement;
        Check(!ui.Q("crosshair").ClassListContains("hidden"), "aim marker shown");
        string answerBefore = board.text;
        session.SwitchRole(); session.SwitchRole();
        Check(board.text == answerBefore, "switching roles does not reroll puzzle");
        return "PASS: " + checks + " random-round, clue, look, cursor and crosshair checks.";
    }

    static void WalkPath(MultiEscapePlayer player, Vector3[] waypoints)
    {
        var motor = player.GetComponent<CharacterController>();
        foreach (var waypoint in waypoints)
        {
            for (int i = 0; i < 180; i++)
            {
                Vector3 delta = Vector3.ProjectOnPlane(waypoint - player.transform.position, Vector3.up);
                if (delta.magnitude < .08f) break;
                motor.Move(Vector3.ClampMagnitude(delta, .05f) + Vector3.down * .01f);
            }
            Check(Vector3.ProjectOnPlane(player.transform.position - waypoint, Vector3.up).magnitude < .1f,
                player.name + " can reach " + waypoint + " through scene colliders");
        }
    }
}
