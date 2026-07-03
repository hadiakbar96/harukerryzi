namespace Harukerryzi.Clash
{
    public static class BattleSession
    {
        public static int SelectedStageIndex { get; private set; } = -1;
        public static AduTosEnemyConfig SelectedEnemy { get; private set; }
        public static bool HasSelection => SelectedStageIndex >= 0 && SelectedEnemy != null;
        public static bool IsReplayStage { get; private set; }
        public static bool HasResult { get; private set; }
        public static bool PlayerWon { get; private set; }

        public static void SelectStage(int stageIndex, AduTosEnemyConfig enemy, bool isReplayStage = false)
        {
            SelectedStageIndex = stageIndex;
            SelectedEnemy = enemy;
            IsReplayStage = isReplayStage;
            HasResult = false;
            PlayerWon = false;
        }

        public static void SetResult(bool playerWon)
        {
            HasResult = true;
            PlayerWon = playerWon;
        }

        public static void ClearResult()
        {
            HasResult = false;
            PlayerWon = false;
        }

        public static void ClearAll()
        {
            SelectedStageIndex = -1;
            SelectedEnemy = null;
            IsReplayStage = false;
            ClearResult();
        }
    }
}
