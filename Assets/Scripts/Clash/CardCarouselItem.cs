using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class CardCarouselItem : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text label;

        private ClashCardConfig card;

        public RectTransform RectTransform => transform as RectTransform;
        public ClashCardConfig Card => card;

        public void ConfigureView(Image newBackgroundImage, Text newLabel)
        {
            backgroundImage = newBackgroundImage;
            label = newLabel;
        }

        public void SetCard(ClashCardConfig newCard)
        {
            card = newCard;

            if (label != null && card != null)
            {
                label.text = $"{card.Rarity}\n{card.DisplayName}\nx{card.PowerMultiplier:0.#}";
            }

            if (backgroundImage != null && card != null)
            {
                backgroundImage.color = GetColor(card.Rarity);
            }
        }

        private static Color GetColor(ClashCardRarity rarity)
        {
            return rarity switch
            {
                ClashCardRarity.N => new Color(0.72f, 0.72f, 0.72f, 1f),
                ClashCardRarity.R => new Color(0.35f, 0.7f, 1f, 1f),
                ClashCardRarity.SR => new Color(1f, 0.65f, 0.18f, 1f),
                _ => Color.white
            };
        }
    }
}
