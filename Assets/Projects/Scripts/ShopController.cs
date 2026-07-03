using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ShopController : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The panel that shows the obtained pack and the 'Open' button.")]
    [SerializeField] private GameObject packObtainedPanel;

    [Tooltip("The main shop panel to hide when a pack is bought.")]
    [SerializeField] private GameObject shopPanel;

    [Tooltip("The header UI to hide when a pack is bought.")]
    [SerializeField] private GameObject headerPanel;

    [Header("Animation Settings")]
    [Tooltip("How long the popup animation takes.")]
    [SerializeField] private float popupDuration = 0.4f;
    
    [Tooltip("Animation curve for the popup scaling. Tip: Make it go slightly above 1 for a bounce effect!")]
    [SerializeField] private AnimationCurve popupCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private void Start()
    {
        // Ensure the pack obtained panel is hidden when the scene starts
        if (packObtainedPanel != null)
        {
            packObtainedPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Call this from the BuyButton's OnClick() event in the inspector.
    /// </summary>
    public void OnBuyButtonClicked()
    {
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
}
