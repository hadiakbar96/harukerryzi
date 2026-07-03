using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Harukerryzi.Clash
{
    public sealed class AduTosMinigameController : MonoBehaviour
    {
        [SerializeField] private ClashController clashController;
        [SerializeField] private CardSelectionUI cardSelectionUI;
        [SerializeField] private CardRevealUI cardRevealUI;
        [SerializeField] private MinigameScoreUI scoreUI;
        [SerializeField] private MinigameResultUI resultUI;
        [SerializeField] private RewardUI rewardUI;
        [SerializeField] private AduTosStageLayout stageLayout;
        [SerializeField] private AduTosEntranceUI entranceUI;
        [SerializeField] private ClashBackgroundUI clashBackgroundUI;
        [SerializeField] private ClashWinFxUI winFxUI;
        [SerializeField] private ClashMashButtonUI mashButtonUI;
        [SerializeField] private AIClashInput aiInput;
        [SerializeField] private AduTosEnemyConfig enemyConfig;
        [SerializeField] private string stageMapSceneName = "StageMap";
        [SerializeField] private Sprite mcHandSprite;
        [SerializeField] private ClashCardConfig[] availableCards;
        [SerializeField, Min(0f)] private float basePlayerPower = 10f;
        [SerializeField, Min(0f)] private float baseAiPower = 10f;
        [SerializeField, Min(0f)] private float fallbackAiMashesPerSecond = 4f;
        [SerializeField, Range(0f, 1f)] private float fallbackAiMashRandomness = 0.2f;
        [SerializeField, Min(1)] private int pointsToWin = 2;
        [SerializeField, Min(0f)] private float nextRoundDelay = 1f;
        [SerializeField, Min(0f)] private float rewardShowDelay = 0.65f;

        private int playerScore;
        private int aiScore;
        private int roundNumber = 1;
        private ClashCardConfig selectedPlayerCard;
        private ClashCardConfig selectedAiCard;

        private void OnEnable()
        {
            if (clashController != null)
            {
                clashController.OnClashFinished.AddListener(OnClashFinished);
            }

            if (resultUI != null)
            {
                resultUI.OnRetry.AddListener(RestartMinigame);
            }
        }

        private void OnDisable()
        {
            if (clashController != null)
            {
                clashController.OnClashFinished.RemoveListener(OnClashFinished);
            }

            if (resultUI != null)
            {
                resultUI.OnRetry.RemoveListener(RestartMinigame);
            }
        }

        private void Start()
        {
            if (BattleSession.SelectedEnemy != null)
            {
                enemyConfig = BattleSession.SelectedEnemy;
            }
#if UNITY_EDITOR
            else
            {
                UseEditorDirectPlaySelection();
            }
#endif

            EnsureRuntimeCards();

            if (aiInput == null)
            {
                aiInput = FindFirstObjectByType<AIClashInput>();
            }

            if (stageLayout == null)
            {
                stageLayout = FindFirstObjectByType<AduTosStageLayout>();
            }

            if (stageLayout == null)
            {
                GameObject canvasObject = GameObject.Find("ClashCanvas");
                if (canvasObject != null)
                {
                    stageLayout = canvasObject.AddComponent<AduTosStageLayout>();
                }
            }

            EnsureBattleVisuals();
            EnsureWinFx();
            EnsureMashButton();
            EnsureResultUI();
            EnsureRewardUI();

            RestartMinigame();
        }

        public void RestartMinigame()
        {
            playerScore = 0;
            aiScore = 0;
            roundNumber = 1;
            rewardUI?.Hide();
            resultUI?.Hide();
            mashButtonUI?.SetMashEnabled(false);
            clashController?.SetStartOnAwake(false);
            clashController?.ResetClash();
            UpdateScore();
            stageLayout?.ShowFullBackgroundImmediate();
            StartCardSelection();
        }

        private void StartCardSelection()
        {
            clashController?.ResetClash();
            mashButtonUI?.SetMashEnabled(false);
            cardRevealUI?.Hide();
            winFxUI?.ResetPushTarget();
            clashBackgroundUI?.SetBackground(GetClashBackgroundSprite());
            stageLayout?.SetBattleHandsAnimationEnabled(false);
            SetBattleHandsVisible(false);
            entranceUI?.HideImmediate();
            stageLayout?.TransitionToFullBackground();
            cardSelectionUI?.Show(availableCards, OnPlayerSelectedCard);
        }

        private void OnPlayerSelectedCard(ClashCardConfig card)
        {
            selectedPlayerCard = card;
            selectedAiCard = PickAiCard();
            cardSelectionUI?.Hide();
            cardRevealUI?.Hide();
            PlayEntranceThenClash();
        }

        private void PlayEntranceThenClash()
        {
            if (entranceUI == null || mcHandSprite == null || enemyConfig == null || enemyConfig.EnemyHandSprite == null)
            {
                StartClashRound();
                return;
            }

            entranceUI.Play(mcHandSprite, enemyConfig.EnemyHandSprite, enemyConfig.EntranceHandScale, enemyConfig.EntranceHandStartPosition, enemyConfig.EntranceHandTargetPosition, StartClashRound);
        }

        private void StartClashRound()
        {
            float playerPower = basePlayerPower * GetMultiplier(selectedPlayerCard);
            float aiPower = GetAiBasePower() * GetMultiplier(selectedAiCard);
            ConfigureAiInput();
            mashButtonUI?.SetMashEnabled(true);
            Sprite clashBackground = GetClashBackgroundSprite();
            winFxUI?.ResetPushTarget();
            clashBackgroundUI?.SetBackground(clashBackground);
            SetClashBackgroundShakeEnabled(false);
            stageLayout?.SetBattleHandsAnimationEnabled(false);
            SetBattleHandsVisible(false);
            entranceUI?.ShowClashHands(GetClashHandsSprite(), enemyConfig != null ? enemyConfig.ClashHandsScale : 1f);
            if (winFxUI != null && entranceUI != null)
            {
                winFxUI.SetPushTarget(entranceUI.ClashHandsRect);
            }
            stageLayout?.TransitionToClashSquare();
            clashController?.Init(playerPower, aiPower);
        }

        private void OnClashFinished(ClashResult result)
        {
            mashButtonUI?.SetMashEnabled(false);

            if (result == ClashResult.PlayerWin)
            {
                playerScore++;
            }
            else if (result == ClashResult.AiWin)
            {
                aiScore++;
            }

            UpdateScore();

            bool matchDeciding = playerScore >= pointsToWin || aiScore >= pointsToWin;
            if (winFxUI != null)
            {
                SetClashBackgroundShakeEnabled(false);
                entranceUI?.SetClashHandsShakeEnabled(false);
                winFxUI.Play(result, matchDeciding, () => ContinueAfterWinFx(matchDeciding));
                return;
            }

            ContinueAfterWinFx(matchDeciding);
        }

        private void ContinueAfterWinFx(bool matchDeciding)
        {
            if (matchDeciding)
            {
                bool playerWon = playerScore > aiScore;
                StartCoroutine(ShowRewardAfterFullScreen(playerWon));
                return;
            }

            roundNumber++;
            StartCoroutine(NextRoundAfterDelay());
        }

        private IEnumerator ShowRewardAfterFullScreen(bool playerWon)
        {
            entranceUI?.HideImmediate();
            SetBattleHandsVisible(false);
            stageLayout?.SetBattleHandsAnimationEnabled(false);
            winFxUI?.ResetPushTarget();
            stageLayout?.TransitionToFullBackground();

            if (rewardShowDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(rewardShowDelay);
            }

            if (resultUI != null)
            {
                resultUI.Show(playerWon, () => ShowReward(playerWon));
                yield break;
            }

            ShowReward(playerWon);
        }

        private void ShowReward(bool playerWon)
        {
            if (rewardUI != null)
            {
                rewardUI.Show(enemyConfig, playerWon, GetReward(playerWon), () => ClaimRewardAndReturnToMap(playerWon));
            }
        }

        private int GetReward(bool playerWon)
        {
            int reward = 0;
            if (enemyConfig != null)
            {
                reward = playerWon ? enemyConfig.RewardOnWin : enemyConfig.RewardOnLose;
            }

            if (!BattleSession.IsReplayStage)
            {
                return reward;
            }

            return playerWon ? reward / 2 : 0;
        }

        private Sprite GetClashBackgroundSprite()
        {
            if (enemyConfig != null && enemyConfig.ClashBackgroundSprite != null)
            {
                return enemyConfig.ClashBackgroundSprite;
            }

#if UNITY_EDITOR
            if (enemyConfig == null)
            {
                return null;
            }

            switch (enemyConfig.Level)
            {
                case 0:
                    return LoadEditorSprite("Assets/Projects/Sprites/StageMap/battle_tikus.png");
                case 1:
                    return LoadEditorSprite("Assets/Projects/Sprites/StageMap/battle_kunti.png");
                case 2:
                    return LoadEditorSprite("Assets/Projects/Sprites/StageMap/battle_tiang.png");
                default:
                    return null;
            }
#else
            return null;
#endif
        }

        private Sprite GetClashHandsSprite()
        {
            if (enemyConfig != null && enemyConfig.ClashHandsSprite != null)
            {
                return enemyConfig.ClashHandsSprite;
            }

#if UNITY_EDITOR
            if (enemyConfig == null)
            {
                return null;
            }

            switch (enemyConfig.Level)
            {
                case 0:
                    return LoadEditorSprite("Assets/Projects/Sprites/Battle/Sprite_Battle_MCvsTikus.png");
                case 1:
                    return LoadEditorSprite("Assets/Projects/Sprites/Battle/Sprite_Battle_MCvsKunti.png");
                case 2:
                    return LoadEditorSprite("Assets/Projects/Sprites/Battle/Sprite_Battle_MCvsTiang.png");
                default:
                    return null;
            }
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        private void UseEditorDirectPlaySelection()
        {
            int stageIndex = Mathf.Clamp(StageProgress.HighestUnlockedStage, 0, 2);
            AduTosEnemyConfig directPlayEnemy = UnityEditor.AssetDatabase.LoadAssetAtPath<AduTosEnemyConfig>(GetEditorEnemyAssetPath(stageIndex));
            if (directPlayEnemy == null)
            {
                return;
            }

            enemyConfig = directPlayEnemy;
            BattleSession.SelectStage(stageIndex, directPlayEnemy);
        }

        private static string GetEditorEnemyAssetPath(int stageIndex)
        {
            switch (stageIndex)
            {
                case 0:
                    return "Assets/Projects/Settings/Clash/Enemy_1_Tikus.asset";
                case 1:
                    return "Assets/Projects/Settings/Clash/Enemy_2_Kunti.asset";
                case 2:
                    return "Assets/Projects/Settings/Clash/Enemy_3_Tiang.asset";
                default:
                    return string.Empty;
            }
        }

        private static Sprite LoadEditorSprite(string path)
        {
            UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif

        private void ClaimRewardAndReturnToMap(bool playerWon)
        {
            BattleSession.SetResult(playerWon);

            if (string.IsNullOrWhiteSpace(stageMapSceneName))
            {
                RestartMinigame();
                return;
            }

            LoadStageMapScene();
        }

        private void LoadStageMapScene()
        {
            if (Application.CanStreamedLevelBeLoaded(stageMapSceneName))
            {
                SceneManager.LoadScene(stageMapSceneName);
                return;
            }

#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(
                "Assets/Projects/Scenes/SandBox/StageMap.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogWarning("[AduTos] Scene is not in Build Settings: " + stageMapSceneName);
#endif
        }

        private IEnumerator NextRoundAfterDelay()
        {
            yield return new WaitForSecondsRealtime(nextRoundDelay);
            StartCardSelection();
        }

        private ClashCardConfig PickAiCard()
        {
            ClashCardConfig[] aiCardPool = GetAiCardPool();
            if (aiCardPool == null || aiCardPool.Length == 0)
            {
                return null;
            }

            return aiCardPool[Random.Range(0, aiCardPool.Length)];
        }

        private ClashCardConfig[] GetAiCardPool()
        {
            if (enemyConfig != null && enemyConfig.CardPool != null && enemyConfig.CardPool.Length > 0)
            {
                return enemyConfig.CardPool;
            }

            return availableCards;
        }

        private float GetAiBasePower()
        {
            return enemyConfig != null ? enemyConfig.BaseMashPower : baseAiPower;
        }

        private void ConfigureAiInput()
        {
            if (aiInput == null)
            {
                return;
            }

            float mashesPerSecond = enemyConfig != null ? enemyConfig.MashesPerSecond : fallbackAiMashesPerSecond;
            float mashRandomness = enemyConfig != null ? enemyConfig.MashRandomness : fallbackAiMashRandomness;
            aiInput.Configure(mashesPerSecond, mashRandomness);
        }

        private void UpdateScore()
        {
            scoreUI?.SetScore(playerScore, aiScore, roundNumber);
        }

        private void EnsureBattleVisuals()
        {
            GameObject canvasObject = GameObject.Find("ClashCanvas");
            if (canvasObject == null)
            {
                return;
            }

            if (entranceUI == null)
            {
                entranceUI = FindFirstObjectByType<AduTosEntranceUI>();
            }

            if (entranceUI == null)
            {
                GameObject entranceObject = new("AduTosEntranceUI");
                entranceObject.transform.SetParent(stageLayout != null && stageLayout.PlayArea != null ? stageLayout.PlayArea : canvasObject.transform, false);
                RectTransform entranceRect = entranceObject.AddComponent<RectTransform>();
                entranceRect.anchorMin = Vector2.zero;
                entranceRect.anchorMax = Vector2.one;
                entranceRect.offsetMin = Vector2.zero;
                entranceRect.offsetMax = Vector2.zero;
                entranceUI = entranceObject.AddComponent<AduTosEntranceUI>();
            }

            PlaceEntranceUiInPlayArea();

            if (clashBackgroundUI == null)
            {
                clashBackgroundUI = FindFirstObjectByType<ClashBackgroundUI>();
            }

            if (clashBackgroundUI == null && stageLayout != null && stageLayout.PlayArea != null)
            {
                GameObject backgroundObject = new("EnemyClashBackground");
                backgroundObject.transform.SetParent(stageLayout.PlayArea, false);
                RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
                backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
                backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                backgroundRect.anchoredPosition = Vector2.zero;
                backgroundObject.transform.SetAsFirstSibling();
                clashBackgroundUI = backgroundObject.AddComponent<ClashBackgroundUI>();
            }

            if (clashBackgroundUI != null && clashBackgroundUI.GetComponent<ClashHandShake>() == null)
            {
                clashBackgroundUI.gameObject.AddComponent<ClashHandShake>();
            }
        }

        private void PlaceEntranceUiInPlayArea()
        {
            if (entranceUI == null || stageLayout == null || stageLayout.PlayArea == null)
            {
                return;
            }

            RectTransform entranceRect = entranceUI.transform as RectTransform;
            if (entranceRect == null)
            {
                return;
            }

            entranceRect.SetParent(stageLayout.PlayArea, false);
            entranceRect.anchorMin = Vector2.zero;
            entranceRect.anchorMax = Vector2.one;
            entranceRect.offsetMin = Vector2.zero;
            entranceRect.offsetMax = Vector2.zero;
            entranceRect.SetAsLastSibling();
        }

        private void EnsureWinFx()
        {
            if (winFxUI == null)
            {
                winFxUI = FindFirstObjectByType<ClashWinFxUI>();
            }

            if (winFxUI == null)
            {
                GameObject canvasObject = GameObject.Find("ClashCanvas");
                if (canvasObject == null)
                {
                    return;
                }

                GameObject fxObject = new("ClashWinFx");
                fxObject.transform.SetParent(canvasObject.transform, false);
                RectTransform fxRect = fxObject.AddComponent<RectTransform>();
                fxRect.anchorMin = Vector2.zero;
                fxRect.anchorMax = Vector2.one;
                fxRect.offsetMin = Vector2.zero;
                fxRect.offsetMax = Vector2.zero;
                winFxUI = fxObject.AddComponent<ClashWinFxUI>();
            }

            if (clashBackgroundUI != null)
            {
                winFxUI.SetPushTarget(clashBackgroundUI.transform as RectTransform);
            }
        }

        private void EnsureMashButton()
        {
            if (mashButtonUI == null)
            {
                mashButtonUI = FindFirstObjectByType<ClashMashButtonUI>();
            }

            mashButtonUI?.SetMashEnabled(false);
        }

        private void EnsureRewardUI()
        {
            GameObject canvasObject = GameObject.Find("ClashCanvas");

            if (rewardUI == null)
            {
                Transform existing = canvasObject != null ? canvasObject.transform.Find("RewardUI") : null;
                rewardUI = existing != null ? existing.GetComponent<RewardUI>() : FindFirstObjectByType<RewardUI>();
            }

            if (rewardUI != null)
            {
                return;
            }

            if (canvasObject == null)
            {
                return;
            }

            GameObject rewardObject = new("RewardUI");
            rewardObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = rewardObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rewardUI = rewardObject.AddComponent<RewardUI>();
        }

        private void EnsureResultUI()
        {
            GameObject canvasObject = GameObject.Find("ClashCanvas");

            if (resultUI == null)
            {
                Transform existing = canvasObject != null ? canvasObject.transform.Find("MinigameResultUI") : null;
                resultUI = existing != null ? existing.GetComponent<MinigameResultUI>() : FindFirstObjectByType<MinigameResultUI>();
            }

            if (resultUI != null)
            {
                return;
            }

            if (canvasObject == null)
            {
                return;
            }

            GameObject resultObject = new("MinigameResultUI");
            resultObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = resultObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            resultUI = resultObject.AddComponent<MinigameResultUI>();
        }

        private void SetBattleHandsVisible(bool visible)
        {
            GameObject battleHands = GameObject.Find("BattleHands");
            if (battleHands == null)
            {
                return;
            }

            Graphic[] graphics = battleHands.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                Color color = graphic.color;
                color.a = visible ? 1f : 0f;
                graphic.color = color;
            }
        }

        private void SetClashBackgroundShakeEnabled(bool enabled)
        {
            if (clashBackgroundUI == null)
            {
                return;
            }

            ClashHandShake shake = clashBackgroundUI.GetComponent<ClashHandShake>();
            if (shake != null)
            {
                shake.SetShakeEnabled(enabled);
            }
        }

        private void EnsureRuntimeCards()
        {
            if (availableCards != null && availableCards.Length >= 15)
            {
                return;
            }

            ClashCardConfig[] runtimeCards = new ClashCardConfig[15];
            int index = 0;

            for (int i = 1; i <= 5; i++)
            {
                runtimeCards[index++] = CreateRuntimeCard($"Normal {i:00}", ClashCardRarity.N, 1f);
            }

            for (int i = 1; i <= 5; i++)
            {
                runtimeCards[index++] = CreateRuntimeCard($"Rare {i:00}", ClashCardRarity.R, 1.5f);
            }

            for (int i = 1; i <= 5; i++)
            {
                runtimeCards[index++] = CreateRuntimeCard($"Super Rare {i:00}", ClashCardRarity.SR, 2f);
            }

            availableCards = runtimeCards;
        }

        private static ClashCardConfig CreateRuntimeCard(string displayName, ClashCardRarity rarity, float multiplier)
        {
            ClashCardConfig card = ScriptableObject.CreateInstance<ClashCardConfig>();
            card.name = displayName;
            card.ConfigureRuntime(displayName, rarity, multiplier);
            return card;
        }

        private static float GetMultiplier(ClashCardConfig card)
        {
            return card != null ? card.PowerMultiplier : 1f;
        }
    }
}
