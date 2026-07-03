using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashBackgroundUI : MonoBehaviour
    {
        [SerializeField] private Image targetImage;

        private void LateUpdate()
        {
            FitCover();
        }

        private void Awake()
        {
            EnsureImage();
        }

        public void SetBackground(Sprite sprite)
        {
            EnsureImage();

            if (targetImage == null)
            {
                return;
            }

            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;
            targetImage.preserveAspect = false;
            targetImage.raycastTarget = false;
            FitCover();
        }

        private void EnsureImage()
        {
            if (targetImage != null)
            {
                return;
            }

            targetImage = GetComponent<Image>();
            if (targetImage == null)
            {
                targetImage = gameObject.AddComponent<Image>();
            }
        }

        private void FitCover()
        {
            RectTransform rect = transform as RectTransform;
            RectTransform parentRect = rect != null ? rect.parent as RectTransform : null;
            if (targetImage == null || targetImage.sprite == null || rect == null || parentRect == null)
            {
                return;
            }

            float parentWidth = parentRect.rect.width;
            float parentHeight = parentRect.rect.height;
            if (parentWidth <= 0f || parentHeight <= 0f)
            {
                return;
            }

            float spriteAspect = targetImage.sprite.rect.width / targetImage.sprite.rect.height;
            float parentAspect = parentWidth / parentHeight;
            Vector2 size = parentAspect > spriteAspect
                ? new Vector2(parentWidth, parentWidth / spriteAspect)
                : new Vector2(parentHeight * spriteAspect, parentHeight);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
        }
    }
}
