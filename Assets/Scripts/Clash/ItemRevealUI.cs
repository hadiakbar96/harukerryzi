using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ItemRevealUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text playerItemText;
        [SerializeField] private Text aiItemText;
        [SerializeField, Min(0f)] private float revealDuration = 1.2f;

        private Coroutine revealSequence;

        public void Show(ClashItemConfig playerItem, ClashItemConfig aiItem, Action onComplete)
        {
            if (revealSequence != null)
            {
                StopCoroutine(revealSequence);
            }

            gameObject.SetActive(true);
            SetVisible(true);
            SetItemText(playerItemText, "YOU", playerItem);
            SetItemText(aiItemText, "AI", aiItem);
            revealSequence = StartCoroutine(CompleteAfterDelay(onComplete));
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        private IEnumerator CompleteAfterDelay(Action onComplete)
        {
            yield return new WaitForSecondsRealtime(revealDuration);
            Hide();
            revealSequence = null;
            onComplete?.Invoke();
        }

        private void SetItemText(Text text, string owner, ClashItemConfig item)
        {
            if (text == null || item == null)
            {
                return;
            }

            text.text = $"{owner}\n{item.Rarity} - {item.DisplayName}\nx{item.PowerMultiplier:0.#}";
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
