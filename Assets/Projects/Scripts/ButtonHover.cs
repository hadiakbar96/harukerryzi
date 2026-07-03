using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Mengganti sprite Image saat mouse hover (pointer enter/exit).
/// Cocok untuk tombol Title Screen yang punya versi normal dan hovered.
///
/// Cara pakai:
///   1. Attach ke GameObject yang punya komponen Image
///   2. Set normalSprite dan hoveredSprite di Inspector
///   3. Saat mouse masuk → sprite berubah ke hovered
///   4. Saat mouse keluar → sprite kembali ke normal
/// </summary>
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    [Tooltip("Sprite saat tidak di-hover")]
    [SerializeField] private Sprite normalSprite;

    [Tooltip("Sprite saat di-hover")]
    [SerializeField] private Sprite hoveredSprite;

    [Header("Scale Animation")]
    [Tooltip("Apakah tombol membesar saat hover")]
    [SerializeField] private bool scaleOnHover = true;

    [Tooltip("Skala saat di-hover (1.0 = normal)")]
    [SerializeField] private float hoverScale = 1.1f;

    [Tooltip("Kecepatan transisi scale")]
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Audio")]
    [Tooltip("SFX saat mouse masuk ke tombol")]
    [SerializeField] private AudioClip hoverSfx;

    private Image _image;
    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _originalScale = transform.localScale;
        _targetScale = _originalScale;
    }

    private void Update()
    {
        // Smooth scale transition
        if (scaleOnHover)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale, _targetScale,
                Time.deltaTime * scaleSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GameAudio.PlaySfx(hoverSfx);

        if (_image != null && hoveredSprite != null)
            _image.sprite = hoveredSprite;

        if (scaleOnHover)
            _targetScale = _originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_image != null && normalSprite != null)
            _image.sprite = normalSprite;

        if (scaleOnHover)
            _targetScale = _originalScale;
    }
}
