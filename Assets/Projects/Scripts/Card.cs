using UnityEngine;

[System.Serializable]
public class Card
{
    public string cardName;
    public Sprite artwork;
    public CardRarity rarity;
}

public enum CardRarity
{
    Normal,
    Rare,
    SuperRare
}