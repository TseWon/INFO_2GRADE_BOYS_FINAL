using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FacilityEscape;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public static class TestHollowVoiceLan
{
    const string Path = "artifacts/hollow-creek/voice-client.json";
    static int checks, command;
    static MultiEscapeLanSession lan;
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; }
    static MultiEscapeLanProbe.Report Report()
    {
        try { return JsonUtility.FromJson<MultiEscapeLanProbe.Report>(File.ReadAllText(Path)); }
        catch (IOException) { return null; }
        catch (ArgumentException) { return null; }
    }
    static int Send(string action)
    {
        int id = ++command;
        File.WriteAllText(Path + ".command.json", JsonUtility.ToJson(new MultiEscapeLanProbe.Command {
            id = id, action = action, address = "127.0.0.1"
        }));
        return id;
    }
    static async Task<MultiEscapeLanProbe.Report> Wait(Func<MultiEscapeLanProbe.Report, bool> predicate, string name)
    {
        for (int i = 0; i < 80; i++)
        {
            var report = Report();
            if (report != null && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - report.timestamp < 2000 && predicate(report))
            { checks++; return report; }
            await Task.Delay(100);
        }
        throw new Exception(name + " | " + JsonUtility.ToJson(Report()));
    }
    public static string Host()
    {
        if (!Application.isPlaying) throw new Exception("Enter Play Mode first.");
        UnityEngine.Object.FindAnyObjectByType<MultiEscapeLanSession>().Host();
        return "Voice test host started.";
    }
    public static string Inspect()
    {
        var local = UnityEngine.Object.FindAnyObjectByType<MultiEscapeLocalSession>();
        var root = local.GetComponent<UIDocument>().rootVisualElement;
        var session = UnityEngine.Object.FindAnyObjectByType<MultiEscapeLanSession>();
        return root.Q<Label>("roleLabel").text + " micEnabled=" + root.Q<Button>("microphoneMute").enabledInHierarchy + " busy=" + session.IsBusy + " connected=" + session.Connected;
    }
    public static async Task<string> Run()
    {
        checks = 0;
        lan = UnityEngine.Object.FindAnyObjectByType<MultiEscapeLanSession>();
        command = Report()?.command ?? 0;
        var report = await Wait(r => lan.Connected && r.connected, "Two peers connected");
        int initiallyReceivedByB = report.voiceReceived;
        int firstFrame = Time.frameCount;
        await Task.Delay(100);
        Check(Time.frameCount > firstFrame, "Player loop advances");
        Check(lan.IsHost && lan.LocalRole == EscapeRole.Blindfolded && report.bInput, "A/B ownership");
        Check(!lan.Voice.IsRecording, "A does not capture a microphone");
        var ui = UnityEngine.Object.FindAnyObjectByType<MultiEscapeLocalSession>();
        var root = ui.GetComponent<UIDocument>().rootVisualElement;
        Check(root.Q<DropdownField>("microphoneField") != null && root.Q<Button>("microphoneMute") != null, "Voice UI bound");
        Check(GameObject.Find("MultiEscapePrototype/03_PlayerRigs/Player_B_Observer/Avatar_ReplaceVisuals/Head_LookPivot/EarDefenders") != null, "B ear defenders visible");
        if (!report.voiceMuted)
        {
            int mute = Send("voice-mute");
            await Wait(r => r.command == mute && r.voiceMuted && !r.voiceRecording, "Mute closes microphone");
        }
        await Task.Delay(500);
        int before = lan.Voice.ReceivedPackets;
        int tone = Send("voice-tone");
        await Wait(r => r.command == tone && lan.Voice.ReceivedPackets >= before + 10, "B tone crosses real LAN transport");
        var speaker = lan.GetComponentsInChildren<AudioSource>().Single(s => s.name == "Partner_Voice");
        Check(speaker.isPlaying && speaker.spatialBlend == 0, "A plays voice without distance attenuation");
        var output = new float[512];
        float peak = 0;
        for (int i = 0; i < 20; i++)
        {
            var filteredPeak = speaker.GetComponent<MultiEscapeVoicePlayback>().OutputPeak;
            peak = Math.Max(peak, filteredPeak);
            await Task.Delay(20);
        }
        Check(peak > .001f, "Synthetic voice reaches actual AudioSource output");
        var manager = NetworkManager.Singleton;
        var game = UnityEngine.Object.FindAnyObjectByType<MultiEscapeGame>();
        using (var writer = new FastBufferWriter(648, Allocator.Temp))
        {
            writer.WriteValueSafe(game.RoundNumber);
            writer.WriteValueSafe((uint)0);
            for (int i = 0; i < 320; i++) writer.WriteValueSafe((short)4000);
            ulong peer = manager.ConnectedClientsIds.First(id => id != NetworkManager.ServerClientId);
            manager.CustomMessagingManager.SendNamedMessage("Facility.Voice.v2", peer, writer, NetworkDelivery.Unreliable);
        }
        await Task.Delay(400);
        Check(Report().voiceReceived == initiallyReceivedByB, "B rejects host voice even if a packet bypasses SendVoice");
        lan.SendVoice(100, new short[320]);
        Check(!lan.Voice.IsRecording && lan.Voice.SentPackets == 0, "A never sends microphone audio");
        ui.StartRound(); ui.SetMouseCaptured(false);
        await Wait(r => r.state.stage == EscapeStage.ColorLock, "Round starts with voice enabled");
        await Task.Delay(500);
        before = lan.Voice.ReceivedPackets; tone = Send("voice-tone");
        await Wait(r => r.command == tone && lan.Voice.ReceivedPackets > before + 5, "Voice continues in round");
        var players = UnityEngine.Object.FindObjectsByType<MultiEscapePlayer>();
        var a = players.Single(p => p.Role == EscapeRole.Blindfolded);
        var b = players.Single(p => p.Role == EscapeRole.Observer);
        var blindfold = a.transform.Find("Avatar_ReplaceVisuals/Head_LookPivot/Blindfold").gameObject;
        var ears = b.transform.Find("Avatar_ReplaceVisuals/Head_LookPivot/EarDefenders").gameObject;
        var gray = a.ViewCamera.GetComponent<MultiGrayscaleCamera>();
        for (int i = 0; i < game.Rules.ColorCount; i++) game.PressColor(a, game.Rules.GetColorAnswer(i));
        await Wait(r => r.state.stage == EscapeStage.SymbolLock && r.blindfoldVisible && r.earDefendersVisible && r.grayscale && !r.canReceiveVoice,
            "One solved puzzle keeps sensory restrictions on both clients");
        Check(!lan.CanSendVoice && blindfold.activeSelf && ears.activeSelf && gray.enabled, "A still restricted after first puzzle");
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < (int)game.Rules.GetSymbolAnswer(i); j++) game.RotateDial(a, i, 1);
        game.PullLever(a);
        await Wait(r => r.state.stage == EscapeStage.Escaped && !r.blindfoldVisible && !r.earDefendersVisible && !r.grayscale && r.canReceiveVoice,
            "Both puzzles remove restrictions on B client");
        Check(lan.VoiceUnlocked && lan.CanSendVoice && lan.CanReceiveVoice && !blindfold.activeSelf && !ears.activeSelf && !gray.enabled,
            "Both puzzles remove host blindfold, ear defenders and grayscale");
        Check((a.ViewCamera.cullingMask & (1 << 30)) != 0, "A can now see clue board");
        Check(lan.VoiceUnlocked && root.Q<Button>("microphoneMute").enabledInHierarchy, "A unlocked microphone control (campaign may replace the role label)");
        if (Microphone.devices.Length > 0)
            await Wait(r => lan.Voice.IsRecording, "A microphone opens automatically on escape");
        lan.Voice.ToggleMute();
        Check(lan.Voice.Muted && !lan.Voice.IsRecording, "A can mute after escape");
        await Task.Delay(500);
        int remoteBefore = Report().voiceReceived;
        using (var writer = new FastBufferWriter(648, Allocator.Temp))
        {
            writer.WriteValueSafe(game.RoundNumber - 1);
            writer.WriteValueSafe((uint)40000);
            for (int i = 0; i < 320; i++) writer.WriteValueSafe((short)4000);
            ulong peer = manager.ConnectedClientsIds.First(id => id != NetworkManager.ServerClientId);
            manager.CustomMessagingManager.SendNamedMessage("Facility.Voice.v2", peer, writer, NetworkDelivery.Unreliable);
        }
        await Task.Delay(300);
        Check(Report().voiceReceived == remoteBefore, "Previous-round packet rejected even with two-way voice unlocked");
        var toneFrame = new short[320];
        float remotePeak = 0;
        for (uint sequence = 50000; sequence < 50060; sequence++)
        {
            for (int i = 0; i < toneFrame.Length; i++) toneFrame[i] = (short)(Math.Sin((sequence * 320 + i) * Math.PI * 2 * 440 / 16000) * 4000);
            lan.SendVoice(sequence, toneFrame);
            remotePeak = Math.Max(remotePeak, Report()?.voiceOutputPeak ?? 0);
            await Task.Delay(20);
        }
        await Wait(r => r.voiceReceived > remoteBefore + 10, "A voice reaches B after escape");
        Check(remotePeak > .001f, "A voice reaches B actual AudioSource output");
        before = lan.Voice.ReceivedPackets; tone = Send("voice-tone");
        await Wait(r => r.command == tone && lan.Voice.ReceivedPackets > before + 5, "B voice still reaches A after escape");
        // Restart while A is unmuted: it must close capture immediately and never play residual A speech on B.
        lan.Voice.ToggleMute();
        ui.StartRound(); ui.SetMouseCaptured(false);
        await Wait(r => r.state.stage == EscapeStage.ColorLock && r.blindfoldVisible && r.earDefendersVisible && r.grayscale && !r.canReceiveVoice && !r.voiceSpeakerPlaying,
            "Restart restores B restrictions and stops its voice playback");
        Check(!lan.VoiceUnlocked && !lan.CanSendVoice && !lan.Voice.IsRecording && blindfold.activeSelf && ears.activeSelf && gray.enabled,
            "Restart restores A restrictions and closes microphone");
        Check((a.ViewCamera.cullingMask & (1 << 30)) == 0, "Restart hides clue board from A");
        remoteBefore = Report().voiceReceived;
        lan.SendVoice(60000, toneFrame);
        await Task.Delay(300);
        Check(Report().voiceReceived == remoteBefore, "A voice blocked again after restart");
        before = lan.Voice.ReceivedPackets; tone = Send("voice-tone");
        await Wait(r => r.command == tone && lan.Voice.ReceivedPackets > before + 5, "B to A remains available after restart");
        lan.Leave();
        await Wait(r => !r.connected && !r.voiceRecording && !lan.IsBusy, "Disconnect stops capture and connection");
        Check(!speaker.isPlaying, "Disconnect stops playback");
        lan.Host();
        int join = Send("join");
        await Wait(r => r.command == join && r.connected && lan.Connected, "Reconnect");
        before = lan.Voice.ReceivedPackets; tone = Send("voice-tone");
        await Wait(r => r.command == tone && lan.Voice.ReceivedPackets > before + 5, "Voice restored after reconnect");
        lan.Leave();
        await Wait(r => !r.connected && !r.voiceRecording && !lan.IsBusy, "Final cleanup");
        string result = checks + " two-process voice checks passed; output peak=" + peak;
        File.WriteAllText("artifacts/hollow-creek/voice-lan-results.txt", result);
        return result;
    }
}
