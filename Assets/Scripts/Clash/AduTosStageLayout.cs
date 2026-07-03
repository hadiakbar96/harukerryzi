using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class AduTosStageLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform playArea;
        [SerializeField] private RectTransform leftScorePanel;
        [SerializeField] private RectTransform rightScorePanel;
        [SerializeField] private RectTransform clashBar;
        [SerializeField] private RectTransform mashButton;
        [SerializeField] private RectTransform promptText;
        [SerializeField] private RectTransform battleHands;
        [SerializeField, Min(0f)] private float transitionDuration = 0.6f;

        [Header("Full Background")]
        [SerializeField] private Vector2 fullPlayAreaSize = new(1920f, 1080f);
        [SerializeField] private Vector2 fullPlayAreaPosition = Vector2.zero;

        [Header("Clash 1:1")]
        [SerializeField] private Vector2 clashPlayAreaSize = new(1080f, 1080f);
        [SerializeField] private Vector2 clashPlayAreaPosition = Vector2.zero;
        [SerializeField] private Vector2 sidePanelSize = new(420f, 1080f);
        [SerializeField] private float sidePanelOffsetX = 750f;

        private static readonly Color SidePanelColor = new(0.1137f, 0.1686f, 0.1176f, 1f);

        private Coroutine transition;
        private bool animateBattleHands;

        public RectTransform PlayArea
        {
            get
            {
                AutoWireMissingReferences();
                return playArea;
            }
        }

        private void Awake()
        {
            AutoWireMissingReferences();
        }

        public void ShowFullBackgroundImmediate()
        {
            StopTransition();
            ApplyFullBackground(1f);
        }

        public void TransitionToClashSquare()
        {
            StartTransition(false);
        }

        public void TransitionToFullBackground()
        {
            StartTransition(true);
        }

        public void SetBattleHandsAnimationEnabled(bool enabled)
        {
            animateBattleHands = enabled;
        }

        private void StartTransition(bool toFull)
        {
            StopTransition();
            transition = StartCoroutine(TransitionRoutine(toFull));
        }

        private IEnumerator TransitionRoutine(bool toFull)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, transitionDuration));
                t = t * t * (3f - 2f * t);

                if (toFull)
                {
                    ApplyFullBackground(t);
                }
                else
                {
                    ApplyClashSquare(t);
                }

                yield return null;
            }

            if (toFull)
            {
                ApplyFullBackground(1f);
            }
            else
            {
                ApplyClashSquare(1f);
            }

            transition = null;
        }

        private void ApplyFullBackground(float t)
        {
            SetPlayArea(Vector2.Lerp(clashPlayAreaSize, fullPlayAreaSize, t), Vector2.Lerp(clashPlayAreaPosition, fullPlayAreaPosition, t));
            SetSidePanel(leftScorePanel, Vector2.Lerp(sidePanelSize, Vector2.zero, t), Vector2.Lerp(new Vector2(-sidePanelOffsetX, 0f), new Vector2(-fullPlayAreaSize.x * 0.5f, 0f), t), 1f - t);
            SetSidePanel(rightScorePanel, Vector2.Lerp(sidePanelSize, Vector2.zero, t), Vector2.Lerp(new Vector2(sidePanelOffsetX, 0f), new Vector2(fullPlayAreaSize.x * 0.5f, 0f), t), 1f - t);
            SetClashElementsVisible(1f - t);
        }

        private void ApplyClashSquare(float t)
        {
            SetPlayArea(Vector2.Lerp(fullPlayAreaSize, clashPlayAreaSize, t), Vector2.Lerp(fullPlayAreaPosition, clashPlayAreaPosition, t));
            SetSidePanel(leftScorePanel, Vector2.Lerp(Vector2.zero, sidePanelSize, t), Vector2.Lerp(new Vector2(-fullPlayAreaSize.x * 0.5f, 0f), new Vector2(-sidePanelOffsetX, 0f), t), t);
            SetSidePanel(rightScorePanel, Vector2.Lerp(Vector2.zero, sidePanelSize, t), Vector2.Lerp(new Vector2(fullPlayAreaSize.x * 0.5f, 0f), new Vector2(sidePanelOffsetX, 0f), t), t);
            SetClashElementsVisible(t);
        }

        private void SetPlayArea(Vector2 size, Vector2 position)
        {
            if (playArea == null)
            {
                return;
            }

            SetRect(playArea, size, position);
        }

        private void SetSidePanel(RectTransform panel, Vector2 size, Vector2 position, float alpha)
        {
            if (panel == null)
            {
                return;
            }

            SetRect(panel, size, position);
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = alpha;
                image.color = color;
            }

            Text[] texts = panel.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                Color color = text.color;
                color.a = alpha;
                text.color = color;
            }
        }

        private void SetClashElementsVisible(float alpha)
        {
            SetGraphicAlpha(clashBar, alpha);
            SetGraphicAlpha(mashButton, alpha);
            SetGraphicAlpha(promptText, alpha);
            if (animateBattleHands)
            {
                SetGraphicAlpha(battleHands, Mathf.Max(0.35f, alpha));
            }
        }

        private static void SetGraphicAlpha(RectTransform root, float alpha)
        {
            if (root == null)
            {
                return;
            }

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private void StopTransition()
        {
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }
        }

        private void AutoWireMissingReferences()
        {
            Transform canvas = transform;
            playArea ??= FindRect(canvas, "Mockup_PlayArea_1x1");
            if (playArea == null)
            {
                playArea = CreateRuntimePanel(canvas, "Runtime_PlayArea", new Color(0.52f, 0.52f, 0.52f, 1f));
            }

            if (playArea.GetComponent<RectMask2D>() == null)
            {
                playArea.gameObject.AddComponent<RectMask2D>();
            }

            leftScorePanel ??= FindRect(canvas, "Mockup_LeftScorePanel");
            if (leftScorePanel == null)
            {
                leftScorePanel = CreateRuntimePanel(canvas, "Runtime_LeftScorePanel", SidePanelColor);
            }

            rightScorePanel ??= FindRect(canvas, "Mockup_RightScorePanel");
            if (rightScorePanel == null)
            {
                rightScorePanel = CreateRuntimePanel(canvas, "Runtime_RightScorePanel", SidePanelColor);
            }

            clashBar ??= FindRect(canvas, "ClashBar");
            mashButton ??= FindRect(canvas, "MashButton");
            promptText ??= FindRect(canvas, "PromptText");
            battleHands ??= FindRect(canvas, "BattleHands");

            playArea.transform.SetAsFirstSibling();
            leftScorePanel.transform.SetSiblingIndex(1);
            rightScorePanel.transform.SetSiblingIndex(2);
            if (battleHands != null)
            {
                battleHands.SetAsLastSibling();
            }
        }

        private static RectTransform CreateRuntimePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            Image image = panel.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform FindRect(Transform parent, string name)
        {
            Transform found = FindDeepChild(parent, name);
            return found as RectTransform;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
