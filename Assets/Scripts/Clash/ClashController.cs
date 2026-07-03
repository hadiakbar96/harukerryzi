using UnityEngine;
using UnityEngine.Events;

namespace Harukerryzi.Clash
{
    public sealed class ClashController : MonoBehaviour
    {
        [Header("Fighters")]
        [SerializeField] private ClashFighterConfig playerConfig;
        [SerializeField] private ClashFighterConfig aiConfig;
        [SerializeField, Min(0f)] private float playerPowerOverride;
        [SerializeField, Min(0f)] private float aiPowerOverride;

        [Header("Inputs")]
        [SerializeField] private MonoBehaviour playerInputSource;
        [SerializeField] private MonoBehaviour aiInputSource;

        [Header("Balance")]
        [SerializeField, Min(0.01f)] private float powerScale = 100f;
        [SerializeField, Min(0f)] private float centerDecayPerSecond = 0.08f;
        [SerializeField] private bool startOnAwake = true;

        [Header("Visuals")]
        [SerializeField] private Transform leftPlayer;
        [SerializeField] private Transform rightPlayer;
        [SerializeField] private Vector3 leftPlayerCenterPosition = new(-2f, 0f, 0f);
        [SerializeField] private Vector3 leftPlayerWinningPosition = new(0f, 0f, 0f);
        [SerializeField] private Vector3 rightPlayerCenterPosition = new(2f, 0f, 0f);
        [SerializeField] private Vector3 rightPlayerWinningPosition = new(0f, 0f, 0f);
        [SerializeField, Min(0f)] private float positionLerpSpeed = 12f;

        [Header("UI")]
        [SerializeField] private ClashBarUI barUI;
        [SerializeField] private ClashHUD hud;

        [Header("Events")]
        [SerializeField] private ClashResultEvent onClashFinished;

        private IClashInput playerInput;
        private IClashInput aiInput;
        private float playerPower;
        private float aiPower;
        private float barValue = 0.5f;
        private bool active;
        private ClashResult result = ClashResult.None;

        public ClashResult Result => result;
        public float BarValue => barValue;
        public ClashResultEvent OnClashFinished => onClashFinished;

        private void Awake()
        {
            playerInput = playerInputSource as IClashInput;
            aiInput = aiInputSource as IClashInput;
            ApplyConfigs();
            ResetClash();
        }

        private void Start()
        {
            if (startOnAwake)
            {
                BeginClash();
            }
        }

        private void Update()
        {
            if (!active)
            {
                UpdateVisuals();
                return;
            }

            ApplyMashInput();
            CheckWin();

            if (!active)
            {
                UpdateVisuals();
                return;
            }

            ApplyCenterDecay();
            CheckWin();
            UpdateVisuals();
        }

        public void Init(float newPlayerPower, float newAiPower)
        {
            playerPower = Mathf.Max(0f, newPlayerPower);
            aiPower = Mathf.Max(0f, newAiPower);
            ResetClash();
            BeginClash();
        }

        public void SetStartOnAwake(bool enabled)
        {
            startOnAwake = enabled;
        }

        public void BeginClash()
        {
            DrainBufferedInput();
            active = true;
            result = ClashResult.None;
            hud?.SetPromptVisible(true);
            hud?.SetResult(result);
        }

        public void ResetClash()
        {
            barValue = 0.5f;
            active = false;
            result = ClashResult.None;
            DrainBufferedInput();
            hud?.SetResult(result);
            UpdateVisuals();
        }

        private void DrainBufferedInput()
        {
            while (playerInput != null && playerInput.ConsumeMash())
            {
            }

            while (aiInput != null && aiInput.ConsumeMash())
            {
            }
        }

        private void ApplyConfigs()
        {
            playerPower = playerPowerOverride > 0f ? playerPowerOverride : GetPower(playerConfig, 1f);
            aiPower = aiPowerOverride > 0f ? aiPowerOverride : GetPower(aiConfig, 1f);

            if (aiInputSource is AIClashInput aiClashInput && aiConfig != null)
            {
                aiClashInput.Configure(aiConfig.AiMashesPerSecond, aiConfig.AiMashRandomness);
            }
        }

        private void ApplyMashInput()
        {
            while (playerInput != null && playerInput.ConsumeMash())
            {
                barValue += playerPower / powerScale;
            }

            while (aiInput != null && aiInput.ConsumeMash())
            {
                barValue -= aiPower / powerScale;
            }

            barValue = Mathf.Clamp01(barValue);
        }

        private void ApplyCenterDecay()
        {
            if (centerDecayPerSecond <= 0f)
            {
                return;
            }

            barValue = Mathf.MoveTowards(barValue, 0.5f, centerDecayPerSecond * Time.deltaTime);
        }

        private void CheckWin()
        {
            if (barValue >= 1f)
            {
                Finish(ClashResult.PlayerWin);
            }
            else if (barValue <= 0f)
            {
                Finish(ClashResult.AiWin);
            }
        }

        private void Finish(ClashResult newResult)
        {
            active = false;
            result = newResult;
            hud?.SetPromptVisible(false);
            hud?.SetResult(result);
            onClashFinished?.Invoke(result);
        }

        private void UpdateVisuals()
        {
            barUI?.SetValue(barValue);

            if (leftPlayer != null)
            {
                Vector3 target = Vector3.Lerp(leftPlayerCenterPosition, leftPlayerWinningPosition, barValue);
                leftPlayer.position = Vector3.Lerp(leftPlayer.position, target, Mathf.Clamp01(positionLerpSpeed * Time.deltaTime));
            }

            if (rightPlayer != null)
            {
                Vector3 target = Vector3.Lerp(rightPlayerWinningPosition, rightPlayerCenterPosition, barValue);
                rightPlayer.position = Vector3.Lerp(rightPlayer.position, target, Mathf.Clamp01(positionLerpSpeed * Time.deltaTime));
            }
        }

        private static float GetPower(ClashFighterConfig config, float fallback)
        {
            return config != null ? config.PowerPerMash : fallback;
        }

        [System.Serializable]
        public sealed class ClashResultEvent : UnityEvent<ClashResult>
        {
        }
    }
}
