using System.Collections;
using Harukerryzi.Clash;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public sealed class StageMapController : MonoBehaviour
{
    private const int NodeCount = 5;
    private const int ConnectorCount = NodeCount - 1;
    private const int MaxBattleStageIndex = 2;

    [Header("References")]
    [SerializeField] private Image levelBackground;
    [SerializeField] private RectTransform nodesContainer;
    [SerializeField] private RectTransform[] nodeRects = new RectTransform[NodeCount];
    [SerializeField] private Image[] nodeImages = new Image[NodeCount];
    [SerializeField] private Image[] connectorImages = new Image[ConnectorCount];
    [Tooltip("Battle enemy config per battle stage: 0=Tikus, 1=Kunti, 2=Tiang")]
    [SerializeField] private AduTosEnemyConfig[] stageEnemies;
    [SerializeField] private string battleSceneName = "ClashScene";
    [SerializeField] private string shopSceneName = "Shop";

    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Sprites")]
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level1SelectedSprite;
    [SerializeField] private Sprite level2Sprite;
    [SerializeField] private Sprite level2SelectedSprite;
    [SerializeField] private Sprite level3Sprite;
    [SerializeField] private Sprite level3SelectedSprite;
    [SerializeField] private Sprite storeSprite;
    [SerializeField] private Sprite storeSelectedSprite;
    [SerializeField] private Sprite disabledSprite;
    [SerializeField] private Sprite connectorSprite;
    [SerializeField] private Sprite connectorDisabledSprite;

    [Header("Backgrounds")]
    [SerializeField] private Sprite defaultBackgroundSprite;
    [SerializeField] private Sprite tikusBackgroundSprite;
    [SerializeField] private Sprite kuntiBackgroundSprite;
    [SerializeField] private Sprite tiangBackgroundSprite;

    [Header("Layout")]
    [SerializeField] private float spacing = 430f;
    [SerializeField] private Vector2 normalNodeSize = new(258f, 276f);
    [SerializeField] private Vector2 selectedNodeSize = new(404f, 416f);
    [SerializeField] private Vector2 connectorSize = new(560f, 136f);
    [SerializeField] private float transitionDuration = 0.4f;

    private readonly MapNode[] _nodes = new MapNode[NodeCount];
    private int _selectedPos;
    private int _frontierPos;
    private bool _isTransitioning;
    private int _ignoreInputFrames;

    private enum NodeKind
    {
        Battle,
        Store
    }

    private sealed class MapNode
    {
        public NodeKind Kind;
        public int BattleStageIndex;
        public RectTransform Rect;
        public Image Image;
    }

    private void Start()
    {
        GameAudio.PlayMusic(backgroundMusic);
#if UNITY_EDITOR
        EnsureEditorSpriteFallbacks();
#endif
        EnsureBackgroundSpriteFallbacks();
        DiscoverNodes();
        int requestedSelection = ApplyReturnedBattleResult();
        _frontierPos = GetFrontierPosition();
        _selectedPos = requestedSelection >= 0 ? Mathf.Min(requestedSelection, _frontierPos) : GetDefaultSelectedPosition();
        ApplyStateImmediate();
        CenterOnSelectedImmediate();
        _ignoreInputFrames = 5;
    }

    private void Update()
    {
        if (_ignoreInputFrames > 0)
        {
            _ignoreInputFrames--;
            return;
        }

        if (_isTransitioning || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            SelectPosition(Mathf.Max(0, _selectedPos - 1));
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            SelectPosition(Mathf.Min(_frontierPos, _selectedPos + 1));
        }
        else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            ConfirmSelected();
        }
    }

    private void DiscoverNodes()
    {
        if (nodesContainer == null)
        {
            Transform found = transform.Find("NodesContainer") ?? GameObject.Find("NodesContainer")?.transform;
            nodesContainer = found as RectTransform;
        }

        for (int i = 0; i < NodeCount; i++)
        {
            RectTransform rect = GetNodeRect(i);
            Image image = GetNodeImage(i, rect);
            _nodes[i] = new MapNode
            {
                Kind = IsStorePosition(i) ? NodeKind.Store : NodeKind.Battle,
                BattleStageIndex = GetBattleStageIndexForPosition(i),
                Rect = rect,
                Image = image
            };

            if (rect == null)
            {
                continue;
            }

            HideChildVisuals(rect);

            Button button = rect.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
            }

            if (image != null)
            {
                button.targetGraphic = image;
            }

            button.transition = Selectable.Transition.None;
            button.colors = ColorBlock.defaultColorBlock;
            int capturedPosition = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnNodeClicked(capturedPosition));
        }
    }

    private int ApplyReturnedBattleResult()
    {
        if (!BattleSession.HasResult)
        {
            return -1;
        }

        int selectedStage = BattleSession.SelectedStageIndex;
        bool advanceToStore = BattleSession.PlayerWon && !BattleSession.IsReplayStage;
        if (advanceToStore)
        {
            StageProgress.MarkStageCleared(selectedStage, MaxBattleStageIndex);
        }

        BattleSession.ClearResult();

        if (!advanceToStore)
        {
            return -1;
        }
        return GetStorePositionAfterBattleStage(selectedStage);
    }

    private int GetDefaultSelectedPosition()
    {
        int highest = StageProgress.HighestUnlockedStage;
        if (highest <= 0)
        {
            return 0;
        }

        return GetPositionForBattleStage(Mathf.Min(highest, MaxBattleStageIndex));
    }

    private void OnNodeClicked(int position)
    {
        if (_isTransitioning || _ignoreInputFrames > 0 || !IsPositionUnlocked(position))
        {
            return;
        }

        if (position != _selectedPos)
        {
            _selectedPos = position;
            ApplyStateImmediate();
            StartCoroutine(TransitionToSelected(null));
            return;
        }

        ConfirmSelected();
    }

    private void SelectPosition(int position)
    {
        position = Mathf.Clamp(position, 0, _frontierPos);
        if (position == _selectedPos)
        {
            return;
        }

        _selectedPos = position;
        ApplySelectedBackground();
        StopAllCoroutines();
        StartCoroutine(TransitionToSelected(null));
    }

    private void ConfirmSelected()
    {
        if (!IsPositionUnlocked(_selectedPos))
        {
            return;
        }

        MapNode node = _nodes[_selectedPos];
        if (node == null || node.Kind == NodeKind.Store)
        {
            LoadShopScene();
            return;
        }

        AduTosEnemyConfig enemy = GetEnemyForStage(node.BattleStageIndex);
        if (enemy == null)
        {
            Debug.LogWarning("[StageMap] Missing enemy config for stage " + node.BattleStageIndex);
            return;
        }

        BattleSession.SelectStage(node.BattleStageIndex, enemy, node.BattleStageIndex < StageProgress.HighestUnlockedStage);
        LoadBattleScene();
    }

    private void LoadBattleScene()
    {
        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogWarning("[StageMap] Missing battle scene name.");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(battleSceneName))
        {
            SceneManager.LoadScene(battleSceneName);
            return;
        }

