using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component for each card entry in the collection scroll grid.
/// Created programmatically by CollectionUIController.
///
/// Shows:
///   - Card artwork (Image)
///   - Count badge (TMP text)
///   - Rarity-colored border
///   - Hover overlay with name
///   - Button → notifies parent controller on click
/// </summary>
public class CollectionCardItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ═══════════════════════════════════════════════════════════════
    //  Data
    // ═══════════════════════════════════════════════════════════════

    [HideInInspector] public Card card;
    [HideInInspector] public int  count;

    // ═══════════════════════════════════════════════════════════════
    //  UI References (set by builder)
    // ═══════════════════════════════════════════════════════════════

    public Image           artworkImage;
    public TextMeshProUGUI countText;
    public Image           borderImage;
    public Image           bgImage;
    public Button          button;

    public GameObject      hoverOverlay;
    public TextMeshProUGUI nameText;

    // ═══════════════════════════════════════════════════════════════
    //  Callback
    // ═══════════════════════════════════════════════════════════════

    private CollectionUIController _controller;

    /// <summary>
    /// Initialise this card item with data and references.
    /// </summary>
    public void Setup(Card card, int count, CollectionUIController controller)
    {
        this.card   = card;
        this.count  = count;
        _controller = controller;

        // Artwork
        if (artworkImage != null && card.artwork != null)
        {
            artworkImage.sprite = card.artwork;
            artworkImage.preserveAspect = true;
        }

        // Count badge
        UpdateCount(count);

        // Name text (hover)
        if (nameText != null)
        {
            nameText.text = card.cardName;
        }
        if (hoverOverlay != null)
        {
            hoverOverlay.SetActive(false);
        }

        // Rarity border color
        if (borderImage != null)
        {
            borderImage.color = GetRarityColor(card.rarity);
        }

        // Button click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverOverlay != null) hoverOverlay.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverOverlay != null) hoverOverlay.SetActive(false);
    }

    /// <summary>
    /// Update the displayed count (called after combine).
    /// </summary>
    public void UpdateCount(int newCount)
    {
        count = newCount;
        if (countText != null)
        {
            countText.text = "x" + count.ToString();
            countText.gameObject.SetActive(count > 0);
        }
    }

    private void OnClicked()
    {
        if (_controller != null)
            _controller.OnCardItemClicked(this);
    }

    /// <summary>
    /// Returns a color for the rarity border/glow.
    /// </summary>
    public static Color GetRarityColor(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Normal:    return new Color(0.55f, 0.55f, 0.60f, 1f); // silver-grey
            case CardRarity.Rare:      return new Color(0.20f, 0.55f, 0.95f, 1f); // blue
            case CardRarity.SuperRare: return new Color(0.95f, 0.75f, 0.10f, 1f); // gold
            default:                   return Color.white;
        }
    }

    /// <summary>
    /// Returns a darker background color per rarity.
    /// </summary>
    public static Color GetRarityBgColor(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Normal:    return new Color(0.15f, 0.15f, 0.20f, 1f);
            case CardRarity.Rare:      return new Color(0.08f, 0.15f, 0.30f, 1f);
            case CardRarity.SuperRare: return new Color(0.30f, 0.22f, 0.05f, 1f);
            default:                   return new Color(0.15f, 0.15f, 0.20f, 1f);
        }
    }
}
