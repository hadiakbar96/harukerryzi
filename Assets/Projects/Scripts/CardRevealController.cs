using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Pokemon TCG Pocket-style card reveal controller (Canvas UI).
///
/// Flow: Pack dibuka → OnPackOpened → StartReveal() →
///   1) Background fade-in
///   2) 5 kartu naik dari bawah (stack)
///   3) Player hold+drag kiri/kanan untuk peek kartu belakang
///   4) Player click untuk reveal kartu depan (fly-out)
///   5) Setelah 5 kartu → summary (5 kartu berdampingan)
///
/// ═══════════════════════════════════════════════════════════════
///  SETUP DI UNITY EDITOR:
/// ═══════════════════════════════════════════════════════════════
///
///  Canvas (Screen Space - Overlay, sort order tinggi mis. 10)
///  └── CardRevealPanel  ← attach script ini
///      │                   + CanvasGroup (alpha=1)
///      │                   + Image (warna hitam, alpha=0, Raycast Target ON)
///      │
///      ├── CardContainer  (RectTransform: stretch-all, anchors 0-1)
///      │   ├── Card_0     (Image, Raycast Target OFF) ← backmost
///      │   ├── Card_1     (Image, Raycast Target OFF)
///      │   ├── Card_2     (Image, Raycast Target OFF)
///      │   ├── Card_3     (Image, Raycast Target OFF)
///      │   └── Card_4     (Image, Raycast Target OFF) ← frontmost
///      │
///      ├── GlowOverlay    (Image, centered, size besar ~800x1100)
///      │                   warna putih, alpha=0, Raycast Target OFF
///      │
///      └── SummaryLabel   (TextMeshPro/Text, opsional, "Tap to continue")
///                          Raycast Target OFF
///
///  Catatan:
///  - Card_0 s/d Card_4 TANPA sprite awal (dikosongkan, diisi runtime)
///  - Semua Card preserve aspect: centang "Preserve Aspect" pada Image
///  - Set "Native Size" pada setiap Card Image setelah assign test sprite
///  - CardRevealPanel: start DISABLED (SetActive false)
///  - Wire PackOpenController.OnPackOpened →
///      1) CardRevealPanel.SetActive(true)
///      2) CardRevealController.StartReveal()
///      3) CardPack.SetActive(false) ← opsional, sembunyikan pack
///
/// ═══════════════════════════════════════════════════════════════
/// </summary>
public class CardRevealController : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════
    //  Enums & Data Classes
    // ════════════════════════════════════════════════════════════

    public enum CardRarity { Normal, Rare, SuperRare }

    [System.Serializable]
    public class CardSlot
    {
        [Tooltip("Sprite kartu yang akan ditampilkan")]
        public Sprite sprite;

        [Tooltip("Rarity kartu untuk efek visual")]
        public CardRarity rarity;

        // Diisi otomatis oleh script dari children CardContainer
        [HideInInspector] public Image image;
        [HideInInspector] public RectTransform rect;
        [HideInInspector] public CanvasGroup canvasGroup;
    }

    private enum State { Inactive, Rising, Ready, RevealAnim, Summary }

    // ════════════════════════════════════════════════════════════
    //  Inspector Fields
    // ════════════════════════════════════════════════════════════

    [Header("=== Card Data (Urutan Reveal: 0=pertama, 4=terakhir) ===")]
    [SerializeField] private CardSlot[] cardSlots = new CardSlot[5];

    [Header("=== UI References ===")]
    [Tooltip("Parent RectTransform yang berisi Card_0 s/d Card_4")]
    [SerializeField] private RectTransform cardContainer;

    [Tooltip("Image background panel ini (untuk fade-in gelap)")]
    [SerializeField] private Image backgroundOverlay;



    [Header("=== Stack Rise Animation ===")]
    [SerializeField] private float riseFromY       = -1500f;
    [SerializeField] private float riseToY          = 0f;
    [SerializeField] private float riseDuration     = 0.7f;
    [SerializeField] private float riseStaggerDelay = 0.06f;
    [SerializeField] private AnimationCurve riseCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("=== Stack Layout ===")]
    [Tooltip("Jarak Y antar kartu yang bertumpuk (negatif = ke bawah)")]
    [SerializeField] private float stackOffsetY    = -30f;
    [Tooltip("Pengecilan scale per depth layer")]
    [SerializeField] private float stackScaleStep  = 0.025f;

    [Header("=== Peek / Drag ===")]
    [Tooltip("Jarak horizontal max saat peek")]
    [SerializeField] private float peekMaxX           = 250f;
    [Tooltip("Rotasi Z max saat peek (derajat)")]
    [SerializeField] private float peekRotationMax    = 10f;
    [Tooltip("Kecepatan spring-back saat lepas drag")]
    [SerializeField] private float peekSpringSpeed    = 12f;
    [Tooltip("Parallax factor untuk kartu di belakang (0-1)")]
    [SerializeField] private float peekParallaxFactor = 0.15f;
    [Tooltip("Minimal pixel drag sebelum dianggap drag (bukan click)")]
    [SerializeField] private float clickThreshold     = 15f;

    [Header("=== Reveal Animation ===")]
    [Tooltip("Scale zoom saat reveal moment")]
    [SerializeField] private float revealZoomScale    = 1.12f;
    [SerializeField] private float revealZoomDuration = 0.2f;
    [Tooltip("Durasi hold sebelum card exit")]
    [SerializeField] private float revealHoldDuration = 0.25f;
    [SerializeField] private float revealExitDuration = 0.35f;
    [Tooltip("Posisi Y akhir saat kartu keluar (ke atas layar)")]
    [SerializeField] private float revealExitY        = 1600f;
    [SerializeField] private AnimationCurve revealExitCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("=== Summary ===")]
    [SerializeField] private float summarySpacing       = 320f;
    [SerializeField] private float summaryCardScale     = 0.55f;
    [SerializeField] private float summaryAnimDuration   = 0.6f;
    [SerializeField] private float summaryStaggerDelay   = 0.08f;
    [SerializeField] private AnimationCurve summaryCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);



    [Header("=== Background ===")]
    [SerializeField] private float bgFadeInDuration  = 0.35f;
    [SerializeField] private float bgTargetAlpha     = 0.75f;

    [Header("=== Events ===")]
    public UnityEvent OnAllCardsRevealed;

    // ════════════════════════════════════════════════════════════
    //  Private State
    // ════════════════════════════════════════════════════════════

    private State  _state = State.Inactive;
    private int    _currentIndex;

    // Input tracking
    private bool    _pointerDown;
    private Vector2 _pointerDownScreenPos;
    private bool    _isDragging;
    private float   _peekCurrentX;
    private float   _peekTargetX;

    // Canvas reference for coordinate conversion
    private Canvas        _canvas;
    private RectTransform _canvasRect;

    // ════════════════════════════════════════════════════════════
    //  Lifecycle
    // ════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Cari Canvas parent
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.GetComponent<RectTransform>();

        // Cache references dari children cardContainer
        CacheCardReferences();

        // Sembunyikan semua kartu awal
        HideAllCards();
    }

    private void Update()
    {
        if (_state == State.Ready)
        {
            HandleInput();
            UpdatePeek();
        }
    }

    // ════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Dipanggil dari PackOpenController.OnPackOpened (wired di Inspector).
    /// Memulai seluruh sequence card reveal.
    /// </summary>
    public void StartReveal()
    {
        _currentIndex = 0;
        _peekCurrentX = 0f;
        _peekTargetX  = 0f;

        // Assign sprite ke setiap card image
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i].image != null && cardSlots[i].sprite != null)
            {
                cardSlots[i].image.sprite = cardSlots[i].sprite;
                cardSlots[i].image.preserveAspect = true;
            }
        }

        StartCoroutine(MainSequence());
    }

    // ════════════════════════════════════════════════════════════
    //  Setup Helpers
    // ════════════════════════════════════════════════════════════

    private void CacheCardReferences()
    {
        if (cardContainer == null) return;

        // Ambil semua Image children dari cardContainer
        // Urutan child di scene: Card_0 (index 0) = backmost → Card_4 (index 4) = frontmost
        // Mapping: cardSlots[0] = pertama direveal = paling depan = child terakhir
        int childCount = cardContainer.childCount;
        int slotCount  = Mathf.Min(cardSlots.Length, childCount);

        for (int i = 0; i < slotCount; i++)
        {
            // cardSlots[0] (reveal pertama) = child terakhir (render paling depan)
            int childIdx = childCount - 1 - i;
            Transform child = cardContainer.GetChild(childIdx);

            cardSlots[i].image = child.GetComponent<Image>();
            cardSlots[i].rect  = child.GetComponent<RectTransform>();

            // Set preserve aspect otomatis (tidak perlu set manual di Inspector)
            if (cardSlots[i].image != null)
                cardSlots[i].image.preserveAspect = true;

            // Tambahkan CanvasGroup jika belum ada (untuk fade)
            cardSlots[i].canvasGroup = child.GetComponent<CanvasGroup>();
            if (cardSlots[i].canvasGroup == null)
                cardSlots[i].canvasGroup = child.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void HideAllCards()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i].image != null)
                cardSlots[i].image.gameObject.SetActive(false);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  Main Sequence
    // ════════════════════════════════════════════════════════════

    private IEnumerator MainSequence()
    {
        // 1) Fade-in background gelap
        yield return StartCoroutine(FadeInBackground());

        // 2) Kartu naik dari bawah
        _state = State.Rising;
        yield return StartCoroutine(StackRise());

        // 3) Ready untuk input
        _state = State.Ready;

        // Tunggu hingga semua kartu di-reveal
        while (_state != State.Summary)
            yield return null;

        // 4) Summary
        yield return StartCoroutine(ShowSummary());

        // 5) Tunggu click terakhir untuk dismiss
        yield return StartCoroutine(WaitForDismissClick());

        OnAllCardsRevealed?.Invoke();
    }

    // ════════════════════════════════════════════════════════════
    //  Phase 1: Background Fade-In
    // ════════════════════════════════════════════════════════════

    private IEnumerator FadeInBackground()
    {
        if (backgroundOverlay == null) yield break;

        Color c = backgroundOverlay.color;
        c.a = 0f;
        backgroundOverlay.color = c;

        float elapsed = 0f;
        while (elapsed < bgFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bgFadeInDuration);
            c.a = Mathf.Lerp(0f, bgTargetAlpha, t);
            backgroundOverlay.color = c;
            yield return null;
        }
        c.a = bgTargetAlpha;
        backgroundOverlay.color = c;
    }

    // ════════════════════════════════════════════════════════════
    //  Phase 2: Stack Rise
    // ════════════════════════════════════════════════════════════

    private IEnumerator StackRise()
    {
        int count = cardSlots.Length;

        // Setup posisi awal: semua di bawah layar
        for (int i = 0; i < count; i++)
        {
            CardSlot slot = cardSlots[i];
            if (slot.rect == null) continue;

            slot.image.gameObject.SetActive(true);
            slot.canvasGroup.alpha = 1f;

            // Posisi awal: di bawah
            slot.rect.anchoredPosition = new Vector2(0f, riseFromY);

            // Scale: kartu depan (index 0) paling besar
            float depth = (float)i;
            float scale = 1f - stackScaleStep * depth;
            slot.rect.localScale = new Vector3(scale, scale, 1f);

            slot.rect.localRotation = Quaternion.identity;
        }

        // Animasi naik dengan stagger (dari belakang ke depan)
        float totalDuration = riseDuration + riseStaggerDelay * (count - 1);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = count - 1; i >= 0; i--)
            {
                if (cardSlots[i].rect == null) continue;

                // Kartu belakang mulai duluan
                float delay = riseStaggerDelay * (count - 1 - i);
                float cardTime = Mathf.Clamp01((elapsed - delay) / riseDuration);
                float curved = riseCurve.Evaluate(cardTime);

                float depth   = (float)i;
                float targetY = riseToY + stackOffsetY * depth;
                float y       = Mathf.LerpUnclamped(riseFromY, targetY, curved);

                cardSlots[i].rect.anchoredPosition = new Vector2(0f, y);
            }

            yield return null;
        }

        // Snap posisi final
        SnapStackPositions();
    }

    /// <summary>
    /// Posisikan kartu yang tersisa ke posisi stack yang benar.
    /// </summary>
    private void SnapStackPositions()
    {
        for (int i = _currentIndex; i < cardSlots.Length; i++)
        {
            if (cardSlots[i].rect == null) continue;

            float depthFromFront = (float)(i - _currentIndex);
            float targetY = riseToY + stackOffsetY * depthFromFront;
            float scale   = 1f - stackScaleStep * depthFromFront;

            cardSlots[i].rect.anchoredPosition = new Vector2(0f, targetY);
            cardSlots[i].rect.localScale       = new Vector3(scale, scale, 1f);
            cardSlots[i].rect.localRotation     = Quaternion.identity;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  Phase 3: Input Handling (Peek & Click)
    // ════════════════════════════════════════════════════════════

    private void HandleInput()
    {
        // --- Pointer Down ---
        if (Input.GetMouseButtonDown(0))
        {
            _pointerDown          = true;
            _pointerDownScreenPos = (Vector2)Input.mousePosition;
            _isDragging           = false;
        }

        // --- Pointer Hold (Drag) ---
        if (_pointerDown && Input.GetMouseButton(0))
        {
            Vector2 current  = (Vector2)Input.mousePosition;
            float   deltaX   = current.x - _pointerDownScreenPos.x;

            if (Mathf.Abs(deltaX) > clickThreshold)
                _isDragging = true;

            if (_isDragging)
            {
                // Convert screen pixels ke canvas units
                float scaleFactor = GetCanvasScaleFactor();
                _peekTargetX = Mathf.Clamp(
                    deltaX / scaleFactor, -peekMaxX, peekMaxX);
            }
        }

        // --- Pointer Up ---
        if (Input.GetMouseButtonUp(0) && _pointerDown)
        {
            if (!_isDragging)
            {
                // Short click → Reveal kartu depan
                _state = State.RevealAnim;
                StartCoroutine(RevealCurrentCard());
            }
            else
            {
                // Release drag → spring back
                _peekTargetX = 0f;
            }

            _pointerDown = false;
            _isDragging  = false;
        }
    }

    /// <summary>
    /// Smooth-update posisi kartu depan berdasarkan peek offset.
    /// Termasuk parallax untuk kartu di belakangnya.
    /// </summary>
    private void UpdatePeek()
    {
        // Spring interpolation
        _peekCurrentX = Mathf.Lerp(_peekCurrentX, _peekTargetX,
            Time.deltaTime * peekSpringSpeed);

        // Toleransi snap ke 0
        if (!_isDragging && Mathf.Abs(_peekCurrentX) < 0.5f)
            _peekCurrentX = 0f;

        // Apply ke kartu-kartu
        for (int i = _currentIndex; i < cardSlots.Length; i++)
        {
            if (cardSlots[i].rect == null) continue;

            float depthFromFront = (float)(i - _currentIndex);
            float targetY = riseToY + stackOffsetY * depthFromFront;

            // Parallax: kartu depan bergerak penuh, belakang lebih sedikit
            float parallax = 1f;
            if (depthFromFront > 0f)
                parallax = peekParallaxFactor / depthFromFront;

            float x = _peekCurrentX * parallax;
            cardSlots[i].rect.anchoredPosition = new Vector2(x, targetY);

            // Rotasi hanya untuk kartu paling depan
            if (i == _currentIndex)
            {
                float rotZ = -(_peekCurrentX / peekMaxX) * peekRotationMax;
                cardSlots[i].rect.localRotation =
                    Quaternion.Euler(0f, 0f, rotZ);
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  Phase 4: Card Reveal (Fly-Out)
    // ════════════════════════════════════════════════════════════

    private IEnumerator RevealCurrentCard()
    {
        int idx       = _currentIndex;
        CardSlot slot = cardSlots[idx];
        RectTransform rect = slot.rect;

        // Reset peek ke tengah
        _peekCurrentX = 0f;
        _peekTargetX  = 0f;
        rect.anchoredPosition = new Vector2(0f, riseToY);
        rect.localRotation    = Quaternion.identity;



        // ── Zoom in (reveal moment) ──
        Vector3 baseScale = rect.localScale;
        Vector3 zoomScale = Vector3.one * revealZoomScale;
        float elapsed = 0f;

        while (elapsed < revealZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(elapsed / revealZoomDuration));
            rect.localScale = Vector3.Lerp(baseScale, zoomScale, t);
            yield return null;
        }
        rect.localScale = zoomScale;

        // ── Hold (dramatic pause) ──
        yield return new WaitForSeconds(revealHoldDuration);

        // ── Exit (fly up + fade out) ──
        elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos   = new Vector2(0f, revealExitY);

        while (elapsed < revealExitDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / revealExitDuration);
            float curved = revealExitCurve.Evaluate(t);

            rect.anchoredPosition =
                Vector2.LerpUnclamped(startPos, endPos, curved);
            rect.localScale =
                Vector3.Lerp(zoomScale, Vector3.one, t);
            slot.canvasGroup.alpha = 1f - t;

            yield return null;
        }

        // Sembunyikan kartu yang sudah di-reveal
        slot.image.gameObject.SetActive(false);
        slot.canvasGroup.alpha = 1f;

        // Next card
        _currentIndex++;

        if (_currentIndex >= cardSlots.Length)
        {
            // Semua kartu sudah di-reveal → summary
            _state = State.Summary;
        }
        else
        {
            // Animasi sisa kartu bergeser ke depan
            yield return StartCoroutine(ShiftStackForward());
            _state = State.Ready;
        }
    }

    /// <summary>
    /// Setelah kartu depan keluar, sisa kartu bergeser maju satu posisi.
    /// </summary>
    private IEnumerator ShiftStackForward()
    {
        int remaining = cardSlots.Length - _currentIndex;
        float duration = 0.25f;
        float elapsed  = 0f;

        // Simpan posisi & scale awal
        Vector2[] startPos   = new Vector2[remaining];
        Vector3[] startScale = new Vector3[remaining];

        for (int i = 0; i < remaining; i++)
        {
            int idx = _currentIndex + i;
            startPos[i]   = cardSlots[idx].rect.anchoredPosition;
            startScale[i] = cardSlots[idx].rect.localScale;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < remaining; i++)
            {
                int idx = _currentIndex + i;
                float depthFromFront = (float)i;
                float targetY        = riseToY + stackOffsetY * depthFromFront;
                float targetScale    = 1f - stackScaleStep * depthFromFront;

                cardSlots[idx].rect.anchoredPosition =
                    Vector2.Lerp(startPos[i], new Vector2(0f, targetY), t);
                cardSlots[idx].rect.localScale =
                    Vector3.Lerp(startScale[i],
                        new Vector3(targetScale, targetScale, 1f), t);
            }

            yield return null;
        }

        SnapStackPositions();
    }



    // ════════════════════════════════════════════════════════════
    //  Phase 5: Summary
    // ════════════════════════════════════════════════════════════

    private IEnumerator ShowSummary()
    {
        int count = cardSlots.Length;
        float totalWidth = summarySpacing * (count - 1);
        float startX = -totalWidth / 2f;

        // Re-aktifkan semua kartu di tengah, scale 0 (akan di-animasi masuk)
        for (int i = 0; i < count; i++)
        {
            CardSlot slot = cardSlots[i];
            slot.image.gameObject.SetActive(true);
            slot.canvasGroup.alpha        = 1f;
            slot.rect.anchoredPosition    = Vector2.zero;
            slot.rect.localScale          = Vector3.zero;
            slot.rect.localRotation       = Quaternion.identity;

            // Reset sibling order: kiri ke kanan
            slot.image.transform.SetSiblingIndex(i);
        }

        // Animate masuk satu per satu (stagger)
        float totalDuration = summaryAnimDuration +
            summaryStaggerDelay * (count - 1);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < count; i++)
            {
                float delay    = summaryStaggerDelay * i;
                float cardTime = Mathf.Clamp01((elapsed - delay) / summaryAnimDuration);
                float curved   = summaryCurve.Evaluate(cardTime);

                float targetX = startX + summarySpacing * i;
                cardSlots[i].rect.anchoredPosition =
                    Vector2.Lerp(Vector2.zero, new Vector2(targetX, 0f), curved);
                cardSlots[i].rect.localScale =
                    Vector3.Lerp(Vector3.zero,
                        Vector3.one * summaryCardScale, curved);
            }

            yield return null;
        }

        // Snap final
        for (int i = 0; i < count; i++)
        {
            float targetX = startX + summarySpacing * i;
            cardSlots[i].rect.anchoredPosition = new Vector2(targetX, 0f);
            cardSlots[i].rect.localScale       = Vector3.one * summaryCardScale;
        }

        // Pulse animasi untuk kartu rare/SR
        for (int i = 0; i < count; i++)
        {
            if (cardSlots[i].rarity != CardRarity.Normal)
                StartCoroutine(SummaryPulse(i));
        }
    }

    /// <summary>
    /// Subtle scale pulse untuk kartu rare/SR di summary view.
    /// </summary>
    private IEnumerator SummaryPulse(int slotIndex)
    {
        CardSlot slot = cardSlots[slotIndex];
        float baseScale   = summaryCardScale;
        float pulseAmount = slot.rarity == CardRarity.SuperRare ? 0.035f : 0.02f;
        float speed       = slot.rarity == CardRarity.SuperRare ? 2.5f  : 1.8f;

        while (_state == State.Summary)
        {
            float t     = Time.time * speed + slotIndex; // phase offset
            float scale = baseScale + Mathf.Sin(t * Mathf.PI) * pulseAmount;
            slot.rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  Dismiss (Click Terakhir)
    // ════════════════════════════════════════════════════════════

    private IEnumerator WaitForDismissClick()
    {
        // Tunggu beberapa frame agar tidak langsung ter-trigger
        yield return null;
        yield return null;

        // Tunggu player click
        while (!Input.GetMouseButtonDown(0))
            yield return null;

        // Fade out semua
        float fadeDuration = 0.4f;
        float elapsed = 0f;

        CanvasGroup panelCG = GetComponent<CanvasGroup>();
        if (panelCG == null) panelCG = gameObject.AddComponent<CanvasGroup>();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            panelCG.alpha = 1f - t;
            yield return null;
        }

        _state = State.Inactive;
        panelCG.alpha = 1f;
        gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════
    //  Utility
    // ════════════════════════════════════════════════════════════

    private float GetCanvasScaleFactor()
    {
        if (_canvas != null)
            return _canvas.scaleFactor;
        return 1f;
    }
}
