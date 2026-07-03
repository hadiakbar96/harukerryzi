using Harukerryzi.Clash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class ClashBakeLayoutTool
{
    // Battle (1:1) baseline values. Play area is square; side panels fill the rest of a 1920x1080 canvas.
    private static readonly Vector2 PlayAreaSize = new(1080f, 1080f);
    private static readonly Vector2 PlayAreaPosition = Vector2.zero;
    private static readonly Vector2 SidePanelSize = new(420f, 1080f);
    private const float SidePanelOffsetX = 750f;
    private static readonly Color SidePanelColor = new(0.1137f, 0.1686f, 0.1176f, 1f);

    [MenuItem("Tools/Clash/Bake Battle Layout To Scene (Non-Destructive)")]
    public static void BakeBattleLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before baking the battle layout.");
            return;
        }

        GameObject canvasObject = GameObject.Find("ClashCanvas");
        if (canvasObject == null)
        {
            Debug.LogError("ClashCanvas not found. Open ClashScene first.");
            return;
        }

        Transform canvas = canvasObject.transform;

        RectTransform playArea = EnsurePanel(canvas, "Mockup_PlayArea_1x1", new Color(0.52f, 0.52f, 0.52f, 1f));
        SetRect(playArea, PlayAreaSize, PlayAreaPosition);

        RectTransform leftPanel = EnsurePanel(canvas, "Mockup_LeftScorePanel", SidePanelColor);
        SetRect(leftPanel, SidePanelSize, new Vector2(-SidePanelOffsetX, 0f));

        RectTransform rightPanel = EnsurePanel(canvas, "Mockup_RightScorePanel", SidePanelColor);
        SetRect(rightPanel, SidePanelSize, new Vector2(SidePanelOffsetX, 0f));

        Text playerScoreText = EnsureScoreText(leftPanel, "PlayerScoreText");
        Text aiScoreText = EnsureScoreText(rightPanel, "AiScoreText");

        EnsurePressureBar(canvasObject.transform);
        WireStageLayout(canvasObject, playArea, leftPanel, rightPanel);
        WireScoreTexts(playerScoreText, aiScoreText);
        EnsureEntranceUI(canvasObject, playArea);
        EnsureClashBackgroundUI(canvasObject, playArea);
        EnsureResultUI(canvasObject.transform);
        EnsureWinFx(canvasObject.transform);
        EnsureRewardUI(canvasObject.transform);
        EnsureSpaceLabel(canvasObject.transform);
        HideLegacyCardReveal(canvasObject.transform);
        HideLegacyBattleHands(canvasObject.transform);
        WireMinigameController();
        RemoveMissingScripts();

        // Keep backgrounds behind gameplay UI.
        playArea.SetAsFirstSibling();
        leftPanel.SetSiblingIndex(1);
        rightPanel.SetSiblingIndex(2);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Baked battle (1:1) layout into ClashCanvas. ClashCanvas was not deleted or rebuilt.");
    }

    private static RectTransform EnsurePanel(Transform parent, string name, Color color)
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
        image.raycastTarget = false;
        return rect;
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

        SetRect(rect, new Vector2(300f, 300f), Vector2.zero);

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

    private static void WireStageLayout(GameObject canvasObject, RectTransform playArea, RectTransform leftPanel, RectTransform rightPanel)
    {
        AduTosStageLayout stageLayout = canvasObject.GetComponent<AduTosStageLayout>();
        if (stageLayout == null)
        {
            stageLayout = canvasObject.AddComponent<AduTosStageLayout>();
        }

        SerializedObject serialized = new(stageLayout);
        AssignIfPresent(serialized, "playArea", playArea);
        AssignIfPresent(serialized, "leftScorePanel", leftPanel);
        AssignIfPresent(serialized, "rightScorePanel", rightPanel);
        AssignIfPresent(serialized, "clashBar", FindRect(canvasObject.transform, "ClashBar"));
        AssignIfPresent(serialized, "mashButton", FindRect(canvasObject.transform, "MashButton"));
        AssignIfPresent(serialized, "promptText", FindRect(canvasObject.transform, "PromptText"));
        AssignIfPresent(serialized, "battleHands", FindRect(canvasObject.transform, "BattleHands"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEntranceUI(GameObject canvasObject, RectTransform playArea)
    {
        Transform existing = playArea != null ? playArea.Find("AduTosEntranceUI") : null;
        GameObject entranceObject = existing != null ? existing.gameObject : new GameObject("AduTosEntranceUI");
        entranceObject.transform.SetParent(playArea != null ? playArea : canvasObject.transform, false);

        RectTransform rect = entranceObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = entranceObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        AduTosEntranceUI entrance = entranceObject.GetComponent<AduTosEntranceUI>();
        if (entrance == null)
        {
            entrance = entranceObject.AddComponent<AduTosEntranceUI>();
        }

        Sprite mc = LoadSprite("Assets/Projects/Sprites/Battle/Sprite_Battle_MC.png");
        Sprite enemy = LoadSprite("Assets/Projects/Sprites/Battle/Sprite_Battle_Tikus.png");
        entrance.BakeEditorPreview(mc, enemy);
        entranceObject.transform.SetAsLastSibling();
    }

    private static void EnsureClashBackgroundUI(GameObject canvasObject, RectTransform playArea)
    {
        Transform existing = playArea != null ? playArea.Find("EnemyClashBackground") : null;
        GameObject backgroundObject = existing != null ? existing.gameObject : new GameObject("EnemyClashBackground");
        backgroundObject.transform.SetParent(playArea != null ? playArea : canvasObject.transform, false);

        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = backgroundObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        ClashBackgroundUI background = backgroundObject.GetComponent<ClashBackgroundUI>();
        if (background == null)
        {
            background = backgroundObject.AddComponent<ClashBackgroundUI>();
        }

        background.SetBackground(LoadSprite("Assets/Projects/Sprites/StageMap/battle_tikus.png"));

        if (backgroundObject.GetComponent<ClashHandShake>() == null)
        {
            backgroundObject.AddComponent<ClashHandShake>();
        }

        backgroundObject.transform.SetAsFirstSibling();
    }

    private static void EnsureResultUI(Transform canvas)
    {
        Transform existing = canvas.Find("MinigameResultOverlay");
        GameObject resultObject = existing != null ? existing.gameObject : new GameObject("MinigameResultUI");
        resultObject.name = "MinigameResultUI";
        resultObject.transform.SetParent(canvas, false);

        RectTransform rect = resultObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = resultObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        MinigameResultUI resultUI = resultObject.GetComponent<MinigameResultUI>();
        if (resultUI == null)
        {
            resultUI = resultObject.AddComponent<MinigameResultUI>();
        }

        resultUI.BakeEditorPreview(true);
    }

    private static void WireScoreTexts(Text playerScoreText, Text aiScoreText)
    {
        MinigameScoreUI scoreUI = Object.FindFirstObjectByType<MinigameScoreUI>();
        if (scoreUI == null)
        {
            Debug.LogWarning("MinigameScoreUI not found. Score texts created but not wired.");
            return;
        }

        SerializedObject serialized = new(scoreUI);
        AssignIfPresent(serialized, "playerScoreText", playerScoreText);
        AssignIfPresent(serialized, "aiScoreText", aiScoreText);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireMinigameController()
    {
        AduTosMinigameController minigame = Object.FindFirstObjectByType<AduTosMinigameController>();
        if (minigame == null)
        {
            Debug.LogWarning("AduTosMinigameController not found in scene. Layout baked, but controller refs were not wired.");
            return;
        }

        SerializedObject serialized = new(minigame);
        AssignIfPresent(serialized, "stageLayout", Object.FindFirstObjectByType<AduTosStageLayout>());
        AssignIfPresent(serialized, "entranceUI", Object.FindFirstObjectByType<AduTosEntranceUI>());
        AssignIfPresent(serialized, "clashBackgroundUI", Object.FindFirstObjectByType<ClashBackgroundUI>());
        AssignIfPresent(serialized, "winFxUI", Object.FindFirstObjectByType<ClashWinFxUI>());
        AssignIfPresent(serialized, "resultUI", Object.FindFirstObjectByType<MinigameResultUI>());
        AssignIfPresent(serialized, "rewardUI", Object.FindFirstObjectByType<RewardUI>());
        AssignIfPresent(serialized, "mcHandSprite", LoadSprite("Assets/Projects/Sprites/Battle/Sprite_Battle_MC.png"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsurePressureBar(Transform canvas)
    {
        Transform existing = FindDeepChild(canvas, "ClashBar");
        GameObject barObject = existing != null ? existing.gameObject : new GameObject("ClashBar");
        barObject.transform.SetParent(canvas, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        if (barRect == null)
        {
            barRect = barObject.AddComponent<RectTransform>();
        }

        SetRect(barRect, new Vector2(650f, 133f), new Vector2(0f, 300f));

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
        AssignIfPresent(serialized, "fillImage", null);
        AssignIfPresent(serialized, "marker", markerRect);
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

    private static void EnsureWinFx(Transform canvas)
    {
        Transform existing = canvas.Find("ClashWinFx");
        GameObject fxObject = existing != null ? existing.gameObject : new GameObject("ClashWinFx");
        fxObject.transform.SetParent(canvas, false);

        RectTransform rect = fxObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = fxObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (fxObject.GetComponent<ClashWinFxUI>() == null)
        {
            fxObject.AddComponent<ClashWinFxUI>();
        }
    }

    private static void EnsureRewardUI(Transform canvas)
    {
        Transform existing = canvas.Find("RewardUI");
        GameObject rewardObject = existing != null ? existing.gameObject : new GameObject("RewardUI");
        rewardObject.transform.SetParent(canvas, false);

        RectTransform rect = rewardObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = rewardObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (rewardObject.GetComponent<RewardUI>() == null)
        {
            rewardObject.AddComponent<RewardUI>();
        }

        RewardUI rewardUI = rewardObject.GetComponent<RewardUI>();
        rewardUI.BakeEditorPreview();
    }

    private static void EnsureSpaceLabel(Transform canvas)
    {
        Transform mashButton = FindDeepChild(canvas, "MashButton");
        if (mashButton == null)
        {
            Debug.LogWarning("MashButton not found. Space label not added.");
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

    private static void AssignIfPresent(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static RectTransform FindRect(Transform parent, string name)
    {
        return FindDeepChild(parent, name) as RectTransform;
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

    private static void RemoveMissingScripts()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        }
    }

    private static void HideLegacyBattleHands(Transform canvas)
    {
        Transform battleHands = FindDeepChild(canvas, "BattleHands");
        if (battleHands != null)
        {
            battleHands.gameObject.SetActive(false);
        }
    }

    private static void HideLegacyCardReveal(Transform canvas)
    {
        Transform cardReveal = FindDeepChild(canvas, "CardRevealOverlay");
        if (cardReveal != null)
        {
            cardReveal.gameObject.SetActive(false);
        }
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

[InitializeOnLoad]
public static class ClashSceneAutoBake
{
    private static bool scheduled;

    static ClashSceneAutoBake()
    {
        EditorApplication.delayCall += MaybeBake;
    }

    private static void MaybeBake()
    {
        if (scheduled || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!SceneManager.GetActiveScene().path.EndsWith("ClashScene.unity"))
        {
            return;
        }

        scheduled = true;
        try
        {
            ClashBakeLayoutTool.BakeBattleLayout();
        }
        finally
        {
            scheduled = false;
        }
    }
}
