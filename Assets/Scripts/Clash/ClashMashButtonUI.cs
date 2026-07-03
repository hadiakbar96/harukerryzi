using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashMashButtonUI : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite smashSprite;
        [SerializeField] private Text labelText;
        [SerializeField] private InputActionReference mashAction;
        [SerializeField, Min(0f)] private float smashDuration = 0.08f;
        [SerializeField] private Key fallbackKey = Key.Space;
        [SerializeField] private bool mashEnabled;

        private float smashTimer;

        private void OnEnable()
        {
            EnsureLabel();

            if (mashAction == null)
            {
                SetDefault();
                return;
            }

            mashAction.action.performed += OnMashPerformed;
            mashAction.action.Enable();
            SetDefault();
        }

        private void OnDisable()
        {
            if (mashAction != null)
            {
                mashAction.action.performed -= OnMashPerformed;
            }
        }

        private void Update()
        {
            if (mashEnabled && mashAction == null && Keyboard.current != null && Keyboard.current[fallbackKey].wasPressedThisFrame)
            {
                ShowSmash();
            }

            if (smashTimer <= 0f)
            {
                return;
            }

            smashTimer -= Time.unscaledDeltaTime;
            if (smashTimer <= 0f)
            {
                SetDefault();
            }
        }

        private void OnMashPerformed(InputAction.CallbackContext context)
        {
            if (!mashEnabled)
            {
                return;
            }

            ShowSmash();
        }

        public void SetMashEnabled(bool enabled)
        {
            mashEnabled = enabled;
            if (!mashEnabled)
            {
                smashTimer = 0f;
                SetDefault();
            }
        }

        private void ShowSmash()
        {
            if (targetImage != null && smashSprite != null)
            {
                targetImage.sprite = smashSprite;
                targetImage.SetNativeSize();
            }

            smashTimer = smashDuration;
        }

        private void SetDefault()
        {
            if (targetImage != null && defaultSprite != null)
            {
                targetImage.sprite = defaultSprite;
                targetImage.SetNativeSize();
            }
        }

        private void EnsureLabel()
        {
            if (labelText == null)
            {
                Transform existing = transform.Find("SpaceLabel");
                labelText = existing != null ? existing.GetComponent<Text>() : null;
            }

            if (labelText == null)
            {
                GameObject labelObject = new("SpaceLabel");
                labelObject.transform.SetParent(transform, false);
                RectTransform rect = labelObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                labelText = labelObject.AddComponent<Text>();
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.fontStyle = FontStyle.Bold;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.fontSize = 18;
                labelText.color = new Color(0.18f, 0.18f, 0.12f, 1f);
                labelText.raycastTarget = false;
            }

            labelText.text = "Space";
            RectTransform labelRect = labelText.transform as RectTransform;
            if (labelRect != null)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                labelRect.anchoredPosition = Vector2.zero;
            }
        }
    }
}
