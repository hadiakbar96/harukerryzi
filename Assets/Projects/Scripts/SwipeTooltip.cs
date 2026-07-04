using System.Collections;
using UnityEngine;

/// <summary>
/// Displays a swipe tooltip over the SliceZone in the openPack scene.
/// Shows a dashed line through the middle of the slice zone and a
/// "Swipe here →" label. Fades out and destroys itself once the player
/// successfully swipes to open the pack.
///
/// Setup: Attach this to the same GameObject that has SwipeDetector
///        (or any root object in the openPack scene). It auto-finds
///        the SliceZone by tag.
/// </summary>
public class SwipeTooltip : MonoBehaviour
{
    [Header("Tooltip Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinAlpha = 0.4f;
    [SerializeField] private float pulseMaxAlpha = 0.9f;

    [Header("Arrow Animation")]
    [SerializeField] private float arrowBobDistance = 0.3f;
    [SerializeField] private float arrowBobSpeed = 1.5f;

    // Runtime references
    private GameObject _tooltipRoot;
    private SpriteRenderer _lineRenderer;
    private SpriteRenderer _arrowRenderer;
    private TextMesh _labelText;
    private TextMesh _labelShadow;
    private float _baseAlpha;
    private bool _isHidden;
    private Vector3 _arrowOriginalPos;

    private void Start()
    {
        // Find the SliceZone
        GameObject sliceZone = GameObject.FindWithTag("SliceZone");
        if (sliceZone == null)
        {
            Debug.LogWarning("[SwipeTooltip] Could not find SliceZone by tag.");
            enabled = false;
            return;
        }

        CreateTooltip(sliceZone.transform);
        StartCoroutine(FadeIn());
    }

    private void Update()
    {
        if (_isHidden || _tooltipRoot == null) return;

        // Pulse the line alpha
        float pulse = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);

        if (_lineRenderer != null)
        {
            Color c = _lineRenderer.color;
            _lineRenderer.color = new Color(c.r, c.g, c.b, pulse);
        }

        // Bob the arrow horizontally
        if (_arrowRenderer != null)
        {
            float offset = Mathf.Sin(Time.time * arrowBobSpeed) * arrowBobDistance;
            _arrowRenderer.transform.localPosition = _arrowOriginalPos + new Vector3(offset, 0f, 0f);
        }

        // Pulse the label
        if (_labelText != null)
        {
            Color tc = _labelText.color;
            _labelText.color = new Color(tc.r, tc.g, tc.b, pulse);
        }
        if (_labelShadow != null)
        {
            Color sc = _labelShadow.color;
            _labelShadow.color = new Color(sc.r, sc.g, sc.b, pulse * 0.5f);
        }
    }

    /// <summary>
    /// Call this to hide and destroy the tooltip (e.g., when the pack opens).
    /// </summary>
    public void Hide()
    {
        if (_isHidden) return;
        _isHidden = true;
        StartCoroutine(FadeOutAndDestroy());
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tooltip Construction
    // ═══════════════════════════════════════════════════════════════

    private void CreateTooltip(Transform sliceZone)
    {
        _tooltipRoot = new GameObject("SwipeTooltipRoot");
        _tooltipRoot.transform.SetParent(sliceZone.parent, false);
        _tooltipRoot.transform.localPosition = sliceZone.localPosition;

        // Get the collider bounds for sizing
        BoxCollider2D col = sliceZone.GetComponent<BoxCollider2D>();
        float zoneWidth = 5f;  // defaults
        float zoneHeight = 1.5f;
        if (col != null)
        {
            zoneWidth = col.size.x * sliceZone.lossyScale.x;
            zoneHeight = col.size.y * sliceZone.lossyScale.y;
        }

        // --- Dashed Line ---
        CreateDashedLine(zoneWidth);

        // --- Arrow indicator ---
        CreateArrow(zoneWidth);

        // --- "Swipe here" label ---
        CreateLabel(zoneWidth);

        // Start fully transparent
        SetAllAlpha(0f);
    }

    private void CreateDashedLine(float totalWidth)
    {
        int dashCount = 12;
        float dashWidth = totalWidth / (dashCount * 2f - 1f);
        float startX = -totalWidth / 2f;

        for (int i = 0; i < dashCount; i++)
        {
            GameObject dash = new GameObject("Dash_" + i);
            dash.transform.SetParent(_tooltipRoot.transform, false);

            SpriteRenderer sr = dash.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWhitePixelSprite();
            sr.color = new Color(1f, 1f, 1f, 0.7f);
            sr.sortingOrder = 100;

            float x = startX + i * dashWidth * 2f + dashWidth / 2f;
            dash.transform.localPosition = new Vector3(x, 0f, -0.1f);
            dash.transform.localScale = new Vector3(dashWidth * 0.8f, 0.04f, 1f);

            // Store first dash renderer for pulse reference
            if (i == 0) _lineRenderer = sr;
        }
    }

    private void CreateArrow(float zoneWidth)
    {
        // Create a simple arrow using "→" text
        GameObject arrowObj = new GameObject("SwipeArrow");
        arrowObj.transform.SetParent(_tooltipRoot.transform, false);
        // Between the label (left of the pack) and the pack's left edge
        _arrowOriginalPos = new Vector3(-(zoneWidth / 2f) - 0.45f, 0f, -0.1f);
        arrowObj.transform.localPosition = _arrowOriginalPos;

        // Arrow head using a SpriteRenderer triangle approximation
        _arrowRenderer = arrowObj.AddComponent<SpriteRenderer>();
        _arrowRenderer.sprite = CreateArrowSprite();
        _arrowRenderer.color = new Color(1f, 0.9f, 0.3f, 0.8f);
        _arrowRenderer.sortingOrder = 100;
        arrowObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }

    private void CreateLabel(float zoneWidth)
    {
        // Place the label to the left of the pack, on the tear line.
        // The bobbing arrow sits between the label and the pack edge.
        float labelX = -(zoneWidth / 2f) - 1f;

        // Shadow text
        GameObject shadowObj = new GameObject("LabelShadow");
        shadowObj.transform.SetParent(_tooltipRoot.transform, false);
        shadowObj.transform.localPosition = new Vector3(labelX + 0.03f, -0.03f, -0.1f);

        _labelShadow = shadowObj.AddComponent<TextMesh>();
        _labelShadow.text = "Swipe here";
        _labelShadow.fontSize = 48;
        _labelShadow.characterSize = 0.15f;
        _labelShadow.anchor = TextAnchor.MiddleRight;
        _labelShadow.alignment = TextAlignment.Center;
        _labelShadow.color = new Color(0f, 0f, 0f, 0.4f);
        _labelShadow.fontStyle = FontStyle.Bold;

        MeshRenderer shadowMR = shadowObj.GetComponent<MeshRenderer>();
        if (shadowMR != null) shadowMR.sortingOrder = 99;

        // Main text
        GameObject labelObj = new GameObject("SwipeLabel");
        labelObj.transform.SetParent(_tooltipRoot.transform, false);
        labelObj.transform.localPosition = new Vector3(labelX, 0f, -0.1f);

        _labelText = labelObj.AddComponent<TextMesh>();
        _labelText.text = "Swipe here";
        _labelText.fontSize = 48;
        _labelText.characterSize = 0.15f;
        _labelText.anchor = TextAnchor.MiddleRight;
        _labelText.alignment = TextAlignment.Center;
        _labelText.color = new Color(1f, 0.95f, 0.7f, 0.85f);
        _labelText.fontStyle = FontStyle.Bold;

        MeshRenderer labelMR = labelObj.GetComponent<MeshRenderer>();
        if (labelMR != null) labelMR.sortingOrder = 100;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sprite Helpers
    // ═══════════════════════════════════════════════════════════════

    private static Sprite CreateWhitePixelSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private static Sprite CreateArrowSprite()
    {
        // Create a simple right-pointing triangle
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color clear = new Color(0, 0, 0, 0);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Triangle pointing right
                float normalizedY = (float)y / size;
                float halfHeight = Mathf.Abs(normalizedY - 0.5f);
                float threshold = (1f - halfHeight * 2f) * size;

                if (x < threshold)
                    tex.SetPixel(x, y, white);
                else
                    tex.SetPixel(x, y, clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), (float)size);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Fade Animations
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            SetAllAlpha(t);
            yield return null;
        }
        SetAllAlpha(1f);
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / duration);
            SetAllAlpha(t);
            yield return null;
        }

        if (_tooltipRoot != null)
            Destroy(_tooltipRoot);
    }

    private void SetAllAlpha(float alpha)
    {
        if (_tooltipRoot == null) return;

        SpriteRenderer[] sprites = _tooltipRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in sprites)
        {
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, c.a * alpha);
        }

        TextMesh[] texts = _tooltipRoot.GetComponentsInChildren<TextMesh>(true);
        foreach (var tm in texts)
        {
            Color c = tm.color;
            tm.color = new Color(c.r, c.g, c.b, c.a * alpha);
        }
    }
}
