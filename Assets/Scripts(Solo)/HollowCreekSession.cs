using System;
using UnityEngine;

namespace HollowCreek
{
    public static class HollowCreekSession
    {
        const string SaveKey = "HollowCreek.Checkpoint.v1";
        [Serializable] sealed class Checkpoint { public int version = 1, scene; public HollowCreekRules rules; }
        public static HollowCreekRules Rules { get; private set; } = new HollowCreekRules();
        public static int Scene { get; private set; } = 1;
        public static bool Resumed { get; set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Rules = new HollowCreekRules(); Scene = 1; Resumed = false; }
        public static void NewGame() { Rules = new HollowCreekRules(); Scene = 1; Resumed = false; }
        public static void Enter(int scene)
        {
            if (Scene == scene) return;
            Scene = scene;
            switch (scene)
            {
                case 1: NewGame(); break;
                case 2: Rules.stage = Stage.Lift; break;
                case 3:
                    if (Rules.route == Route.Undecided) Rules.route = Route.Upper;
                    Rules.stage = Stage.Search; Rules.puzzleSolved = false; break;
                case 4:
                    if (Rules.route == Route.Undecided) Rules.route = Route.Upper;
                    Rules.stage = Stage.Chase; break;
            }
        }
        public static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new Checkpoint { scene = Scene, rules = Rules }));
            PlayerPrefs.Save();
        }
        static Checkpoint Read()
        {
            try
            {
                var save = JsonUtility.FromJson<Checkpoint>(PlayerPrefs.GetString(SaveKey, ""));
                if (save == null || save.version != 1 || save.rules == null || save.scene < 1 || save.scene > 4 ||
                    !Enum.IsDefined(typeof(Stage), save.rules.stage) || !Enum.IsDefined(typeof(Route), save.rules.route)) return null;
                var s = save.rules.stage;
                bool valid = save.scene == 1 && (s == Stage.Arrival || s == Stage.Entrance) ||
                    save.scene == 2 && (s == Stage.Lift || s == Stage.Acrostic || s == Stage.WeakWall) ||
                    save.scene == 3 && (s == Stage.Search || s == Stage.Radio) ||
                    save.scene == 4 && (s == Stage.Chase || s == Stage.Vent);
                return valid ? save : null;
            }
            catch (Exception) { return null; }
        }
        public static bool HasSave => Read() != null;
        public static bool Restore()
        {
            var save = Read(); if (save == null) return false;
            Rules = save.rules; Scene = save.scene; Resumed = true; return true;
        }
    }
}
