using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashBackgroundUI : MonoBehaviour
    {
        [SerializeField] private Image targetImage;

        public void SetBackground(Sprite sprite)
        {
            EnsureImage();

            if (targetImage == null)
            {
                return;
            }

            if (sprite != null)
            {
                targetImage.sprite = sprite;
                targetImage.enabled = true;
            }
            targetImage.preserveAspect = false;
            targetImage.raycastTarget = false;
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
    }
}
