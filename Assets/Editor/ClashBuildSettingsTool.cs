using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ClashBuildSettingsTool
{
    private static readonly string[] RequiredScenes =
    {
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
        Debug.Log("StageMap and ClashScene are enabled in Build Settings.");
    }
}
