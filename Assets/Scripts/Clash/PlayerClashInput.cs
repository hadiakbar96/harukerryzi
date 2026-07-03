using UnityEngine;
using UnityEngine.InputSystem;

namespace Harukerryzi.Clash
{
    public sealed class PlayerClashInput : MonoBehaviour, IClashInput
    {
        [SerializeField] private InputActionReference mashAction;
        [SerializeField] private Key fallbackKey = Key.Space;

        private int bufferedMashes;

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
            if (mashAction == null)
            {
                return;
            }

            mashAction.action.performed -= OnMashPerformed;
            mashAction.action.Disable();
            bufferedMashes = 0;
        }

        private void Update()
        {
            if (mashAction != null || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[fallbackKey].wasPressedThisFrame)
            {
                bufferedMashes++;
            }
        }

        public bool ConsumeMash()
        {
            if (bufferedMashes <= 0)
            {
                return false;
            }

            bufferedMashes--;
            return true;
        }

        private void OnMashPerformed(InputAction.CallbackContext context)
        {
            bufferedMashes++;
        }
    }
}
