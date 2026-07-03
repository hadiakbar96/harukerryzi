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
    [SerializeField] private RectTransform collectionButtonRect;
    [SerializeField] private string openingStorySceneName = "OpeningStory";
    [SerializeField] private string stageMapSceneName = "StageMap";
    [SerializeField] private string collectionSceneName = "Collection";

    private const string SEED_KEY = "Harukerryzi.Seeded";
    private const string STARTER_CARD = "NormalCard";

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
        controller.WireCollectionButton();
    }

    private void Awake()
    {
        SeedFirstTime();
        WirePlayButton();
        WireCollectionButton();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetProgress();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverPlayButton())
            {
                OnPlayClicked();
            }
            else if (IsPointerOverCollectionButton())
            {
                OnCollectionClicked();
            }
        }
    }

    private void SeedFirstTime()
    {
        if (PlayerPrefs.HasKey(SEED_KEY)) return;

        CardInventory.AddCard(STARTER_CARD);
        CurrencyWallet.AddCoins(0);
        PlayerPrefs.SetInt(SEED_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[TitleScreen] First-time seed: 1× " + STARTER_CARD);
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

    private void WireCollectionButton()
    {
        if (collectionButtonRect == null)
        {
            GameObject found = GameObject.Find("Button_Collection");
            if (found != null)
            {
                collectionButtonRect = found.GetComponent<RectTransform>();
            }
        }

        if (collectionButtonRect == null) return;

        BoxCollider2D collider = collectionButtonRect.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = collectionButtonRect.gameObject.AddComponent<BoxCollider2D>();
        }

        Vector2 size = collectionButtonRect.sizeDelta;
        collider.size = size;
        collider.offset = Vector2.zero;
    }

    private bool IsPointerOverButton(RectTransform buttonRect)
    {
        if (buttonRect == null) return false;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(buttonRect, Input.mousePosition, null, out localPoint))
            return false;

        return buttonRect.rect.Contains(localPoint);
    }

    private bool IsPointerOverPlayButton() => IsPointerOverButton(playButtonRect);
    private bool IsPointerOverCollectionButton() => IsPointerOverButton(collectionButtonRect);

    private void OnPlayClicked()
    {
        PlayerPrefs.SetInt(OpeningStoryController.IntroSeenKey, 1);
        LoadScene(stageMapSceneName);
    }

    private void OnCollectionClicked()
    {
        LoadScene(collectionSceneName);
    }

    private void ResetProgress()
    {
        PlayerPrefs.DeleteKey(OpeningStoryController.IntroSeenKey);
        PlayerPrefs.DeleteKey(SEED_KEY);
        StageProgress.Reset();
        CardInventory.ClearAll();
        CurrencyWallet.Reset();
        Debug.Log("[TitleScreen] All progress reset.");
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
