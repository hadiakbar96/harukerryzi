using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    private const string TITLE_SCENE = "TitleScreen";
    private const string RESOURCES_FOLDER = "PauseMenu/";

    private Canvas _canvas;
    private CanvasGroup _overlayGroup;
    private RectTransform _pauseButtonRect;
    private RectTransform _resumeButtonRect;
    private RectTransform _mainMenuButtonRect;
    private Image _resumeButtonImage;
    private Image _mainMenuButtonImage;
    private Sprite _pauseButtonSprite;
    private Sprite _pauseMenuSprite;
    private Sprite _buttonNormalSprite;
    private Sprite _buttonHoveredSprite;
    private bool _isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == TITLE_SCENE)
        {
            return;
        }

        PauseMenuController existing = FindFirstObjectByType<PauseMenuController>();
        if (existing != null)
        {
            existing.Rebuild();
            return;
        }

        GameObject host = new GameObject("PauseMenuController");
        host.AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        LoadSprites();
        Build();
        SceneManager.sceneLoaded += OnSceneLoadedCallback;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedCallback;
        if (_isPaused)
        {
            Time.timeScale = 1f;
        }
        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
        }
    }

    private void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == TITLE_SCENE)
        {
            if (_isPaused)
            {
                Time.timeScale = 1f;
                _isPaused = false;
            }
            Destroy(gameObject);
            return;
        }

        Rebuild();
    }

    private void Rebuild()
    {
        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
        }

        LoadSprites();
        Build();
    }

    private void LoadSprites()
    {
        if (_pauseButtonSprite == null) _pauseButtonSprite = Resources.Load<Sprite>(RESOURCES_FOLDER + "UI_PauseButton");
        if (_pauseMenuSprite == null) _pauseMenuSprite = Resources.Load<Sprite>(RESOURCES_FOLDER + "UI_PauseMenu");
        if (_buttonNormalSprite == null) _buttonNormalSprite = Resources.Load<Sprite>(RESOURCES_FOLDER + "UI_PauseMenuButton");
        if (_buttonHoveredSprite == null) _buttonHoveredSprite = Resources.Load<Sprite>(RESOURCES_FOLDER + "UI_PauseMenuButton_Hovered");
    }

    private void Build()
    {
        GameObject canvasObj = new GameObject("PauseMenuCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        DontDestroyOnLoad(canvasObj);

        CreatePauseButton(canvasObj.transform);
        CreateOverlay(canvasObj.transform);
    }

    private void CreatePauseButton(Transform parent)
    {
        GameObject btnObj = new GameObject("PauseButton");
        btnObj.transform.SetParent(parent, false);

        _pauseButtonRect = btnObj.AddComponent<RectTransform>();
        _pauseButtonRect.anchorMin = new Vector2(0f, 1f);
        _pauseButtonRect.anchorMax = new Vector2(0f, 1f);
        _pauseButtonRect.pivot = new Vector2(0f, 1f);
        _pauseButtonRect.anchoredPosition = new Vector2(20f, -20f);
        _pauseButtonRect.sizeDelta = new Vector2(80f, 80f);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = _pauseButtonSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    private void CreateOverlay(Transform parent)
    {
        GameObject overlayObj = new GameObject("PauseOverlay");
        overlayObj.transform.SetParent(parent, false);

        RectTransform rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        _overlayGroup = overlayObj.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.interactable = false;
        _overlayGroup.blocksRaycasts = false;

        Image bg = overlayObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = false;

        GameObject menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(overlayObj.transform, false);

        RectTransform panelRt = menuPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(500f, 600f);

        Image panelBg = menuPanel.AddComponent<Image>();
        panelBg.sprite = _pauseMenuSprite;
        panelBg.preserveAspect = true;
        panelBg.raycastTarget = false;

        _resumeButtonRect = CreateMenuButton(menuPanel.transform, "ResumeButton", new Vector2(0f, 40f));
        _resumeButtonImage = _resumeButtonRect.GetComponent<Image>();

        _mainMenuButtonRect = CreateMenuButton(menuPanel.transform, "MainMenuButton", new Vector2(0f, -80f));
        _mainMenuButtonImage = _mainMenuButtonRect.GetComponent<Image>();
    }

    private RectTransform CreateMenuButton(Transform parent, string name, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(340f, 80f);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = _buttonNormalSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = name == "ResumeButton" ? "Resume" : "Main Menu";
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.12f, 0.18f, 0.12f, 1f);
        text.raycastTarget = false;

        return rt;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) OnResumeClicked();
            else OnPauseClicked();
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 mousePos = Input.mousePosition;

        if (!_isPaused)
        {
            if (IsPointerOverRect(_pauseButtonRect, mousePos))
            {
                OnPauseClicked();
            }
            return;
        }

        if (IsPointerOverRect(_resumeButtonRect, mousePos))
        {
            OnResumeClicked();
        }
        else if (IsPointerOverRect(_mainMenuButtonRect, mousePos))
        {
            OnMainMenuClicked();
        }
        else if (IsPointerOverOverlayBackground(mousePos))
        {
            OnResumeClicked();
        }

        UpdateHoverStates(mousePos);
    }

    private void UpdateHoverStates(Vector2 mousePos)
    {
        if (!_isPaused) return;

        bool overResume = IsPointerOverRect(_resumeButtonRect, mousePos);
        bool overMainMenu = IsPointerOverRect(_mainMenuButtonRect, mousePos);

        if (_resumeButtonImage != null)
            _resumeButtonImage.sprite = overResume ? _buttonHoveredSprite : _buttonNormalSprite;
        if (_mainMenuButtonImage != null)
            _mainMenuButtonImage.sprite = overMainMenu ? _buttonHoveredSprite : _buttonNormalSprite;
    }

    private void OnPauseClicked()
    {
        if (_isPaused) return;
        _isPaused = true;
        Time.timeScale = 0f;
        ShowOverlay(true);
    }

    private void OnResumeClicked()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = 1f;
        ShowOverlay(false);
    }

    private void OnMainMenuClicked()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        ShowOverlay(false);
        SceneManager.LoadScene(TITLE_SCENE);
    }

    private void ShowOverlay(bool show)
    {
        if (_overlayGroup == null) return;
        _overlayGroup.alpha = show ? 1f : 0f;
        _overlayGroup.interactable = show;
        _overlayGroup.blocksRaycasts = show;
    }

    private bool IsPointerOverOverlayBackground(Vector2 mousePos)
    {
        if (_overlayGroup == null || _overlayGroup.alpha < 0.5f) return false;
        RectTransform overlayRt = _overlayGroup.GetComponent<RectTransform>();
        if (overlayRt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(overlayRt, mousePos, null);
    }

    private static bool IsPointerOverRect(RectTransform rect, Vector2 mousePos)
    {
        if (rect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null);
    }

}
