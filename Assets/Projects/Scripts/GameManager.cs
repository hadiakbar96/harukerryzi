using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Card[] cards;

    public Transform cardSpawnPoint;

    public GameObject cardPrefab;

    public void RevealRandomCard()
    {
        int random = Random.Range(0, cards.Length);

        Card chosen = cards[random];

        GameObject card = Instantiate(
            cardPrefab,
            cardSpawnPoint.position,
            Quaternion.identity
            );

        CardDisplay display = card.GetComponent<CardDisplay>();
        display.SetCard(chosen);
    }
}