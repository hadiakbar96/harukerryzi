using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Harukerryzi.Clash
{
    public sealed class MinigameResultUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image dialogImage;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueText;
        [SerializeField] private Sprite winDialogSprite;
        [SerializeField] private Sprite loseDialogSprite;
        [SerializeField] private UnityEvent onRetry = new();

        private Action onContinue;

        public UnityEvent OnRetry => onRetry;

        private void Awake()
        {
            EnsureReferences();
            Hide();
        }

        public void Show(bool playerWon)
        {
            Show(playerWon, null);
        }

        public void Show(bool playerWon, Action continueCallback)
        {
            EnsureReferences();
            onContinue = continueCallback;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (dialogImage != null)
            {
                dialogImage.sprite = playerWon ? winDialogSprite : loseDialogSprite;
                dialogImage.enabled = dialogImage.sprite != null;
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        public void BakeEditorPreview(bool playerWon = true)
        {
            Show(playerWon, null);
        }

        private void Continue()
        {
            Hide();
            if (onContinue != null)
            {
                onContinue.Invoke();
                return;
            }

            onRetry?.Invoke();
        }

        private void EnsureReferences()
        {
            winDialogSprite ??= LoadSprite("Assets/Projects/Sprites/WinLose/UI_WinDialog.png");
            loseDialogSprite ??= LoadSprite("Assets/Projects/Sprites/WinLose/UI_LoseDialog.png");

            canvasGroup ??= GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            Image rootImage = GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = Color.clear;
                rootImage.raycastTarget = false;
            }

            HideLegacyRetryObjects(transform);

            if (dialogImage == null)
            {
                RectTransform dialogRect = CreateRect(transform, "ResultDialog", new Vector2(760f, 390f), new Vector2(0f, 60f));
                dialogImage = dialogRect.gameObject.AddComponent<Image>();
                dialogImage.preserveAspect = true;
                dialogImage.raycastTarget = false;
            }

            if (continueButton == null)
            {
                continueButton = CreateContinueButton(transform);
            }

            continueButton.onClick.RemoveListener(Continue);
            continueButton.onClick.AddListener(Continue);
        }

        private void HideLegacyRetryObjects(Transform root)
        {
            foreach (Transform child in root)
            {
                if (child.name.ToLowerInvariant().Contains("retry"))
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                HideLegacyRetryObjects(child);
            }
        }

        private Button CreateContinueButton(Transform parent)
        {
            RectTransform buttonRect = CreateRect(parent, "ContinueButton", new Vector2(300f, 74f), new Vector2(0f, -205f));
            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(0.78f, 0.83f, 0.68f, 1f);

            Button button = buttonRect.gameObject.AddComponent<Button>();
            continueText = CreateText(buttonRect, "ContinueButtonText", "Continue", 30, Vector2.zero, new Color(0.17f, 0.31f, 0.2f, 1f), new Vector2(280f, 60f));
            continueText.fontStyle = FontStyle.Bold;
            return button;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 position, Color color, Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, size, position);
            Text uiText = rect.gameObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = color;
            uiText.raycastTarget = false;
            return uiText;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            GameObject rectObject = new(name);
            rectObject.transform.SetParent(parent, false);
            RectTransform rect = rectObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private static Sprite LoadSprite(string path)
        {
#if UNITY_EDITOR
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}
