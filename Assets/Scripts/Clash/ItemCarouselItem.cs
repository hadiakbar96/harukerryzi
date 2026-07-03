using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ItemCarouselItem : MonoBehaviour
    {
        [SerializeField] private Image artworkImage;

        private ClashItemConfig item;

        public RectTransform RectTransform => transform as RectTransform;
        public ClashItemConfig Item => item;

        public void ConfigureView(Image newArtworkImage)
        {
            artworkImage = newArtworkImage;
        }

        public void SetItem(ClashItemConfig newItem)
        {
            item = newItem;

            if (artworkImage != null)
            {
                artworkImage.sprite = item != null ? item.Artwork : null;
                artworkImage.preserveAspect = true;
                artworkImage.color = item != null && item.Artwork != null ? Color.white : Color.clear;
            }
        }
    }
}
