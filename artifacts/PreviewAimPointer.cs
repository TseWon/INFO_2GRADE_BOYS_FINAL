using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FacilityEscape;
using UnityEngine;
using UnityEngine.UIElements;

public static class PreviewAimPointer
{
    public static async Task<string> Capture()
    {
        var game = Object.FindAnyObjectByType<MultiEscapeGame>();
        var session = Object.FindAnyObjectByType<MultiEscapeLocalSession>();
        // Temporary Play Mode preview only; saved LAN configuration is untouched.
        game.Lan = null;
        typeof(MultiEscapeLocalSession).GetField("lan", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(session, null);
        game.StartRound(); session.SetMouseCaptured(false);
        await Task.Delay(300);
        var root = session.GetComponent<UIDocument>().rootVisualElement;
        var aim = root.Q("crosshair");
        return "Aim visible=" + aim.visible + ", center=" + aim.worldBound.center +
            ", viewport center=" + root.worldBound.center + ", pieces=" + aim.childCount +
            ", picking=" + aim.pickingMode + ", mouse captured=" + session.MouseCaptured;
    }
}
