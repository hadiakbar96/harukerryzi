using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashWinFxUI : MonoBehaviour
    {
        [SerializeField] private RectTransform pushTarget;
        [SerializeField] private Image flashImage;
        [SerializeField, Range(0.01f, 1f)] private float slowMoScale = 0.15f;
        [SerializeField, Min(0f)] private float pushDistance = 280f;
        [SerializeField, Min(0f)] private float pushDuration = 1.15f;
        [SerializeField, Min(1f)] private float matchWinDurationMultiplier = 1.45f;
        [SerializeField, Min(0f)] private float flashDuration = 0.75f;
        [SerializeField, Min(0f)] private float shakeAmount = 22f;
        [SerializeField, Min(1f)] private float matchWinMultiplier = 1f;
        [SerializeField] private Color flashColor = new(1f, 1f, 1f, 0.82f);
        private Coroutine sequence;
        private Vector2 pushRestPosition;

        private void Awake()
        {
            EnsureReferences();
            HideFx();
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        public void SetPushTarget(RectTransform target)
        {
            pushTarget = target;
            CaptureRestPosition();
        }

        public void Play(ClashResult result, bool isMatchDeciding, Action onComplete)
        {
            EnsureReferences();

            if (sequence != null)
            {
                StopCoroutine(sequence);
                Time.timeScale = 1f;
            }

            sequence = StartCoroutine(PlayRoutine(result, isMatchDeciding, onComplete));
        }

        private IEnumerator PlayRoutine(ClashResult result, bool isMatchDeciding, Action onComplete)
        {
            float originalTimeScale = Time.timeScale;
            float strength = isMatchDeciding ? matchWinMultiplier : 1f;
            float durationMultiplier = isMatchDeciding ? matchWinDurationMultiplier : 1f;
            float effectivePushDuration = pushDuration * durationMultiplier;
            float effectiveFlashDuration = flashDuration * durationMultiplier;
            float direction = result == ClashResult.PlayerWin ? 1f : -1f;

            Time.timeScale = slowMoScale;

            float elapsed = 0f;
            while (elapsed < Mathf.Max(effectivePushDuration, effectiveFlashDuration))
            {
                elapsed += Time.unscaledDeltaTime;
                float pushT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, effectivePushDuration));
                float flashT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, effectiveFlashDuration));

                ApplyPush(direction, pushT, strength);
                ApplyFlash(direction, flashT, strength);
                yield return null;
            }

            ApplyPush(direction, 1f, strength);
            HideFx();
            Time.timeScale = originalTimeScale <= 0f ? 1f : originalTimeScale;
            sequence = null;
            onComplete?.Invoke();
        }

        private void ApplyPush(float direction, float t, float strength)
        {
            if (pushTarget == null)
            {
                return;
            }

            float eased = t * t * (3f - 2f * t);
            float shove = Mathf.Lerp(0f, pushDistance * strength, eased);
            float shakeFade = Mathf.Pow(1f - t, 2f);
            Vector2 shake = UnityEngine.Random.insideUnitCircle * shakeAmount * strength * shakeFade;
            pushTarget.anchoredPosition = pushRestPosition + new Vector2(direction * shove, 0f) + shake;
        }

        public void ResetPushTarget()
        {
            if (pushTarget != null)
            {
                pushTarget.anchoredPosition = pushRestPosition;
            }
        }

        private void CaptureRestPosition()
        {
            if (pushTarget != null)
            {
                pushRestPosition = pushTarget.anchoredPosition;
            }
        }

        private void ApplyFlash(float direction, float t, float strength)
        {
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(true);
                flashImage.rectTransform.anchorMin = Vector2.zero;
                flashImage.rectTransform.anchorMax = Vector2.one;
                flashImage.rectTransform.offsetMin = Vector2.zero;
                flashImage.rectTransform.offsetMax = Vector2.zero;
                Color color = flashColor;
                color.a = Mathf.Clamp01(color.a * Mathf.Pow(1f - t, 1.5f) * strength);
                flashImage.color = color;
            }
        }

        private void EnsureReferences()
        {
            if (flashImage == null)
            {
                GameObject flash = new("WinFlash");
                flash.transform.SetParent(transform, false);
                RectTransform rect = flash.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                flashImage = flash.AddComponent<Image>();
                flashImage.raycastTarget = false;
            }

        }

        private void HideFx()
        {
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(false);
            }

        }
    }
}
