using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class ClashHUD : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text resultText;
        [SerializeField] private string mashPrompt = "Mash Spacebar";

        private void Awake()
        {
            SetPromptVisible(true);
            SetResult(ClashResult.None);
        }

        public void SetPromptVisible(bool visible)
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(visible);
                promptText.text = mashPrompt;
            }
        }

        public void SetResult(ClashResult result)
        {
            if (resultText == null)
            {
                return;
            }

            resultText.gameObject.SetActive(result != ClashResult.None);
            resultText.text = result switch
            {
                ClashResult.PlayerWin => "You Win!",
                ClashResult.AiWin => "You Lose!",
                _ => string.Empty
            };
        }
    }
}
