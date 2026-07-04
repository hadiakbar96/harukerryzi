using UnityEngine;
using UnityEngine.SceneManagement;
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

    private Coroutine _feedbackCoroutine;

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
