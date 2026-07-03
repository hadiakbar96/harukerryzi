using UnityEngine;

namespace Harukerryzi.Clash
{
    public static class StageProgress
    {
        private const string HighestUnlockedStageKey = "clash.highestStage";

        public static int HighestUnlockedStage
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedStageKey, 0));
            set
            {
                PlayerPrefs.SetInt(HighestUnlockedStageKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static void MarkStageCleared(int stageIndex, int maxStageIndex)
        {
            if (stageIndex < HighestUnlockedStage)
            {
                return;
            }

            HighestUnlockedStage = Mathf.Min(stageIndex + 1, maxStageIndex);
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(HighestUnlockedStageKey);
            PlayerPrefs.Save();
        }
    }
}
