using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Harukerryzi.Clash
{
    public sealed class RewardUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image popupImage;
        [SerializeField] private Image coinsImage;
        [SerializeField] private Image claimButtonImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text gotText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text claimText;
        [SerializeField] private Button claimButton;
        [SerializeField] private RectTransform cardPanel;
        [SerializeField] private Sprite popupSprite;
        [SerializeField] private Sprite coinsSprite;
        [SerializeField] private Sprite claimButtonSprite;
        [SerializeField] private string currencyLabel = "Coins";

        private readonly Color textColor = new(0.02f, 0.24f, 0.09f, 1f);
        private Action onClaim;

        private void Awake()
        {
            EnsureReferences();
            Hide();
        }

        public void Show(AduTosEnemyConfig enemyConfig, bool playerWon, Action claimCallback)
        {
            Show(enemyConfig, playerWon, GetReward(enemyConfig, playerWon), claimCallback);
        }

        public void Show(AduTosEnemyConfig enemyConfig, bool playerWon, int reward, Action claimCallback)
        {
            EnsureReferences();
            onClaim = claimCallback;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SetVisible(true);

            if (titleText != null)
            {
                titleText.text = "Reward";
            }

            if (gotText != null)
            {
                gotText.text = "You Got";
            }

            if (rewardText != null)
            {
                rewardText.text = $"{reward} {currencyLabel}!";
            }

            if (claimText != null)
            {
                claimText.text = "Claim";
            }
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        public void BakeEditorPreview(int reward = 50, bool playerWon = true)
        {
            Show(null, playerWon, reward, null);
        }

        private void Claim()
        {
            Hide();
            onClaim?.Invoke();
        }

        private void EnsureReferences()
        {
            popupSprite ??= LoadSprite("Assets/Projects/Sprites/PostBattle/UI_PopUpReward.png");
            coinsSprite ??= LoadSprite("Assets/Projects/Sprites/PostBattle/UI_Coins.png");
            claimButtonSprite ??= LoadSprite("Assets/Projects/Sprites/PostBattle/UI_ClaimButton.png");

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

            if (cardPanel == null)
            {
                cardPanel = CreateRect(transform, "RewardCardPanel", new Vector2(660f, 660f), Vector2.zero);
            }

            popupImage ??= EnsureImage(cardPanel, popupSprite, true);
            popupImage.sprite = popupSprite;
            popupImage.preserveAspect = true;
            popupImage.raycastTarget = false;

            titleText ??= CreateText(cardPanel, "RewardTitle", "Reward", 34, new Vector2(0f, 232f), Color.white, new Vector2(420f, 72f));
            gotText ??= CreateText(cardPanel, "RewardGot", "You Got", 34, new Vector2(0f, 72f), textColor, new Vector2(440f, 70f));
            coinsImage ??= CreateImage(cardPanel, "RewardCoins", coinsSprite, new Vector2(190f, 150f), new Vector2(0f, -34f));
            rewardText ??= CreateText(cardPanel, "RewardAmount", "50 Coins!", 34, new Vector2(0f, -142f), textColor, new Vector2(440f, 74f));

            if (claimButton == null)
            {
                claimButton = CreateClaimButton(cardPanel);
            }

            claimButtonImage ??= claimButton.GetComponent<Image>();
            if (claimButtonImage != null)
            {
                claimButtonImage.sprite = claimButtonSprite;
                claimButtonImage.color = Color.white;
                claimButtonImage.preserveAspect = true;
            }

            claimText ??= claimButton.GetComponentInChildren<Text>(true);
            if (claimText != null)
            {
                claimText.text = "Claim";
                claimText.color = new Color(0.19f, 0.29f, 0.22f, 1f);
                claimText.fontSize = 32;
                claimText.fontStyle = FontStyle.Bold;
            }

            claimButton.onClick.RemoveListener(Claim);
            claimButton.onClick.AddListener(Claim);
        }

        private Button CreateClaimButton(Transform parent)
        {
            RectTransform buttonRect = CreateRect(parent, "ClaimButton", new Vector2(300f, 90f), new Vector2(0f, -240f));
            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.sprite = claimButtonSprite;
            image.color = Color.white;
            image.preserveAspect = true;

            Button button = buttonRect.gameObject.AddComponent<Button>();
            claimText = CreateText(buttonRect, "ClaimButtonText", "Claim", 32, Vector2.zero, new Color(0.19f, 0.29f, 0.22f, 1f), new Vector2(260f, 60f));
            claimText.fontStyle = FontStyle.Bold;
            return button;
        }

        private Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position)
        {
            RectTransform rect = CreateRect(parent, name, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Image EnsureImage(RectTransform rect, Sprite sprite, bool preserveAspect)
        {
            Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
            return image;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 position, Color color, Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, size, position);
            Text uiText = rect.gameObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.fontStyle = FontStyle.Bold;
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

        private static int GetReward(AduTosEnemyConfig enemyConfig, bool playerWon)
        {
            if (enemyConfig == null)
            {
                return playerWon ? 20 : 20;
            }

            return playerWon ? enemyConfig.RewardOnWin : enemyConfig.RewardOnLose;
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
