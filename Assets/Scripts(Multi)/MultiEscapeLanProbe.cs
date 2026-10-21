#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FacilityEscape
{
    // Opt-in two-process integration probe. Omitted from release players.
    public sealed class MultiEscapeLanProbe : MonoBehaviour
    {
        public string output;
        private MultiEscapeLanSession lan;
        private MultiEscapeGame game;
        private MultiEscapeLocalSession ui;
        private MultiEscapePlayer a, b;
        private double nextWrite;
        private int lastCommand;
        [Serializable] public class Report
        {
            public long timestamp;
            public bool connected, host, aInput, bInput, timerVisible, aAvatar, bAvatar;
            public string status, clue;
            public int command, shocks;
            public MultiEscapeSnapshot state;
            public Vector3 aPosition, bPosition;
            public Quaternion aRotation, bRotation;
        }
        [Serializable] public class Command
        {
            public int id;
            public string action, address;
            public Vector3 position;
            public Vector2 look;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-escape-lan-probe")
                    new GameObject("LAN_Test_Probe").AddComponent<MultiEscapeLanProbe>().output = args[i + 1];
        }
        void Start()
        {
            lan = FindAnyObjectByType<MultiEscapeLanSession>(); game = FindAnyObjectByType<MultiEscapeGame>();
            ui = FindAnyObjectByType<MultiEscapeLocalSession>();
            var players = FindObjectsByType<MultiEscapePlayer>();
            a = players.Single(p => p.Role == EscapeRole.Blindfolded); b = players.Single(p => p.Role == EscapeRole.Observer);
        }
        void LateUpdate()
        {
            if (string.IsNullOrEmpty(output) || Time.realtimeSinceStartupAsDouble < nextWrite) return;
            nextWrite = Time.realtimeSinceStartupAsDouble + .1;
            try
            {
                string commandPath = output + ".command.json";
                if (File.Exists(commandPath))
                {
                    var command = JsonUtility.FromJson<Command>(File.ReadAllText(commandPath));
                    if (command.id > lastCommand)
                    {
                        lastCommand = command.id;
                        var local = lan.LocalRole == EscapeRole.Blindfolded ? a : b;
                        if (command.action == "look") local.Look(command.look);
                        if (command.action == "move" && game.Rules.Stage == EscapeStage.Escaped)
                        {
                            var motor = local.GetComponent<CharacterController>(); motor.enabled = false;
                            local.transform.position = command.position; motor.enabled = true;
                        }
                        if (command.action == "start") ui.StartRound();
                        if (command.action == "leave") lan.Leave();
                        if (command.action == "join") lan.Join(command.address);
                        if (command.action == "host") lan.Host();
                        if (command.action == "screenshot") ScreenCapture.CaptureScreenshot(output + ".png");
                    }
                }
                var root = ui.GetComponent<UIDocument>().rootVisualElement;
                var report = new Report {
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), connected = lan.Connected, host = lan.IsHost,
                    status = lan.Status, command = lastCommand, state = game.CaptureSnapshot(),
                    aInput = a.LocalInput, bInput = b.LocalInput,
                    aPosition = a.transform.position, bPosition = b.transform.position,
                    aRotation = a.ViewCamera.transform.rotation, bRotation = b.ViewCamera.transform.rotation,
                    aAvatar = a.transform.Find("Avatar_ReplaceVisuals").gameObject.activeSelf,
                    bAvatar = b.transform.Find("Avatar_ReplaceVisuals").gameObject.activeSelf,
                    timerVisible = !root.Q("timerLabel").ClassListContains("hidden"),
                    shocks = FindAnyObjectByType<MultiEscapeFeedback>().ShockCount,
                    clue = FindAnyObjectByType<MultiEscapeClueBoard>().GetComponent<TextMesh>().text
                };
                File.WriteAllText(output, JsonUtility.ToJson(report));
            }
            catch (IOException) { /* The test runner may be replacing its command file. Retry next tick. */ }
        }
    }
}
#endif