#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Projects/Scenes/SandBox/ClashScene.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogWarning("[StageMap] Scene is not in Build Settings: " + battleSceneName);
#endif
    }

    private void LoadShopScene()
    {
        if (string.IsNullOrWhiteSpace(shopSceneName))
        {
            Debug.LogWarning("[StageMap] Missing shop scene name.");
            return;
        }

        SceneHistory.SetReturnScene("StageMap");

        if (Application.CanStreamedLevelBeLoaded(shopSceneName))
        {
            SceneManager.LoadScene(shopSceneName);
            return;
        }

#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Projects/Scenes/SandBox/Shop.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogWarning("[StageMap] Scene is not in Build Settings: " + shopSceneName);
#endif
    }

    private IEnumerator TransitionToSelected(System.Action onComplete)
    {
        _isTransitioning = true;
        float targetContainerX = -_selectedPos * spacing;
        float startContainerX = nodesContainer != null ? nodesContainer.anchoredPosition.x : 0f;

        Vector2[] startSizes = new Vector2[NodeCount];
        Vector2[] targetSizes = new Vector2[NodeCount];
        for (int i = 0; i < NodeCount; i++)
        {
            RectTransform rect = _nodes[i]?.Rect;
            startSizes[i] = rect != null ? rect.sizeDelta : normalNodeSize;
            targetSizes[i] = GetTargetSize(i);
        }

        ApplySpritesOnly();
        ApplySelectedBackground();

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, transitionDuration)));

            if (nodesContainer != null)
            {
                nodesContainer.anchoredPosition = new Vector2(Mathf.Lerp(startContainerX, targetContainerX, t), nodesContainer.anchoredPosition.y);
            }

            for (int i = 0; i < NodeCount; i++)
            {
                RectTransform rect = _nodes[i]?.Rect;
                if (rect != null)
                {
                    rect.sizeDelta = Vector2.Lerp(startSizes[i], targetSizes[i], t);
                }
            }

            yield return null;
        }

        CenterOnSelectedImmediate();
        ApplyStateImmediate();
        _isTransitioning = false;
        onComplete?.Invoke();
    }

    private void ApplyStateImmediate()
    {
        ApplySpritesOnly();
        ApplySelectedBackground();
        for (int i = 0; i < NodeCount; i++)
        {
            RectTransform rect = _nodes[i]?.Rect;
            if (rect != null)
            {
                rect.sizeDelta = GetTargetSize(i);
                rect.anchoredPosition = new Vector2(i * spacing, 0f);
            }
        }

        for (int i = 0; i < ConnectorCount; i++)
        {
            Image connector = GetConnectorImage(i);
            if (connector == null)
            {
                continue;
            }

            connector.sprite = IsPositionUnlocked(i + 1) ? connectorSprite : connectorDisabledSprite;
            connector.color = Color.white;
            connector.preserveAspect = true;
            connector.raycastTarget = false;

            RectTransform connectorRect = connector.transform as RectTransform;
            if (connectorRect != null)
            {
                connectorRect.sizeDelta = connectorSize;
                connectorRect.anchoredPosition = new Vector2((i + 0.5f) * spacing, 0f);
            }
        }
    }

    private void ApplySpritesOnly()
    {
        for (int i = 0; i < NodeCount; i++)
        {
            MapNode node = _nodes[i];
            if (node?.Image == null)
            {
                continue;
            }

            node.Image.sprite = GetSpriteForPosition(i);
            node.Image.color = Color.white;
            node.Image.preserveAspect = true;
            node.Image.raycastTarget = true;
        }
    }

    private void ApplySelectedBackground()
    {
        Image background = GetLevelBackground();
        if (background == null)
        {
            return;
        }

        Sprite selectedBackground = GetBackgroundSpriteForPosition(_selectedPos);
        background.sprite = selectedBackground != null ? selectedBackground : defaultBackgroundSprite;
        background.color = Color.white;
        background.preserveAspect = false;
        background.raycastTarget = false;
    }

    private Image GetLevelBackground()
    {
        if (levelBackground != null)
        {
            return levelBackground;
        }

        Transform found = transform.Find("LevelBackground") ?? GameObject.Find("LevelBackground")?.transform;
        levelBackground = found != null ? found.GetComponent<Image>() : null;
        return levelBackground;
    }

    private Sprite GetBackgroundSpriteForPosition(int position)
    {
        switch (position)
        {
            case 0:
            case 1:
                return tikusBackgroundSprite;
            case 2:
            case 3:
                return kuntiBackgroundSprite;
            case 4:
                return tiangBackgroundSprite;
            default:
                return defaultBackgroundSprite;
        }
    }

    private void CenterOnSelectedImmediate()
    {
        if (nodesContainer != null)
        {
            nodesContainer.anchoredPosition = new Vector2(-_selectedPos * spacing, nodesContainer.anchoredPosition.y);
        }
    }

    private Vector2 GetTargetSize(int position)
    {
        if (!IsPositionUnlocked(position))
        {
            return normalNodeSize;
        }

        return position == _selectedPos ? selectedNodeSize : normalNodeSize;
    }

    private Sprite GetSpriteForPosition(int position)
    {
        if (!IsPositionUnlocked(position))
        {
            return disabledSprite;
        }

        bool selected = position == _selectedPos;
        switch (position)
        {
            case 0:
                return selected ? level1SelectedSprite : level1Sprite;
            case 1:
            case 3:
                return selected ? storeSelectedSprite : storeSprite;
            case 2:
                return selected ? level2SelectedSprite : level2Sprite;
            case 4:
                return selected ? level3SelectedSprite : level3Sprite;
            default:
                return disabledSprite;
        }
    }


    private void EnsureBackgroundSpriteFallbacks()
    {
        if (tikusBackgroundSprite == null)
            tikusBackgroundSprite = Resources.Load<Sprite>("BattleSprites/battle_tikus");
        if (kuntiBackgroundSprite == null)
            kuntiBackgroundSprite = Resources.Load<Sprite>("BattleSprites/battle_kunti");
        if (tiangBackgroundSprite == null)
            tiangBackgroundSprite = Resources.Load<Sprite>("BattleSprites/battle_tiang");

#if UNITY_EDITOR
        if (tikusBackgroundSprite == null)
        {
            string path = "Assets/Projects/Sprites/StageMap/battle_tikus.png";
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded != null) tikusBackgroundSprite = loaded;
        }
        if (kuntiBackgroundSprite == null)
        {
            string path = "Assets/Projects/Sprites/StageMap/battle_kunti.png";
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded != null) kuntiBackgroundSprite = loaded;
        }
        if (tiangBackgroundSprite == null)
        {
            string path = "Assets/Projects/Sprites/StageMap/battle_tiang.png";
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded != null) tiangBackgroundSprite = loaded;
        }
