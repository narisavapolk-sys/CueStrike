using UnityEngine;

namespace CueStrike.Characters.Skins
{
    /// <summary>
    /// Handles skin unlock progression - level-based, coin-based, and seasonal
    /// </summary>
    public class SkinUnlockManager : MonoBehaviour
    {
        [Header("Unlock Thresholds")]
        [Tooltip("Player levels at which rarity tiers unlock")]
        public int[] levelThresholds = { 5, 15, 30, 50 }; // Common, Rare, Epic, Legendary

        [Tooltip("Coin costs for each rarity tier (index matches SkinRarity enum - 1)")]
        public int[] coinCosts = { 100, 500, 2000, 10000 };

        [Header("References")]
        [SerializeField] private SkinManager skinManager;

        // Events
        public event System.Action<CharacterSkinData> OnSkinAutoUnlocked;
        public event System.Action<CharacterSkinData> OnSkinPurchased;
        public event System.Action<SeasonalEvent> OnSeasonalSkinsGranted;

        private void Awake()
        {
            if (skinManager == null)
                skinManager = SkinManager.Instance;
        }

        private void Start()
        {
            // Subscribe to level up events (if you have a LevelManager)
            // LevelManager.OnLevelUp += CheckLevelUpUnlocks;
        }

        private void OnDestroy()
        {
            // LevelManager.OnLevelUp -= CheckLevelUpUnlocks;
        }

        /// <summary>
        /// Call when player levels up to check for new skin unlocks
        /// </summary>
        public void CheckLevelUpUnlocks(int newLevel)
        {
            if (skinManager == null) return;

            var allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");
            int unlockedCount = 0;

            foreach (var skin in allSkins)
            {
                // Skip if already unlocked
                if (skinManager.IsUnlocked(skin)) continue;

                // Skip seasonal skins (handled separately)
                if (skin.isSeasonal) continue;

                // Check level requirement
                if (skin.unlockLevel <= newLevel && skin.unlockLevel > 0)
                {
                    skinManager.UnlockSkin(skin);
                    OnSkinAutoUnlocked?.Invoke(skin);
                    unlockedCount++;
                    Debug.Log($"[SkinUnlockManager] Auto-unlocked {skin.skinId} at level {newLevel}");
                }
            }

            if (unlockedCount > 0)
            {
                ShowUnlockNotification(unlockedCount);
            }
        }

        /// <summary>
        /// Attempt to purchase a skin with coins
        /// </summary>
        public bool TryPurchaseSkin(CharacterSkinData skin)
        {
            if (skin == null || skinManager == null) return false;

            if (skinManager.IsUnlocked(skin))
            {
                Debug.Log($"[SkinUnlockManager] Skin already unlocked: {skin.skinId}");
                return true;
            }

            int coins = PlayerPrefs.GetInt("player_coins", 0);
            int cost = skin.unlockCost > 0 ? skin.unlockCost : GetCostForRarity(skin.rarity);

            if (coins >= cost)
            {
                PlayerPrefs.SetInt("player_coins", coins - cost);
                PlayerPrefs.Save();

                skinManager.UnlockSkin(skin);
                OnSkinPurchased?.Invoke(skin);

                Debug.Log($"[SkinUnlockManager] Purchased {skin.skinId} for {cost} coins. Remaining: {coins - cost}");
                return true;
            }

            Debug.Log($"[SkinUnlockManager] Insufficient coins for {skin.skinId}. Need {cost}, have {coins}");
            return false;
        }

        /// <summary>
        /// Grant all seasonal skins for a specific event
        /// </summary>
        public void GrantSeasonalSkins(SeasonalEvent eventType)
        {
            if (skinManager == null) return;

            var allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");
            int grantedCount = 0;

            foreach (var skin in allSkins)
            {
                if (skin.eventType == eventType && skin.isSeasonal)
                {
                    if (!skinManager.IsUnlocked(skin) && skin.IsCurrentlyAvailable())
                    {
                        skinManager.UnlockSkin(skin);
                        grantedCount++;
                    }
                }
            }

            if (grantedCount > 0)
            {
                OnSeasonalSkinsGranted?.Invoke(eventType);
                Debug.Log($"[SkinUnlockManager] Granted {grantedCount} seasonal skins for {eventType}");
            }
        }

