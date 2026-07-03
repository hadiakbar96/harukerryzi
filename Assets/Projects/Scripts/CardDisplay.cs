using UnityEngine;

public class CardDisplay : MonoBehaviour
{
    private SpriteRenderer artwork;

    private void Awake()
    {
        artwork = GetComponent<SpriteRenderer>();

        if (artwork == null)
        {
            Debug.LogError("No SpriteRenderer found on Card prefab!");
        }
    }

    public void SetCard(Card card)
    {
        artwork.sprite = card.artwork;
    }
}