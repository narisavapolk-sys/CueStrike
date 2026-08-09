using UnityEngine;

namespace CueStrike.Characters.Skins
{
    /// <summary>
    /// Skin rarity tiers for CueStrike characters
    /// </summary>
    public enum SkinRarity
    {
        Common = 1,    // Green - Texture swap only
        Rare = 2,      // Blue - Texture + Accessory
        Epic = 3,      // Purple - New outfit mesh + VFX
        Legendary = 4  // Orange - Full remodel + Custom anim + Voice
    }

    /// <summary>
    /// Seasonal event types for time-limited skins
    /// </summary>
    public enum SeasonalEvent
    {
        None = 0,
        Songkran,           // Thai New Year (April)
        Halloween,          // October
        Christmas,          // December
        LunarNewYear,       // January/February
        Anniversary,        // Game launch anniversary
        Spring,             // March-May
        Summer,             // June-August
        Winter,             // December-February
        DevExclusive        // Developer only
    }

    /// <summary>
    /// ScriptableObject defining a character skin
    /// Supports both material-override (Common/Rare) and full-prefab-swap (Epic/Legendary) approaches
    /// </summary>
    [CreateAssetMenu(fileName = "Skin_", menuName = "CueStrike/Skins/Character Skin")]
    public class CharacterSkinData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier: characterId_skinName (e.g., somchay_songkran_2024)")]
        public string skinId;

        [Tooltip("Character this skin belongs to (somchay, meiling, etc.)")]
        public string characterId;

        [Tooltip("Rarity tier determines visual complexity and unlock cost")]
        public SkinRarity rarity = SkinRarity.Common;

        [Tooltip("Seasonal event type (if applicable)")]
        public SeasonalEvent eventType = SeasonalEvent.None;

        [Header("UI Display")]
        [Tooltip("Display name shown in Character Select")]
        public string displayName;

        [Tooltip("Description for skin detail panel")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("UI icon (256x256 recommended)")]
        public Sprite icon;

        [Header("Visual - Full Prefab Swap (Epic/Legendary)")]
        [Tooltip("Complete replacement prefab with custom meshes, materials, VFX")]
        public GameObject skinPrefab;

        [Header("Visual - Material Overlay (Common/Rare)")]
        [Tooltip("Material overrides for simple texture/color swaps")]
        public Material[] materialOverrides;

        [Tooltip("Accessory objects (hats, glasses, props) - instantiated as children")]
        public GameObject[] accessories;

        [Tooltip("VFX prefabs (trails, auras, particles) - instantiated as children")]
        public ParticleSystem[] vfxPrefabs;

        [Header("Audio")]
        [Tooltip("Skin-specific voice lines (Legendary only typically)")]
        public AudioClip[] voiceLines;

        [Header("Unlock Requirements")]
        [Tooltip("Player level required to unlock (0 = starting)")]
        public int unlockLevel = 0;

        [Tooltip("Coin cost to purchase (0 = free at level)")]
        public int unlockCost = 0;

        [Header("Seasonal Availability")]
        [Tooltip("Is this a time-limited seasonal skin?")]
        public bool isSeasonal = false;

        [Tooltip("Seasonal availability start date (UTC)")]
        public System.DateTime seasonalStart;

        [Tooltip("Seasonal availability end date (UTC)")]
        public System.DateTime seasonalEnd;

        [Header("Legendary Only")]
        [Tooltip("Custom animation clips for this skin")]
        public AnimationClip[] customAnimations;

        [Tooltip("Override animator controller for unique movement/emotes")]
        public RuntimeAnimatorController animatorOverride;

        /// <summary>
        /// Check if skin is currently available (for seasonal skins)
        /// </summary>
        public bool IsCurrentlyAvailable()
        {
            if (!isSeasonal) return true;
            
            var now = System.DateTime.UtcNow;
            return now >= seasonalStart && now <= seasonalEnd;
        }

        /// <summary>
        /// Get rarity color for UI
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                SkinRarity.Common => new Color(0.2f, 0.8f, 0.2f),      // Green
                SkinRarity.Rare => new Color(0.2f, 0.5f, 1.0f),        // Blue
                SkinRarity.Epic => new Color(0.7f, 0.2f, 0.9f),        // Purple
                SkinRarity.Legendary => new Color(1.0f, 0.5f, 0.0f),   // Orange
                _ => Color.gray
            };
        }

        /// <summary>
        /// Get expected unlock cost for rarity (for validation)
        /// </summary>
        public static int GetExpectedCost(SkinRarity rarity)
        {
            return rarity switch
            {
                SkinRarity.Common => 0,
                SkinRarity.Rare => 500,
                SkinRarity.Epic => 2000,
                SkinRarity.Legendary => 10000,
                _ => 0
            };
        }
    }
}