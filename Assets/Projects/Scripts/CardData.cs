using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    public string cardName;

    public Sprite artwork;

    public Rarity rarity;
}

public enum Rarity
{
    Normal,
    Rare,
    SuperRare,
}