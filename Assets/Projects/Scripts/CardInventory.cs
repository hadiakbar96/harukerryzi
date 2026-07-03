using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static class that persists collected cards across scenes using PlayerPrefs.
/// Cards are stored as a JSON dictionary of { cardName → count }.
///
/// Usage:
///   CardInventory.AddCard("Amulet");         // +1
///   CardInventory.GetCount("Amulet");         // → 1
///   CardInventory.RemoveCards("Amulet", 1);   // → 0
///   var all = CardInventory.GetAllEntries();  // → Dictionary<string, int>
/// </summary>
public static class CardInventory
{
    private const string PREFS_KEY = "CardInventory_Data";

    // ═══════════════════════════════════════════════════════════════
    //  Internal Data
    // ═══════════════════════════════════════════════════════════════

    [System.Serializable]
    private class InventoryData
    {
        public List<string> cardNames = new List<string>();
        public List<int>    counts    = new List<int>();
    }

    private static Dictionary<string, int> _inventory;

    // ═══════════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Add one copy of a card (by name) to the inventory.
    /// </summary>
    public static void AddCard(string cardName)
    {
        EnsureLoaded();

        if (_inventory.ContainsKey(cardName))
            _inventory[cardName]++;
        else
            _inventory[cardName] = 1;

        Save();
    }

    /// <summary>
    /// Add one copy of a Card object to the inventory.
    /// </summary>
    public static void AddCard(Card card)
    {
        if (card == null) return;
        AddCard(card.cardName);
    }

    /// <summary>
    /// Remove a number of copies of a card from the inventory.
    /// Returns true if successful, false if not enough copies.
    /// </summary>
    public static bool RemoveCards(string cardName, int count)
    {
        EnsureLoaded();

        if (!_inventory.ContainsKey(cardName) || _inventory[cardName] < count)
            return false;

        _inventory[cardName] -= count;
        if (_inventory[cardName] <= 0)
            _inventory.Remove(cardName);

        Save();
        return true;
    }

    /// <summary>
    /// Get the count of a specific card.
    /// </summary>
    public static int GetCount(string cardName)
    {
        EnsureLoaded();
        return _inventory.ContainsKey(cardName) ? _inventory[cardName] : 0;
    }

    /// <summary>
    /// Get all card entries as a dictionary (cardName → count).
    /// Returns a copy so callers can't corrupt the data.
    /// </summary>
    public static Dictionary<string, int> GetAllEntries()
    {
        EnsureLoaded();
        return new Dictionary<string, int>(_inventory);
    }

    /// <summary>
    /// Clears the entire inventory (useful for testing/debugging).
    /// </summary>
    public static void ClearAll()
    {
        _inventory = new Dictionary<string, int>();
        Save();
    }

    /// <summary>
    /// Force reload from PlayerPrefs (useful after scene changes).
    /// </summary>
    public static void ForceReload()
    {
        _inventory = null;
        EnsureLoaded();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Persistence (PlayerPrefs + JSON)
    // ═══════════════════════════════════════════════════════════════

    private static void EnsureLoaded()
    {
        if (_inventory != null) return;

        _inventory = new Dictionary<string, int>();

        if (PlayerPrefs.HasKey(PREFS_KEY))
        {
            string json = PlayerPrefs.GetString(PREFS_KEY);
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);

            if (data != null && data.cardNames != null)
            {
                for (int i = 0; i < data.cardNames.Count; i++)
                {
                    string name = data.cardNames[i];
                    int count = (i < data.counts.Count) ? data.counts[i] : 1;
                    if (count > 0)
                        _inventory[name] = count;
                }
            }
        }
    }

    private static void Save()
    {
        InventoryData data = new InventoryData();

        foreach (var kvp in _inventory)
        {
            data.cardNames.Add(kvp.Key);
            data.counts.Add(kvp.Value);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }
}
