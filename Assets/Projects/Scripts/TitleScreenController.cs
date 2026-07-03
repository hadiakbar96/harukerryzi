using Harukerryzi.Clash;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public sealed class TitleScreenController : MonoBehaviour
{
    [SerializeField] private RectTransform playButtonRect;
    [SerializeField] private string openingStorySceneName = "OpeningStory";
    [SerializeField] private string stageMapSceneName = "StageMap";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoWireTitleScene()
    {
        if (SceneManager.GetActiveScene().name != "TitleScreen")
        {
            return;
        }

        TitleScreenController controller = Object.FindFirstObjectByType<TitleScreenController>();
        if (controller == null)
        {
            GameObject host = GameObject.Find("Canvas") ?? new GameObject("TitleScreenController");
            controller = host.AddComponent<TitleScreenController>();
        }

        controller.WirePlayButton();
    }

    private void Awake()
    {
        WirePlayButton();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetProgress();
        }

        if (Input.GetMouseButtonDown(0) && IsPointerOverPlayButton())
        {
            OnPlayClicked();
        }
    }

    private void WirePlayButton()
    {
        if (playButtonRect == null)
        {
            GameObject found = GameObject.Find("Button_Play");
            if (found != null)
            {
                playButtonRect = found.GetComponent<RectTransform>();
            }
        }

        if (playButtonRect == null)
        {
            Debug.LogWarning("[TitleScreen] Button_Play not found.");
            return;
        }

        BoxCollider2D collider = playButtonRect.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = playButtonRect.gameObject.AddComponent<BoxCollider2D>();
        }

        Vector2 size = playButtonRect.sizeDelta;
        collider.size = size;
        collider.offset = Vector2.zero;
    }

    private bool IsPointerOverPlayButton()
    {
        if (playButtonRect == null)
        {
            return false;
        }

        Vector2 localPoint;
        RectTransform canvasRect = playButtonRect.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out localPoint))
        {
            return false;
        }

        return playButtonRect.rect.Contains(localPoint);
    }

    private void OnPlayClicked()
    {
        string targetScene = PlayerPrefs.GetInt(OpeningStoryController.IntroSeenKey, 0) == 1
            ? stageMapSceneName
            : openingStorySceneName;

        LoadScene(targetScene);
    }

    private void ResetProgress()
    {
        PlayerPrefs.DeleteKey(OpeningStoryController.IntroSeenKey);
        StageProgress.Reset();
        Debug.Log("[TitleScreen] Story progress reset. Intro will replay on next Play.");
    }

    private static void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Projects/Scenes/SandBox/" + sceneName + ".unity",
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogWarning("[TitleScreen] Scene is not in Build Settings: " + sceneName);
#endif
    }
}
