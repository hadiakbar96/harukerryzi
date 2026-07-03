using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Card reveal controller — works with the existing Pack/Card prefab setup.
///
/// Flow:
///   PackController calls SetCards() then StartReveal()
///   1) Dim background fades in
///   2) 5 card GameObjects rise from below the screen (stacked)
///   3) Player drags left/right to peek the card behind
///   4) Player clicks to reveal the front card (fly-out)
///   5) After 5 cards → summary (5 cards side by side)
///   6) Click to dismiss
///
/// ════════════════════════════════════════════════════════════════
///  SETUP IN UNITY EDITOR:
/// ════════════════════════════════════════════════════════════════
///
///  Scene hierarchy:
///  ├── Pack             ← has PackController + SwipeDetector
///  ├── DimBackground    ← SpriteRenderer (black quad, size covers screen)
///  │                       alpha = 0 at start, sorting order = 5
///  └── CardRevealRoot   ← empty GameObject, attach THIS script
///                           sorting order above DimBackground
///
///  Card prefab:
///  - Has SpriteRenderer + CardDisplay (already exists in project)
///
///  Inspector wiring:
///  - cardPrefab        → your Card.prefab
///  - dimBackground     → DimBackground SpriteRenderer
///  - revealAnchor      → world-space Transform at screen centre
///
///  PackController calls:
///    cardRevealController.SetCards(selectedCards);
///    cardRevealController.StartReveal();
/// </summary>
public class CardRevealController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  Inspector Fields
    // ═══════════════════════════════════════════════════════════════

    [Header("References")]
    [Tooltip("The Card prefab (must have SpriteRenderer + CardDisplay)")]
    [SerializeField] private GameObject cardPrefab;

    [Tooltip("SpriteRenderer on a black full-screen quad for the dim background")]
    [SerializeField] private SpriteRenderer dimBackground;

    [Tooltip("World-space position that acts as the centre anchor for revealed cards")]
    [SerializeField] private Transform revealAnchor;

    [Header("Stack Rise")]
    [Tooltip("World Y to start cards from (below screen)")]
    [SerializeField] private float riseFromY        = -8f;
    [Tooltip("World Y the front card rests at")]
    [SerializeField] private float riseToY          = 0f;
    [Tooltip("Duration of the rise animation")]
    [SerializeField] private float riseDuration     = 0.7f;
    [Tooltip("Delay between each card starting to rise (back to front)")]
    [SerializeField] private float riseStaggerDelay = 0.06f;
    [SerializeField] private AnimationCurve riseCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Stack Layout")]
    [Tooltip("World-space Y offset per depth layer (negative = behind card is lower)")]
    [SerializeField] private float stackOffsetY    = -0.15f;
    [Tooltip("Scale reduction per depth layer")]
    [SerializeField] private float stackScaleStep  = 0.03f;

    [Header("Peek / Drag")]
    [Tooltip("Max world-space X offset when peeking")]
    [SerializeField] private float peekMaxX           = 1.5f;
    [Tooltip("Max Z rotation in degrees when peeking")]
    [SerializeField] private float peekRotationMax    = 10f;
    [Tooltip("Spring-back speed when finger is lifted")]
    [SerializeField] private float peekSpringSpeed    = 12f;
    [Tooltip("Parallax factor for cards behind the front one (0–1)")]
    [SerializeField] private float peekParallaxFactor = 0.15f;
    [Tooltip("Minimum screen-pixel drag before it counts as a drag (not a click)")]
    [SerializeField] private float clickThresholdPx   = 15f;

    [Header("Reveal Animation")]
    [SerializeField] private float revealZoomScale    = 1.1f;
    [SerializeField] private float revealZoomDuration = 0.2f;
    [SerializeField] private float revealHoldDuration = 0.25f;
    [SerializeField] private float revealExitDuration = 0.35f;
    [Tooltip("World Y the card flies out to")]
    [SerializeField] private float revealExitY        = 10f;
    [SerializeField] private AnimationCurve revealExitCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Summary")]
    [Tooltip("World-space X spacing between card centres in summary. Leave 0 to auto-fit across the screen.")]
    [SerializeField] private float summarySpacingX     = 0f;
    [Tooltip("Scale of each card in the summary row. Leave 0 to auto-fit.")]
    [SerializeField] private float summaryCardScale    = 0f;
    [Tooltip("Fraction of the visible screen width used by all cards together (0–1).")]
    [SerializeField] private float summaryScreenFill   = 0.85f;
    [SerializeField] private float summaryAnimDuration  = 0.5f;
    [SerializeField] private float summaryStaggerDelay  = 0.08f;
    [SerializeField] private AnimationCurve summaryCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Dim Background")]
    [SerializeField] private float bgFadeInDuration  = 0.35f;
    [SerializeField] private float bgTargetAlpha     = 0.75f;

    [Header("Events")]
    public UnityEvent OnAllCardsRevealed;

    // ═══════════════════════════════════════════════════════════════
    //  Private State
    // ═══════════════════════════════════════════════════════════════

    private enum State { Inactive, Rising, Ready, RevealAnim, Summary }
    private State _state = State.Inactive;

    // The 5 Card GameObjects instantiated at reveal time
    private GameObject[]    _cardObjects;
    private SpriteRenderer[] _cardRenderers;
    private CardDisplay[]    _cardDisplays;
    private Card[]           _cards;          // data set from PackController

    private int   _currentIndex;
    private float _resolvedSummaryScale; // actual card scale used in summary (may differ from summaryCardScale when auto-fit is on)

    // Input
    private bool    _pointerDown;
    private Vector2 _pointerDownScreenPos;
    private bool    _isDragging;
    private float   _peekCurrentX;
    private float   _peekTargetX;

    // ═══════════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by PackController to pass the selected cards before StartReveal().
    /// </summary>
    public void SetCards(Card[] selectedCards)
    {
        _cards = selectedCards;
    }

    /// <summary>
    /// Call this (from PackController or BottomPackAnimationEvents) to
    /// begin the card reveal sequence.
    /// </summary>
    public void StartReveal()
    {
        if (_cards == null || _cards.Length == 0)
        {
            Debug.LogError("[CardRevealController] No cards set. Call SetCards() first.");
            return;
        }

        _currentIndex = 0;
        _peekCurrentX = 0f;
        _peekTargetX  = 0f;

        SpawnCards();
        StartCoroutine(MainSequence());
    }

    // ═══════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Keep dim background invisible at start
        if (dimBackground != null)
        {
            // Auto-fix: if there is no sprite assigned in the Editor, create a white one
            // and scale it up massively so it covers the whole screen.
            if (dimBackground.sprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                // We set PixelsPerUnit to 1f so the 1x1 pixel texture becomes exactly 1x1 Unity units in world space.
                // Then scaling it by 100 makes it 100x100 world units, easily covering the camera.
                dimBackground.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                dimBackground.transform.localScale = new Vector3(100f, 100f, 1f);
            }

            // Ensure it renders behind the cards (cards use sortingOrder 1..5)
            dimBackground.sortingOrder = -10;

            Color c = dimBackground.color;
            c.a = 0f;
            dimBackground.color = c;
            dimBackground.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (_state == State.Ready)
        {
            HandleInput();
            UpdatePeek();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Card Spawning
    // ═══════════════════════════════════════════════════════════════

    private void SpawnCards()
    {
        int count = _cards.Length;
        _cardObjects   = new GameObject[count];
        _cardRenderers = new SpriteRenderer[count];
        _cardDisplays  = new CardDisplay[count];

        // Anchor position — use revealAnchor if assigned, otherwise screen centre
        Vector3 anchor = revealAnchor != null
            ? revealAnchor.position
            : Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        anchor.z = 0f;

        for (int i = 0; i < count; i++)
        {
            // Instantiate below screen
            Vector3 spawnPos = new Vector3(anchor.x, riseFromY, 0f);
            GameObject obj = Instantiate(cardPrefab, spawnPos, Quaternion.identity, transform);
            obj.SetActive(false);

            _cardObjects[i]   = obj;
            _cardRenderers[i] = obj.GetComponent<SpriteRenderer>();
            _cardDisplays[i]  = obj.GetComponent<CardDisplay>();

            // Assign card artwork via CardDisplay
            if (_cardDisplays[i] != null)
                _cardDisplays[i].SetCard(_cards[i]);
            else if (_cardRenderers[i] != null)
                _cardRenderers[i].sprite = _cards[i].artwork;

            // Sorting order: card[0] = front (highest order), card[4] = back (lowest)
            if (_cardRenderers[i] != null)
                _cardRenderers[i].sortingOrder = count - i;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Main Sequence
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator MainSequence()
    {
        // 1) Fade in dim background
        yield return StartCoroutine(FadeInBackground());

        // 2) Cards rise from below
        _state = State.Rising;
        yield return StartCoroutine(StackRise());

        // 3) Wait for player to reveal all cards
        _state = State.Ready;
        while (_state != State.Summary)
            yield return null;

        // 4) Show summary
        yield return StartCoroutine(ShowSummary());

        // 5) Wait for dismiss click
        yield return StartCoroutine(WaitForDismissClick());

        OnAllCardsRevealed?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Phase 1: Dim Background Fade-In
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator FadeInBackground()
    {
        if (dimBackground == null) yield break;

        Color c = dimBackground.color;
        c.a = 0f;
        dimBackground.color = c;

        float elapsed = 0f;
        while (elapsed < bgFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bgFadeInDuration);
            c.a = Mathf.Lerp(0f, bgTargetAlpha, t);
            dimBackground.color = c;
            yield return null;
        }

        c.a = bgTargetAlpha;
        dimBackground.color = c;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Phase 2: Stack Rise
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator StackRise()
    {
        int count = _cardObjects.Length;

        // Get anchor Y
        Vector3 anchor = GetAnchorWorldPos();

        // Activate all cards, place them below screen
        for (int i = 0; i < count; i++)
        {
            _cardObjects[i].SetActive(true);

            float depth   = (float)i;
            float targetY = anchor.y + riseToY + stackOffsetY * depth;
            float scale   = 1f - stackScaleStep * depth;

            _cardObjects[i].transform.position   = new Vector3(anchor.x, riseFromY, 0f);
            _cardObjects[i].transform.localScale  = new Vector3(scale, scale, 1f);
            _cardObjects[i].transform.rotation    = Quaternion.identity;
        }

        // Rise with stagger — back cards start first
        float totalDuration = riseDuration + riseStaggerDelay * (count - 1);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = count - 1; i >= 0; i--)
            {
                float delay    = riseStaggerDelay * (count - 1 - i);
                float cardTime = Mathf.Clamp01((elapsed - delay) / riseDuration);
                float curved   = riseCurve.Evaluate(cardTime);

                float depth   = (float)i;
                float targetY = anchor.y + riseToY + stackOffsetY * depth;
                float y       = Mathf.LerpUnclamped(riseFromY, targetY, curved);

                Vector3 pos = _cardObjects[i].transform.position;
                pos.y = y;
                _cardObjects[i].transform.position = pos;
            }

            yield return null;
        }

        SnapStackPositions();
    }

    private void SnapStackPositions()
    {
        Vector3 anchor = GetAnchorWorldPos();
        for (int i = _currentIndex; i < _cardObjects.Length; i++)
        {
            if (_cardObjects[i] == null) continue;
            float depth   = (float)(i - _currentIndex);
            float targetY = anchor.y + riseToY + stackOffsetY * depth;
            float scale   = 1f - stackScaleStep * depth;

            _cardObjects[i].transform.position   = new Vector3(anchor.x, targetY, 0f);
            _cardObjects[i].transform.localScale  = new Vector3(scale, scale, 1f);
            _cardObjects[i].transform.rotation    = Quaternion.identity;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Phase 3: Input — Peek & Click
    // ═══════════════════════════════════════════════════════════════

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _pointerDown          = true;
            _pointerDownScreenPos = (Vector2)Input.mousePosition;
            _isDragging           = false;
        }

        if (_pointerDown && Input.GetMouseButton(0))
        {
            Vector2 current = (Vector2)Input.mousePosition;
            float   deltaX  = current.x - _pointerDownScreenPos.x;

            if (Mathf.Abs(deltaX) > clickThresholdPx)
                _isDragging = true;

            if (_isDragging)
            {
                // Convert screen-pixel delta to world units
                float worldPerPixel = GetWorldUnitsPerPixel();
                _peekTargetX = Mathf.Clamp(
                    deltaX * worldPerPixel, -peekMaxX, peekMaxX);
            }
        }

        if (Input.GetMouseButtonUp(0) && _pointerDown)
        {
            if (!_isDragging)
            {
                _state = State.RevealAnim;
                StartCoroutine(RevealCurrentCard());
            }
            else
            {
                _peekTargetX = 0f;
            }

            _pointerDown = false;
            _isDragging  = false;
        }
    }

    private void UpdatePeek()
    {
        _peekCurrentX = Mathf.Lerp(
            _peekCurrentX, _peekTargetX, Time.deltaTime * peekSpringSpeed);

        if (!_isDragging && Mathf.Abs(_peekCurrentX) < 0.01f)
            _peekCurrentX = 0f;

        Vector3 anchor = GetAnchorWorldPos();

        for (int i = _currentIndex; i < _cardObjects.Length; i++)
        {
            if (_cardObjects[i] == null) continue;

            float depth   = (float)(i - _currentIndex);
            float targetY = anchor.y + riseToY + stackOffsetY * depth;

            // Parallax: front card moves fully, cards behind move less
            float parallax = (depth > 0f)
                ? peekParallaxFactor / depth
                : 1f;

            float x = _peekCurrentX * parallax;
            _cardObjects[i].transform.position = new Vector3(
                anchor.x + x, targetY, 0f);

            // Rotation only for the frontmost card
            if (i == _currentIndex)
            {
                float rotZ = -(_peekCurrentX / peekMaxX) * peekRotationMax;
                _cardObjects[i].transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Phase 4: Reveal (Fly-Out)
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator RevealCurrentCard()
    {
        int        idx  = _currentIndex;
        GameObject obj  = _cardObjects[idx];
        Vector3    anchor = GetAnchorWorldPos();

        // Reset peek
        _peekCurrentX = 0f;
        _peekTargetX  = 0f;
        obj.transform.position = new Vector3(anchor.x, anchor.y + riseToY, 0f);
        obj.transform.rotation = Quaternion.identity;

        // ── Zoom in ──
        Vector3 baseScale = obj.transform.localScale;
        Vector3 zoomScale = Vector3.one * revealZoomScale;
        float elapsed = 0f;

        while (elapsed < revealZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / revealZoomDuration));
            obj.transform.localScale = Vector3.Lerp(baseScale, zoomScale, t);
            yield return null;
        }
        obj.transform.localScale = zoomScale;

        // ── Dramatic hold ──
        yield return new WaitForSeconds(revealHoldDuration);

        // ── Fly out (up + fade) ──
        SpriteRenderer sr = _cardRenderers[idx];
        elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Vector3 endPos   = new Vector3(anchor.x, revealExitY, 0f);
        Color   startCol = sr != null ? sr.color : Color.white;

        while (elapsed < revealExitDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / revealExitDuration);
            float curved = revealExitCurve.Evaluate(t);

            obj.transform.position   = Vector3.LerpUnclamped(startPos, endPos, curved);
            obj.transform.localScale = Vector3.Lerp(zoomScale, Vector3.one, t);

            if (sr != null)
            {
                Color c = startCol;
                c.a = 1f - t;
                sr.color = c;
            }

            yield return null;
        }

        // Reset alpha, deactivate
        if (sr != null) { Color c = startCol; c.a = 1f; sr.color = c; }
        obj.SetActive(false);

        _currentIndex++;

        if (_currentIndex >= _cardObjects.Length)
        {
            _state = State.Summary;
        }
        else
        {
            yield return StartCoroutine(ShiftStackForward());
            _state = State.Ready;
        }
    }

    private IEnumerator ShiftStackForward()
    {
        int remaining = _cardObjects.Length - _currentIndex;
        Vector3 anchor = GetAnchorWorldPos();
        float duration = 0.25f;
        float elapsed  = 0f;

        Vector3[] startPos   = new Vector3[remaining];
        Vector3[] startScale = new Vector3[remaining];

        for (int i = 0; i < remaining; i++)
        {
            int idx = _currentIndex + i;
            startPos[i]   = _cardObjects[idx].transform.position;
            startScale[i] = _cardObjects[idx].transform.localScale;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < remaining; i++)
            {
                int   idx        = _currentIndex + i;
                float depth      = (float)i;
                float targetY    = anchor.y + riseToY + stackOffsetY * depth;
                float targetSc   = 1f - stackScaleStep * depth;

                _cardObjects[idx].transform.position = Vector3.Lerp(
                    startPos[i], new Vector3(anchor.x, targetY, 0f), t);
                _cardObjects[idx].transform.localScale = Vector3.Lerp(
                    startScale[i], new Vector3(targetSc, targetSc, 1f), t);
            }

            yield return null;
        }

        SnapStackPositions();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Phase 5: Summary
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator ShowSummary()
    {
        int     count  = _cardObjects.Length;
        Vector3 anchor = GetAnchorWorldPos();

        // ── Auto-calculate spacing and scale from camera width ──────────
        float resolvedSpacing = summarySpacingX;
        float resolvedScale   = summaryCardScale;

        if (resolvedSpacing <= 0f || resolvedScale <= 0f)
        {
            // Visible world width of the camera
            float camHalfWidth = Camera.main.orthographic
                ? Camera.main.orthographicSize * Camera.main.aspect
                : Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad)
                  * Mathf.Abs(Camera.main.transform.position.z) * Camera.main.aspect;

            float usableWidth = camHalfWidth * 2f * summaryScreenFill;

            // Spacing = usable width divided evenly between card centres
            if (resolvedSpacing <= 0f)
                resolvedSpacing = count > 1 ? usableWidth / (count - 1) : 0f;

            // Scale: make each card fit inside its slot (slot width = spacing)
            // We measure the card's sprite width in world units at scale 1
            if (resolvedScale <= 0f)
            {
                float slotWidth   = count > 1 ? resolvedSpacing : usableWidth;
                float cardWorldW  = GetCardSpriteWorldWidth();
                float fitScale    = cardWorldW > 0f ? (slotWidth * 0.9f) / cardWorldW : 0.45f;
                resolvedScale     = Mathf.Clamp(fitScale, 0.1f, 1.5f);
            }
        }
        // ───────────────────────────────────────────────────────────────

        // Cache the resolved scale so SummaryPulse can use it
        _resolvedSummaryScale = resolvedScale;

        float totalWidth = resolvedSpacing * (count - 1);
        float startX     = -totalWidth / 2f;

        // Re-activate all cards at anchor, scale 0
        for (int i = 0; i < count; i++)
        {
            _cardObjects[i].SetActive(true);
            _cardObjects[i].transform.position   = new Vector3(anchor.x, anchor.y, 0f);
            _cardObjects[i].transform.localScale  = Vector3.zero;
            _cardObjects[i].transform.rotation    = Quaternion.identity;

            if (_cardRenderers[i] != null)
            {
                Color c = _cardRenderers[i].color;
                c.a = 1f;
                _cardRenderers[i].color = c;
            }

            // Sort left-to-right in the render order
            if (_cardRenderers[i] != null)
                _cardRenderers[i].sortingOrder = i + 1;
        }

        // Animate cards spreading out and scaling up
        float totalDuration = summaryAnimDuration + summaryStaggerDelay * (count - 1);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < count; i++)
            {
                float delay    = summaryStaggerDelay * i;
                float cardTime = Mathf.Clamp01((elapsed - delay) / summaryAnimDuration);
                float curved   = summaryCurve.Evaluate(cardTime);

                float targetX = anchor.x + startX + resolvedSpacing * i;
                _cardObjects[i].transform.position = Vector3.Lerp(
                    new Vector3(anchor.x, anchor.y, 0f),
                    new Vector3(targetX, anchor.y, 0f), curved);

                float sc = Mathf.Lerp(0f, resolvedScale, curved);
                _cardObjects[i].transform.localScale = new Vector3(sc, sc, 1f);
            }

            yield return null;
        }

        // Snap final positions
        for (int i = 0; i < count; i++)
        {
            float targetX = anchor.x + startX + resolvedSpacing * i;
            _cardObjects[i].transform.position  = new Vector3(targetX, anchor.y, 0f);
            _cardObjects[i].transform.localScale = Vector3.one * resolvedScale;
        }

        // Subtle pulse for Rare / SuperRare cards
        for (int i = 0; i < count; i++)
        {
            if (_cards[i].rarity != CardRarity.Normal)
                StartCoroutine(SummaryPulse(i));
        }
    }

    private IEnumerator SummaryPulse(int slotIndex)
    {
        // Use the resolved scale (auto-computed), NOT summaryCardScale which may be 0
        float baseScale   = _resolvedSummaryScale;
        float pulseAmount = _cards[slotIndex].rarity == CardRarity.SuperRare ? 0.04f : 0.02f;
        float speed       = _cards[slotIndex].rarity == CardRarity.SuperRare ? 2.5f  : 1.8f;

        while (_state == State.Summary)
        {
            float t     = Time.time * speed + slotIndex;
            float scale = baseScale + Mathf.Sin(t * Mathf.PI) * pulseAmount;
            _cardObjects[slotIndex].transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Dismiss
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator WaitForDismissClick()
    {
        // Skip a couple frames so the last click doesn't trigger dismiss immediately
        yield return null;
        yield return null;

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        // Fade out everything
        float fadeDuration = 0.4f;
        float elapsed      = 0f;

        Color bgColor = dimBackground != null ? dimBackground.color : Color.black;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (dimBackground != null)
            {
                Color c = bgColor;
                c.a = Mathf.Lerp(bgTargetAlpha, 0f, t);
                dimBackground.color = c;
            }

            foreach (SpriteRenderer sr in _cardRenderers)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = 1f - t;
                sr.color = c;
            }

            yield return null;
        }

        // Clean up spawned cards
        foreach (GameObject obj in _cardObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        _cardObjects   = null;
        _cardRenderers = null;
        _cardDisplays  = null;
        _state         = State.Inactive;
        SceneManager.LoadScene("Collection");

        
    }

    // ═══════════════════════════════════════════════════════════════
    //  Utility
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Returns the world-space anchor position for card placement.</summary>
    private Vector3 GetAnchorWorldPos()
    {
        if (revealAnchor != null)
            return new Vector3(revealAnchor.position.x, revealAnchor.position.y, 0f);

        // Fallback: world-space centre of the camera view
        Vector3 p = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        p.z = 0f;
        return p;
    }

    /// <summary>Returns how many world units correspond to one screen pixel.</summary>
    private float GetWorldUnitsPerPixel()
    {
        Camera cam = Camera.main;
        if (cam == null) return 0.01f;

        if (cam.orthographic)
            return (cam.orthographicSize * 2f) / Screen.height;

        // Perspective fallback
        float distToAnchor = Mathf.Abs(cam.transform.position.z - 0f);
        float halfHeight   = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * distToAnchor;
        return (halfHeight * 2f) / Screen.height;
    }

    /// <summary>
    /// Returns the world-space width of the card sprite at scale 1.
    /// Uses the first card renderer's sprite to measure. Falls back to 0 if unavailable.
    /// </summary>
    private float GetCardSpriteWorldWidth()
    {
        // Try to get sprite from the first instantiated card
        for (int i = 0; i < _cardRenderers.Length; i++)
        {
            if (_cardRenderers[i] != null && _cardRenderers[i].sprite != null)
            {
                Sprite s = _cardRenderers[i].sprite;
                // sprite.bounds gives the local-space size at scale 1
                return s.bounds.size.x;
            }
        }
        return 0f;
    }
}
