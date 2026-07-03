using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashIntroUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text countdownText;
        [SerializeField, Min(0f)] private float stepDuration = 0.7f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
        [SerializeField] private UnityEvent onIntroComplete;

        private Coroutine sequence;

        public UnityEvent OnIntroComplete => onIntroComplete;

        public void Play()
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }

            sequence = StartCoroutine(PlaySequence());
        }

        public void HideImmediate()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator PlaySequence()
        {
            gameObject.SetActive(true);
            SetAlpha(1f);

            yield return ShowStep("GET READY");
            yield return ShowStep("3");
            yield return ShowStep("2");
            yield return ShowStep("1");
            yield return ShowStep("GO!");

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            HideImmediate();
            sequence = null;
            onIntroComplete?.Invoke();
        }

        private IEnumerator ShowStep(string text)
        {
            if (countdownText != null)
            {
                countdownText.text = text;
            }

            yield return new WaitForSecondsRealtime(stepDuration);
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.blocksRaycasts = alpha > 0f;
                canvasGroup.interactable = alpha > 0f;
            }
        }
    }
}
