using Harukerryzi.Clash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClashMockupLayoutTool
{
    private static readonly Color SidePanelColor = new(0.1137f, 0.1686f, 0.1176f, 1f);

    [MenuItem("Tools/Clash/Apply Mockup Layout (Non-Destructive)")]
    public static void ApplyMockupLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before applying the mockup layout.");
            return;
        }

        GameObject canvasObject = GameObject.Find("ClashCanvas");
        if (canvasObject == null)
        {
            Debug.LogError("ClashCanvas not found. Open ClashScene first.");
            return;
        }

        Transform canvasTransform = canvasObject.transform;

        GameObject playArea = EnsurePanel(canvasTransform, "Mockup_PlayArea_1x1", new Color(0.52f, 0.52f, 0.52f, 1f), false);
        SetRect(playArea.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(760f, 760f), Vector2.zero);

        GameObject leftScorePanel = EnsurePanel(canvasTransform, "Mockup_LeftScorePanel", SidePanelColor, false);
        SetRect(leftScorePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(190f, 760f), new Vector2(-475f, 0f));

        GameObject rightScorePanel = EnsurePanel(canvasTransform, "Mockup_RightScorePanel", SidePanelColor, false);
        SetRect(rightScorePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(190f, 760f), new Vector2(475f, 0f));

        Text playerScoreText = EnsureScoreText(leftScorePanel.transform, "PlayerScoreText");
        Text aiScoreText = EnsureScoreText(rightScorePanel.transform, "AiScoreText");

        PositionExisting(canvasTransform, "BattleHands", new Vector2(0.5f, 0.5f), new Vector2(520f, 520f), new Vector2(0f, 40f));
        EnsurePressureBar(canvasTransform);
        PositionExisting(canvasTransform, "MashButton", new Vector2(0.5f, 0.5f), null, new Vector2(0f, -300f));
        PositionExisting(canvasTransform, "PromptText", new Vector2(0.5f, 0.5f), null, new Vector2(0f, -300f));
        EnsureSpaceLabel(canvasTransform);
        HideLegacyCardReveal(canvasTransform);

        WireScoreTexts(playerScoreText, aiScoreText);

        playArea.transform.SetAsFirstSibling();
        leftScorePanel.transform.SetSiblingIndex(1);
        rightScorePanel.transform.SetSiblingIndex(2);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Applied non-destructive mockup layout. Existing generated objects were moved, not rebuilt/deleted.");
    }

    private static GameObject EnsurePanel(Transform parent, string name, Color color, bool raycastTarget)
    {
        Transform existing = parent.Find(name);
        GameObject panel = existing != null ? existing.gameObject : new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = panel.AddComponent<RectTransform>();
        }

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            image = panel.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = raycastTarget;
        return panel;
    }

    private static Text EnsureScoreText(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = textObject.AddComponent<RectTransform>();
        }

        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(180f, 220f), Vector2.zero);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
        {
            text = textObject.AddComponent<Text>();
        }

        text.text = "0";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 150;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void PositionExisting(Transform root, string name, Vector2 anchor, Vector2? size, Vector2 position)
    {
        Transform target = FindDeepChild(root, name);
        if (target == null)
        {
            Debug.LogWarning($"{name} not found. Skipped.");
            return;
        }

        RectTransform rect = target as RectTransform;
        if (rect == null)
        {
            Debug.LogWarning($"{name} is not a RectTransform. Skipped.");
            return;
        }

        SetRect(rect, anchor, size ?? rect.sizeDelta, position);
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void WireScoreTexts(Text playerScoreText, Text aiScoreText)
    {
        MinigameScoreUI scoreUI = Object.FindFirstObjectByType<MinigameScoreUI>();
        if (scoreUI == null)
        {
            Debug.LogWarning("MinigameScoreUI not found. Score panel texts created but not wired.");
            return;
        }

        SerializedObject serializedScore = new(scoreUI);
        serializedScore.FindProperty("playerScoreText").objectReferenceValue = playerScoreText;
        serializedScore.FindProperty("aiScoreText").objectReferenceValue = aiScoreText;
        serializedScore.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsurePressureBar(Transform canvas)
    {
        Transform existing = FindDeepChild(canvas, "ClashBar");
        if (existing == null)
        {
            Debug.LogWarning("ClashBar not found. Skipped pressure bar setup.");
            return;
        }

        GameObject barObject = existing.gameObject;
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        if (barRect == null)
        {
            return;
        }

        SetRect(barRect, new Vector2(0.5f, 0.5f), new Vector2(650f, 133f), new Vector2(0f, 300f));

        Image barImage = barObject.GetComponent<Image>();
        if (barImage == null)
        {
            barImage = barObject.AddComponent<Image>();
        }

        barImage.sprite = LoadSprite("Assets/Projects/Sprites/Clash/UI_PressureBar.png");
        barImage.color = Color.white;
        barImage.preserveAspect = true;
        barImage.raycastTarget = false;

        Transform fill = barObject.transform.Find("Fill");
        if (fill != null)
        {
            fill.gameObject.SetActive(false);
        }

        Transform marker = barObject.transform.Find("Marker");
        GameObject markerObject = marker != null ? marker.gameObject : new GameObject("Marker");
        markerObject.transform.SetParent(barObject.transform, false);

        RectTransform markerRect = markerObject.GetComponent<RectTransform>();
        if (markerRect == null)
        {
            markerRect = markerObject.AddComponent<RectTransform>();
        }

        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(65f, 133f);
        markerRect.anchoredPosition = Vector2.zero;

        Image markerImage = markerObject.GetComponent<Image>();
        if (markerImage == null)
        {
            markerImage = markerObject.AddComponent<Image>();
        }

        markerImage.sprite = LoadSprite("Assets/Projects/Sprites/Clash/UI_PressureBar_Indicator.png");
        markerImage.color = Color.white;
        markerImage.preserveAspect = true;
        markerImage.raycastTarget = false;
        markerObject.transform.SetAsLastSibling();

        ClashBarUI barUI = barObject.GetComponent<ClashBarUI>();
        if (barUI == null)
        {
            barUI = barObject.AddComponent<ClashBarUI>();
        }

        SerializedObject serialized = new(barUI);
        SerializedProperty fillImage = serialized.FindProperty("fillImage");
        if (fillImage != null)
        {
            fillImage.objectReferenceValue = null;
        }

        SerializedProperty markerProperty = serialized.FindProperty("marker");
        if (markerProperty != null)
        {
            markerProperty.objectReferenceValue = markerRect;
        }

        SerializedProperty left = serialized.FindProperty("markerLeftX");
        if (left != null)
        {
            left.floatValue = -280f;
        }

        SerializedProperty right = serialized.FindProperty("markerRightX");
        if (right != null)
        {
            right.floatValue = 280f;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite LoadSprite(string path)
    {
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
    }

    private static void HideLegacyCardReveal(Transform canvas)
    {
        Transform cardReveal = FindDeepChild(canvas, "CardRevealOverlay");
        if (cardReveal != null)
        {
            cardReveal.gameObject.SetActive(false);
        }
    }

    private static void EnsureSpaceLabel(Transform canvas)
    {
        Transform mashButton = FindDeepChild(canvas, "MashButton");
        if (mashButton == null)
        {
            return;
        }

        Transform existing = mashButton.Find("SpaceLabel");
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject("SpaceLabel");
        labelObject.transform.SetParent(mashButton, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = labelObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        Text text = labelObject.GetComponent<Text>();
        if (text == null)
        {
            text = labelObject.AddComponent<Text>();
        }

        text.text = "Space";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.18f, 0.18f, 0.12f, 1f);
        text.raycastTarget = false;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform found = FindDeepChild(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
