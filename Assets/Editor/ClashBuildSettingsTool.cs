using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ClashBuildSettingsTool
{
    private static readonly string[] RequiredScenes =
    {
        "Assets/Projects/Scenes/SandBox/TitleScreen.unity",
        "Assets/Projects/Scenes/SandBox/OpeningStory.unity",
        "Assets/Projects/Scenes/SandBox/StageMap.unity",
        "Assets/Projects/Scenes/SandBox/ClashScene.unity"
    };

    [MenuItem("Tools/Clash/Add Scenes To Build (Non-Destructive)")]
    public static void AddScenesToBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before updating Build Settings.");
            return;
        }

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        foreach (string path in RequiredScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning($"Scene not found: {path}");
                continue;
            }

            EditorBuildSettingsScene existing = scenes.FirstOrDefault(scene => scene.path == path);
            if (existing != null)
            {
                existing.enabled = true;
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("TitleScreen, OpeningStory, StageMap, and ClashScene are enabled in Build Settings.");
    }

    [MenuItem("Tools/Clash/Reset Intro Flag")]
    public static void ResetIntroFlag()
    {
        PlayerPrefs.DeleteKey(OpeningStoryController.IntroSeenKey);
        PlayerPrefs.Save();
        Debug.Log("Reset intro flag. Title Play will open OpeningStory next time.");
    }
}
