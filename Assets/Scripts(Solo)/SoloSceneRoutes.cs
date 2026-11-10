using System;
using UnityEngine.SceneManagement;

namespace HollowCreek
{
    public static class SoloSceneRoutes
    {
        public const string Laboratory = "SoloScene2";
        public const string ElevatorChapter = "SoloScene3";

        public static int BuildIndex(string sceneName) =>
            SceneUtility.GetBuildIndexByScenePath("Assets/Scene/" + sceneName + ".unity");

        // Campaign chapter numbers are retained for existing checkpoints.
        public static string CampaignScene(int chapter)
        {
            if (chapter < 1 || chapter > 4) throw new ArgumentOutOfRangeException(nameof(chapter));
            return "SoloScene" + (chapter == 1 ? 1 : chapter + 1);
        }
    }
}
