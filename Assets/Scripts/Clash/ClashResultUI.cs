using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashResultUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text resultText;
        [SerializeField] private Button retryButton;
        [SerializeField] private UnityEvent onRetry;

        public UnityEvent OnRetry => onRetry;

        private void Awake()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(Retry);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Retry);
            }
        }

        public void Show(ClashResult result)
        {
            gameObject.SetActive(true);

            if (resultText != null)
            {
                resultText.text = result == ClashResult.PlayerWin ? "YOU WIN" : "YOU LOSE";
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        private void Retry()
        {
            onRetry?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
