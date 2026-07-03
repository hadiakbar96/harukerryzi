using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class AduTosEntranceUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image mcHandImage;
        [SerializeField] private Image enemyHandImage;
        [SerializeField] private RectTransform mcHand;
        [SerializeField] private RectTransform enemyHand;
        [SerializeField, Min(0f)] private float duration = 0.95f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.12f;
        [SerializeField, Min(0f)] private float shakeAmount = 18f;
        [SerializeField] private Vector2 mcStartPosition = new(-980f, -520f);
        [SerializeField] private Vector2 mcTargetPosition = new(-390f, -90f);
        [SerializeField] private Vector2 enemyStartPosition = new(980f, -520f);
        [SerializeField] private Vector2 enemyTargetPosition = new(390f, -90f);
        [SerializeField] private Vector2 handSize = new(540f, 960f);
        [SerializeField] private Vector2 clashHandsSize = new(1800f, 1800f);
        [SerializeField, Min(0f)] private float mcTargetScale = 1.18f;
        [SerializeField, Min(0f)] private float enemyTargetScale = 1.08f;
        [SerializeField, Min(0f)] private float betweenShotsDelay = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioClip handAppearSfx;

        private Coroutine sequence;

        public RectTransform ClashHandsRect => mcHand;

        private void Awake()
        {
            EnsureReferences();
            HideImmediate();
        }

        public void Play(Sprite mcSprite, Sprite enemySprite, Action onComplete)
        {
            Play(mcSprite, enemySprite, 1f, enemyStartPosition, enemyTargetPosition, onComplete);
        }

        public void Play(Sprite mcSprite, Sprite enemySprite, float enemyEntranceScale, Action onComplete)
        {
            Play(mcSprite, enemySprite, enemyEntranceScale, enemyStartPosition, enemyTargetPosition, onComplete);
        }

        public void Play(Sprite mcSprite, Sprite enemySprite, float enemyEntranceScale, Vector2 enemyEntranceStartPosition, Action onComplete)
        {
            Play(mcSprite, enemySprite, enemyEntranceScale, enemyEntranceStartPosition, enemyTargetPosition, onComplete);
        }

        public void Play(Sprite mcSprite, Sprite enemySprite, float enemyEntranceScale, Vector2 enemyEntranceStartPosition, Vector2 enemyEntranceTargetPosition, Action onComplete)
        {
            EnsureReferences();

            if (sequence != null)
            {
                StopCoroutine(sequence);
            }

            sequence = StartCoroutine(PlayRoutine(mcSprite, enemySprite, enemyEntranceScale, enemyEntranceStartPosition, enemyEntranceTargetPosition, onComplete));
        }

        public void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            SetGraphicsVisible(false);
            SetClashHandsShakeEnabled(false);
        }

        public void ShowClashHands(Sprite clashSprite, float scale = 1f)
        {
            EnsureReferences();

            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            SetSprite(mcHandImage, clashSprite);
            SetSprite(enemyHandImage, null);
            mcHand.sizeDelta = clashHandsSize * Mathf.Max(0.1f, scale);
            SetHand(mcHand, Vector2.zero, Vector3.one);
            EnsureClashHandsShake(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            SetGraphicVisible(mcHandImage, clashSprite != null);
            SetGraphicVisible(enemyHandImage, false);
        }

        public void BakeEditorPreview(Sprite mcSprite, Sprite enemySprite)
        {
            EnsureReferences();
            SetSprite(mcHandImage, mcSprite);
            SetSprite(enemyHandImage, enemySprite);
            SetHand(mcHand, mcTargetPosition, Vector3.one * mcTargetScale);
            SetHand(enemyHand, enemyTargetPosition, Vector3.one * enemyTargetScale);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            SetGraphicVisible(mcHandImage, mcSprite != null);
            SetGraphicVisible(enemyHandImage, enemySprite != null);
        }

        public void SetClashHandsShakeEnabled(bool enabled)
        {
            ClashHandShake shake = mcHand != null ? mcHand.GetComponent<ClashHandShake>() : null;
            if (shake != null)
            {
                shake.SetShakeEnabled(enabled);
            }
        }

        private IEnumerator PlayRoutine(Sprite mcSprite, Sprite enemySprite, float enemyEntranceScale, Vector2 enemyEntranceStartPosition, Vector2 enemyEntranceTargetPosition, Action onComplete)
        {
            enemyEntranceScale = Mathf.Max(0.1f, enemyEntranceScale);
            SetSprite(mcHandImage, mcSprite);
            SetSprite(enemyHandImage, enemySprite);
            mcHand.sizeDelta = handSize;
            enemyHand.sizeDelta = handSize;
            SetHand(mcHand, mcStartPosition, Vector3.one * 0.86f);
            SetHand(enemyHand, enemyEntranceStartPosition, Vector3.one * 0.86f);
            SetGraphicVisible(mcHandImage, false);
            SetGraphicVisible(enemyHandImage, false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            yield return PlayHandShot(mcHandImage, mcHand, mcStartPosition, mcTargetPosition, mcTargetScale);
            SetGraphicVisible(mcHandImage, false);

            if (betweenShotsDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(betweenShotsDelay);
            }

            yield return PlayHandShot(enemyHandImage, enemyHand, enemyEntranceStartPosition, enemyEntranceTargetPosition, enemyTargetScale * enemyEntranceScale);
            SetGraphicVisible(enemyHandImage, false);

            HideImmediate();
            sequence = null;
            onComplete?.Invoke();
        }

        private IEnumerator PlayHandShot(Image image, RectTransform hand, Vector2 start, Vector2 target, float targetScale)
        {
            GameAudio.PlaySfx(handAppearSfx);
            SetGraphicVisible(image, true);
            SetHand(hand, start, Vector3.one * 0.86f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
                float eased = BackOut(t);
                SetHand(hand, Vector2.LerpUnclamped(start, target, eased), Vector3.one * Mathf.LerpUnclamped(0.86f, targetScale, eased));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float strength = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, shakeDuration));
                Vector2 offset = UnityEngine.Random.insideUnitCircle * shakeAmount * strength;
                SetHand(hand, target + offset, Vector3.one * targetScale);
                yield return null;
            }
        }

        private void SetGraphicsVisible(bool visible)
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                Color color = graphic.color;
                color.a = visible ? 1f : 0f;
                graphic.color = color;
                graphic.raycastTarget = false;
            }
        }

        private void EnsureClashHandsShake(bool enabled)
        {
            if (mcHand == null)
            {
                return;
            }

            ClashHandShake shake = mcHand.GetComponent<ClashHandShake>();
            if (shake == null)
            {
                shake = mcHand.gameObject.AddComponent<ClashHandShake>();
            }

            shake.CaptureRestPosition();
            shake.SetShakeEnabled(enabled);
        }

        private void SetGraphicVisible(Graphic graphic, bool visible)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = visible ? 1f : 0f;
            graphic.color = color;
            graphic.raycastTarget = false;
        }

        private void EnsureReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            }

            if (mcHand == null)
            {
                mcHandImage = CreateHandImage("MCEntranceHand", out mcHand);
            }

            if (enemyHand == null)
            {
                enemyHandImage = CreateHandImage("EnemyEntranceHand", out enemyHand);
            }
        }

        private Image CreateHandImage(string name, out RectTransform rect)
        {
            GameObject hand = new(name);
            hand.transform.SetParent(transform, false);
            rect = hand.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = handSize;
            Image image = hand.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void SetSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void SetHand(RectTransform hand, Vector2 position, Vector3 scale)
        {
            if (hand == null)
            {
                return;
            }

            hand.anchoredPosition = position;
            hand.localScale = scale;
        }

        private static float BackOut(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
