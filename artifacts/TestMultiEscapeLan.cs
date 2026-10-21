using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FacilityEscape;
using UnityEngine;
using UnityEngine.UIElements;

public static class TestMultiEscapeLan
{
    static string reportPath = "artifacts/lan-client.json";
    static MultiEscapeLanSession lan;
    static MultiEscapeGame game;
    static MultiEscapeLocalSession ui;
    static MultiEscapePlayer a, b;
    static int checks, command;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static void Setup()
    {
        if (!Application.isPlaying) throw new Exception("LAN integration checks require Play Mode.");
        lan = UnityEngine.Object.FindAnyObjectByType<MultiEscapeLanSession>();
        game = UnityEngine.Object.FindAnyObjectByType<MultiEscapeGame>();
        ui = UnityEngine.Object.FindAnyObjectByType<MultiEscapeLocalSession>();
        var players = UnityEngine.Object.FindObjectsByType<MultiEscapePlayer>();
        a = players.Single(p => p.Role == EscapeRole.Blindfolded); b = players.Single(p => p.Role == EscapeRole.Observer);
    }
    static MultiEscapeLanProbe.Report Report()
    {
        try { return JsonUtility.FromJson<MultiEscapeLanProbe.Report>(File.ReadAllText(reportPath)); }
        catch (IOException) { return null; }
        catch (ArgumentException) { return null; }
    }
    static async Task<MultiEscapeLanProbe.Report> Wait(Func<MultiEscapeLanProbe.Report, bool> predicate, string name)
    {
        var deadline = DateTime.UtcNow.AddSeconds(9);
        while (DateTime.UtcNow < deadline)
        {
            var report = Report();
            if (report != null && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - report.timestamp < 2000 && predicate(report))
            { checks++; return report; }
            await Task.Delay(100);
        }
        throw new Exception("Timed out: " + name + ". Client: " + JsonUtility.ToJson(Report()));
    }
    static int Send(string action, Vector2 look = default, Vector3 position = default)
    {
        command++;
        File.WriteAllText(reportPath + ".command.json", JsonUtility.ToJson(new MultiEscapeLanProbe.Command {
            id = command, action = action, look = look, position = position, address = "127.0.0.1"
        }));
        return command;
    }
    static void Forward(double seconds)
    {
        var tick = typeof(MultiEscapeGame).GetField("lastTick", BindingFlags.Instance | BindingFlags.NonPublic);
        tick.SetValue(game, (double)tick.GetValue(game) - seconds);
        typeof(MultiEscapeGame).GetMethod("AdvanceClock", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
    }
    static void Click(string suffix)
    {
        var target = GameObject.Find("MultiEscapePrototype/04_OperatorConsole/" + suffix);
        Check(target != null, "Control exists: " + suffix);
        Vector3 desired = Quaternion.LookRotation(target.transform.position - a.ViewCamera.transform.position).eulerAngles;
        Vector3 current = a.ViewCamera.transform.eulerAngles;
        a.Look(new Vector2(Mathf.DeltaAngle(current.y, desired.y), -Mathf.DeltaAngle(current.x, desired.x)) / 2);
        Physics.SyncTransforms();
        Check(Physics.Raycast(a.InteractionRay, out var hit, 4) && hit.collider.GetComponentInParent<MultiEscapeInteractable>() == target.GetComponent<MultiEscapeInteractable>(), "Collider reachable: " + suffix);
        a.TryInteract(a.InteractionRay);
    }
    static void Colors()
    {
        string[] buttons = { "Button_3_Red", "Button_1_Blue", "Button_2_Yellow", "Button_4_Green" };
        for (int i = 0; i < 4; i++) Click("ColorButtons_KeepLogicRoots/" + buttons[(int)game.Rules.GetColorAnswer(i)]);
    }
    public static string Host()
    {
        Setup(); lan.Host(); ui.SetMouseCaptured(false);
        return lan.Status;
    }
    public static async Task<string> Run()
    {
        Setup(); checks = 0; command = Report()?.command ?? 0;
        await Wait(r => r.connected && lan.Connected, "Two peers connected");
        Check(lan.IsHost && a.LocalInput && !b.LocalInput, "Host owns only A");
        var r = await Wait(x => !x.host && !x.aInput && x.bInput && x.aAvatar && !x.bAvatar, "Client owns only B and sees A");
        Check(b.transform.Find("Avatar_ReplaceVisuals").gameObject.activeSelf && !a.transform.Find("Avatar_ReplaceVisuals").gameObject.activeSelf, "Host sees only partner avatar");
        ui.SwitchRole(); Check(ui.ActiveRole == EscapeRole.Blindfolded, "Cannot switch network roles");
        int id = Send("start"); await Wait(x => x.command == id, "Client start attempted");
        Check(game.Rules.Stage == EscapeStage.Ready, "Client cannot start host round");
        ui.StartRound(); ui.SetMouseCaptured(false);
        r = await Wait(x => x.state.round == game.RoundNumber && x.state.stage == EscapeStage.ColorLock, "Shared start");
        Check(r.state.colors.SequenceEqual(game.CaptureSnapshot().colors) && r.state.symbols.SequenceEqual(game.CaptureSnapshot().symbols), "Same randomized puzzle answers");
        Check(r.clue.Contains(game.Rules.GetColorAnswer(0).ToString().ToUpperInvariant()), "Client clue updated");
        Check(Math.Abs(r.state.duration - r.state.elapsed - game.Rules.RemainingSeconds) < .5, "Timer synchronized");
        game.PressColor(a, (LockColor)(((int)game.Rules.GetColorAnswer(0) + 1) % 4));
        await Wait(x => x.state.wrong == 1 && x.state.lockedUntil > x.state.elapsed && x.state.heartsA == 3, "Wrong input lockout and no damage");
        Check(game.PressColor(b, game.Rules.GetColorAnswer(0)) == EscapeInputResult.Ignored, "B cannot input");
        await Task.Delay(3200);
        Colors();
        await Wait(x => x.state.stage == EscapeStage.SymbolLock, "Wrist release and symbol stage");
        Click("SymbolPanel_UnlocksAfterColors/ConfirmLever_KeepLogicRoot");
        await Wait(x => x.state.wrong == 2 && x.state.dials.All(d => d == 0), "Wrong lever resets all dials");
        for (int dial = 0; dial < 3; dial++)
            for (int step = 0; step < (int)game.Rules.GetSymbolAnswer(dial); step++)
                Click("SymbolPanel_UnlocksAfterColors/Dial_" + dial + "/TurnRight");
        await Wait(x => x.state.dials.SequenceEqual(game.CaptureSnapshot().dials), "Dial angles synchronized");
        Forward(20);
        await Wait(x => x.state.heartsA == 2 && x.state.heartsB == 2 && x.shocks == 1, "First simultaneous shock");
        Forward(20);
        await Wait(x => x.state.heartsA == 1 && x.state.heartsB == 1 && x.shocks == 2, "Second simultaneous shock");
        Click("SymbolPanel_UnlocksAfterColors/ConfirmLever_KeepLogicRoot");
        await Wait(x => x.state.stage == EscapeStage.Escaped && !x.timerVisible, "Client escaped and timer hidden");
        Check(!ui.GetComponent<UIDocument>().rootVisualElement.Q("timerLabel").visible || ui.GetComponent<UIDocument>().rootVisualElement.Q("timerLabel").ClassListContains("hidden"), "Host timer hidden");
        double stopped = game.Rules.RemainingSeconds;
        Forward(30); Check(game.Rules.RemainingSeconds == stopped, "Escape stops timer and damage");
        id = Send("look", new Vector2(24, 9));
        r = await Wait(x => x.command == id && Quaternion.Angle(b.ViewCamera.transform.rotation, x.bRotation) < 2, "Client head rotation reaches host");
        a.Look(new Vector2(30, 5));
        await Wait(x => Quaternion.Angle(a.ViewCamera.transform.rotation, x.aRotation) < 2, "Host head rotation reaches client");
        id = Send("move", position: new Vector3(2.85f, .05f, 2));
        await Wait(x => x.command == id && Vector3.Distance(b.transform.position, x.bPosition) < .1, "Client movement reaches host");
        var motor = a.GetComponent<CharacterController>(); motor.enabled = false;
        a.transform.position = new Vector3(-2.85f, .05f, 2); motor.enabled = true;
        await Wait(x => Vector3.Distance(a.transform.position, x.aPosition) < .1, "Host movement reaches client");
        var old = game.CaptureSnapshot(); ui.StartRound(); ui.SetMouseCaptured(false);
        r = await Wait(x => x.state.round == game.RoundNumber && x.state.stage == EscapeStage.ColorLock && x.timerVisible && x.state.heartsA == 3, "Restart synchronizes state and timer");
        Check(!old.colors.SequenceEqual(r.state.colors) && !old.symbols.SequenceEqual(r.state.symbols), "Both answers change on restart");
        Check(Math.Abs(r.bPosition.z + 1.65f) < .1 && Math.Abs(a.transform.position.z + 1.65f) < .1, "Both return to chairs");
        Forward(61);
        await Wait(x => x.state.stage == EscapeStage.Failed && x.state.heartsA == 0 && x.state.heartsB == 0 && x.shocks == 3, "Deadline and HP failure synchronized");
        id = Send("leave");
        await Wait(x => x.command == id && !x.connected && !lan.IsBusy && game.Rules.Stage == EscapeStage.Ready, "Disconnect resets host to lobby");
        lan.Host(); Send("join");
        await Wait(x => x.connected && lan.Connected, "Reconnect after disconnect");
        ui.StartRound(); ui.SetMouseCaptured(false);
        await Wait(x => x.state.stage == EscapeStage.ColorLock, "New round after reconnect");
        lan.Leave();
        await Wait(x => !x.connected && x.state.stage == EscapeStage.Ready, "Host departure resets client to lobby");
        return checks + " two-process LAN checks passed.";
    }
}
