using System;
using UnityEngine;

namespace FacilityEscape.Editor
{
    /// <summary>씬을 변경하지 않는 회귀 검사. 빌드에는 포함되지 않습니다.</summary>
    public static class MultiEscapeChecks
    {
        private static int checks;
        private static readonly LockColor[] Colors = { LockColor.Red, LockColor.Blue, LockColor.Yellow, LockColor.Green };
        private static readonly DialSymbol[] Symbols = { DialSymbol.Triangle, DialSymbol.Star, DialSymbol.Diamond };

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("MultiEscape check failed: " + name);
            checks++;
        }

        private static MultiEscapeRules NewRound(double seconds = 60, int hearts = 3, double interval = 20)
        {
            var rules = new MultiEscapeRules(Colors, Symbols, seconds, hearts, interval);
            rules.StartRound();
            return rules;
        }

        private static void SolveColors(MultiEscapeRules rules)
        {
            foreach (var color in Colors) rules.PressColor(EscapeRole.Blindfolded, (int)color);
        }

        private static void SolveDials(MultiEscapeRules rules)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < (int)Symbols[i]; j++)
                    rules.RotateDial(EscapeRole.Blindfolded, i, 1);
        }

        public static string Run()
        {
            checks = 0;
            var rules = NewRound();
            Check(rules.HeartsA == 3 && rules.HeartsB == 3 && rules.RemainingSeconds == 60, "initial state");
            Check(rules.Advance(19.5) == 0 && rules.HeartsA == 3, "before first shock");
            Check(rules.Advance(0.5) == 1 && rules.HeartsA == 2 && rules.HeartsB == 2, "20 seconds");
            Check(rules.Advance(20) == 1 && rules.HeartsA == 1 && rules.HeartsB == 1, "40 seconds");
            Check(rules.Advance(20) == 1 && rules.HeartsA == 0 && rules.HeartsB == 0 && rules.Stage == EscapeStage.Failed, "60 seconds");
            Check(rules.Advance(100) == 0 && rules.RemainingSeconds == 0, "terminal state stable");
            Check(rules.PressColor(EscapeRole.Blindfolded, 0) == EscapeInputResult.Ignored, "input after failure");
            rules.StartRound();
            Check(rules.HeartsA == 3 && rules.ColorProgress == 0 && rules.Stage == EscapeStage.ColorLock, "restart after failure");
            Check(rules.Advance(60) == 3 && rules.Stage == EscapeStage.Failed, "long frame catches all shocks");

            rules = NewRound(60, 3, 40);
            rules.Advance(40);
            Check(rules.HeartsA == 2, "40-second interval option");
            rules.Advance(20);
            Check(rules.Stage == EscapeStage.Failed && rules.HeartsA == 2, "timeout even with HP remaining");
            rules = NewRound(60, 1, 20);
            rules.Advance(20);
            Check(rules.Stage == EscapeStage.Failed && rules.RemainingSeconds == 40, "HP failure before timeout");

            rules = NewRound();
            Check(rules.PressColor(EscapeRole.Observer, 0) == EscapeInputResult.Ignored && rules.ColorProgress == 0, "observer color rejected");
            Check(rules.RotateDial(EscapeRole.Blindfolded, 0, 1) == EscapeInputResult.Ignored, "dial gated by lock 1");
            Check(rules.PullLever(EscapeRole.Blindfolded) == EscapeInputResult.Ignored, "lever gated by lock 1");
            Check(rules.PressColor(EscapeRole.Blindfolded, -1) == EscapeInputResult.Ignored, "invalid color rejected");
            rules.PressColor(EscapeRole.Blindfolded, 0);
            Check(rules.PressColor(EscapeRole.Blindfolded, 3) == EscapeInputResult.Wrong && rules.ColorProgress == 0, "wrong color resets sequence");
            Check(rules.HeartsA == 3 && rules.HeartsB == 3 && rules.ColorLockoutSeconds == 3, "wrong answer does not damage");
            rules.Advance(2.5);
            Check(rules.PressColor(EscapeRole.Blindfolded, 0) == EscapeInputResult.Ignored, "locked panel ignores input");
            rules.Advance(0.5);
            SolveColors(rules);
            Check(rules.Stage == EscapeStage.SymbolLock, "unlock at exact 3-second boundary");
            Check(rules.PressColor(EscapeRole.Blindfolded, 0) == EscapeInputResult.Ignored, "completed color panel ignores input");
            Check(rules.RotateDial(EscapeRole.Observer, 0, 1) == EscapeInputResult.Ignored, "observer dial rejected");
            Check(rules.PullLever(EscapeRole.Observer) == EscapeInputResult.Ignored, "observer lever rejected");
            Check(rules.RotateDial(EscapeRole.Blindfolded, 3, 1) == EscapeInputResult.Ignored, "invalid dial index");
            Check(rules.RotateDial(EscapeRole.Blindfolded, 0, 2) == EscapeInputResult.Ignored, "invalid direction");
            rules.RotateDial(EscapeRole.Blindfolded, 0, -1);
            Check(rules.GetDial(0) == DialSymbol.Cross, "counterclockwise wrap");
            rules.RotateDial(EscapeRole.Blindfolded, 0, 1);
            Check(rules.GetDial(0) == DialSymbol.Circle, "clockwise wrap");
            for (int i = 0; i < 3; i++) rules.RotateDial(EscapeRole.Blindfolded, i, 1);
            Check(rules.PullLever(EscapeRole.Blindfolded) == EscapeInputResult.Wrong, "wrong lever");
            Check(rules.GetDial(0) == DialSymbol.Circle && rules.GetDial(1) == DialSymbol.Circle && rules.GetDial(2) == DialSymbol.Circle, "all dials reset");
            Check(rules.HeartsA == 3 && rules.HeartsB == 3, "wrong lever does not damage");
            SolveDials(rules);
            rules.Advance(56.5);
            Check(rules.PullLever(EscapeRole.Blindfolded) == EscapeInputResult.Escaped, "escape just before deadline");
            Check(rules.Advance(100) == 0 && rules.Stage == EscapeStage.Escaped && rules.HeartsA == 1, "success is final");
            rules.StartRound();
            Check(rules.Stage == EscapeStage.ColorLock && rules.HeartsA == 3 && rules.GetDial(2) == DialSymbol.Circle, "restart after success");

            rules = NewRound();
            SolveColors(rules);
            SolveDials(rules);
            rules.Advance(60);
            Check(rules.PullLever(EscapeRole.Blindfolded) == EscapeInputResult.Ignored && rules.Stage == EscapeStage.Failed, "deadline wins over correct lever");
            rules = NewRound();
            rules.Advance(59);
            rules.PressColor(EscapeRole.Blindfolded, 3);
            rules.Advance(1);
            Check(rules.Stage == EscapeStage.Failed, "timeout during color lockout");

            bool invalidRejected = false;
            try { new MultiEscapeRules(Colors, new DialSymbol[2]); }
            catch (ArgumentException) { invalidRejected = true; }
            Check(invalidRejected, "invalid configuration rejected");
            rules = NewRound();
            rules.Advance(21);
            rules.PressColor(EscapeRole.Blindfolded, 3);
            var replica = MultiEscapeRules.FromSnapshot(JsonUtility.FromJson<MultiEscapeSnapshot>(JsonUtility.ToJson(rules.Capture())));
            Check(replica.Stage == rules.Stage && replica.HeartsA == 2 && replica.HeartsB == 2, "snapshot preserves stage and HP");
            Check(replica.RemainingSeconds == 39 && replica.ColorLockoutSeconds == 3, "snapshot preserves timer and lockout");
            rules.Advance(3);
            SolveColors(rules); SolveDials(rules);
            replica = MultiEscapeRules.FromSnapshot(rules.Capture());
            Check(replica.Stage == EscapeStage.SymbolLock && replica.GetDial(0) == Symbols[0], "snapshot preserves dial state");
            rules.PullLever(EscapeRole.Blindfolded);
            replica = MultiEscapeRules.FromSnapshot(rules.Capture());
            Check(replica.Stage == EscapeStage.Escaped && replica.Advance(100) == 0, "escaped snapshot is terminal");
            CheckGrayscale();
            return $"PASS: {checks} checks (rules + GPU grayscale), no scene changes.";
        }

        private static void CheckGrayscale()
        {
            var shader = Resources.Load<Shader>("MultiEscapeGrayscale");
            Check(shader != null && shader.isSupported && !UnityEditor.ShaderUtil.ShaderHasError(shader), "grayscale shader compiles");
            var material = new Material(shader);
            var source = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            var output = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            try
            {
                source.SetPixel(0, 0, Color.red);
                source.Apply();
                Graphics.Blit(source, output, material);
                RenderTexture.active = output;
                readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                readback.Apply();
                Color pixel = readback.GetPixel(0, 0);
                Check(Mathf.Abs(pixel.r - pixel.g) < 0.01f && Mathf.Abs(pixel.g - pixel.b) < 0.01f && pixel.r > 0.05f && pixel.r < 0.95f,
                    "red becomes visible neutral gray on GPU");
                Check(source.GetPixel(0, 0) == Color.red, "source color preserved for observer");
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(output);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }
    }
}