        /// <summary>
        /// Revoke seasonal skins when event ends (optional - or keep if purchased)
        /// </summary>
        public void RevokeSeasonalSkins(SeasonalEvent eventType, bool keepIfPurchased = true)
        {
            if (skinManager == null) return;

            var allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");

            foreach (var skin in allSkins)
            {
                if (skin.eventType == eventType && skin.isSeasonal)
                {
                    // Check if player purchased it (has spent coins)
                    bool wasPurchased = PlayerPrefs.GetInt($"skin_purchased_{skin.skinId}", 0) == 1;

                    if (!keepIfPurchased || !wasPurchased)
                    {
                        PlayerPrefs.SetInt($"skin_unlocked_{skin.skinId}", 0);
                        PlayerPrefs.Save();
                        Debug.Log($"[SkinUnlockManager] Revoked seasonal skin: {skin.skinId}");
                    }
                }
            }
        }

        /// <summary>
        /// Add coins to player balance (for testing or rewards)
        /// </summary>
        public void AddCoins(int amount)
        {
            int current = PlayerPrefs.GetInt("player_coins", 0);
            PlayerPrefs.SetInt("player_coins", current + amount);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Get current coin balance
        /// </summary>
        public int GetCoinBalance()
        {
            return PlayerPrefs.GetInt("player_coins", 0);
        }

        /// <summary>
        /// Spend coins (returns true if successful)
        /// </summary>
        public bool SpendCoins(int amount)
        {
            int current = GetCoinBalance();
            if (current >= amount)
            {
                PlayerPrefs.SetInt("player_coins", current - amount);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mark skin as purchased (for seasonal keep logic)
        /// </summary>
        public void MarkAsPurchased(CharacterSkinData skin)
        {
            PlayerPrefs.SetInt($"skin_purchased_{skin.skinId}", 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Check if skin was purchased (vs level unlocked)
        /// </summary>
        public bool WasPurchased(CharacterSkinData skin)
        {
            return PlayerPrefs.GetInt($"skin_purchased_{skin.skinId}", 0) == 1;
        }

        private int GetCostForRarity(SkinRarity rarity)
        {
            int index = (int)rarity - 1; // SkinRarity.Common = 1
            if (index >= 0 && index < coinCosts.Length)
                return coinCosts[index];
            return 0;
        }

        private void ShowUnlockNotification(int count)
        {
            // Hook up to your UI notification system
            Debug.Log($"[SkinUnlockManager] 🎉 {count} new skin(s) unlocked!");
            // NotificationManager.Show($"Unlocked {count} new skin(s)!");
        }

        /// <summary>
        /// Debug: Unlock all skins for testing
        /// </summary>
        [ContextMenu("Debug: Unlock All Skins")]
        public void DebugUnlockAllSkins()
        {
            if (skinManager == null) return;

            var allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");
            foreach (var skin in allSkins)
            {
                skinManager.UnlockSkin(skin);
            }
            Debug.Log($"[SkinUnlockManager] Debug unlocked {allSkins.Length} skins");
        }

        /// <summary>
        /// Debug: Grant all seasonal skins
        /// </summary>
        [ContextMenu("Debug: Grant All Seasonal")]
        public void DebugGrantAllSeasonal()
        {
            foreach (SeasonalEvent evt in System.Enum.GetValues(typeof(SeasonalEvent)))
            {
                if (evt != SeasonalEvent.None)
                    GrantSeasonalSkins(evt);
            }
        }

        /// <summary>
        /// Debug: Reset all unlocks
        /// </summary>
        [ContextMenu("Debug: Reset All Unlocks")]
        public void DebugResetAllUnlocks()
        {
            var allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");
            foreach (var skin in allSkins)
            {
                if (skin.rarity != SkinRarity.Common || skin.unlockLevel > 0)
                {
                    PlayerPrefs.SetInt($"skin_unlocked_{skin.skinId}", 0);
                    PlayerPrefs.SetInt($"skin_purchased_{skin.skinId}", 0);
                }
            }
            PlayerPrefs.Save();

            // Reset equipped to defaults
            foreach (var kvp in skinManager.GetType().GetField("_equippedSkins", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(skinManager) as System.Collections.IDictionary)
            {
                // Would need reflection to clear, simpler to just reinitialize
            }
            skinManager.Refresh();
            Debug.Log("[SkinUnlockManager] Debug reset all unlocks");
        }
    }
}