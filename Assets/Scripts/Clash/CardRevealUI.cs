using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class CardRevealUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text playerCardText;
        [SerializeField] private Text aiCardText;
        [SerializeField, Min(0f)] private float revealDuration = 1.2f;

        private Coroutine revealSequence;

        public void Show(ClashCardConfig playerCard, ClashCardConfig aiCard, Action onComplete)
        {
            if (revealSequence != null)
            {
                StopCoroutine(revealSequence);
            }

            gameObject.SetActive(true);
            SetVisible(true);
            SetCardText(playerCardText, "YOU", playerCard);
            SetCardText(aiCardText, "AI", aiCard);
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

        private void SetCardText(Text text, string owner, ClashCardConfig card)
        {
            if (text == null || card == null)
            {
                return;
            }

            text.text = $"{owner}\n{card.Rarity} - {card.DisplayName}\nx{card.PowerMultiplier:0.#}";
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
