using UnityEngine;

public class PackController : MonoBehaviour
{
    [Header("Animation")]
    public Animator cameraAnimator;

    [Header("Card Database")]
    public Card[] normalCards;
    public Card[] rareCards;
    public Card[] superRareCards;

    [Header("Card Reveal")]
    public CardRevealController cardRevealController;

    [Header("Pack Sequence")]
    public PackSequenceController sequenceController;

    private bool isOpened = false;

    public void OpenPack()
    {
        if (isOpened) return;
        isOpened = true;
        sequenceController.StartSequence();
    }

    /// <summary>
    /// Called by BottomPackAnimationEvents at the end of PackDisappear.
    /// Picks 5 random cards, passes them to CardRevealController, then starts the reveal.
    /// </summary>
    public void RevealCard()
    {
        if (cardRevealController == null)
        {
            Debug.LogError("[PackController] cardRevealController is not assigned!");
            return;
        }

        Card[] selectedCards = new Card[5];
        for (int i = 0; i < 5; i++)
            selectedCards[i] = GetRandomCard();

        cardRevealController.SetCards(selectedCards);
        cardRevealController.StartReveal();

        Debug.Log("5 Cards Revealed");
    }

    private Card GetRandomCard()
    {
        int roll = Random.Range(1, 101);

        if (roll <= 70)
            return normalCards[Random.Range(0, normalCards.Length)];

        if (roll <= 97)
            return rareCards[Random.Range(0, rareCards.Length)];

        return superRareCards[Random.Range(0, superRareCards.Length)];
    }
}