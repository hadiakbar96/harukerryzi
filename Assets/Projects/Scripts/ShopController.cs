using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Harukerryzi.Clash;
using TMPro;

public class ShopController : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The panel that shows the obtained pack and the 'Open' button.")]
    [SerializeField] private GameObject packObtainedPanel;

    [Tooltip("The main shop panel to hide when a pack is bought.")]
    [SerializeField] private GameObject shopPanel;

    [Tooltip("The header UI to hide when a pack is bought.")]
    [SerializeField] private GameObject headerPanel;

    [Header("Coin Display")]
    [Tooltip("TMP text that displays the player's current coin balance. Auto-found by name 'CoinText' if not assigned.")]
    [SerializeField] private TextMeshProUGUI coinText;

    [Tooltip("TMP text for showing 'Not enough coins' feedback. Auto-created if not assigned.")]
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Pack Settings")]
    [SerializeField] private int packPrice = 75;

    [Header("Animation Settings")]
    [Tooltip("How long the popup animation takes.")]
    [SerializeField] private float popupDuration = 0.4f;
    
    [Tooltip("Animation curve for the popup scaling. Tip: Make it go slightly above 1 for a bounce effect!")]
    [SerializeField] private AnimationCurve popupCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Navigation")]
    [Tooltip("Scene loaded by the back button when no return scene was recorded.")]
    [SerializeField] private string defaultBackScene = "StageMap";

    [Header("Swipe To Open")]
    [Tooltip("Horizontal drag distance (fraction of screen width) needed to swipe-open the pack.")]
    [SerializeField] private float swipeThreshold = 0.1f;

    private Coroutine _feedbackCoroutine;

    // Pack panel extras (created at runtime the first time the panel opens)
    private bool _panelExtrasCreated;
    private RectTransform _packRect;
    private CanvasGroup _guideGroup;
    private RectTransform _guideArrow;
    private Vector2 _arrowBasePos;
    private Sprite _backSprite;
    private Canvas _canvas;

    // Swipe tracking
    private Vector2 _swipeStart;
    private bool _swipeStartedOnPack;

    private void Start()
    {
        // Ensure the pack obtained panel is hidden when the scene starts
        if (packObtainedPanel != null)
        {
            packObtainedPanel.SetActive(false);
        }

        // Auto-find CoinText if not assigned
        if (coinText == null)
        {
            GameObject coinTextObj = GameObject.Find("CoinText");
            if (coinTextObj != null)
                coinText = coinTextObj.GetComponent<TextMeshProUGUI>();
        }

        // Auto-find or create feedback text
        if (feedbackText == null)
        {
            GameObject feedbackObj = GameObject.Find("FeedbackText");
            if (feedbackObj != null)
            {
                feedbackText = feedbackObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                feedbackText = CreateFeedbackText();
            }
        }

        UpdateCoinDisplay();
        HideFeedback();

        _canvas = FindObjectOfType<Canvas>();
        WireBackButton();
    }

    private void Update()
    {
        if (packObtainedPanel == null || !packObtainedPanel.activeInHierarchy)
            return;

        HandlePackSwipe();
        AnimateSwipeGuide();
    }

    /// <summary>
    /// Call this from the BuyButton's OnClick() event in the inspector.
    /// </summary>
    public void OnBuyButtonClicked()
    {
        // Check if player has enough coins
        int currentCoins = CurrencyWallet.GetCoins();
        if (currentCoins < packPrice)
        {
            ShowNotEnoughCoinsFeedback(currentCoins);
            return;
        }

        // Deduct coins
        CurrencyWallet.SpendCoins(packPrice);
        UpdateCoinDisplay();

        if (packObtainedPanel != null)
        {
            packObtainedPanel.SetActive(true);

            // Hide the shop panel and header
            if (shopPanel != null) shopPanel.SetActive(false);
            if (headerPanel != null) headerPanel.SetActive(false);

            EnsurePanelExtras();

            // Start the scale-in animation
            StartCoroutine(AnimatePanelIn());
        }
        else
        {
            Debug.LogWarning("Pack Obtained Panel is not assigned in the ShopController inspector!");
        }
    }

    private IEnumerator AnimatePanelIn()
    {
        float timeElapsed = 0f;
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        packObtainedPanel.transform.localScale = initialScale;

        while (timeElapsed < popupDuration)
        {
            timeElapsed += Time.deltaTime;
            
            // Normalize time between 0 and 1
            float t = Mathf.Clamp01(timeElapsed / popupDuration);
            
            // Evaluate the animation curve
            float curveValue = popupCurve.Evaluate(t);
            
            // Apply scale (using Unclamped so it can overshoot if the curve goes above 1 for a bounce)
            packObtainedPanel.transform.localScale = Vector3.LerpUnclamped(initialScale, targetScale, curveValue);
            
            yield return null;
        }

        packObtainedPanel.transform.localScale = targetScale;
    }

    /// <summary>
    /// Call this from the Open button's OnClick() event in the inspector.
    /// </summary>
    public void OnOpenButtonClicked()
    {
        // Load the openPack scene
        SceneManager.LoadScene("openPack");
    }
    
    /// <summary>
    /// Optional: Call this from a Close or Back button if the user wants to cancel opening the pack.
    /// </summary>
    public void OnClosePanelButtonClicked()
    {
        if (packObtainedPanel != null)
        {
            packObtainedPanel.SetActive(false);

            // Restore the shop panel and header if we close the pack panel
            if (shopPanel != null) shopPanel.SetActive(true);
            if (headerPanel != null) headerPanel.SetActive(true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Back Navigation
    // ═══════════════════════════════════════════════════════════════

    private void WireBackButton()
    {
        GameObject backObj = GameObject.Find("BackButton");
        if (backObj == null) return;

        Image backImage = backObj.GetComponent<Image>();
        if (backImage != null) _backSprite = backImage.sprite;

        Button backButton = backObj.GetComponent<Button>();
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
    }

    /// <summary>
    /// Leaves the shop. Returns to the scene recorded in SceneHistory,
    /// or to defaultBackScene when none was recorded.
    /// </summary>
    public void OnBackButtonClicked()
    {
        string destination = string.IsNullOrEmpty(SceneHistory.ReturnScene)
            ? defaultBackScene
            : SceneHistory.ReturnScene;

        if (Application.CanStreamedLevelBeLoaded(destination))
        {
            SceneManager.LoadScene(destination);
        }
        else
        {
            Debug.LogWarning("[Shop] Back destination scene is not in Build Settings: " + destination);
        }
    }

    /// <summary>
    /// Back button on the pack obtained panel: refunds the purchase and
    /// returns to the shop front.
    /// </summary>
    public void OnCancelPackClicked()
    {
        CurrencyWallet.AddCoins(packPrice);
        UpdateCoinDisplay();
        OnClosePanelButtonClicked();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Pack Panel Extras (back button + swipe guide)
    // ═══════════════════════════════════════════════════════════════

    private void EnsurePanelExtras()
    {
        if (_panelExtrasCreated) return;
        _panelExtrasCreated = true;

        Transform packTransform = packObtainedPanel.transform.Find("Pack");
        if (packTransform != null)
            _packRect = packTransform as RectTransform;

        CreatePanelBackButton();
        CreateSwipeGuide();
    }

    private void CreatePanelBackButton()
    {
        GameObject buttonObj = new GameObject("PanelBackButton", typeof(RectTransform));
        buttonObj.transform.SetParent(packObtainedPanel.transform, false);

        // Same spot as the shop front back button (top-left corner)
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(100f, -75f);
        rect.sizeDelta = new Vector2(150f, 100f);

        Image image = buttonObj.AddComponent<Image>();
        if (_backSprite != null)
        {
            image.sprite = _backSprite;
            image.preserveAspect = true;
        }

        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(OnCancelPackClicked);
    }

    private void CreateSwipeGuide()
    {
        if (_packRect == null) return;

        GameObject guideObj = new GameObject("SwipeGuide", typeof(RectTransform));
        guideObj.transform.SetParent(_packRect, false);

        RectTransform guideRect = guideObj.GetComponent<RectTransform>();
        guideRect.anchorMin = Vector2.zero;
        guideRect.anchorMax = Vector2.one;
        guideRect.offsetMin = Vector2.zero;
        guideRect.offsetMax = Vector2.zero;

        _guideGroup = guideObj.AddComponent<CanvasGroup>();
        _guideGroup.blocksRaycasts = false;
        _guideGroup.interactable = false;

        float packWidth = _packRect.rect.width;
        float packHeight = _packRect.rect.height;
        float lineY = packHeight / 2f - 45f; // tear line near the top of the pack

        // Dashed line across the pack
        int dashCount = 8;
        float dashWidth = packWidth / (dashCount * 2f - 1f);
        float startX = -packWidth / 2f;
        for (int i = 0; i < dashCount; i++)
        {
            GameObject dashObj = new GameObject("Dash_" + i, typeof(RectTransform));
            dashObj.transform.SetParent(guideRect, false);

            RectTransform dashRect = dashObj.GetComponent<RectTransform>();
            dashRect.anchoredPosition = new Vector2(startX + i * dashWidth * 2f + dashWidth / 2f, lineY);
            dashRect.sizeDelta = new Vector2(dashWidth * 0.8f, 6f);

            Image dashImage = dashObj.AddComponent<Image>();
            dashImage.color = new Color(1f, 1f, 1f, 0.8f);
        }

        // "Swipe here" label to the left of the pack, aligned with the tear line
        GameObject labelObj = new GameObject("SwipeLabel", typeof(RectTransform));
        labelObj.transform.SetParent(guideRect, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-packWidth / 2f - 250f, lineY);
        labelRect.sizeDelta = new Vector2(300f, 60f);

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "Swipe here";
        label.fontSize = 40f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.95f, 0.7f, 1f);
        label.raycastTarget = false;

        // Bobbing arrow to the right of the label
        GameObject arrowObj = new GameObject("SwipeArrow", typeof(RectTransform));
        arrowObj.transform.SetParent(guideRect, false);

        _guideArrow = arrowObj.GetComponent<RectTransform>();
        _arrowBasePos = new Vector2(-packWidth / 2f - 80f, lineY);
        _guideArrow.anchoredPosition = _arrowBasePos;
        _guideArrow.sizeDelta = new Vector2(80f, 60f);

        TextMeshProUGUI arrow = arrowObj.AddComponent<TextMeshProUGUI>();
        arrow.text = ">>";
        arrow.fontSize = 52f;
        arrow.fontStyle = FontStyles.Bold;
        arrow.alignment = TextAlignmentOptions.Center;
        arrow.color = new Color(1f, 0.9f, 0.3f, 1f);
        arrow.raycastTarget = false;
    }

    private void HandlePackSwipe()
    {
        if (_packRect == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            _swipeStart = Input.mousePosition;
            Camera uiCamera = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera
                : null;
            _swipeStartedOnPack = RectTransformUtility.RectangleContainsScreenPoint(_packRect, _swipeStart, uiCamera);
        }
        else if (Input.GetMouseButtonUp(0) && _swipeStartedOnPack)
        {
            _swipeStartedOnPack = false;

            Vector2 delta = (Vector2)Input.mousePosition - _swipeStart;
            if (delta.x > Screen.width * swipeThreshold && Mathf.Abs(delta.y) < delta.x)
            {
                OnOpenButtonClicked();
            }
        }
    }

    private void AnimateSwipeGuide()
    {
        if (_guideGroup == null) return;

        // Pulse the whole guide, bob the arrow horizontally
        float pulse = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
        _guideGroup.alpha = Mathf.Lerp(0.45f, 1f, pulse);

        if (_guideArrow != null)
        {
            float bob = Mathf.Sin(Time.time * 3f) * 12f;
            _guideArrow.anchoredPosition = _arrowBasePos + new Vector2(bob, 0f);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Coin Display
    // ═══════════════════════════════════════════════════════════════

    private void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            coinText.text = CurrencyWallet.GetCoins().ToString();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Not Enough Coins Feedback
    // ═══════════════════════════════════════════════════════════════

    private void ShowNotEnoughCoinsFeedback(int currentCoins)
    {
        if (feedbackText == null)
        {
            // Log fallback if no UI text is available
            Debug.Log("[Shop] Not enough coins! Need " + packPrice + ", have " + currentCoins);
            return;
        }

        // Stop any existing feedback animation
        if (_feedbackCoroutine != null)
            StopCoroutine(_feedbackCoroutine);

        feedbackText.text = "Not enough coins!\nNeed " + packPrice + ", you have " + currentCoins;
        feedbackText.gameObject.SetActive(true);

        _feedbackCoroutine = StartCoroutine(FeedbackSequence());
    }

    private IEnumerator FeedbackSequence()
    {
        // Shake animation
        RectTransform rect = feedbackText.rectTransform;
        Vector2 originalPos = rect.anchoredPosition;

        float shakeDuration = 0.4f;
        float shakeIntensity = 8f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity) * (1f - elapsed / shakeDuration);
            rect.anchoredPosition = originalPos + new Vector2(offsetX, 0f);
            yield return null;
        }

        rect.anchoredPosition = originalPos;

        // Hold visible for a moment
        yield return new WaitForSeconds(1.5f);

        // Fade out
        float fadeDuration = 0.5f;
        elapsed = 0f;
        Color startColor = feedbackText.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            feedbackText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Reset and hide
        feedbackText.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
        feedbackText.gameObject.SetActive(false);
        _feedbackCoroutine = null;
    }

    private void HideFeedback()
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Auto-Create Feedback Text
    // ═══════════════════════════════════════════════════════════════

    private TextMeshProUGUI CreateFeedbackText()
    {
        // Find the Canvas to parent under
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return null;

        GameObject feedbackObj = new GameObject("FeedbackText", typeof(RectTransform));
        feedbackObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = feedbackObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 100f);
        rect.sizeDelta = new Vector2(600f, 120f);

        TextMeshProUGUI tmp = feedbackObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.25f, 0.25f, 1f); // Red warning color
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;

        feedbackObj.SetActive(false);
        return tmp;
    }
}
