using UnityEngine;
using UnityEngine.InputSystem;

namespace Harukerryzi.Clash
{
    public sealed class ClashHandShake : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private InputActionReference mashAction;
        [SerializeField] private Key fallbackKey = Key.Space;
        [SerializeField, Min(0f)] private float shakePerPress = 14f;
        [SerializeField, Min(0f)] private float maxShake = 36f;
        [SerializeField, Min(0f)] private float decayPerSecond = 90f;
        [SerializeField, Min(0f)] private float frequency = 60f;

        private Vector2 restPosition;
        private float intensity;
        private float seed;
        private bool shakeEnabled = true;

        private void Awake()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }

            if (target != null)
            {
                restPosition = target.anchoredPosition;
            }

            seed = Random.value * 1000f;
        }

        private void OnEnable()
        {
            if (mashAction == null)
            {
                return;
            }

            mashAction.action.performed += OnMashPerformed;
            mashAction.action.Enable();
        }

        private void OnDisable()
        {
            if (mashAction != null)
            {
                mashAction.action.performed -= OnMashPerformed;
            }

            if (target != null)
            {
                target.anchoredPosition = restPosition;
            }
        }

        private void Update()
        {
            if (!shakeEnabled)
            {
                return;
            }

            if (mashAction == null && Keyboard.current != null && Keyboard.current[fallbackKey].wasPressedThisFrame)
            {
                AddShake();
            }

            if (target == null)
            {
                return;
            }

            if (intensity <= 0f)
            {
                target.anchoredPosition = restPosition;
                return;
            }

            float time = Time.unscaledTime * frequency;
            Vector2 offset = new(
                (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f,
                (Mathf.PerlinNoise(seed + 31.7f, time) - 0.5f) * 2f
            );

            target.anchoredPosition = restPosition + offset * intensity;
            intensity = Mathf.MoveTowards(intensity, 0f, decayPerSecond * Time.unscaledDeltaTime);
        }

        private void OnMashPerformed(InputAction.CallbackContext context)
        {
            AddShake();
        }

        private void AddShake()
        {
            intensity = Mathf.Min(maxShake, intensity + shakePerPress);
        }

        public void SetShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
            if (!shakeEnabled)
            {
                intensity = 0f;
                if (target != null)
                {
                    target.anchoredPosition = restPosition;
                }
            }
        }

        public void CaptureRestPosition()
        {
            if (target != null)
            {
                restPosition = target.anchoredPosition;
            }
        }
    }
}
