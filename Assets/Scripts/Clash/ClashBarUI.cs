using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Slider slider;
        [SerializeField] private RectTransform marker;
        [SerializeField] private RectTransform movingTransform;
        [SerializeField] private float markerLeftX = -280f;
        [SerializeField] private float markerRightX = 280f;

        public void SetValue(float value)
        {
            value = Mathf.Clamp01(value);

            if (fillImage != null)
            {
                fillImage.fillAmount = value;
            }

            if (slider != null)
            {
                slider.value = value;
            }

            if (marker != null)
            {
                Vector2 position = marker.anchoredPosition;
                position.x = Mathf.Lerp(markerLeftX, markerRightX, value);
                marker.anchoredPosition = position;
            }

            if (movingTransform != null)
            {
                Vector2 position = movingTransform.anchoredPosition;
                position.x = Mathf.Lerp(markerLeftX, markerRightX, value);
                movingTransform.anchoredPosition = position;
            }
        }
    }
}
