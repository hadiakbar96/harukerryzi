using UnityEngine;

namespace Harukerryzi.Clash
{
    public static class CurrencyWallet
    {
        private const string PREFS_KEY = "Harukerryzi.Coins";

        [System.Serializable]
        private class WalletData
        {
            public int coins;
        }

        public static int GetCoins()
        {
            if (!PlayerPrefs.HasKey(PREFS_KEY))
                return 0;

            string json = PlayerPrefs.GetString(PREFS_KEY);
            WalletData data = JsonUtility.FromJson<WalletData>(json);
            return data != null ? data.coins : 0;
        }

        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;

            int current = GetCoins();
            WalletData data = new WalletData { coins = current + amount };
            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;

            int current = GetCoins();
            if (current < amount) return false;

            WalletData data = new WalletData { coins = current - amount };
            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            return true;
        }

        public static void Reset()
        {
            WalletData data = new WalletData { coins = 0 };
            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
