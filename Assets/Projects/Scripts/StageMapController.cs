using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Harukerryzi.Clash;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Mengatur Stage Map interaktif:
/// - Stage aktif berwarna kuning, di tengah layar, lebih besar
/// - Klik stage aktif → selesai → geser ke stage berikutnya
/// - Stage selesai jadi biru, stage terkunci jadi abu gelap
///
/// Script ini otomatis menemukan node berdasarkan nama:
///   Node_0, Node_1, Node_2 ... (children dari nodesContainer)
///   StageLabel_0, StageLabel_1 ... (label "Stage" di atas node)
///
/// Setiap Node harus memiliki children:
///   GlowOuter, GlowMid, GlowInner, Outline, BG, Label
/// </summary>
public class StageMapController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────────────────────

    [Header("=== References ===")]
    [Tooltip("Parent container dari semua nodes (akan digeser untuk centering)")]
    [SerializeField] private RectTransform nodesContainer;

    [Tooltip("Enemy config per stage index: 0=Tikus, 1=Kunti, 2=Tiang")]
    [SerializeField] private AduTosEnemyConfig[] stageEnemies;

    [SerializeField] private string battleSceneName = "ClashScene";

    [Header("=== Layout ===")]
    [Tooltip("Jarak horizontal antar node")]
    [SerializeField] private float spacing = 420f;

    [Tooltip("Ukuran node aktif")]
    [SerializeField] private float activeNodeSize = 200f;

    [Tooltip("Ukuran node tidak aktif")]
    [SerializeField] private float inactiveNodeSize = 140f;

    [Header("=== Animation ===")]
    [Tooltip("Durasi transisi geser + perubahan warna")]
    [SerializeField] private float transitionDuration = 0.6f;

    // ──────────────────────────────────────────────────────────────
    //  Colors
    // ──────────────────────────────────────────────────────────────

    // Active (kuning/emas)
    private readonly Color _activeBg        = HexToColor("D4A017");
    private readonly Color _activeOutline   = HexToColor("FFD700");
    private readonly Color _activeText      = Color.white;
    private readonly Color _activeStageLabel = HexToColor("FFD700");

    // Completed (biru)
    private readonly Color _completedBg        = HexToColor("1A2744");
    private readonly Color _completedOutline   = HexToColor("4A5568");
    private readonly Color _completedText      = HexToColor("AABBCC");
    private readonly Color _completedStageLabel = HexToColor("888899");

    // Locked (gelap)
    private readonly Color _lockedBg        = HexToColor("1A1A2E");
    private readonly Color _lockedOutline   = HexToColor("3A3A4E");
    private readonly Color _lockedText      = HexToColor("555566");
    private readonly Color _lockedStageLabel = HexToColor("555566");

    // ──────────────────────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────────────────────

    private int _activeIndex = 0;
    private int _totalStages = 0;
    private NodeInfo[] _nodes;
    private bool _isTransitioning = false;

    private class NodeInfo
    {
        public RectTransform rect;
        public Image outline;
        public Image bg;
        public GameObject glowOuter;
        public GameObject glowMid;
        public GameObject glowInner;
        public TMP_Text label;
        public TMP_Text stageLabel;
        public GameObject lockIcon;
    }

    // ──────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────

    private void Start()
    {
        DiscoverNodes();
        ApplyReturnedBattleResult();
        _activeIndex = Mathf.Clamp(StageProgress.HighestUnlockedStage, 0, Mathf.Max(0, _totalStages - 1));
        ApplyStateImmediate();
        CenterOnActiveImmediate();
    }

    private void ApplyReturnedBattleResult()
    {
        if (!BattleSession.HasResult)
        {
            return;
        }

        if (BattleSession.PlayerWon && !BattleSession.IsReplayStage)
        {
            StageProgress.MarkStageCleared(BattleSession.SelectedStageIndex, Mathf.Max(0, _totalStages - 1));
        }

        BattleSession.ClearResult();
    }

    // ──────────────────────────────────────────────────────────────
    //  Discovery — menemukan node berdasarkan nama
    // ──────────────────────────────────────────────────────────────

    private void DiscoverNodes()
    {
        // Hitung total stages
        _totalStages = 0;
        while (nodesContainer.Find("Node_" + _totalStages) != null)
            _totalStages++;

        _nodes = new NodeInfo[_totalStages];

        for (int i = 0; i < _totalStages; i++)
        {
            Transform nodeT = nodesContainer.Find("Node_" + i);
            var info = new NodeInfo();

            info.rect       = nodeT as RectTransform;
            info.outline    = FindChildImage(nodeT, "Outline");
            info.bg         = FindChildImage(nodeT, "BG");
            info.glowOuter  = FindChildGO(nodeT, "GlowOuter");
            info.glowMid    = FindChildGO(nodeT, "GlowMid");
            info.glowInner  = FindChildGO(nodeT, "GlowInner");
            info.label      = FindChildTMP(nodeT, "Label");
            info.lockIcon   = FindChildGO(nodeT, "LockIcon");

            Transform stageLabelT = nodesContainer.Find("StageLabel_" + i);
            if (stageLabelT != null)
                info.stageLabel = stageLabelT.GetComponent<TMP_Text>();

            _nodes[i] = info;

            // Tambah Button untuk deteksi klik
            Button btn = nodeT.GetComponent<Button>();
            if (btn == null)
                btn = nodeT.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            int capturedIndex = i; // capture untuk closure
            btn.onClick.AddListener(() => OnNodeClicked(capturedIndex));
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Click Handler
    // ──────────────────────────────────────────────────────────────

    private void OnNodeClicked(int index)
    {
        // Current and cleared previous stages can be replayed. Future stages stay locked.
        if (index > _activeIndex) return;
        if (_isTransitioning) return;

        AduTosEnemyConfig enemy = GetEnemyForStage(index);
        if (enemy == null)
        {
            Debug.LogWarning("[StageMap] Missing enemy config for stage " + index);
            return;
        }

        BattleSession.SelectStage(index, enemy, index < _activeIndex);
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

    // ──────────────────────────────────────────────────────────────
    //  Transition Animation
    // ──────────────────────────────────────────────────────────────

    private IEnumerator TransitionToActive()
    {
        _isTransitioning = true;

        // Target posisi container agar active node di tengah
        float targetContainerX = -_activeIndex * spacing;
        float startContainerX  = nodesContainer.anchoredPosition.x;

        // Snapshot ukuran node sebelumnya
        float[] startSizes = new float[_totalStages];
        for (int i = 0; i < _totalStages; i++)
            startSizes[i] = _nodes[i].rect.sizeDelta.x;

        // Target ukuran
        float[] targetSizes = new float[_totalStages];
        for (int i = 0; i < _totalStages; i++)
            targetSizes[i] = (i == _activeIndex) ? activeNodeSize : inactiveNodeSize;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

            // Geser container
            float x = Mathf.Lerp(startContainerX, targetContainerX, t);
            nodesContainer.anchoredPosition = new Vector2(x, 0f);

            // Animate ukuran nodes
            for (int i = 0; i < _totalStages; i++)
            {
                float s = Mathf.Lerp(startSizes[i], targetSizes[i], t);
                _nodes[i].rect.sizeDelta = new Vector2(s, s);
            }

            yield return null;
        }

        // Finalize
        nodesContainer.anchoredPosition = new Vector2(targetContainerX, 0f);
        ApplyStateImmediate();

        _isTransitioning = false;
    }

    // ──────────────────────────────────────────────────────────────
    //  Apply State (warna, glow, ukuran)
    // ──────────────────────────────────────────────────────────────

    private void ApplyStateImmediate()
    {
        for (int i = 0; i < _totalStages; i++)
        {
            var node = _nodes[i];

            if (i == _activeIndex)
            {
                node.rect.sizeDelta = new Vector2(activeNodeSize, activeNodeSize);
                SetNodeColors(node, _activeBg, _activeOutline, _activeText);
                SetGlowActive(node, true);
                if (node.stageLabel != null) node.stageLabel.color = _activeStageLabel;
                if (node.label != null) node.label.gameObject.SetActive(true);
                if (node.lockIcon != null) node.lockIcon.SetActive(false);
            }
            else if (i < _activeIndex)
            {
                node.rect.sizeDelta = new Vector2(inactiveNodeSize, inactiveNodeSize);
                SetNodeColors(node, _completedBg, _completedOutline, _completedText);
                SetGlowActive(node, true);
                if (node.stageLabel != null) node.stageLabel.color = _completedStageLabel;
                if (node.label != null) node.label.gameObject.SetActive(true);
                if (node.lockIcon != null) node.lockIcon.SetActive(false);
            }
            else
            {
                // LOCKED — gelap, kecil, glow OFF, sembunyikan angka, tampil lock
                node.rect.sizeDelta = new Vector2(inactiveNodeSize, inactiveNodeSize);
                SetNodeColors(node, _lockedBg, _lockedOutline, _lockedText);
                SetGlowActive(node, false);
                if (node.stageLabel != null) node.stageLabel.color = _lockedStageLabel;
                if (node.label != null) node.label.gameObject.SetActive(false);
                if (node.lockIcon != null) node.lockIcon.SetActive(true);
            }
        }
    }

    private void CenterOnActiveImmediate()
    {
        float x = -_activeIndex * spacing;
        nodesContainer.anchoredPosition = new Vector2(x, 0f);
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────

    private void SetNodeColors(NodeInfo node, Color bg, Color outline, Color text)
    {
        if (node.bg      != null) node.bg.color      = bg;
        if (node.outline != null) node.outline.color = outline;
        if (node.label   != null) node.label.color   = text;
    }

    private void SetGlowActive(NodeInfo node, bool active)
    {
        if (node.glowOuter != null) node.glowOuter.SetActive(active);
        if (node.glowMid   != null) node.glowMid.SetActive(active);
        if (node.glowInner != null) node.glowInner.SetActive(active);
    }

    private Image FindChildImage(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private GameObject FindChildGO(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.gameObject : null;
    }

    private TMP_Text FindChildTMP(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static Color HexToColor(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);
        return color;
    }
}
