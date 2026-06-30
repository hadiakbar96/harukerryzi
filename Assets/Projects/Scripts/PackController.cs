using UnityEngine;

public class PackController : MonoBehaviour
{
    [Header("Animation")]
    public Animator cameraAnimator;

    [Header("Card Database")]

    [Header("Card Database")]
    public Card[] normalCards;
    public Card[] rareCards;
    public Card[] superRareCards;

    [Header("Card Spawn")]
    public GameObject cardPrefab;
    public Transform cardSpawnPoint;
    public PackSequenceController sequenceController;

    private bool isOpened = false;

    public void OpenPack()
    {
        if (isOpened)
            return;

        isOpened = true;
        
        sequenceController.StartSequence();
    }

    public void RevealCard()
    {
        for (int i = 0; i < 5; i++)
        {
            Card selectedCard = GetRandomCard();

            Vector3 offset = new Vector3(0f, -0.25f * i, 0f);

            GameObject card = Instantiate(
                cardPrefab,
                cardSpawnPoint.position + offset,
                Quaternion.identity
            );

            CardDisplay display = card.GetComponent<CardDisplay>();
            display.SetCard(selectedCard);

            SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 5 - i;

            
        }
        Debug.Log("5 Cards Revealed");

    }

    private Card GetRandomCard()
    {
        int roll = Random.Range(1, 101);

        if (roll <= 70)
        {
            return normalCards[Random.Range(0, normalCards.Length)];
        }

        if (roll <= 97)
        {
            return rareCards[Random.Range(0, rareCards.Length)];
        }

        return superRareCards[Random.Range(0, superRareCards.Length)];
    }
}