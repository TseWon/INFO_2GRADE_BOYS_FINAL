using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using HollowCreek;

public static class TestHollowCreek
{
    static readonly List<string> log = new List<string>();
    static HollowCreekCampaign C => UnityEngine.Object.FindAnyObjectByType<HollowCreekCampaign>();
    static void Check(bool value, string message) { if (!value) throw new Exception(message); log.Add("PASS " + message); }
    static async Task Until(Func<bool> predicate, int milliseconds = 10000)
    { float end = Time.realtimeSinceStartup + milliseconds / 1000f; while (!predicate()) { if (Time.realtimeSinceStartup > end) throw new Exception("Timed out waiting for gameplay"); await Task.Delay(40); } }
    static HollowCreekTarget Find(Interaction interaction) => UnityEngine.Object.FindObjectsByType<HollowCreekTarget>(FindObjectsInactive.Exclude).First(t => t.interaction == interaction && t.Available);
    static void Use(Interaction interaction) => Find(interaction).Use();
    static void Click(string text)
    {
        var root = C.hud.GetComponent<UIDocument>().rootVisualElement;
        var button = root.Query<Button>().ToList().First(b => b.text == text);
        typeof(Clickable).GetMethod("SimulateSingleClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(button.clickable, new object[] { null, 0 });
    }
    static void Answer(string value)
    {
        var root = C.hud.GetComponent<UIDocument>().rootVisualElement;
        root.Q<TextField>().value = value; Click("입력 확인");
    }
    static void Backup()
    {
        Directory.CreateDirectory("artifacts/hollow-creek");
        string path = "artifacts/hollow-creek/checkpoint-before-tests.txt";
        if (!File.Exists(path)) File.WriteAllText(path, PlayerPrefs.HasKey("HollowCreek.Checkpoint.v1") ? PlayerPrefs.GetString("HollowCreek.Checkpoint.v1") : "<none>");
    }
    static string Report(string name)
    { string text = string.Join("\n", log); File.WriteAllText("artifacts/hollow-creek/" + name + ".txt", text); return text; }
    public static string Rules()
    {
        log.Clear(); Directory.CreateDirectory("artifacts/hollow-creek");
        var r = new HollowCreekRules();
        Check(HollowCreekRules.SegmentCount(8)==7 && HollowCreekRules.SegmentCount(7)==4 && HollowCreekRules.SegmentCount(4)==4, "Displayed segment masks match the 8-7-4-4 puzzle sequence");
        Check(!r.UnlockEntrance("1953"), "Profile required before entrance code"); r.ReadProfile();
        Check(!r.UnlockEntrance("1952") && r.UnlockEntrance("1953"), "Entrance wrong/correct answer");
        r.stage = Stage.Lift; Check(r.StartLift() && !r.StartLift(), "Lift is one-shot");
        Check(r.ResolveRescue(true) && r.route == Route.Upper && !r.ResolveRescue(false), "QTE resolves exactly once");
        Check(!r.OpenPassage("NO") && r.OpenPassage(" open "), "Acrostic normalizes whitespace and case");
        r.stage = Stage.Search; Check(!r.SolveDigits("7") && r.SolveDigits("4"), "Seven-segment puzzle");
        Check(!r.TuneRadio("1224"), "Radio requires channel document"); r.ReadNote(3);
        Check(!r.TuneRadio("1212") && r.TuneRadio("1224"), "Radio channel code");
        r.stage = Stage.Lift; r.StartLift(); Check(r.ResolveRescue(false) && r.route == Route.Prison, "Missed QTE takes prison branch");
        Check(r.OpenPassage("PUSH"), "Weak wall opens prison passage");
        for (int seed = 0; seed < 200; seed++) {
            var p = new SlidingPuzzle(seed); int inversions = 0;
            for (int a = 0; a < 9; a++) for (int b = a + 1; b < 9; b++) if (p.Tiles[a] != 0 && p.Tiles[b] != 0 && p.Tiles[a] > p.Tiles[b]) inversions++;
            if (inversions % 2 != 0 || SlidingPuzzle.IsSolved(p.Tiles)) throw new Exception("Invalid board seed " + seed);
        }
        Check(true, "200 generated sliding boards are solvable and not already solved");
        var puzzle = new SlidingPuzzle(19); Check(!puzzle.Move(-1) && !puzzle.Move(9), "Invalid tile indices rejected");
        r.stage = Stage.Search; Check(!r.SolveSliding(new[] { 1, 2, 3, 4, 5, 6, 7, 0, 8 }), "Incomplete flag does not unlock");
        Check(r.SolveSliding(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 }), "Completed flag unlocks radio");
        r.stage = Stage.Chase; Check(!r.EnterVent() && !r.Escape(), "Cannot skip seal/vent stages");
        Check(r.Seal() && r.EnterVent() && r.Escape() && !r.Seal(), "Seal to vent to ending guards");
        return Report("rules-tests");
    }
    public static async Task<string> SurfaceAndUpper()
    {
        log.Clear(); Backup(); Check(Application.isPlaying && C.sceneNumber == 1, "Surface play mode ready");
        int frame = Time.frameCount; await Task.Delay(150); Check(Time.frameCount > frame, "Actual frames advancing");
        Click("수색 시작"); Click("계속"); Click("계속"); Click("계속");
        Check(!C.hud.IsOpen, "Tutorial dialogue buttons dismiss modal");
        Use(Interaction.Profile); Check(C.Rules.profileRead && C.hud.IsOpen, "Researcher profile retained in journal"); C.hud.Close();
        Use(Interaction.Entrance); Answer("0000"); Check(C.hud.IsOpen && C.Rules.stage == Stage.Arrival, "Wrong entrance code stays open");
        Answer("1953"); Check(!C.hud.IsOpen && C.Rules.stage == Stage.Entrance, "Keypad UI unlocks entrance");
        Use(Interaction.Entrance); await Until(() => C != null && C.sceneNumber == 2 && C.hud != null); await Task.Delay(200);
        Check(Physics.Raycast(C.player.transform.position + Vector3.up, Vector3.down, 2), "B1 lift has collidable floor");
        Use(Interaction.Lift); await Until(() => C.Rules.stage == Stage.Rescue && !C.Busy && C.RescueRemaining > 0);
        Check(C.ResolveRescue(true), "Rescue success inside visible QTE window");
        Check(C.Rules.route == Route.Upper && C.Rules.stage == Stage.Acrostic, "CASE 1 landing");
        Use(Interaction.Acrostic); Answer("OPEN"); Check(!C.passageBarrier.activeSelf, "Acrostic physically opens barrier");
        Use(Interaction.NextArea); await Until(() => C != null && C.sceneNumber == 3); await Task.Delay(200);
        Check(C.Rules.route == Route.Upper && C.Rules.stage == Stage.Search && C.upperProps.activeSelf && !C.prisonProps.activeSelf, "Upper branch persists across scene transition");
        Use(Interaction.Digits); Answer("4"); Check(C.Rules.stage == Stage.Radio, "Digital puzzle UI restores power");
        Use(Interaction.Note4); C.hud.Close(); Use(Interaction.Radio); Answer("1224");
        await Until(() => C != null && C.sceneNumber == 4, 18000); await Task.Delay(200);
        Check(C.hud.IsOpen && C.Busy, "Radio dialogue transitions to final scene briefing");
        return Report("upper-integration");
    }
    static async Task DismissFinal()
    { Click("계속"); Click("계속"); await Until(() => !C.Busy); }
    static void MoveForward(float distance, bool jumping)
    {
        var p = C.player; p.InputBlocked = false; float initial = p.transform.position.z;
        int steps = Mathf.CeilToInt(distance / (p.runSpeed / 60));
        for (int i = 0; i < steps; i++) {
            if (jumping) p.Jump();
            p.Move(Vector2.up, true, 1f / 60);
        }
        Check(p.transform.position.z - initial > distance - 2, "Physical corridor traversal: " + distance + "m, end=" + p.transform.position);
    }
    public static async Task<string> Finale()
    {
        log.Clear(); await DismissFinal();
        C.hud.Document("QA", "Pause pursuit during collision probe");
        MoveForward(64, true); C.hud.Close();
        Check(C.player.transform.position.z > 62, "Run and Space jump clear all chase obstacles");
        Use(Interaction.SealDoor); await Until(() => !C.Busy);
        Check(C.sealDoor.activeSelf && C.collapsedExit.activeSelf && C.Rules.stage == Stage.Sealed, "Fire door closes, emergency exit collapses");
        Use(Interaction.Vent); Check(C.player.IsCrawling && C.Rules.stage == Stage.Vent, "Vent changes collider and camera height");
        C.hud.Document("QA", "Pause pursuit during duct collision probe");
        var p = C.player; p.InputBlocked = false;
        for (int i = 0; i < 870; i++) p.Move(Vector2.up, false, 1f / 60);
        Check(p.transform.position.z > 98.8f, "Low duct straight section passable");
        p.transform.rotation = Quaternion.Euler(0, 90, 0);
        for (int i = 0; i < 625; i++) p.Move(Vector2.up, false, 1f / 60);
        Check(p.transform.position.x > 35, "Low duct corner and exit passable");
        C.hud.Close(); Use(Interaction.Exit); await Until(() => C.Rules.stage == Stage.Ending && !C.Busy, 8000);
        Check(C.hud.IsOpen && !C.monster.gameObject.activeSelf, "Collapse completes with ending UI");
        C.Retry(); await Until(() => C != null && C.Rules.stage == Stage.Vent, 10000); await Task.Delay(200);
        Check(C.player.IsCrawling && C.player.transform.position.z >= 64, "Vent checkpoint restores crawling and spawn");
        C.Die(); Check(C.Rules.stage == Stage.Dead && C.hud.IsOpen, "Capture opens retry UI");
        C.Retry(); await Until(() => C != null && C.Rules.stage == Stage.Vent); await Task.Delay(100);
        Check(C.Rules.stage == Stage.Vent, "Retry restores last checkpoint after capture");
        return Report("finale-integration");
    }
    public static async Task<string> Prison()
    {
        log.Clear(); HollowCreekSession.NewGame(); HollowCreekSession.Enter(2);
        await SceneManager.LoadSceneAsync("SoloScene2"); await Task.Delay(200);
        Use(Interaction.Lift); await Until(() => C.Rules.stage == Stage.WeakWall && !C.Busy, 10000);
        Check(C.Rules.route == Route.Prison, "Letting X window expire takes CASE 2");
        Check(Physics.Raycast(C.player.transform.position + Vector3.up, Vector3.down, 2), "B2 landing has collidable floor");
        Use(Interaction.Wounded); C.hud.Close(); Use(Interaction.WeakWall); await Until(() => !C.Busy);
        Check(!C.prisonBarrier.activeSelf && C.Rules.passageOpen, "Pushing weak wall removes physical barrier");
        Use(Interaction.NextArea); await Until(() => C != null && C.sceneNumber == 3); await Task.Delay(200);
        Check(C.Rules.route == Route.Prison && C.prisonProps.activeSelf && !C.upperProps.activeSelf, "Prison branch persists across scene transition");
        Check(C.labLeaves.Length >= 2 && C.labLeaves.All(g => g.activeSelf), "Existing lab door leaves initially locked");
        Use(Interaction.Sliding); Check(C.hud.IsOpen, "Sliding flag UI opens"); C.hud.Close();
        var puzzle = typeof(HollowCreekCampaign).GetField("sliding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(C) as SlidingPuzzle;
        var solution = Solve(puzzle.Tiles);
        Use(Interaction.Sliding);
        foreach (int move in solution) {
            var buttons = C.hud.GetComponent<UIDocument>().rootVisualElement.Query<Button>(className: "tile").ToList();
            typeof(Clickable).GetMethod("SimulateSingleClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(buttons[move].clickable, new object[] { null, 0 });
        }
        Check(!C.hud.IsOpen && C.Rules.puzzleSolved && C.labLeaves.All(g => !g.activeSelf), "Legal tile clicks solve board and open original lab doors");
        Use(Interaction.Note4); C.hud.Close(); Use(Interaction.Radio); Answer("1224");
        await Until(() => C != null && C.sceneNumber == 4, 18000); await Task.Delay(200);
        Check(C.Rules.route == Route.Prison, "Prison radio converges on final scene");
        return Report("prison-integration");
    }
    static List<int> Solve(int[] start)
    {
        string key = string.Concat(start.Select(i => (char)('0' + i))), end = "123456780";
        var queue = new Queue<string>(); queue.Enqueue(key);
        var prev = new Dictionary<string, (string parent, int move)>(); prev[key] = (null, -1);
        while (queue.Count > 0) {
            string s = queue.Dequeue(); if (s == end) break; int empty = s.IndexOf('0');
            for (int i = 0; i < 9; i++) if (SlidingPuzzle.Adjacent(i, empty)) {
                var chars = s.ToCharArray(); chars[empty] = chars[i]; chars[i] = '0'; string n = new string(chars);
                if (prev.ContainsKey(n)) continue; prev[n] = (s, i); queue.Enqueue(n);
            }
        }
        var result = new List<int>(); for (string s = end; prev[s].parent != null; s = prev[s].parent) result.Add(prev[s].move);
        result.Reverse(); return result;
    }
    public static string RestoreUserSave()
    {
        string path = "artifacts/hollow-creek/checkpoint-before-tests.txt";
        if (!File.Exists(path)) return "No backup";
        string previous = File.ReadAllText(path);
        if (previous == "<none>") PlayerPrefs.DeleteKey("HollowCreek.Checkpoint.v1"); else PlayerPrefs.SetString("HollowCreek.Checkpoint.v1", previous);
        PlayerPrefs.Save(); return "Original player checkpoint restored";
    }
    public static string SoloIsolation()
    {
        if (C == null || C.sceneNumber != 1) throw new Exception("Open SoloScene1 in Play Mode first");
        var ui = C.hud.GetComponent<UIDocument>().rootVisualElement;
        if (ui.Query<Button>().ToList().Any(b => b.text == "멀티게임")) throw new Exception("Cross-mode button remains");
        if (UnityEngine.Object.FindAnyObjectByType<FacilityEscape.MultiEscapeLanSession>() != null) throw new Exception("Multiplayer session in solo scene");
        if (Application.CanStreamedLevelBeLoaded("MultiScene1")) throw new Exception("Multiplayer scene included in solo profile");
        for (int i = 1; i <= 4; i++) if (!Application.CanStreamedLevelBeLoaded("SoloScene" + i)) throw new Exception("Missing solo chapter " + i);
        return "Solo welcome has no multiplayer button/session; only all four solo chapters are loadable";
    }
    public static string MultiplayerIsolation()
    {
        var session = UnityEngine.Object.FindAnyObjectByType<FacilityEscape.MultiEscapeLocalSession>();
        if (session == null || C != null) throw new Exception("Incorrect multiplayer bootstrap");
        var ui = session.GetComponent<UIDocument>().rootVisualElement;
        if (ui.Q<Slider>("voiceInputGain") == null || ui.Q<Slider>("voiceOutputVolume") == null) throw new Exception("Missing multiplayer voice controls");
        if (!Application.CanStreamedLevelBeLoaded("MultiScene1")) throw new Exception("Missing multiplayer entry scene");
        for (int i = 1; i <= 4; i++) if (Application.CanStreamedLevelBeLoaded("SoloScene" + i)) throw new Exception("Solo chapter included in multiplayer profile");
        return "Multiplayer starts directly with voice controls and no solo campaign; solo chapters are not loadable";
    }
}
