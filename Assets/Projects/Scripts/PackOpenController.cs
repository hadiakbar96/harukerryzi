using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives the card pack opening animation: top flap peels backward,
/// pack body slides down to reveal the card sitting behind it.
///
/// Hierarchy:
///   CardPack (this script + BoxCollider2D)
///   ├── Card             (SpriteRenderer)  ← cardPiece  (behind pack, revealed as pack drops)
///   ├── CardPack_Bottom  (SpriteRenderer)  ← bottomPiece (pack body, slides down)
///   └── CardPack_Top     (SpriteRenderer)  ← topPiece   (flap, peels open)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PackOpenController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ──────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private SpriteRenderer topPiece;
    [SerializeField] private SpriteRenderer bottomPiece;
    [SerializeField] private SpriteRenderer cardPiece;
    [Tooltip("Drag the Pack_Container object here (the one with the Animator)")]
    [SerializeField] private Animator packAnimator;

    [Header("Idle Bob")]
    [SerializeField] private bool  enableIdleBob = true;
    [SerializeField] private float bobAmplitude  = 0.06f;
    [SerializeField] private float bobSpeed      = 1.4f;

    [Header("Shake Anticipation")]
    [SerializeField] private float shakeDuration  = 0.3f;
    [SerializeField] private float shakeIntensity = 0.05f;
    [SerializeField] private int   shakeCount     = 8;

    [Header("Top Peel")]
    [SerializeField] private float          peelDuration = 0.4f;
    [SerializeField] private float          peelAngle    = 150f;
    [SerializeField] private AnimationCurve peelCurve    = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Pack Drop (reveals card)")]
    [SerializeField] private float          packDropDelay    = 0.15f;
    [SerializeField] private float          packDropDuration = 0.5f;
    [SerializeField] private float          packDropDistance = 8f;
    [SerializeField] private AnimationCurve packDropCurve    = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Card Pop")]
    [SerializeField] private float cardPopDelay    = 0.25f;
    [SerializeField] private float cardPopDuration = 0.35f;

    [Header("Events")]
    public UnityEvent OnPackOpened;

    // ──────────────────────────────────────────────────────────────────────────
    //  Private State
    // ──────────────────────────────────────────────────────────────────────────

    private bool      _isOpening;
    private Vector3   _originalPosition;
    private Coroutine _bobCoroutine;

    // ──────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        _originalPosition = transform.position;

        if (enableIdleBob)
            _bobCoroutine = StartCoroutine(IdleBob());
    }

    private void OnMouseDown()
    {
        if (_isOpening) return;
        _isOpening = true;
        StartCoroutine(OpenSequence());
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Idle Animation
    // ──────────────────────────────────────────────────────────────────────────

    private IEnumerator IdleBob()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * bobSpeed;
            transform.position = _originalPosition
                + new Vector3(0f, Mathf.Sin(t) * bobAmplitude, 0f);
            yield return null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Main Open Sequence
    // ──────────────────────────────────────────────────────────────────────────

    private IEnumerator OpenSequence()
    {
        if (_bobCoroutine != null)
            StopCoroutine(_bobCoroutine);
        transform.position   = _originalPosition;
        transform.localScale = Vector3.one;

        // Fire the Zoom trigger → CameraZoomIn → PackOpen plays via Animator
        if (packAnimator != null)
            packAnimator.SetTrigger("Zoom");

        // 1 — Shake
        yield return StartCoroutine(Shake());

        // 2 — Peel top + drop pack body + card pop (overlapping)
        StartCoroutine(PeelTop());
        StartCoroutine(DropPackBody());
        yield return StartCoroutine(CardPop());

        // 3 — Done
        yield return new WaitForSeconds(0.25f);
        OnPackOpened?.Invoke();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Shake Anticipation
    // ──────────────────────────────────────────────────────────────────────────

    private IEnumerator Shake()
    {
        Vector3 origin   = transform.localPosition;
        float   stepTime = shakeDuration / shakeCount;

        for (int i = 0; i < shakeCount; i++)
        {
            float decay = 1f - (float)i / shakeCount;
            float dir   = (i % 2 == 0) ? 1f : -1f;
            Vector3 target = origin + new Vector3(dir * shakeIntensity * decay, 0f, 0f);

            float   e    = 0f;
            Vector3 from = transform.localPosition;
            while (e < stepTime)
            {
                e += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, target,
                    Mathf.Clamp01(e / stepTime));
                yield return null;
            }
        }
        transform.localPosition = origin;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Top Flap Peel
    // ──────────────────────────────────────────────────────────────────────────

    private IEnumerator PeelTop()
    {
        Quaternion startRot   = topPiece.transform.localRotation;
        Vector3    startScale = topPiece.transform.localScale;
        Quaternion endRot     = Quaternion.Euler(0f, 0f, peelAngle);

        float elapsed = 0f;
        while (elapsed < peelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / peelDuration);
            float c = peelCurve.Evaluate(t);

            topPiece.transform.localRotation =
                Quaternion.SlerpUnclamped(startRot, endRot, c);

            float sy = Mathf.Lerp(1f, 0.25f, c);
            topPiece.transform.localScale = new Vector3(startScale.x, sy, startScale.z);

            float alpha = Mathf.Clamp01(1f - t * 1.8f);
            topPiece.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        topPiece.color = new Color(1f, 1f, 1f, 0f);
        topPiece.gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Pack Body Drop (reveals the card behind it)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Slides both the pack body and the top flap downward and off-screen,
    /// revealing the card that sits behind them.
    /// </summary>
    private IEnumerator DropPackBody()
    {
        yield return new WaitForSeconds(packDropDelay);

        Vector3 bottomStart = bottomPiece.transform.localPosition;
        Vector3 bottomEnd   = bottomStart + new Vector3(0f, -packDropDistance, 0f);

        // Also drag the top piece down (if still active)
        Vector3 topStart = topPiece.transform.localPosition;

        float elapsed = 0f;
        while (elapsed < packDropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / packDropDuration);
            float c = packDropCurve.Evaluate(t);

            // Slide pack body down
            bottomPiece.transform.localPosition =
                Vector3.LerpUnclamped(bottomStart, bottomEnd, c);

            // Drag the top piece down too (it's peeling + dropping simultaneously)
            if (topPiece.gameObject.activeSelf)
            {
                topPiece.transform.localPosition =
                    topStart + new Vector3(0f, -packDropDistance * c, 0f);
            }

            // Fade out the pack body as it drops
            float alpha = Mathf.Clamp01(1f - t * t);
            bottomPiece.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        bottomPiece.color = new Color(1f, 1f, 1f, 0f);
        bottomPiece.gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Card Pop (subtle scale bounce once revealed)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// After a delay (once the pack has moved enough to reveal the card),
    /// the card does a subtle scale bounce for a satisfying pop feel.
    /// </summary>
    private IEnumerator CardPop()
    {
        if (cardPiece == null) yield break;

        yield return new WaitForSeconds(cardPopDelay);

        float elapsed = 0f;
        while (elapsed < cardPopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardPopDuration);

            // Overshoot bounce: 1.0 → 1.12 → 1.0
            float scale;
            if (t < 0.4f)
            {
                scale = Mathf.Lerp(1f, 1.12f, Mathf.SmoothStep(0f, 1f, t / 0.4f));
            }
            else
            {
                float st = (t - 0.4f) / 0.6f;
                scale = Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0f, 1f, st));
            }
            cardPiece.transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        cardPiece.transform.localScale = Vector3.one;
    }
}
