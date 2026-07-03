using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Harukerryzi.Clash
{
    public sealed class CardSelectionUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleText;

        [Header("Carousel")]
        [SerializeField] private RectTransform itemRoot;
        [SerializeField] private CardCarouselItem itemTemplate;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button selectButton;
        [SerializeField, Min(1f)] private float itemSpacing = 180f;
        [SerializeField, Min(0f)] private float dragSensitivity = 0.004f;
        [SerializeField, Min(0f)] private float snapSpeed = 12f;
        [SerializeField, Min(0f)] private float centerScale = 1f;
        [SerializeField, Min(0f)] private float minScale = 0.55f;
        [SerializeField, Min(0.01f)] private float scaleRange = 2f;

        private readonly List<CardCarouselItem> items = new();
        private ClashCardConfig[] cards;
        private Action<ClashCardConfig> onSelected;
        private float scrollPosition;
        private bool dragging;

        private int CenterIndex => cards == null || cards.Length == 0
            ? 0
            : WrapIndex(Mathf.RoundToInt(scrollPosition));

        private void Awake()
        {
            EnsureRuntimeReferences();

            if (previousButton != null)
            {
                previousButton.onClick.AddListener(StepPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(StepNext);
            }

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(ConfirmSelection);
            }

            if (itemTemplate != null)
            {
                itemTemplate.gameObject.SetActive(false);
            }
        }

        private void EnsureRuntimeReferences()
        {
            HideLegacyFixedSlots();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (itemRoot == null)
            {
                GameObject root = CreateRuntimeUiObject("RuntimeCarouselRoot", transform, new Vector2(900f, 300f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f));
                Image image = root.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.001f);
                itemRoot = root.GetComponent<RectTransform>();
            }

            if (itemTemplate == null)
            {
                itemTemplate = CreateRuntimeItemTemplate(itemRoot);
            }

            if (previousButton == null)
            {
                previousButton = CreateRuntimeButton("RuntimePrevButton", "<", new Vector2(-520f, -20f), new Vector2(80f, 80f));
            }

            if (nextButton == null)
            {
                nextButton = CreateRuntimeButton("RuntimeNextButton", ">", new Vector2(520f, -20f), new Vector2(80f, 80f));
            }

            if (selectButton == null)
            {
                selectButton = CreateRuntimeButton("RuntimeSelectButton", "SELECT", new Vector2(0f, -220f), new Vector2(260f, 72f));
            }
        }

        private void HideLegacyFixedSlots()
        {
            string[] legacyNames = { "Card_N", "Card_R", "Card_SR" };
            foreach (string legacyName in legacyNames)
            {
                Transform child = transform.Find(legacyName);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private CardCarouselItem CreateRuntimeItemTemplate(Transform parent)
        {
            GameObject itemObject = CreateRuntimeUiObject("RuntimeCardTemplate", parent, new Vector2(200f, 260f), new Vector2(0.5f, 0.5f), Vector2.zero);
            itemObject.AddComponent<CanvasGroup>();
            Image background = itemObject.AddComponent<Image>();
            background.color = new Color(0.72f, 0.72f, 0.72f, 1f);

            GameObject clickArea = CreateRuntimeStretchObject("RuntimeCardClickArea", itemObject.transform);
            Image clickImage = clickArea.AddComponent<Image>();
            clickImage.color = new Color(1f, 1f, 1f, 0.001f);
            clickArea.AddComponent<Button>();

            Text label = CreateRuntimeText("RuntimeCardLabel", itemObject.transform, "N\nCard\nx1", 34, Vector2.zero, new Vector2(180f, 220f));
            label.color = Color.black;
            label.fontStyle = FontStyle.Bold;

            CardCarouselItem item = itemObject.AddComponent<CardCarouselItem>();
            itemObject.SetActive(false);

            item.ConfigureView(background, label);
            return item;
        }

        private Button CreateRuntimeButton(string name, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = CreateRuntimeUiObject(name, transform, size, new Vector2(0.5f, 0.5f), position);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.9f, 0.75f, 0.25f, 1f);
            Button button = buttonObject.AddComponent<Button>();

            Text text = CreateRuntimeText($"{name}_Text", buttonObject.transform, label, 32, Vector2.zero, size);
            text.color = Color.black;
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        private static Text CreateRuntimeText(string name, Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
        {
            GameObject textObject = CreateRuntimeUiObject(name, parent, size, new Vector2(0.5f, 0.5f), position);
            Text uiText = textObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;
            uiText.raycastTarget = false;
            return uiText;
        }

        private static GameObject CreateRuntimeUiObject(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position)
        {
            GameObject uiObject = new(name);
            uiObject.transform.SetParent(parent, false);
            RectTransform rect = uiObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return uiObject;
        }

        private static GameObject CreateRuntimeStretchObject(string name, Transform parent)
        {
            GameObject uiObject = new(name);
            uiObject.transform.SetParent(parent, false);
            RectTransform rect = uiObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return uiObject;
        }

        private void OnDestroy()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(StepPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(StepNext);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(ConfirmSelection);
            }
        }

        private void Update()
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            if (!dragging)
            {
                scrollPosition = Mathf.Lerp(scrollPosition, Mathf.Round(scrollPosition), Mathf.Clamp01(snapSpeed * Time.unscaledDeltaTime));
            }

            NormalizeScrollPosition();

            LayoutItems();
        }

        public void Show(ClashCardConfig[] availableCards, Action<ClashCardConfig> selectedCallback)
        {
            cards = availableCards;
            onSelected = selectedCallback;
            scrollPosition = 0f;
            gameObject.SetActive(true);
            SetVisible(true);

            if (titleText != null)
            {
                titleText.text = "Choose 1 Card";
            }

            RebuildItems();
            LayoutItems();
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            scrollPosition -= eventData.delta.x * dragSensitivity;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            scrollPosition = Mathf.Round(scrollPosition);
        }

        public void StepPrevious()
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            scrollPosition = Mathf.Round(scrollPosition) - 1f;
        }

        public void StepNext()
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            scrollPosition = Mathf.Round(scrollPosition) + 1f;
        }

        public void ConfirmSelection()
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            onSelected?.Invoke(cards[CenterIndex]);
        }

        private void RebuildItems()
        {
            foreach (CardCarouselItem item in items)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            items.Clear();

            if (cards == null || itemTemplate == null || itemRoot == null)
            {
                return;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                CardCarouselItem item = Instantiate(itemTemplate, itemRoot);
                item.name = $"Card_{i + 1:00}_{cards[i].Rarity}";
                item.gameObject.SetActive(true);
                item.SetCard(cards[i]);

                Button itemButton = item.GetComponentInChildren<Button>();
                if (itemButton != null)
                {
                    itemButton.onClick.RemoveAllListeners();
                    itemButton.onClick.AddListener(ConfirmSelection);
                }

                items.Add(item);
            }
        }

        private void LayoutItems()
        {
            for (int i = 0; i < items.Count; i++)
            {
                CardCarouselItem item = items[i];
                if (item == null || item.RectTransform == null)
                {
                    continue;
                }

                float offset = GetWrappedOffset(i, scrollPosition, items.Count);
                float distance = Mathf.Abs(offset);
                float focus = 1f - Mathf.Clamp01(distance / scaleRange);
                float scale = Mathf.Lerp(minScale, centerScale, focus);
                float alpha = Mathf.Lerp(0.55f, 1f, focus);

                item.RectTransform.anchoredPosition = new Vector2(offset * itemSpacing, 0f);
                item.RectTransform.localScale = Vector3.one * scale;
                if (distance < 0.6f)
                {
                    item.transform.SetAsLastSibling();
                }

                CanvasGroup group = item.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = alpha;
                    group.blocksRaycasts = distance < 0.6f;
                    group.interactable = distance < 0.6f;
                }
            }
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

        private void NormalizeScrollPosition()
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            if (Mathf.Abs(scrollPosition) <= cards.Length * 2f)
            {
                return;
            }

            int wholeLoops = Mathf.FloorToInt(scrollPosition / cards.Length);
            scrollPosition -= wholeLoops * cards.Length;
        }

        private int WrapIndex(int index)
        {
            if (cards == null || cards.Length == 0)
            {
                return 0;
            }

            int count = cards.Length;
            return ((index % count) + count) % count;
        }

        private static float GetWrappedOffset(int itemIndex, float position, int count)
        {
            if (count <= 0)
            {
                return 0f;
            }

            float offset = itemIndex - position;
            float half = count * 0.5f;

            while (offset > half)
            {
                offset -= count;
            }

            while (offset < -half)
            {
                offset += count;
            }

            return offset;
        }
    }
}
