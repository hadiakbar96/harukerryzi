using System.Collections.Generic;
using UnityEngine;

namespace Harukerryzi.Clash
{
    [CreateAssetMenu(fileName = "ClashItem", menuName = "Clash/Item Config")]
    public sealed class ClashItemConfig : ScriptableObject
    {
        private static readonly Dictionary<string, ClashItemConfig> s_registry = new();

        [SerializeField] private string itemId;
        [SerializeField] private string displayName = "Item";
        [SerializeField] private ClashItemRarity rarity;
        [SerializeField, Min(0f)] private float powerMultiplier = 1f;
        [SerializeField] private Sprite artwork;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public ClashItemRarity Rarity => rarity;
        public float PowerMultiplier => powerMultiplier;
        public Sprite Artwork => artwork;

        public void ConfigureRuntime(string newItemId, string newDisplayName, ClashItemRarity newRarity, float newPowerMultiplier)
        {
            itemId = newItemId;
            displayName = newDisplayName;
            rarity = newRarity;
            powerMultiplier = Mathf.Max(0f, newPowerMultiplier);
        }

        public void ConfigureRuntime(Card card, float newPowerMultiplier)
        {
            itemId = card.cardName;
            displayName = card.cardName;
            artwork = card.artwork;
            rarity = card.rarity switch
            {
                CardRarity.Normal => ClashItemRarity.N,
                CardRarity.Rare => ClashItemRarity.R,
                CardRarity.SuperRare => ClashItemRarity.SR,
                _ => ClashItemRarity.N
            };
            powerMultiplier = Mathf.Max(0f, newPowerMultiplier);
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                s_registry[itemId] = this;
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                s_registry.Remove(itemId);
            }
        }

        public static ClashItemConfig FindByItemId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            s_registry.TryGetValue(id, out ClashItemConfig config);
            return config;
        }

        public static IEnumerable<ClashItemConfig> GetAllRegistered()
        {
            return s_registry.Values;
        }
    }
}
