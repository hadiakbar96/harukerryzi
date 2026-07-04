using UnityEngine;

/// <summary>
/// Allows any scene to set a "return to" destination that persists
/// across multiple scene transitions (e.g. StageMap → Shop → openPack → Collection).
///
/// Usage:
///   SceneHistory.SetReturnScene("StageMap");   // call before navigating away
///   string back = SceneHistory.ReturnScene;     // read in destination scene
/// </summary>
public static class SceneHistory
{
    /// <summary>
    /// The scene to return to. Set this before starting a multi-scene flow.
    /// </summary>
    public static string ReturnScene { get; private set; }

    /// <summary>
    /// Set the scene name that the player should eventually return to.
    /// </summary>
    public static void SetReturnScene(string sceneName)
    {
        ReturnScene = sceneName;
    }

    /// <summary>
    /// Clears the return scene after it has been consumed.
    /// </summary>
    public static void Clear()
    {
        ReturnScene = null;
    }
}
