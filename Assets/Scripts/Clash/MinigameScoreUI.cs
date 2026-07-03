using UnityEngine;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class MinigameScoreUI : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private Text playerScoreText;
        [SerializeField] private Text aiScoreText;

        public void SetScore(int playerScore, int aiScore, int roundNumber)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Round {roundNumber}";
            }

            if (playerScoreText != null)
            {
                playerScoreText.text = playerScore.ToString();
            }

            if (aiScoreText != null)
            {
                aiScoreText.text = aiScore.ToString();
            }
        }
    }
}
