using Harukerryzi.Clash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageMapLayoutTool
{
    private const string LevelSpriteRoot = "Assets/Projects/Sprites/LevelScreen";
    private const string StageMapSpriteRoot = "Assets/Projects/Sprites/StageMap";
    private const float NodeSpacing = 430f;

    [MenuItem("Tools/Clash/Bake Level Map (Non-Destructive)")]
    public static void BakeLevelMap()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before baking the level map.");
            return;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found. Open StageMap scene first.");
            return;
        }

        Transform canvasTransform = canvas.transform;
        RectTransform nodesContainer = EnsureRect(canvasTransform, "NodesContainer");
        Image backgroundImage = EnsureBackground(canvasTransform);
        SetStretch(backgroundImage.transform as RectTransform);
        SetRect(nodesContainer, new Vector2(1920f, 1080f), Vector2.zero);

        Image[] nodeImages = new Image[5];
        RectTransform[] nodeRects = new RectTransform[5];
        Image[] connectorImages = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            RectTransform connector = EnsureRect(nodesContainer, "Connector_" + i);
            connectorImages[i] = EnsureImage(connector.gameObject);
            connectorImages[i].sprite = LoadSprite("UI_LevelConnector.png");
            connectorImages[i].color = Color.white;
            connectorImages[i].preserveAspect = true;
            connectorImages[i].raycastTarget = false;
            SetRect(connector, new Vector2(560f, 136f), new Vector2((i + 0.5f) * NodeSpacing, 0f));
            connector.SetAsFirstSibling();
        }

        for (int i = 0; i < 5; i++)
        {
            RectTransform node = EnsureRect(nodesContainer, "Node_" + i);
            HideLegacyNodeChildren(node);
            nodeImages[i] = EnsureImage(node.gameObject);
            nodeImages[i].color = Color.white;
            nodeImages[i].preserveAspect = true;
            nodeImages[i].raycastTarget = true;
            nodeImages[i].sprite = GetNormalSprite(i);
            SetRect(node, new Vector2(258f, 276f), new Vector2(i * NodeSpacing, 0f));

            Button button = node.GetComponent<Button>();
            if (button == null)
            {
                button = node.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = nodeImages[i];
            button.transition = Selectable.Transition.None;
            button.colors = ColorBlock.defaultColorBlock;
            node.SetAsLastSibling();
            nodeRects[i] = node;
        }

        HideLegacyStageLabels(nodesContainer);
        WireController(canvas.gameObject, backgroundImage, nodesContainer, nodeImages, nodeRects, connectorImages);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Baked level map layout: Tikus -> Store -> Kunti -> Store -> Tiang.");
    }

    private static Image EnsureBackground(Transform canvas)
    {
        RectTransform background = EnsureRect(canvas, "LevelBackground");
        Image image = EnsureImage(background.gameObject);
        image.sprite = LoadStageMapSprite("battle_tikus.png");
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
        background.SetAsFirstSibling();
        return image;
    }

    private static void WireController(GameObject canvasObject, Image backgroundImage, RectTransform nodesContainer, Image[] nodeImages, RectTransform[] nodeRects, Image[] connectorImages)
    {
        StageMapController controller = Object.FindFirstObjectByType<StageMapController>();
        if (controller == null)
        {
            controller = canvasObject.AddComponent<StageMapController>();
        }

        SerializedObject serialized = new(controller);
        AssignIfPresent(serialized, "levelBackground", backgroundImage);
        AssignIfPresent(serialized, "nodesContainer", nodesContainer);
        AssignIfPresent(serialized, "nodeImages", nodeImages);
        AssignIfPresent(serialized, "nodeRects", nodeRects);
        AssignIfPresent(serialized, "connectorImages", connectorImages);
        AssignIfPresent(serialized, "level1Sprite", LoadSprite("UI_Level1Button.png"));
        AssignIfPresent(serialized, "level1SelectedSprite", LoadSprite("UI_Level1Button_Selected.png"));
        AssignIfPresent(serialized, "level2Sprite", LoadSprite("UI_Level2Button.png"));
        AssignIfPresent(serialized, "level2SelectedSprite", LoadSprite("UI_Level2Button_Selected.png"));
        AssignIfPresent(serialized, "level3Sprite", LoadSprite("UI_Level3Button.png"));
        AssignIfPresent(serialized, "level3SelectedSprite", LoadSprite("UI_Level3Button_Selected.png"));
        AssignIfPresent(serialized, "storeSprite", LoadSprite("UI_LevelStoreButton.png"));
        AssignIfPresent(serialized, "storeSelectedSprite", LoadSprite("UI_LevelStoreButton_Selected.png"));
        AssignIfPresent(serialized, "disabledSprite", LoadSprite("UI_LevelButtonDisabled.png"));
        AssignIfPresent(serialized, "connectorSprite", LoadSprite("UI_LevelConnector.png"));
        AssignIfPresent(serialized, "connectorDisabledSprite", LoadSprite("UI_LevelConnector_Disabled.png"));
        AssignIfPresent(serialized, "defaultBackgroundSprite", LoadSprite("UI_LevelBackground.png"));
        AssignIfPresent(serialized, "tikusBackgroundSprite", LoadStageMapSprite("battle_tikus.png"));
        AssignIfPresent(serialized, "kuntiBackgroundSprite", LoadStageMapSprite("battle_kunti.png"));
        AssignIfPresent(serialized, "tiangBackgroundSprite", LoadStageMapSprite("battle_tiang.png"));
        AssignIfPresent(serialized, "stageEnemies", new Object[]
        {
            AssetDatabase.LoadAssetAtPath<AduTosEnemyConfig>("Assets/Projects/Settings/Clash/Enemy_1_Tikus.asset"),
            AssetDatabase.LoadAssetAtPath<AduTosEnemyConfig>("Assets/Projects/Settings/Clash/Enemy_2_Kunti.asset"),
            AssetDatabase.LoadAssetAtPath<AduTosEnemyConfig>("Assets/Projects/Settings/Clash/Enemy_3_Tiang.asset")
        });
        SetFloatIfPresent(serialized, "spacing", NodeSpacing);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignIfPresent(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void AssignIfPresent(SerializedObject serialized, string propertyName, Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return;
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetFloatIfPresent(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static Sprite GetNormalSprite(int position)
    {
        switch (position)
        {
            case 0:
                return LoadSprite("UI_Level1Button.png");
            case 1:
            case 3:
                return LoadSprite("UI_LevelStoreButton.png");
            case 2:
                return LoadSprite("UI_Level2Button.png");
            case 4:
                return LoadSprite("UI_Level3Button.png");
            default:
                return null;
        }
    }

    private static Sprite LoadSprite(string fileName)
    {
        string path = $"{LevelSpriteRoot}/{fileName}";
        return LoadSpriteAtPath(path, "Level map sprite not loaded: ");
    }

    private static Sprite LoadStageMapSprite(string fileName)
    {
        string path = $"{StageMapSpriteRoot}/{fileName}";
        return LoadSpriteAtPath(path, "Stage map sprite not loaded: ");
    }

    private static Sprite LoadSpriteAtPath(string path, string missingLogPrefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError(missingLogPrefix + path);
        }

        return sprite;
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = obj.AddComponent<RectTransform>();
        }

        return rect;
    }

    private static Image EnsureImage(GameObject obj)
    {
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }

        return image;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void HideLegacyNodeChildren(RectTransform node)
    {
        for (int i = 0; i < node.childCount; i++)
        {
            node.GetChild(i).gameObject.SetActive(false);
        }
    }

    private static void HideLegacyStageLabels(Transform nodesContainer)
    {
        for (int i = 0; i < 10; i++)
        {
            Transform label = nodesContainer.Find("StageLabel_" + i);
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}

[InitializeOnLoad]
public static class StageMapSceneAutoBake
{
    private static bool scheduled;

    static StageMapSceneAutoBake()
    {
        EditorApplication.delayCall += MaybeBake;
    }

    private static void MaybeBake()
    {
        if (scheduled || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!SceneManager.GetActiveScene().path.EndsWith("StageMap.unity"))
        {
            return;
        }

        scheduled = true;
        try
        {
            StageMapLayoutTool.BakeLevelMap();
        }
        finally
        {
            scheduled = false;
        }
    }
}
