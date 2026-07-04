using UnityEditor;
using UnityEngine;
using Harukerryzi.Clash;

public class ClearSaveDataTool
{
    [MenuItem("Tools/Clear All Save Data")]
    public static void ClearAllSaveData()
    {
        // Clear all Unity PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Reset all static caches in case the editor is running or has cached data
        CardInventory.ClearAll();
        CurrencyWallet.Reset();
        StageProgress.Reset();

        Debug.Log("<b>[Save Data]</b> All game data (Cards, Coins, Stage Progress) has been completely reset. You can now start fresh!");
    }
}
