using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Main controller for the Collection scene canvas.
///
/// LEFT PANEL  — Scrollable grid of collected cards + card preview
/// RIGHT PANEL — 5 combine slots (fan layout) + combine button
///
/// ════════════════════════════════════════════════════════════════
///  SETUP IN UNITY EDITOR:
/// ════════════════════════════════════════════════════════════════
///
///  Attach this script to the Canvas GameObject in the Collection scene.
///  Wire the following in the Inspector:
///
///  [Card Database]
///   cardDatabase         → Your CardDatabase ScriptableObject asset
///
///  [Left Panel — Collection]
///   collectionPanel      → CollectionPanel RectTransform
///   cardPreviewImage     → Image on the CardPreview object
///   cardNameText         → TextMeshProUGUI on the NameText object
///   previewPlaceholder   → The Placeholder TextMeshProUGUI (hidden when a card is selected)
///
///  [Right Panel — Combine]
///   combineSlotImages    → Image[] array with 5 entries (CardSlot_1 → CardSlot_5)
///   combineSlotLabels    → TMP[] array with 5 entries (Label children of each slot)
///   combineButton        → Button component on CombineButton
///   ruleText             → TextMeshProUGUI on RuleText
///
///  [Navigation]
///   backButton           → Button component on BackButton
///   previousSceneName    → Name of the scene to load when Back is pressed
/// </summary>
public class CollectionUIController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  Inspector Fields
    // ═══════════════════════════════════════════════════════════════

    [Header("Card Database")]
    [SerializeField] private CardDatabase cardDatabase;

    [Header("Left Panel — Collection")]
    [SerializeField] private RectTransform     collectionPanel;
    [SerializeField] private Image             cardPreviewImage;
    [SerializeField] private TextMeshProUGUI   cardNameText;
    [SerializeField] private TextMeshProUGUI   previewPlaceholder;

    [Header("Right Panel — Combine")]
    [SerializeField] private Image[]           combineSlotImages;
    [SerializeField] private TextMeshProUGUI[] combineSlotLabels;
    [SerializeField] private Button            combineButton;
    [SerializeField] private TextMeshProUGUI   ruleText;

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private string previousSceneName = "openPack";

    [Header("Grid Settings")]
    [SerializeField] private int   gridColumns    = 3;
    [SerializeField] private float cellSize       = 150f;
    [SerializeField] private float cellSpacing    = 15f;
    [SerializeField] private float gridTopPadding = 80f;

    // ═══════════════════════════════════════════════════════════════
    //  Private State
    // ═══════════════════════════════════════════════════════════════

    // Scroll grid
    private ScrollRect               _scrollRect;
    private RectTransform            _contentRect;
    private List<CollectionCardItem> _cardItems = new List<CollectionCardItem>();

    // Combine slots
    private Card[]   _combineSlots  = new Card[5];
    private Image[]  _slotArtworks  = new Image[5]; // child artwork images we create on slots
    private int      _filledSlotCount;
    private CardRarity? _lockedRarity;

    // Currently selected card for preview
    private Card _selectedCard;

    // Combine animation
    private bool _isCombining;

    // ═══════════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        CardInventory.ForceReload();

        BuildScrollGrid();
        PopulateGrid();
        SetupCombineSlots();
        SetupButtons();
        ClearPreview();
        UpdateCombineButton();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Scroll Grid Construction
    // ═══════════════════════════════════════════════════════════════

    private void BuildScrollGrid()
    {
        if (collectionPanel == null)
        {
            Debug.LogError("[CollectionUI] collectionPanel is not assigned!");
            return;
        }

        // Create ScrollView container
        GameObject scrollObj = new GameObject("CardScrollView", typeof(RectTransform));
        scrollObj.transform.SetParent(collectionPanel, false);

        RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
        // Position below the title, extending all the way to the bottom (0.05f)
        scrollRect.anchorMin = new Vector2(0f, 0.05f);
        scrollRect.anchorMax = new Vector2(1f, 0.88f);
        scrollRect.offsetMin = new Vector2(15f, 0f);
        scrollRect.offsetMax = new Vector2(-15f, 0f);

        // Add ScrollRect component
        _scrollRect = scrollObj.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical   = true;
        _scrollRect.movementType = ScrollRect.MovementType.Elastic;
        _scrollRect.elasticity   = 0.1f;
        _scrollRect.scrollSensitivity = 20f;

        // Add Mask + Image for clipping
        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.06f, 0.08f, 0.16f, 0.5f);
        scrollBg.raycastTarget = true;
        Mask mask = scrollObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Create Content container
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(scrollObj.transform, false);

        _contentRect = contentObj.GetComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(1f, 1f);
        _contentRect.pivot     = new Vector2(0.5f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;

        // Add GridLayoutGroup
        GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(cellSize, cellSize * 1.4f); // card aspect ratio
        grid.spacing         = new Vector2(cellSpacing, cellSpacing);
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperCenter;
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridColumns;
        grid.padding         = new RectOffset(8, 8, 8, 8);

        // Add ContentSizeFitter for auto-sizing
        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Wire content to scroll rect
        _scrollRect.content  = _contentRect;
        _scrollRect.viewport = scrollRect;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Grid Population
    // ═══════════════════════════════════════════════════════════════

    private void PopulateGrid()
    {
        // Clear existing items
        foreach (var item in _cardItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _cardItems.Clear();

        if (cardDatabase == null)
        {
            Debug.LogError("[CollectionUI] cardDatabase is not assigned!");
            return;
        }

        Dictionary<string, int> inventory = CardInventory.GetAllEntries();

        // Show all cards from database, greyed out if count == 0
        Card[] allCards = cardDatabase.GetAllCards();

        foreach (Card card in allCards)
        {
            int count = 0;
            if (inventory.ContainsKey(card.cardName))
                count = inventory[card.cardName];

            // Only show cards that the player has at least 1 of
            if (count <= 0) continue;

            CollectionCardItem item = CreateCardItem(card, count);
            _cardItems.Add(item);
        }
    }

    private CollectionCardItem CreateCardItem(Card card, int count)
    {
        // Root container
        GameObject root = new GameObject("CardItem_" + card.cardName, typeof(RectTransform));
        root.transform.SetParent(_contentRect, false);

        // Background / border
        Image borderImg = root.AddComponent<Image>();
        borderImg.color = CollectionCardItem.GetRarityColor(card.rarity);
        borderImg.raycastTarget = true;

        // Inner background (slightly inset)
        GameObject innerBg = new GameObject("InnerBg", typeof(RectTransform));
        innerBg.transform.SetParent(root.transform, false);
        RectTransform innerRect = innerBg.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);
        Image innerBgImg = innerBg.AddComponent<Image>();
        innerBgImg.color = CollectionCardItem.GetRarityBgColor(card.rarity);
        innerBgImg.raycastTarget = false;

        // Card artwork
        GameObject artObj = new GameObject("Artwork", typeof(RectTransform));
        artObj.transform.SetParent(innerBg.transform, false);
        RectTransform artRect = artObj.GetComponent<RectTransform>();
        artRect.anchorMin = new Vector2(0.05f, 0.15f);
        artRect.anchorMax = new Vector2(0.95f, 0.95f);
        artRect.offsetMin = Vector2.zero;
        artRect.offsetMax = Vector2.zero;
        Image artImg = artObj.AddComponent<Image>();
        artImg.raycastTarget = false;

        // Count badge
        GameObject countObj = new GameObject("Count", typeof(RectTransform));
        countObj.transform.SetParent(root.transform, false);
        RectTransform countRect = countObj.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot     = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-4f, 4f);
        countRect.sizeDelta = new Vector2(40f, 22f);

        // Count badge background
        Image countBg = countObj.AddComponent<Image>();
        countBg.color = new Color(0f, 0f, 0f, 0.7f);
        countBg.raycastTarget = false;

        // Count text
        GameObject countTextObj = new GameObject("CountText", typeof(RectTransform));
        countTextObj.transform.SetParent(countObj.transform, false);
        RectTransform countTextRect = countTextObj.GetComponent<RectTransform>();
        countTextRect.anchorMin = Vector2.zero;
        countTextRect.anchorMax = Vector2.one;
        countTextRect.offsetMin = Vector2.zero;
        countTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI countText = countTextObj.AddComponent<TextMeshProUGUI>();
        countText.fontSize  = 14f;
        countText.alignment = TextAlignmentOptions.Center;
        countText.color     = Color.white;
        countText.raycastTarget = false;

        // Button component
        Button btn = root.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor     = new Color(0.65f, 0.65f, 0.65f, 1f);
        colors.selectedColor    = Color.white;
        btn.colors = colors;

        // Hover Overlay
        GameObject hoverObj = new GameObject("HoverOverlay", typeof(RectTransform));
        hoverObj.transform.SetParent(root.transform, false);
        RectTransform hoverRect = hoverObj.GetComponent<RectTransform>();
        hoverRect.anchorMin = Vector2.zero;
        hoverRect.anchorMax = Vector2.one;
        hoverRect.offsetMin = Vector2.zero;
        hoverRect.offsetMax = Vector2.zero;
        Image hoverBg = hoverObj.AddComponent<Image>();
        hoverBg.color = new Color(0f, 0f, 0f, 0.8f); // Dark overlay
        hoverBg.raycastTarget = false;

        // Hover Text (Card Name)
        GameObject hoverTextObj = new GameObject("HoverText", typeof(RectTransform));
        hoverTextObj.transform.SetParent(hoverObj.transform, false);
        RectTransform hoverTextRect = hoverTextObj.GetComponent<RectTransform>();
        hoverTextRect.anchorMin = Vector2.zero;
        hoverTextRect.anchorMax = Vector2.one;
        hoverTextRect.offsetMin = new Vector2(5f, 5f);
        hoverTextRect.offsetMax = new Vector2(-5f, -5f);
        TextMeshProUGUI hoverText = hoverTextObj.AddComponent<TextMeshProUGUI>();
        hoverText.fontSize = 18f;
        hoverText.alignment = TextAlignmentOptions.Center;
        hoverText.enableWordWrapping = true;
        hoverText.color = Color.white;
        hoverText.raycastTarget = false;

        // Add CollectionCardItem component
        CollectionCardItem item = root.AddComponent<CollectionCardItem>();
        item.artworkImage = artImg;
        item.countText    = countText;
        item.borderImage  = borderImg;
        item.bgImage      = innerBgImg;
        item.button       = btn;
        item.hoverOverlay = hoverObj;
        item.nameText     = hoverText;

        item.Setup(card, count, this);

        return item;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Combine Slots Setup
    // ═══════════════════════════════════════════════════════════════

    private void SetupCombineSlots()
    {
        // Auto-find slots if not assigned in Inspector
        if (combineSlotImages == null || combineSlotImages.Length == 0 || combineSlotImages[0] == null)
        {
            GameObject fan = GameObject.Find("CardSlots_Fan");
            if (fan != null)
            {
                combineSlotImages = new Image[5];
                combineSlotLabels = new TextMeshProUGUI[5];
                for (int i = 0; i < 5; i++)
                {
                    Transform slot = fan.transform.Find("CardSlot_" + (i + 1));
                    if (slot != null)
                    {
                        combineSlotImages[i] = slot.GetComponent<Image>();
                        Transform label = slot.Find("Label");
                        if (label != null)
                            combineSlotLabels[i] = label.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
        }

        _combineSlots    = new Card[5];
        _filledSlotCount = 0;
        _lockedRarity    = null;

        // Create artwork images inside each slot (initially hidden)
        for (int i = 0; i < 5; i++)
        {
            if (combineSlotImages == null || i >= combineSlotImages.Length || combineSlotImages[i] == null)
                continue;

            // Create an artwork child image inside the slot
            GameObject artObj = new GameObject("SlotArtwork", typeof(RectTransform));
            artObj.transform.SetParent(combineSlotImages[i].transform, false);
            artObj.transform.SetAsFirstSibling(); // put behind the label

            RectTransform artRect = artObj.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;

            Image artImg = artObj.AddComponent<Image>();
            artImg.preserveAspect = true;
            artImg.raycastTarget  = false;
            artImg.color = Color.white;
            artObj.SetActive(false);

            _slotArtworks[i] = artImg;

            // Add button to each slot for removing cards
            int slotIndex = i; // capture for closure
            Button slotBtn = combineSlotImages[i].gameObject.AddComponent<Button>();
            slotBtn.onClick.AddListener(() => OnCombineSlotClicked(slotIndex));

            // Make the slot image a raycast target
            combineSlotImages[i].raycastTarget = true;
        }
    }

    private void SetupButtons()
    {
        // Combine button
        if (combineButton != null)
        {
            combineButton.onClick.RemoveAllListeners();
            combineButton.onClick.AddListener(OnCombineClicked);

            // Ensure the button image is a raycast target
            Image btnImg = combineButton.GetComponent<Image>();
            if (btnImg != null)
                btnImg.raycastTarget = true;
        }

        // Back button
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);

            // Ensure the button image is a raycast target
            Image btnImg = backButton.GetComponent<Image>();
            if (btnImg != null)
                btnImg.raycastTarget = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Card Preview
    // ═══════════════════════════════════════════════════════════════

    private void ShowPreview(Card card)
    {
        _selectedCard = card;

        if (cardPreviewImage != null)
        {
            cardPreviewImage.sprite = card.artwork;
            cardPreviewImage.preserveAspect = true;
            cardPreviewImage.color = Color.white;
        }

        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (previewPlaceholder != null)
            previewPlaceholder.gameObject.SetActive(false);
    }

    private void ClearPreview()
    {
        _selectedCard = null;

        if (cardPreviewImage != null)
            cardPreviewImage.color = new Color(1f, 1f, 1f, 0f); // transparent

        if (cardNameText != null)
            cardNameText.text = "";

        if (previewPlaceholder != null)
            previewPlaceholder.gameObject.SetActive(true);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Card Item Click Handler
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when a card in the collection grid is clicked.
    /// Shows preview and adds to next available combine slot.
    /// </summary>
    public void OnCardItemClicked(CollectionCardItem item)
    {
        if (_isCombining) return;

        // Show preview
        ShowPreview(item.card);

        // Try to add to combine slot
        TryAddToCombineSlot(item.card);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Combine Slot Logic
    // ═══════════════════════════════════════════════════════════════

    private void TryAddToCombineSlot(Card card)
    {
        // Check if all slots are full
        if (_filledSlotCount >= 5) return;

        // Check rarity lock — all cards must be same rarity
        if (_lockedRarity.HasValue && card.rarity != _lockedRarity.Value)
        {
            // Wrong rarity — shake the slots as feedback
            StartCoroutine(ShakeWarning());
            return;
        }

        // Check if player has enough copies
        int inSlots = CountCardInSlots(card.cardName);
        int inInventory = CardInventory.GetCount(card.cardName);
        if (inSlots >= inInventory)
        {
            // Not enough copies
            StartCoroutine(ShakeWarning());
            return;
        }

        // SuperRare cannot be combined (max rarity)
        if (card.rarity == CardRarity.SuperRare)
        {
            if (ruleText != null)
                ruleText.text = "SuperRare cards cannot\nbe combined (max rarity)";
            StartCoroutine(ShakeWarning());
            return;
        }

        // Find next empty slot
        int slotIdx = -1;
        for (int i = 0; i < 5; i++)
        {
            if (_combineSlots[i] == null)
            {
                slotIdx = i;
                break;
            }
        }

        if (slotIdx < 0) return;

        // Place card in slot
        _combineSlots[slotIdx] = card;
        _filledSlotCount++;
        _lockedRarity = card.rarity;

        // Update visual
        if (_slotArtworks[slotIdx] != null)
        {
            _slotArtworks[slotIdx].sprite = card.artwork;
            _slotArtworks[slotIdx].gameObject.SetActive(true);
        }

        // Hide the slot label number
        if (combineSlotLabels != null && slotIdx < combineSlotLabels.Length && combineSlotLabels[slotIdx] != null)
            combineSlotLabels[slotIdx].gameObject.SetActive(false);

        // Update slot to make placeholder disappear (fully transparent)
        if (combineSlotImages != null && slotIdx < combineSlotImages.Length && combineSlotImages[slotIdx] != null)
            combineSlotImages[slotIdx].color = new Color(0f, 0f, 0f, 0f);

        UpdateCombineButton();
        UpdateRuleText();
    }

    private void OnCombineSlotClicked(int slotIndex)
    {
        if (_isCombining) return;
        if (_combineSlots[slotIndex] == null) return;

        // Remove card from slot
        _combineSlots[slotIndex] = null;
        _filledSlotCount--;

        // Hide artwork
        if (_slotArtworks[slotIndex] != null)
            _slotArtworks[slotIndex].gameObject.SetActive(false);

        // Show label again
        if (combineSlotLabels != null && slotIndex < combineSlotLabels.Length && combineSlotLabels[slotIndex] != null)
            combineSlotLabels[slotIndex].gameObject.SetActive(true);

        // Reset slot color
        if (combineSlotImages != null && slotIndex < combineSlotImages.Length && combineSlotImages[slotIndex] != null)
            combineSlotImages[slotIndex].color = new Color(0.17f, 0.17f, 0.24f, 1f);

        // If all slots are empty, reset rarity lock
        if (_filledSlotCount <= 0)
        {
            _filledSlotCount = 0;
            _lockedRarity = null;
        }

        UpdateCombineButton();
        UpdateRuleText();
    }

    private void ClearAllCombineSlots()
    {
        for (int i = 0; i < 5; i++)
        {
            _combineSlots[i] = null;

            if (_slotArtworks[i] != null)
                _slotArtworks[i].gameObject.SetActive(false);

            if (combineSlotLabels != null && i < combineSlotLabels.Length && combineSlotLabels[i] != null)
                combineSlotLabels[i].gameObject.SetActive(true);

            if (combineSlotImages != null && i < combineSlotImages.Length && combineSlotImages[i] != null)
                combineSlotImages[i].color = new Color(0.17f, 0.17f, 0.24f, 1f);
        }

        _filledSlotCount = 0;
        _lockedRarity = null;
    }

    private int CountCardInSlots(string cardName)
    {
        int count = 0;
        for (int i = 0; i < 5; i++)
            if (_combineSlots[i] != null && _combineSlots[i].cardName == cardName)
                count++;
        return count;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Combine Button
    // ═══════════════════════════════════════════════════════════════

    private void UpdateCombineButton()
    {
        bool canCombine = _filledSlotCount >= 5 && !_isCombining;

        if (combineButton != null)
        {
            combineButton.interactable = canCombine;

            // Visual feedback — change button color
            Image btnImg = combineButton.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = canCombine
                    ? new Color(0.10f, 0.45f, 0.80f, 1f) // bright blue when ready
                    : new Color(0.06f, 0.20f, 0.38f, 1f); // dim blue otherwise
            }
        }
    }

    private void UpdateRuleText()
    {
        if (ruleText == null) return;

        if (_lockedRarity.HasValue)
        {
            string rarityName = _lockedRarity.Value.ToString();
            int remaining = 5 - _filledSlotCount;
            if (remaining > 0)
                ruleText.text = $"Add {remaining} more {rarityName}\ncards to combine";
            else
                ruleText.text = $"Ready to combine!\n5 {rarityName} cards";
        }
        else
        {
            ruleText.text = "Only combine cards if\nsame rarity";
        }
    }

    private void OnCombineClicked()
    {
        if (_filledSlotCount < 5 || _isCombining) return;

        StartCoroutine(CombineSequence());
    }

    // ═══════════════════════════════════════════════════════════════
    //  Combine Animation & Logic
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator CombineSequence()
    {
        _isCombining = true;
        UpdateCombineButton();

        CardRarity currentRarity = _lockedRarity.Value;
        CardRarity nextRarity = GetNextRarity(currentRarity);

        // Remove cards from inventory
        Dictionary<string, int> removeCounts = new Dictionary<string, int>();
        for (int i = 0; i < 5; i++)
        {
            string name = _combineSlots[i].cardName;
            if (removeCounts.ContainsKey(name))
                removeCounts[name]++;
            else
                removeCounts[name] = 1;
        }

        foreach (var kvp in removeCounts)
            CardInventory.RemoveCards(kvp.Key, kvp.Value);

        // Get the result card (random from next rarity)
        Card resultCard = cardDatabase.GetRandomCardOfRarity(nextRarity);

        // Animate slots converging to center
        yield return StartCoroutine(AnimateCombine());

        // Add result card to inventory
        if (resultCard != null)
        {
            CardInventory.AddCard(resultCard);
        }

        // Flash effect
        yield return StartCoroutine(CombineFlash());

        // Clear slots
        ClearAllCombineSlots();

        // Refresh grid
        PopulateGrid();

        // Show the result card and wait for the user to click to dismiss
        yield return StartCoroutine(ShowCombineResult(resultCard));

        _isCombining = false;
        UpdateCombineButton();
        UpdateRuleText();
    }

    private IEnumerator AnimateCombine()
    {
        float duration = 0.5f;
        float elapsed  = 0f;

        // Store original positions and scales
        Vector3[] startPositions = new Vector3[5];
        Vector3[] startScales    = new Vector3[5];

        // Calculate center position (average of all slots)
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < 5; i++)
        {
            if (combineSlotImages != null && i < combineSlotImages.Length && combineSlotImages[i] != null)
            {
                startPositions[i] = combineSlotImages[i].rectTransform.anchoredPosition;
                startScales[i]    = combineSlotImages[i].rectTransform.localScale;
                center += (Vector3)combineSlotImages[i].rectTransform.anchoredPosition;
                count++;
            }
        }
        if (count > 0) center /= count;

        // Animate
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < 5; i++)
            {
                if (combineSlotImages == null || i >= combineSlotImages.Length || combineSlotImages[i] == null)
                    continue;

                combineSlotImages[i].rectTransform.anchoredPosition =
                    Vector2.Lerp(startPositions[i], (Vector2)center, t);
                float scale = Mathf.Lerp(1f, 0.3f, t);
                combineSlotImages[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        // Reset positions and scales
        for (int i = 0; i < 5; i++)
        {
            if (combineSlotImages == null || i >= combineSlotImages.Length || combineSlotImages[i] == null)
                continue;

            combineSlotImages[i].rectTransform.anchoredPosition = startPositions[i];
            combineSlotImages[i].rectTransform.localScale       = Vector3.one;
        }
    }

    private IEnumerator CombineFlash()
    {
        // Create a full-screen white flash
        GameObject flashObj = new GameObject("CombineFlash", typeof(RectTransform));
        flashObj.transform.SetParent(transform, false);

        RectTransform flashRect = flashObj.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.raycastTarget = false;

        // Flash in
        float flashDuration = 0.15f;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
            flashImg.color = new Color(1f, 1f, 1f, t * 0.6f);
            yield return null;
        }

        // Flash out
        elapsed = 0f;
        float fadeDuration = 0.4f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            flashImg.color = new Color(1f, 1f, 1f, (1f - t) * 0.6f);
            yield return null;
        }

        Destroy(flashObj);
    }

    private IEnumerator ShowCombineResult(Card resultCard)
    {
        if (resultCard == null) yield break;

        // Create a blocker that covers the whole screen to catch clicks
        GameObject blockerObj = new GameObject("ResultBlocker", typeof(RectTransform));
        blockerObj.transform.SetParent(transform, false);
        RectTransform blockerRect = blockerObj.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        
        Image blockerImg = blockerObj.AddComponent<Image>();
        blockerImg.color = new Color(0f, 0f, 0f, 0.9f); // Darken the screen more (almost black)
        Button blockerBtn = blockerObj.AddComponent<Button>();

        // Create the result card image
        GameObject resultObj = new GameObject("ResultCard", typeof(RectTransform));
        resultObj.transform.SetParent(blockerObj.transform, false);
        RectTransform resultRect = resultObj.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = new Vector2(380f, 60f); // Center of the combine area
        resultRect.sizeDelta = new Vector2(240f, 340f); // Big presentation size

        // Just the raw sprite, no background square
        Image resultImg = resultObj.AddComponent<Image>();
        resultImg.sprite = resultCard.artwork;
        resultImg.preserveAspect = true;

        // Add "Click anywhere to continue" text
        GameObject textObj = new GameObject("ClickText", typeof(RectTransform));
        textObj.transform.SetParent(blockerObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(380f, -160f);
        textRect.sizeDelta = new Vector2(400f, 50f);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "Click to continue";
        tmpText.fontSize = 24f;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(1f, 1f, 1f, 0.7f);

        // Wait for player to click
        bool clicked = false;
        blockerBtn.onClick.AddListener(() => clicked = true);

        // Pop-in animation
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out bounce/overshoot
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f) + 0.1f * Mathf.Sin(t * Mathf.PI);
            resultObj.transform.localScale = Vector3.one * ease;
            yield return null;
        }
        resultObj.transform.localScale = Vector3.one;

        while (!clicked)
            yield return null;

        Destroy(blockerObj);
    }

    private CardRarity GetNextRarity(CardRarity current)
    {
        switch (current)
        {
            case CardRarity.Normal: return CardRarity.Rare;
            case CardRarity.Rare:   return CardRarity.SuperRare;
            default:                return CardRarity.SuperRare;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Warning Shake
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator ShakeWarning()
    {
        if (combineSlotImages == null) yield break;

        // Find the parent of the fan slots to shake
        RectTransform shakeTarget = null;
        if (combineSlotImages.Length > 0 && combineSlotImages[0] != null)
        {
            Transform parent = combineSlotImages[0].transform.parent;
            if (parent != null)
                shakeTarget = parent.GetComponent<RectTransform>();
        }

        if (shakeTarget == null) yield break;

        Vector2 origin = shakeTarget.anchoredPosition;
        float   duration  = 0.3f;
        float   intensity = 8f;
        int     shakes    = 6;
        float   stepTime  = duration / shakes;

        for (int i = 0; i < shakes; i++)
        {
            float decay = 1f - (float)i / shakes;
            float dir   = (i % 2 == 0) ? 1f : -1f;
            Vector2 target = origin + new Vector2(dir * intensity * decay, 0f);

            float elapsed = 0f;
            Vector2 from = shakeTarget.anchoredPosition;
            while (elapsed < stepTime)
            {
                elapsed += Time.deltaTime;
                shakeTarget.anchoredPosition = Vector2.Lerp(from, target,
                    Mathf.Clamp01(elapsed / stepTime));
                yield return null;
            }
        }

        shakeTarget.anchoredPosition = origin;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Navigation
    // ═══════════════════════════════════════════════════════════════

    private void OnBackClicked()
    {
        string sceneToLoad = string.IsNullOrEmpty(SceneHistory.ReturnScene) ? previousSceneName : SceneHistory.ReturnScene;
        SceneManager.LoadScene(sceneToLoad);
    }
}
