using UnityEngine;

namespace FacilityEscape
{
    // Optional launch shortcuts: -escape-host or -escape-join 192.168.0.10.
    public static class MultiEscapeLanLaunch
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            var lan = Object.FindAnyObjectByType<MultiEscapeLanSession>();
            if (lan == null) return;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-escape-host") lan.Host();
                if (args[i] == "-escape-join" && i + 1 < args.Length) lan.Join(args[++i]);
            }
        }
    }
}
