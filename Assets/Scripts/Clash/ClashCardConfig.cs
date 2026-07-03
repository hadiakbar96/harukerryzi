using UnityEngine;

namespace Harukerryzi.Clash
{
    [CreateAssetMenu(fileName = "ClashCard", menuName = "Clash/Card Config")]
    public sealed class ClashCardConfig : ScriptableObject
    {
        [SerializeField] private string displayName = "Card";
        [SerializeField] private ClashCardRarity rarity;
        [SerializeField, Min(0f)] private float powerMultiplier = 1f;
        [SerializeField] private Sprite artwork;

        public string DisplayName => displayName;
        public ClashCardRarity Rarity => rarity;
        public float PowerMultiplier => powerMultiplier;
        public Sprite Artwork => artwork;

        public void ConfigureRuntime(string newDisplayName, ClashCardRarity newRarity, float newPowerMultiplier)
        {
            displayName = newDisplayName;
            rarity = newRarity;
            powerMultiplier = Mathf.Max(0f, newPowerMultiplier);
        }
    }
}
