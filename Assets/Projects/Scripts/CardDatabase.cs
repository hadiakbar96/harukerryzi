using UnityEngine;

/// <summary>
/// ScriptableObject that holds references to all cards in the game.
/// Both PackController and CollectionUIController reference this asset
/// so card data is shared and can be looked up by name.
///
/// SETUP:
///   1) Right-click in Project → Create → Cards → Card Database
///   2) Drag all your Card entries into the appropriate rarity arrays
///   3) Assign this asset to PackController.cardDatabase and CollectionUIController.cardDatabase
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "Cards/Card Database")]
public class CardDatabase : ScriptableObject
{
    [Header("All Cards By Rarity")]
    public Card[] normalCards;
    public Card[] rareCards;
    public Card[] superRareCards;

    /// <summary>
    /// Returns ALL cards across every rarity tier.
    /// </summary>
    public Card[] GetAllCards()
    {
        int total = (normalCards != null ? normalCards.Length : 0)
                  + (rareCards != null ? rareCards.Length : 0)
                  + (superRareCards != null ? superRareCards.Length : 0);

        Card[] all = new Card[total];
        int idx = 0;

        if (normalCards != null)
            foreach (var c in normalCards) all[idx++] = c;
        if (rareCards != null)
            foreach (var c in rareCards) all[idx++] = c;
        if (superRareCards != null)
            foreach (var c in superRareCards) all[idx++] = c;

        return all;
    }

    /// <summary>
    /// Look up a card by name. Returns null if not found.
    /// </summary>
    public Card FindCardByName(string cardName)
    {
        if (normalCards != null)
            foreach (var c in normalCards)
                if (c.cardName == cardName) return c;

        if (rareCards != null)
            foreach (var c in rareCards)
                if (c.cardName == cardName) return c;

        if (superRareCards != null)
            foreach (var c in superRareCards)
                if (c.cardName == cardName) return c;

        return null;
    }

    /// <summary>
    /// Returns all cards of a specific rarity.
    /// </summary>
    public Card[] GetCardsByRarity(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Normal:    return normalCards ?? new Card[0];
            case CardRarity.Rare:      return rareCards ?? new Card[0];
            case CardRarity.SuperRare: return superRareCards ?? new Card[0];
            default:                   return new Card[0];
        }
    }

    /// <summary>
    /// Returns a random card from the given rarity tier.
    /// </summary>
    public Card GetRandomCardOfRarity(CardRarity rarity)
    {
        Card[] pool = GetCardsByRarity(rarity);
        if (pool.Length == 0) return null;
        return pool[Random.Range(0, pool.Length)];
    }
}
