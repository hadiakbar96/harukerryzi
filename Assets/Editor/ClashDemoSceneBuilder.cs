using System.IO;
using Harukerryzi.Clash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ClashDemoSceneBuilder
{
    private const string ConfigDirectory = "Assets/Projects/Settings/Clash";
    private const string PlayerConfigPath = ConfigDirectory + "/PlayerClashConfig.asset";
    private const string AiConfigPath = ConfigDirectory + "/AIClashConfig.asset";
    private const string BattleHandsPath = "Assets/Projects/Sprites/Clash/Battle_Hands.PNG";
    private const string ButtonDefaultPath = "Assets/Projects/Sprites/Clash/UI_ButtonDefault.PNG";
    private const string ButtonSmashPath = "Assets/Projects/Sprites/Clash/UI_ButtonSmash.PNG";

    [MenuItem("Tools/Clash/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before running Tools > Clash > Build Demo Scene.");
            return;
        }

        if (GameObject.Find("ClashCanvas") != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Overwrite Clash Layout?",
                "This rebuild deletes ClashCanvas and recreates generated UI. Any manual layout changes to hands, bar, cards, overlays, or buttons will be lost. Continue only if you intentionally want to reset layout.",
                "Overwrite Layout",
                "Cancel"
            );

            if (!overwrite)
            {
                Debug.Log("Build Demo Scene cancelled to preserve existing ClashCanvas layout.");
                return;
            }
        }

        EnsureDirectory(ConfigDirectory);
        ClearExistingDemoObjects();

        ClashFighterConfig playerConfig = EnsureConfig(PlayerConfigPath);
        ClashFighterConfig aiConfig = EnsureConfig(AiConfigPath);
        ClashItemConfig[] items = CreatePlaceholderItems();
        EnsureEventSystem();
        Sprite battleHands = AssetDatabase.LoadAssetAtPath<Sprite>(BattleHandsPath);
        Sprite buttonDefault = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonDefaultPath);
        Sprite buttonSmash = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSmashPath);

        GameObject leftPlayer = CreateAnchor("LeftPlayer");
        GameObject rightPlayer = CreateAnchor("RightPlayer");

        GameObject controllerObject = new("ClashController");
        PlayerClashInput playerInput = controllerObject.AddComponent<PlayerClashInput>();
        AIClashInput aiInput = controllerObject.AddComponent<AIClashInput>();
        ClashController controller = controllerObject.AddComponent<ClashController>();

        GameObject canvasObject = CreateCanvas();
        CreateCanvasGuide(canvasObject.transform);
        CreateBattleHands(canvasObject.transform, battleHands);
        ClashBarUI barUI = CreateBar(canvasObject.transform);
        ClashHUD hud = CreateHud(canvasObject.transform, buttonDefault, buttonSmash);
        MinigameScoreUI scoreUI = CreateScore(canvasObject.transform);
        ItemSelectionUI itemSelectionUI = CreateItemSelection(canvasObject.transform);
        ItemRevealUI itemRevealUI = CreateReveal(canvasObject.transform);
        MinigameResultUI resultUI = CreateMinigameResult(canvasObject.transform);

        GameObject minigameObject = new("AduTosMinigame");
        AduTosMinigameController minigameController = minigameObject.AddComponent<AduTosMinigameController>();

        SerializedObject playerInputSerialized = new(playerInput);
        playerInputSerialized.FindProperty("mashAction").objectReferenceValue = null;
        playerInputSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSerialized = new(controller);
        controllerSerialized.FindProperty("playerConfig").objectReferenceValue = playerConfig;
        controllerSerialized.FindProperty("aiConfig").objectReferenceValue = aiConfig;
        controllerSerialized.FindProperty("playerInputSource").objectReferenceValue = playerInput;
        controllerSerialized.FindProperty("aiInputSource").objectReferenceValue = aiInput;
        controllerSerialized.FindProperty("leftPlayer").objectReferenceValue = leftPlayer.transform;
        controllerSerialized.FindProperty("rightPlayer").objectReferenceValue = rightPlayer.transform;
        controllerSerialized.FindProperty("leftPlayerCenterPosition").vector3Value = new Vector3(-4f, 0f, 0f);
        controllerSerialized.FindProperty("leftPlayerWinningPosition").vector3Value = new Vector3(0f, 0f, 0f);
        controllerSerialized.FindProperty("rightPlayerCenterPosition").vector3Value = new Vector3(4f, 0f, 0f);
        controllerSerialized.FindProperty("rightPlayerWinningPosition").vector3Value = new Vector3(0f, 0f, 0f);
        controllerSerialized.FindProperty("playerPowerOverride").floatValue = 10f;
        controllerSerialized.FindProperty("aiPowerOverride").floatValue = 1f;
        controllerSerialized.FindProperty("powerScale").floatValue = 100f;
        controllerSerialized.FindProperty("centerDecayPerSecond").floatValue = 0.01f;
        controllerSerialized.FindProperty("startOnAwake").boolValue = false;
        controllerSerialized.FindProperty("barUI").objectReferenceValue = barUI;
        controllerSerialized.FindProperty("hud").objectReferenceValue = hud;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject minigameSerialized = new(minigameController);
        minigameSerialized.FindProperty("clashController").objectReferenceValue = controller;
        minigameSerialized.FindProperty("itemSelectionUI").objectReferenceValue = itemSelectionUI;
        minigameSerialized.FindProperty("itemRevealUI").objectReferenceValue = itemRevealUI;
        minigameSerialized.FindProperty("scoreUI").objectReferenceValue = scoreUI;
        minigameSerialized.FindProperty("resultUI").objectReferenceValue = resultUI;
        SerializedProperty itemsProperty = minigameSerialized.FindProperty("availableItems");
        itemsProperty.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
        minigameSerialized.FindProperty("basePlayerPower").floatValue = 10f;
        minigameSerialized.FindProperty("baseAiPower").floatValue = 10f;
        minigameSerialized.FindProperty("pointsToWin").intValue = 2;
        minigameSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Clash demo scene built. Press Play and mash Spacebar.");
    }

    private static GameObject CreateAnchor(string name)
    {
        GameObject anchor = new(name);
        anchor.transform.position = Vector3.zero;
        return anchor;
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObject = new("ClashCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<CanvasBoundsGizmo>();
        return canvasObject;
    }

    private static void CreateCanvasGuide(Transform parent)
    {
        GameObject guide = new("CanvasGuide_1920x1080_ToggleOffBeforePlay");
        guide.transform.SetParent(parent, false);
        RectTransform rect = guide.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CreateGuideLine("Top", guide.transform, new Vector2(1920f, 4f), new Vector2(0.5f, 1f), new Vector2(0f, -2f));
        CreateGuideLine("Bottom", guide.transform, new Vector2(1920f, 4f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
        CreateGuideLine("Left", guide.transform, new Vector2(4f, 1080f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateGuideLine("Right", guide.transform, new Vector2(4f, 1080f), new Vector2(1f, 0.5f), new Vector2(-2f, 0f));
    }

    private static void CreateGuideLine(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position)
    {
        GameObject line = CreateUiObject(name, parent, size, anchor, position);
        Image image = line.AddComponent<Image>();
        image.color = new Color(1f, 0.9f, 0.1f, 0.8f);
        image.raycastTarget = false;
    }

    private static RectTransform CreateBattleHands(Transform parent, Sprite battleHands)
    {
        if (battleHands == null)
        {
            return null;
        }

        GameObject hands = CreateUiObject("BattleHands", parent, new Vector2(430f, 430f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f));
        Image handsImage = hands.AddComponent<Image>();
        handsImage.sprite = battleHands;
        handsImage.preserveAspect = true;

        ClashHandShake handShake = hands.AddComponent<ClashHandShake>();
        SerializedObject shakeSerialized = new(handShake);
        shakeSerialized.FindProperty("target").objectReferenceValue = hands.GetComponent<RectTransform>();
        shakeSerialized.FindProperty("mashAction").objectReferenceValue = null;
        shakeSerialized.FindProperty("shakePerPress").floatValue = 14f;
        shakeSerialized.FindProperty("maxShake").floatValue = 36f;
        shakeSerialized.FindProperty("decayPerSecond").floatValue = 90f;
        shakeSerialized.FindProperty("frequency").floatValue = 60f;
        shakeSerialized.ApplyModifiedPropertiesWithoutUndo();

        return hands.GetComponent<RectTransform>();
    }

    private static ClashBarUI CreateBar(Transform parent)
    {
        GameObject background = CreateUiObject("ClashBar", parent, new Vector2(720f, 232f), new Vector2(0.5f, 0f), new Vector2(0f, 115f));
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = Color.clear;
        ClashBarUI barUI = background.AddComponent<ClashBarUI>();

        GameObject fill = CreateUiObject("Fill", background.transform, new Vector2(560f, 18f), new Vector2(0f, 0.5f), new Vector2(80f, -18f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

        GameObject marker = CreateUiObject("Marker", background.transform, new Vector2(42f, 126f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f));
        Image markerImage = marker.AddComponent<Image>();
        markerImage.color = new Color(1f, 0.78f, 0.16f, 0.82f);

        SerializedObject barSerialized = new(barUI);
        barSerialized.FindProperty("fillImage").objectReferenceValue = fillImage;
        barSerialized.FindProperty("marker").objectReferenceValue = marker.GetComponent<RectTransform>();
        barSerialized.FindProperty("movingTransform").objectReferenceValue = null;
        barSerialized.FindProperty("markerLeftX").floatValue = -280f;
        barSerialized.FindProperty("markerRightX").floatValue = 280f;
        barSerialized.ApplyModifiedPropertiesWithoutUndo();
        return barUI;
    }

    private static ClashHUD CreateHud(Transform parent, Sprite buttonDefault, Sprite buttonSmash)
    {
        GameObject hudObject = new("ClashHUD");
        hudObject.transform.SetParent(parent, false);
        ClashHUD hud = hudObject.AddComponent<ClashHUD>();

        GameObject buttonObject = CreateUiObject("MashButton", hudObject.transform, new Vector2(244f, 142f), new Vector2(0.5f, 0f), new Vector2(0f, 240f));
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = buttonDefault;
        buttonImage.preserveAspect = true;
        buttonImage.color = buttonDefault != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        ClashMashButtonUI mashButtonUI = buttonObject.AddComponent<ClashMashButtonUI>();

        SerializedObject buttonSerialized = new(mashButtonUI);
        buttonSerialized.FindProperty("targetImage").objectReferenceValue = buttonImage;
        buttonSerialized.FindProperty("defaultSprite").objectReferenceValue = buttonDefault;
        buttonSerialized.FindProperty("smashSprite").objectReferenceValue = buttonSmash;
        buttonSerialized.FindProperty("mashAction").objectReferenceValue = null;
        buttonSerialized.ApplyModifiedPropertiesWithoutUndo();

        Text promptText = CreateText("PromptText", hudObject.transform, "SPACEBAR", 38, new Vector2(0.5f, 0f), new Vector2(0f, 242f));
        Text resultText = CreateText("ResultText", hudObject.transform, string.Empty, 72, new Vector2(0.5f, 0.5f), Vector2.zero);

        SerializedObject hudSerialized = new(hud);
        hudSerialized.FindProperty("promptText").objectReferenceValue = promptText;
        hudSerialized.FindProperty("resultText").objectReferenceValue = resultText;
        hudSerialized.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    private static ClashIntroUI CreateIntro(Transform parent)
    {
        GameObject introObject = CreateFullScreenPanel("IntroOverlay", parent, new Color(0f, 0f, 0f, 0.65f));
        ClashIntroUI introUI = introObject.AddComponent<ClashIntroUI>();

        Text countdownText = CreateText("CountdownText", introObject.transform, "GET READY", 96, new Vector2(0.5f, 0.5f), Vector2.zero);
        countdownText.fontStyle = FontStyle.Bold;

        SerializedObject introSerialized = new(introUI);
        introSerialized.FindProperty("canvasGroup").objectReferenceValue = introObject.GetComponent<CanvasGroup>();
        introSerialized.FindProperty("countdownText").objectReferenceValue = countdownText;
        introSerialized.FindProperty("stepDuration").floatValue = 0.7f;
        introSerialized.FindProperty("fadeDuration").floatValue = 0.2f;
        introSerialized.ApplyModifiedPropertiesWithoutUndo();
        return introUI;
    }

    private static ClashResultUI CreateResult(Transform parent)
    {
        GameObject resultObject = CreateFullScreenPanel("ResultOverlay", parent, new Color(0f, 0f, 0f, 0.72f));
        ClashResultUI resultUI = resultObject.AddComponent<ClashResultUI>();

        Text resultText = CreateText("ResultTitle", resultObject.transform, "YOU WIN", 88, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f));
        resultText.fontStyle = FontStyle.Bold;

        GameObject retryObject = CreateUiObject("RetryButton", resultObject.transform, new Vector2(260f, 88f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f));
        Image retryImage = retryObject.AddComponent<Image>();
        retryImage.color = new Color(0.9f, 0.75f, 0.25f, 1f);
        Button retryButton = retryObject.AddComponent<Button>();

        Text retryText = CreateText("RetryText", retryObject.transform, "RETRY", 34, new Vector2(0.5f, 0.5f), Vector2.zero);
        retryText.color = Color.black;
        retryText.fontStyle = FontStyle.Bold;

        SerializedObject resultSerialized = new(resultUI);
        resultSerialized.FindProperty("canvasGroup").objectReferenceValue = resultObject.GetComponent<CanvasGroup>();
        resultSerialized.FindProperty("resultText").objectReferenceValue = resultText;
        resultSerialized.FindProperty("retryButton").objectReferenceValue = retryButton;
        resultSerialized.ApplyModifiedPropertiesWithoutUndo();

        CanvasGroup resultGroup = resultObject.GetComponent<CanvasGroup>();
        resultGroup.alpha = 0f;
        resultGroup.blocksRaycasts = false;
        resultGroup.interactable = false;
        return resultUI;
    }

    private static MinigameScoreUI CreateScore(Transform parent)
    {
        GameObject scoreObject = CreateUiObject("MinigameScore", parent, new Vector2(800f, 70f), new Vector2(0.5f, 1f), new Vector2(0f, -48f));
        MinigameScoreUI scoreUI = scoreObject.AddComponent<MinigameScoreUI>();
        Text scoreText = scoreObject.AddComponent<Text>();
        scoreText.text = "Round 1   Player 0 - 0 AI";
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 32;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = Color.white;

        SerializedObject scoreSerialized = new(scoreUI);
        scoreSerialized.FindProperty("scoreText").objectReferenceValue = scoreText;
        scoreSerialized.ApplyModifiedPropertiesWithoutUndo();
        return scoreUI;
    }

    private static ItemSelectionUI CreateItemSelection(Transform parent)
    {
        GameObject panel = CreateFullScreenPanel("ItemSelectionOverlay", parent, new Color(0f, 0f, 0f, 0.72f));
        ItemSelectionUI selectionUI = panel.AddComponent<ItemSelectionUI>();

        Text title = CreateText("ItemSelectionTitle", panel.transform, "Choose 1 Item", 64, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f));
        title.fontStyle = FontStyle.Bold;

        GameObject carouselRoot = CreateUiObject("CarouselRoot", panel.transform, new Vector2(900f, 300f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f));
        Image dragCatcher = carouselRoot.AddComponent<Image>();
        dragCatcher.color = new Color(1f, 1f, 1f, 0.001f);

        GameObject itemTemplateObject = CreateUiObject("ItemTemplate", carouselRoot.transform, new Vector2(200f, 260f), new Vector2(0.5f, 0.5f), Vector2.zero);
        CanvasGroup itemGroup = itemTemplateObject.AddComponent<CanvasGroup>();
        itemGroup.alpha = 1f;
        itemGroup.interactable = true;
        itemGroup.blocksRaycasts = true;
        Image itemImage = itemTemplateObject.AddComponent<Image>();
        itemImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        ItemCarouselItem itemTemplate = itemTemplateObject.AddComponent<ItemCarouselItem>();

        GameObject itemClickArea = CreateStretchObject("ItemClickArea", itemTemplateObject.transform);
        Image itemClickImage = itemClickArea.AddComponent<Image>();
        itemClickImage.color = new Color(1f, 1f, 1f, 0.001f);
        Button itemButton = itemClickArea.AddComponent<Button>();
        itemButton.onClick.AddListener(selectionUI.ConfirmSelection);

        Text itemLabel = CreateText("ItemLabel", itemTemplateObject.transform, "N\nItem\nx1", 34, new Vector2(0.5f, 0.5f), Vector2.zero);
        itemLabel.color = Color.black;
        itemLabel.fontStyle = FontStyle.Bold;
        itemLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 220f);

        SerializedObject itemSerialized = new(itemTemplate);
        itemSerialized.FindProperty("backgroundImage").objectReferenceValue = itemImage;
        itemSerialized.FindProperty("label").objectReferenceValue = itemLabel;
        itemSerialized.ApplyModifiedPropertiesWithoutUndo();

        Button previousButton = CreateButton(panel.transform, "PrevButton", "<", new Vector2(0.5f, 0.5f), new Vector2(-520f, -20f), new Vector2(80f, 80f));
        Button nextButton = CreateButton(panel.transform, "NextButton", ">", new Vector2(0.5f, 0.5f), new Vector2(520f, -20f), new Vector2(80f, 80f));
        Button selectButton = CreateButton(panel.transform, "SelectButton", "SELECT", new Vector2(0.5f, 0.5f), new Vector2(0f, -220f), new Vector2(260f, 72f));

        SerializedObject selectionSerialized = new(selectionUI);
        selectionSerialized.FindProperty("canvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
        selectionSerialized.FindProperty("titleText").objectReferenceValue = title;
        selectionSerialized.FindProperty("itemRoot").objectReferenceValue = carouselRoot.GetComponent<RectTransform>();
        selectionSerialized.FindProperty("itemTemplate").objectReferenceValue = itemTemplate;
        selectionSerialized.FindProperty("previousButton").objectReferenceValue = previousButton;
        selectionSerialized.FindProperty("nextButton").objectReferenceValue = nextButton;
        selectionSerialized.FindProperty("selectButton").objectReferenceValue = selectButton;
        selectionSerialized.FindProperty("itemSpacing").floatValue = 180f;
        selectionSerialized.FindProperty("dragSensitivity").floatValue = 0.004f;
        selectionSerialized.FindProperty("snapSpeed").floatValue = 12f;
        selectionSerialized.FindProperty("centerScale").floatValue = 1f;
        selectionSerialized.FindProperty("minScale").floatValue = 0.55f;
        selectionSerialized.FindProperty("scaleRange").floatValue = 2f;
        selectionSerialized.ApplyModifiedPropertiesWithoutUndo();

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return selectionUI;
    }

    private static ItemRevealUI CreateReveal(Transform parent)
    {
        GameObject panel = CreateFullScreenPanel("ItemRevealOverlay", parent, new Color(0f, 0f, 0f, 0.68f));
        ItemRevealUI revealUI = panel.AddComponent<ItemRevealUI>();

        Text title = CreateText("RevealTitle", panel.transform, "REVEAL", 60, new Vector2(0.5f, 0.5f), new Vector2(0f, 215f));
        title.fontStyle = FontStyle.Bold;
        Text playerText = CreateText("PlayerRevealItem", panel.transform, "YOU", 44, new Vector2(0.5f, 0.5f), new Vector2(-260f, 20f));
        Text aiText = CreateText("AiRevealItem", panel.transform, "AI", 44, new Vector2(0.5f, 0.5f), new Vector2(260f, 20f));

        SerializedObject revealSerialized = new(revealUI);
        revealSerialized.FindProperty("canvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
        revealSerialized.FindProperty("playerItemText").objectReferenceValue = playerText;
        revealSerialized.FindProperty("aiItemText").objectReferenceValue = aiText;
        revealSerialized.FindProperty("revealDuration").floatValue = 1.2f;
        revealSerialized.ApplyModifiedPropertiesWithoutUndo();

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return revealUI;
    }

    private static MinigameResultUI CreateMinigameResult(Transform parent)
    {
        GameObject panel = CreateFullScreenPanel("MinigameResultOverlay", parent, new Color(0f, 0f, 0f, 0.72f));
        MinigameResultUI resultUI = panel.AddComponent<MinigameResultUI>();

        Text resultText = CreateText("MinigameResultTitle", panel.transform, "YOU WIN BEST OF 3", 72, new Vector2(0.5f, 0.5f), new Vector2(0f, 95f));
        resultText.fontStyle = FontStyle.Bold;

        GameObject retryObject = CreateUiObject("RetryButton", panel.transform, new Vector2(260f, 88f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f));
        Image retryImage = retryObject.AddComponent<Image>();
        retryImage.color = new Color(0.9f, 0.75f, 0.25f, 1f);
        Button retryButton = retryObject.AddComponent<Button>();
        Text retryText = CreateText("RetryText", retryObject.transform, "RETRY", 34, new Vector2(0.5f, 0.5f), Vector2.zero);
        retryText.color = Color.black;
        retryText.fontStyle = FontStyle.Bold;

        SerializedObject resultSerialized = new(resultUI);
        resultSerialized.FindProperty("canvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
        resultSerialized.FindProperty("resultText").objectReferenceValue = resultText;
        resultSerialized.FindProperty("retryButton").objectReferenceValue = retryButton;
        resultSerialized.ApplyModifiedPropertiesWithoutUndo();

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return resultUI;
    }

    private static GameObject CreateFullScreenPanel(string name, Transform parent, Color color)
    {
        GameObject panel = new(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = color;
        panel.AddComponent<CanvasGroup>();
        return panel;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize, Vector2 anchor, Vector2 position)
    {
        GameObject textObject = CreateUiObject(name, parent, new Vector2(600f, 120f), anchor, position);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.raycastTarget = false;
        return uiText;
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position)
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

    private static GameObject CreateStretchObject(string name, Transform parent)
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

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(name, parent, size, anchor, position);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.9f, 0.75f, 0.25f, 1f);
        Button button = buttonObject.AddComponent<Button>();

        Text text = CreateText($"{name}_Text", buttonObject.transform, label, 32, new Vector2(0.5f, 0.5f), Vector2.zero);
        text.color = Color.black;
        text.fontStyle = FontStyle.Bold;
        text.GetComponent<RectTransform>().sizeDelta = size;
        return button;
    }

    private static ClashFighterConfig EnsureConfig(string path)
    {
        ClashFighterConfig config = AssetDatabase.LoadAssetAtPath<ClashFighterConfig>(path);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<ClashFighterConfig>();
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    private static ClashItemConfig[] CreatePlaceholderItems()
    {
        ClashItemConfig[] items = new ClashItemConfig[15];
        int index = 0;

        for (int i = 1; i <= 5; i++)
        {
            items[index++] = EnsureItem($"{ConfigDirectory}/Item_N_{i:00}.asset", $"Normal {i:00}", ClashItemRarity.N, 1f);
        }

        for (int i = 1; i <= 5; i++)
        {
            items[index++] = EnsureItem($"{ConfigDirectory}/Item_R_{i:00}.asset", $"Rare {i:00}", ClashItemRarity.R, 1.5f);
        }

        for (int i = 1; i <= 5; i++)
        {
            items[index++] = EnsureItem($"{ConfigDirectory}/Item_SR_{i:00}.asset", $"Super Rare {i:00}", ClashItemRarity.SR, 2f);
        }

        return items;
    }

    private static ClashItemConfig EnsureItem(string path, string displayName, ClashItemRarity rarity, float multiplier)
    {
        ClashItemConfig item = AssetDatabase.LoadAssetAtPath<ClashItemConfig>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ClashItemConfig>();
            AssetDatabase.CreateAsset(item, path);
        }

        SerializedObject itemSerialized = new(item);
        itemSerialized.FindProperty("displayName").stringValue = displayName;
        itemSerialized.FindProperty("rarity").enumValueIndex = (int)rarity;
        itemSerialized.FindProperty("powerMultiplier").floatValue = multiplier;
        itemSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void ClearExistingDemoObjects()
    {
        string[] names =
        {
            "LeftPlayer",
            "RightPlayer",
            "ClashController",
            "ClashFlow",
            "AduTosMinigame",
            "ClashCanvas"
        };

        foreach (string name in names)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
        {
            Object.DestroyImmediate(oldModule);
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputModule.AssignDefaultActions();
    }

    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureDirectory(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
