using Harukerryzi.Clash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClashEnemySetupTool
{
    private const string ConfigDirectory = "Assets/Projects/Settings/Clash";
    private const string SpriteDirectory = "Assets/Projects/Sprites/Battle";

    [MenuItem("Tools/Clash/Setup Battle Enemy Assets (Non-Destructive)")]
    public static void SetupBattleEnemies()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before setting up enemy assets.");
            return;
        }

        EnsureFolder("Assets/Projects/Settings");
        EnsureFolder(ConfigDirectory);

        AduTosEnemyConfig tikus = EnsureEnemy(
            "Enemy_1_Tikus.asset",
            "Tikus",
            0,
            7f,
            3f,
            0.3f,
            20,
            20,
            LoadSprite("Sprite_Battle_Tikus.png"),
            LoadSprite("Sprite_Battle_MCvsTikus.png"),
            LoadSprite("StageMap/battle_tikus.png"),
            1f,
            new Vector2(980f, -520f),
            new Vector2(390f, -90f)
        );

        EnsureEnemy(
            "Enemy_2_Kunti.asset",
            "Kunti",
            1,
            10f,
            4f,
            0.2f,
            50,
            20,
            LoadSprite("Sprite_Battle_Kunti.png"),
            LoadSprite("Sprite_Battle_MCvsKunti.png"),
            LoadSprite("StageMap/battle_kunti.png"),
            1f,
            new Vector2(980f, -520f),
            new Vector2(390f, -90f)
        );

        EnsureEnemy(
            "Enemy_3_Tiang.asset",
            "Tiang",
            2,
            14f,
            5.5f,
            0.1f,
            75,
            30,
            LoadSprite("Sprite_Battle_Tiang.png"),
            LoadSprite("Sprite_Battle_MCvsTiang.png"),
            LoadSprite("StageMap/battle_tiang.png"),
            1.6f,
            new Vector2(980f, 520f),
            new Vector2(390f, -90f)
        );

        AduTosMinigameController minigame = Object.FindFirstObjectByType<AduTosMinigameController>();
        if (minigame != null)
        {
            SerializedObject serialized = new(minigame);
            AssignIfPresent(serialized, "enemyConfig", tikus);
            AssignIfPresent(serialized, "mcHandSprite", LoadSprite("Sprite_Battle_MC.png"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        else
        {
            Debug.LogWarning("AduTosMinigameController not found in scene. Enemy assets were created but not assigned.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Battle enemy assets set up. Default scene enemy assigned to Tikus when AduTosMinigame exists.");
    }

    private static AduTosEnemyConfig EnsureEnemy(string fileName, string displayName, int level, float power, float mashesPerSecond, float randomness, int rewardOnWin, int rewardOnLose, Sprite enemyHand, Sprite clashHands, Sprite clashBackground, float clashHandsScale, Vector2 entranceHandStartPosition, Vector2 entranceHandTargetPosition)
    {
        string path = $"{ConfigDirectory}/{fileName}";
        AduTosEnemyConfig enemy = AssetDatabase.LoadAssetAtPath<AduTosEnemyConfig>(path);
        if (enemy == null)
        {
            enemy = ScriptableObject.CreateInstance<AduTosEnemyConfig>();
            AssetDatabase.CreateAsset(enemy, path);
        }

        SerializedObject serialized = new(enemy);
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("level").intValue = level;
        serialized.FindProperty("baseMashPower").floatValue = power;
        serialized.FindProperty("mashesPerSecond").floatValue = mashesPerSecond;
        serialized.FindProperty("mashRandomness").floatValue = randomness;
        serialized.FindProperty("rewardOnWin").intValue = rewardOnWin;
        serialized.FindProperty("rewardOnLose").intValue = rewardOnLose;
        serialized.FindProperty("enemyHandSprite").objectReferenceValue = enemyHand;
        serialized.FindProperty("entranceHandStartPosition").vector2Value = entranceHandStartPosition;
        serialized.FindProperty("entranceHandTargetPosition").vector2Value = entranceHandTargetPosition;
        serialized.FindProperty("clashHandsSprite").objectReferenceValue = clashHands;
        serialized.FindProperty("clashHandsScale").floatValue = clashHandsScale;
        serialized.FindProperty("clashBackgroundSprite").objectReferenceValue = clashBackground;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(enemy);
        return enemy;
    }

    private static Sprite LoadSprite(string fileName)
    {
        string path = fileName.Contains("/")
            ? $"Assets/Projects/Sprites/{fileName}"
            : $"{SpriteDirectory}/{fileName}";
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

    private static void AssignIfPresent(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
