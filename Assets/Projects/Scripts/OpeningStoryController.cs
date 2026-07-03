using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller untuk scene Opening Story.
/// Mengatur alur: Layar Gelap → Narasi Typewriter → Fade ke Dialog →
/// Tikus Muncul → MC Muncul → Saling Berdialog → Scene Kartu Loli
///
/// Hierarchy yang dibutuhkan (dibuat oleh Editor Tool):
///   Canvas
///   ├── Background (Image, sprite Background_Center)
///   ├── BlackOverlay (Image hitam, untuk fade)
///   ├── NarrationPanel
///   │   └── NarrationText (TMP_Text)
///   ├── DialogPanel
///   │   ├── CharacterMC (Image, slide dari kiri)
///   │   ├── CharacterTikus (Image, slide dari kanan)
///   │   ├── DialogBox (Image, sprite UI_DialogBox)
///   │   ├── SpeakerName (TMP_Text)
///   │   └── DialogText (TMP_Text)
///   ├── CardPanel
///   │   ├── CardPromptText (TMP_Text)
///   │   ├── CardImage (Image, sprite NormalCard)
///   │   └── UseItemButton (Button)
///   └── ClickPrompt (TMP_Text, "Click to continue...")
/// </summary>
public class OpeningStoryController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    //  Inspector References
    // ══════════════════════════════════════════════════════════════

    [Header("=== Panels ===")]
    [SerializeField] private GameObject narrationPanel;
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private GameObject cardPanel;

    [Header("=== Overlay & Background ===")]
    [SerializeField] private Image blackOverlay;
    [SerializeField] private Image background;

    [Header("=== Narration ===")]
    [SerializeField] private TMP_Text narrationText;

    [Header("=== Dialog ===")]
    [SerializeField] private Image characterMC;
    [SerializeField] private Image characterTikus;
    [SerializeField] private Image dialogBox;
    [SerializeField] private TMP_Text speakerName;
    [SerializeField] private TMP_Text dialogText;

    [Header("=== Card ===")]
    [SerializeField] private TMP_Text cardPromptText;
    [SerializeField] private Image cardImage;
    [SerializeField] private Button useItemButton;

    [Header("=== UI ===")]
    [SerializeField] private TMP_Text clickPrompt;

    [Header("=== Settings ===")]
    [SerializeField] private float typeSpeed = 0.03f;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float slideDuration = 0.6f;

    // ══════════════════════════════════════════════════════════════
    //  Private State
    // ══════════════════════════════════════════════════════════════

    private bool _waitingForClick = false;
    private bool _isTyping = false;
    private bool _skipRequested = false;
    private Coroutine _typewriterCoroutine;

    // Warna karakter saat aktif / tidak aktif
    private readonly Color _activeTint   = Color.white;
    private readonly Color _inactiveTint = new Color(0.5f, 0.5f, 0.5f, 1f);

    // Posisi slide karakter
    private float _mcOnScreenX;
    private float _mcOffScreenX;
    private float _tikusOnScreenX;
    private float _tikusOffScreenX;

    // ══════════════════════════════════════════════════════════════
    //  Narration Text
    // ══════════════════════════════════════════════════════════════

    // Tiap section ditampilkan bergantian: ketik → klik → hilang → section berikutnya
    private readonly string[] _narrationSections = new string[]
    {
        // Section 1
        "A nerdy office worker wearing glasses has just finished work. " +
        "Grinning from ear to ear, he stops by his favorite gacha shop to buy the latest Loli Gacha Pack.\n\n" +
        "As he leaves the shop, he walks down the street while happily admiring the cards he just pulled. " +
        "Suddenly, without noticing where he's going, he falls straight into an open sewer.",

        // Section 2
        "Moments later, he wakes up, still dizzy and confused. " +
        "Looking around, he realizes he's trapped deep inside the sewer. " +
        "Before he can figure out what happened, a sewer rat acting as the dungeon's gatekeeper appears " +
        "and tells him that if he wants to escape, he'll have to defeat it in battle.",
    };

    // ══════════════════════════════════════════════════════════════
    //  Dialog Data
    // ══════════════════════════════════════════════════════════════

    private struct DialogLine
    {
        public string speaker;
        public string text;
        public bool isTikus; // true = Tikus, false = MC

        public DialogLine(string speaker, string text, bool isTikus)
        {
            this.speaker = speaker;
            this.text = text;
            this.isTikus = isTikus;
        }
    }

    private readonly DialogLine[] _dialogLines = new DialogLine[]
    {
        new DialogLine("Tikus", "Oh hey, new around here?", true),
        new DialogLine("You",   "...", false),
        new DialogLine("Tikus", "Rest assured, you'll stuck here 4 evah!!", true),
        new DialogLine("You",   "...", false),
        new DialogLine("Tikus", "Okay don't give me that hopeless look you poor thing. I know you have no idea where you are. But there's one way you can get out from here....", true),
        new DialogLine("You",   "Well... Spare some mercy for me please.", false),
        new DialogLine("Tikus", "You have to fight the gods!", true),
        new DialogLine("Tikus", "But first, I have to test you. Use whatever you have to fight me!", true),
    };

    // ══════════════════════════════════════════════════════════════
    //  Lifecycle
    // ══════════════════════════════════════════════════════════════

    private void Start()
    {
        // Cache slide positions
        var mcRect = characterMC.GetComponent<RectTransform>();
        _mcOnScreenX = mcRect.anchoredPosition.x;
        _mcOffScreenX = _mcOnScreenX - 900f; // off screen kiri

        var tikusRect = characterTikus.GetComponent<RectTransform>();
        _tikusOnScreenX = tikusRect.anchoredPosition.x;
        _tikusOffScreenX = _tikusOnScreenX + 900f; // off screen kanan

        // Set initial state
        blackOverlay.color = Color.black;
        blackOverlay.gameObject.SetActive(true);

        narrationPanel.SetActive(false);
        dialogPanel.SetActive(false);
        cardPanel.SetActive(false);
        clickPrompt.gameObject.SetActive(false);

        // Karakter off screen
        mcRect.anchoredPosition = new Vector2(_mcOffScreenX, mcRect.anchoredPosition.y);
        tikusRect.anchoredPosition = new Vector2(_tikusOffScreenX, tikusRect.anchoredPosition.y);

        // Hide dialog elements
        dialogBox.gameObject.SetActive(false);
        speakerName.gameObject.SetActive(false);
        dialogText.gameObject.SetActive(false);

        // Start story!
        StartCoroutine(RunStory());
    }

    private void Update()
    {
        // Input: klik mouse atau spasi
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (_isTyping)
            {
                _skipRequested = true;
            }
            else if (_waitingForClick)
            {
                _waitingForClick = false;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  Main Story Coroutine
    // ══════════════════════════════════════════════════════════════

    private IEnumerator RunStory()
    {
        // ── FASE 1: Layar Hitam (tunggu sebentar) ──
        yield return new WaitForSeconds(1.0f);

        // ── FASE 2: Narasi Typewriter (per section) ──
        narrationPanel.SetActive(true);
        narrationText.text = "";

        for (int i = 0; i < _narrationSections.Length; i++)
        {
            yield return StartCoroutine(TypewriterEffect(narrationText, _narrationSections[i]));

            // Tampilkan prompt "click to continue"
            clickPrompt.gameObject.SetActive(true);
            yield return WaitForClick();
            clickPrompt.gameObject.SetActive(false);

            // Bersihkan teks sebelum section berikutnya
            narrationText.text = "";
        }

        // ── FASE 3: Fade ke Dialog Screen ──
        narrationPanel.SetActive(false);

        // Fade out black overlay (reveal background)
        yield return StartCoroutine(FadeImage(blackOverlay, 1f, 0f, fadeDuration));

        yield return new WaitForSeconds(0.5f);

        // ── FASE 4: Tikus Muncul ──
        dialogPanel.SetActive(true);

        // Slide tikus masuk dari kanan
        yield return StartCoroutine(SlideCharacter(
            characterTikus.GetComponent<RectTransform>(),
            _tikusOffScreenX, _tikusOnScreenX, slideDuration));

        yield return new WaitForSeconds(0.3f);

        // Tampilkan dialog box
        dialogBox.gameObject.SetActive(true);
        speakerName.gameObject.SetActive(true);
        dialogText.gameObject.SetActive(true);

        // Dialog pertama tikus
        SetActiveSpeaker(true); // tikus aktif
        yield return ShowDialog(_dialogLines[0]);

        // ── FASE 5: MC Muncul setelah dialog pertama ──
        // Slide MC masuk dari kiri
        yield return StartCoroutine(SlideCharacter(
            characterMC.GetComponent<RectTransform>(),
            _mcOffScreenX, _mcOnScreenX, slideDuration));

        // MC merespon "..."
        yield return ShowDialog(_dialogLines[1]);

        // ── FASE 6: Saling Berdialog (dialog 2-7) ──
        for (int i = 2; i < _dialogLines.Length; i++)
        {
            yield return ShowDialog(_dialogLines[i]);
        }

        // ── FASE 7: Scene Kartu Loli ──
        yield return new WaitForSeconds(0.5f);

        // Fade out dialog elements
        dialogBox.gameObject.SetActive(false);
        speakerName.gameObject.SetActive(false);
        dialogText.gameObject.SetActive(false);

        // Fade out karakter
        yield return StartCoroutine(FadeImage(characterMC, 1f, 0f, 0.5f));
        yield return StartCoroutine(FadeImage(characterTikus, 1f, 0f, 0.5f));

        // Darken background
        blackOverlay.gameObject.SetActive(true);
        yield return StartCoroutine(FadeImage(blackOverlay, 0f, 0.6f, 0.5f));

        // Show card panel
        cardPanel.SetActive(true);
        cardPromptText.text = "";
        yield return StartCoroutine(TypewriterEffect(
            cardPromptText, "You will fight with the only card you have"));

        // Card muncul (langsung tampil, tanpa animasi)
        cardImage.gameObject.SetActive(true);

        // Show Use Item button
        useItemButton.gameObject.SetActive(true);

        // UseItemButton onClick bisa di-wire ke scene loading
        // Untuk sekarang, log saja
        useItemButton.onClick.AddListener(OnUseItemClicked);
    }

    // ══════════════════════════════════════════════════════════════
    //  Dialog Helper
    // ══════════════════════════════════════════════════════════════

    private IEnumerator ShowDialog(DialogLine line)
    {
        SetActiveSpeaker(line.isTikus);
        speakerName.text = line.speaker;
        dialogText.text = "";

        yield return StartCoroutine(TypewriterEffect(dialogText, line.text));

        clickPrompt.gameObject.SetActive(true);
        yield return WaitForClick();
        clickPrompt.gameObject.SetActive(false);
    }

    private void SetActiveSpeaker(bool isTikus)
    {
        characterTikus.color = isTikus ? _activeTint : _inactiveTint;
        characterMC.color    = isTikus ? _inactiveTint : _activeTint;
    }

    // ══════════════════════════════════════════════════════════════
    //  Typewriter Effect
    // ══════════════════════════════════════════════════════════════

    private IEnumerator TypewriterEffect(TMP_Text textComponent, string fullText)
    {
        _isTyping = true;
        _skipRequested = false;
        textComponent.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (_skipRequested)
            {
                textComponent.text = fullText;
                break;
            }

            textComponent.text += fullText[i];
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;
        _skipRequested = false;
    }

    // ══════════════════════════════════════════════════════════════
    //  Wait for Click
    // ══════════════════════════════════════════════════════════════

    private IEnumerator WaitForClick()
    {
        _waitingForClick = true;
        while (_waitingForClick)
        {
            yield return null;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  Fade Animation
    // ══════════════════════════════════════════════════════════════

    private IEnumerator FadeImage(Image img, float fromAlpha, float toAlpha, float duration)
    {
        Color c = img.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            img.color = c;
            yield return null;
        }

        c.a = toAlpha;
        img.color = c;
    }

    // ══════════════════════════════════════════════════════════════
    //  Slide Animation
    // ══════════════════════════════════════════════════════════════

    private IEnumerator SlideCharacter(RectTransform rect, float fromX, float toX, float duration)
    {
        float elapsed = 0f;
        Vector2 pos = rect.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            pos.x = Mathf.Lerp(fromX, toX, t);
            rect.anchoredPosition = pos;
            yield return null;
        }

        pos.x = toX;
        rect.anchoredPosition = pos;
    }

    // ══════════════════════════════════════════════════════════════
    //  Scale In Animation
    // ══════════════════════════════════════════════════════════════

    private IEnumerator ScaleIn(RectTransform rect, float duration)
    {
        float elapsed = 0f;
        rect.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Overshoot curve for bouncy feel
            float scale = 1f + 0.1f * Mathf.Sin(t * Mathf.PI);
            t = Mathf.SmoothStep(0f, 1f, t);
            rect.localScale = Vector3.one * t * scale;
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    // ══════════════════════════════════════════════════════════════
    //  Button Callback
    // ══════════════════════════════════════════════════════════════

    private void OnUseItemClicked()
    {
        Debug.Log("[OpeningStory] Use Item clicked! → Load battle scene");
        // TODO: SceneManager.LoadScene("BattleScene");
    }
}