#endif

        defaultBackgroundSprite ??= tikusBackgroundSprite;
    }

#if UNITY_EDITOR
    private void EnsureEditorSpriteFallbacks()
    {
        level1Sprite ??= LoadEditorSprite("UI_Level1Button.png");
        level1SelectedSprite ??= LoadEditorSprite("UI_Level1Button_Selected.png");
        level2Sprite ??= LoadEditorSprite("UI_Level2Button.png");
        level2SelectedSprite ??= LoadEditorSprite("UI_Level2Button_Selected.png");
        level3Sprite ??= LoadEditorSprite("UI_Level3Button.png");
        level3SelectedSprite ??= LoadEditorSprite("UI_Level3Button_Selected.png");
        storeSprite ??= LoadEditorSprite("UI_LevelStoreButton.png");
        storeSelectedSprite ??= LoadEditorSprite("UI_LevelStoreButton_Selected.png");
        disabledSprite ??= LoadEditorSprite("UI_LevelButtonDisabled.png");
        connectorSprite ??= LoadEditorSprite("UI_LevelConnector.png");
        connectorDisabledSprite ??= LoadEditorSprite("UI_LevelConnector_Disabled.png");
        defaultBackgroundSprite ??= LoadEditorSprite("UI_LevelBackground.png");
        tikusBackgroundSprite ??= LoadEditorSpriteAtPath("Assets/Projects/Sprites/StageMap/battle_tikus.png");
        kuntiBackgroundSprite ??= LoadEditorSpriteAtPath("Assets/Projects/Sprites/StageMap/battle_kunti.png");
        tiangBackgroundSprite ??= LoadEditorSpriteAtPath("Assets/Projects/Sprites/StageMap/battle_tiang.png");
    }

    private static Sprite LoadEditorSprite(string fileName)
    {
        string path = "Assets/Projects/Sprites/LevelScreen/" + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite LoadEditorSpriteAtPath(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
#endif

    private int GetFrontierPosition()
    {
        int frontier = 0;
        for (int i = 0; i < NodeCount; i++)
        {
            if (IsPositionUnlocked(i))
            {
                frontier = i;
            }
        }

        return frontier;
    }

    private bool IsPositionUnlocked(int position)
    {
        int highestStage = StageProgress.HighestUnlockedStage;
        switch (position)
        {
            case 0:
                return true;
            case 1:
                return highestStage >= 1;
            case 2:
                return highestStage >= 1;
            case 3:
                return highestStage >= 2;
            case 4:
                return highestStage >= 2;
            default:
                return false;
        }
    }

    private static bool IsStorePosition(int position)
    {
        return position == 1 || position == 3;
    }

    private static int GetBattleStageIndexForPosition(int position)
    {
        switch (position)
        {
            case 0:
                return 0;
            case 2:
                return 1;
            case 4:
                return 2;
            default:
                return -1;
        }
    }

    private static int GetPositionForBattleStage(int battleStageIndex)
    {
        switch (battleStageIndex)
        {
            case 0:
                return 0;
            case 1:
                return 2;
            case 2:
                return 4;
            default:
                return 0;
        }
    }

    private static int GetStorePositionAfterBattleStage(int battleStageIndex)
    {
        switch (battleStageIndex)
        {
            case 0:
                return 1;
            case 1:
                return 3;
            default:
                return 4;
        }
    }

    private RectTransform GetNodeRect(int position)
    {
        if (nodeRects != null && position < nodeRects.Length && nodeRects[position] != null)
        {
            return nodeRects[position];
        }

        Transform found = nodesContainer != null ? nodesContainer.Find("Node_" + position) : null;
        return found as RectTransform;
    }

    private Image GetNodeImage(int position, RectTransform rect)
    {
        if (nodeImages != null && position < nodeImages.Length && nodeImages[position] != null)
        {
            return nodeImages[position];
        }

        return rect != null ? rect.GetComponent<Image>() : null;
    }

    private Image GetConnectorImage(int position)
    {
        if (connectorImages != null && position < connectorImages.Length && connectorImages[position] != null)
        {
            return connectorImages[position];
        }

        Transform found = nodesContainer != null ? nodesContainer.Find("Connector_" + position) : null;
        return found != null ? found.GetComponent<Image>() : null;
    }

    private static void HideChildVisuals(RectTransform rect)
    {
        for (int i = 0; i < rect.childCount; i++)
        {
            rect.GetChild(i).gameObject.SetActive(false);
        }
    }

    private AduTosEnemyConfig GetEnemyForStage(int index)
    {
        if (index < 0)
        {
            return null;
        }

        if (stageEnemies != null && index < stageEnemies.Length && stageEnemies[index] != null)
        {
            return stageEnemies[index];
        }

        string[] names = { "Enemy_1_Tikus", "Enemy_2_Kunti", "Enemy_3_Tiang" };
        if (index < names.Length)
        {
            AduTosEnemyConfig loaded = Resources.Load<AduTosEnemyConfig>("Enemies/" + names[index]);
            if (loaded != null) return loaded;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<AduTosEnemyConfig>(GetEnemyAssetPath(index));
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    private static string GetEnemyAssetPath(int index)
    {
        switch (index)
        {
            case 0:
                return "Assets/Projects/Settings/Clash/Enemy_1_Tikus.asset";
            case 1:
                return "Assets/Projects/Settings/Clash/Enemy_2_Kunti.asset";
            case 2:
                return "Assets/Projects/Settings/Clash/Enemy_3_Tiang.asset";
            default:
                return string.Empty;
        }
    }
#endif
}
