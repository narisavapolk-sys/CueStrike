using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Characters.Skins
{
    /// <summary>
    /// Runtime manager for character skins - handles equip, unlock, and application
    /// Singleton pattern with event-driven architecture
    /// </summary>
    public class SkinManager : MonoBehaviour
    {
        public static SkinManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private CharacterSkinData[] allSkins;
        [SerializeField] private bool autoLoadOnAwake = true;

        // Lookup dictionaries
        private readonly Dictionary<string, List<CharacterSkinData>> _skinsByCharacter = new();
        private readonly Dictionary<string, CharacterSkinData> _equippedSkins = new(); // characterId -> skin
        private readonly Dictionary<string, GameObject> _activeCharacterInstances = new(); // characterId -> instance

        // Events
        public event System.Action<CharacterSkinData> OnSkinUnlocked;
        public event System.Action<string, CharacterSkinData> OnSkinEquipped; // characterId, skin
        public event System.Action<string, CharacterSkinData> OnSkinApplied;  // characterId, skin (after visual applied)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoLoadOnAwake)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize the skin system - call manually if autoLoadOnAwake is false
        /// </summary>
        public void Initialize()
        {
            BuildLookup();
            LoadEquippedSkins();
            Debug.Log($"[SkinManager] Initialized with {allSkins?.Length ?? 0} skins across {_skinsByCharacter.Count} characters");
        }

        private void BuildLookup()
        {
            _skinsByCharacter.Clear();
            
            if (allSkins == null) return;

            foreach (var skin in allSkins)
            {
                if (string.IsNullOrEmpty(skin.characterId) || string.IsNullOrEmpty(skin.skinId))
                {
                    Debug.LogWarning($"[SkinManager] Skin missing characterId or skinId: {skin.name}");
                    continue;
                }

                if (!_skinsByCharacter.ContainsKey(skin.characterId))
                    _skinsByCharacter[skin.characterId] = new List<CharacterSkinData>();

                _skinsByCharacter[skin.characterId].Add(skin);
            }

            // Sort each character's skins by rarity (Common first, Legendary last)
            foreach (var list in _skinsByCharacter.Values)
            {
                list.Sort((a, b) => a.rarity.CompareTo(b.rarity));
            }
        }

        private void LoadEquippedSkins()
        {
            _equippedSkins.Clear();
            
            foreach (var kvp in _skinsByCharacter)
            {
                string characterId = kvp.Key;
                string savedSkinId = PlayerPrefs.GetString($"equipped_skin_{characterId}", "");
                
                if (!string.IsNullOrEmpty(savedSkinId))
                {
                    var skin = kvp.Value.Find(s => s.skinId == savedSkinId);
                    if (skin != null && IsUnlocked(skin))
                    {
                        _equippedSkins[characterId] = skin;
                    }
                }

                // Fallback to default skin
                if (!_equippedSkins.ContainsKey(characterId))
                {
                    var defaultSkin = kvp.Value.Find(s => s.rarity == SkinRarity.Common && s.unlockLevel == 0);
                    if (defaultSkin != null)
                    {
                        _equippedSkins[characterId] = defaultSkin;
                    }
                }
            }
        }

        private void SaveEquippedSkin(string characterId, CharacterSkinData skin)
        {
            PlayerPrefs.SetString($"equipped_skin_{characterId}", skin.skinId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Get all skins for a specific character
        /// </summary>
        public List<CharacterSkinData> GetSkinsForCharacter(string characterId)
        {
            return _skinsByCharacter.TryGetValue(characterId, out var list) ? list : new List<CharacterSkinData>();
        }

        /// <summary>
        /// Get the currently equipped skin for a character
        /// </summary>
        public CharacterSkinData GetEquippedSkin(string characterId)
        {
            return _equippedSkins.TryGetValue(characterId, out var skin) ? skin : null;
        }

        /// <summary>
        /// Get the default (base) skin for a character
        /// </summary>
        public CharacterSkinData GetDefaultSkin(string characterId)
        {
            var skins = GetSkinsForCharacter(characterId);
            return skins.Find(s => s.rarity == SkinRarity.Common && s.unlockLevel == 0);
        }

        /// <summary>
        /// Check if a skin is unlocked
        /// </summary>
        public bool IsUnlocked(CharacterSkinData skin)
        {
            if (skin == null) return false;
            
            // Base skins (Common, level 0) are always unlocked
            if (skin.rarity == SkinRarity.Common && skin.unlockLevel == 0)
                return true;

            // Check PlayerPrefs for unlock status
            return PlayerPrefs.GetInt($"skin_unlocked_{skin.skinId}", 0) == 1;
        }

        /// <summary>
        /// Unlock a skin permanently
        /// </summary>
        public void UnlockSkin(CharacterSkinData skin)
        {
            if (skin == null || IsUnlocked(skin)) return;

            PlayerPrefs.SetInt($"skin_unlocked_{skin.skinId}", 1);
            PlayerPrefs.Save();

            OnSkinUnlocked?.Invoke(skin);
            Debug.Log($"[SkinManager] Unlocked skin: {skin.skinId} for {skin.characterId}");
        }

        /// <summary>
        /// Equip a skin for a character (unlocks if not already unlocked)
        /// </summary>
        public bool EquipSkin(string characterId, string skinId)
        {
            var skins = GetSkinsForCharacter(characterId);
            var skin = skins.Find(s => s.skinId == skinId);

            if (skin == null)
            {
                Debug.LogWarning($"[SkinManager] Skin not found: {skinId} for character {characterId}");
                return false;
            }

            if (!IsUnlocked(skin))
            {
                Debug.LogWarning($"[SkinManager] Skin not unlocked: {skinId}");
                return false;
            }

            _equippedSkins[characterId] = skin;
            SaveEquippedSkin(characterId, skin);

            OnSkinEquipped?.Invoke(characterId, skin);

            // Apply to active character instance if exists
            if (_activeCharacterInstances.TryGetValue(characterId, out var instance))
            {
                ApplySkinToInstance(instance, characterId, skin);
            }

            Debug.Log($"[SkinManager] Equipped skin: {skinId} for {characterId}");
            return true;
        }

        /// <summary>
        /// Register a character instance in the scene for skin application
        /// </summary>
        public void RegisterCharacterInstance(string characterId, GameObject instance)
        {
            if (instance == null) return;
            
            _activeCharacterInstances[characterId] = instance;
            
            // Apply current equipped skin immediately
            if (_equippedSkins.TryGetValue(characterId, out var skin))
            {
                ApplySkinToInstance(instance, characterId, skin);
            }
        }

        /// <summary>
        /// Unregister a character instance (e.g., on scene change)
        /// </summary>
        public void UnregisterCharacterInstance(string characterId)
        {
            _activeCharacterInstances.Remove(characterId);
        }

        /// <summary>
        /// Apply skin to a specific character instance
        /// </summary>
        public void ApplySkinToCharacter(string characterId, CharacterSkinData skin)
        {
            if (_activeCharacterInstances.TryGetValue(characterId, out var instance))
            {
                ApplySkinToInstance(instance, characterId, skin);
            }
        }

        private void ApplySkinToInstance(GameObject instance, string characterId, CharacterSkinData skin)
        {
            if (instance == null || skin == null) return;

            // Option A: Full prefab swap (Epic/Legendary with custom prefab)
            if (skin.skinPrefab != null)
            {
                var position = instance.transform.position;
                var rotation = instance.transform.rotation;
                var scale = instance.transform.localScale;
                var parent = instance.transform.parent;

                // Preserve components that need to persist (IK, RCA, etc.)
                var preservedComponents = PreserveCriticalComponents(instance);

                Destroy(instance);

                var newInstance = Instantiate(skin.skinPrefab, position, rotation, parent);
                newInstance.name = $"{characterId}_Instance";
                newInstance.transform.localScale = scale;

                // Re-attach preserved components
                RestoreCriticalComponents(newInstance, preservedComponents, characterId);

                _activeCharacterInstances[characterId] = newInstance;
            }
            // Option B: Material/Accessory/VFX overlay (Common/Rare)
            else
            {
                ApplyMaterialOverrides(instance, skin.materialOverrides);
                ApplyAccessories(instance, skin.accessories);
                ApplyVFX(instance, skin.vfxPrefabs);

                // Apply animator override if present
                if (skin.animatorOverride != null)
                {
                    var animator = instance.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.runtimeAnimatorController = skin.animatorOverride;
                    }
                }
            }

            OnSkinApplied?.Invoke(characterId, skin);
        }

        private Dictionary<System.Type, Component> PreserveCriticalComponents(GameObject instance)
        {
            var preserved = new Dictionary<System.Type, Component>();
            var criticalTypes = new System.Type[]
            {
                typeof(CueStrike.Characters.CharacterIKAssist),
                typeof(CueStrike.RCA.CueStrikeRCAManager),
                typeof(CueStrike.RCA.CueStrikeRCACalibrator),
                typeof(CueStrike.RCA.CueStrikeDualHandTracker),
                typeof(CueStrike.RCA.CueStrikeVisualVelocityCompensation),
                typeof(CueStrike.RCA.CueStrikeKalmanPredictor),
                typeof(Animator),
                typeof(Animation)
            };

            foreach (var type in criticalTypes)
            {
                var comp = instance.GetComponent(type);
                if (comp != null)
                {
                    preserved[type] = comp;
                }
            }

            return preserved;
        }

        private void RestoreCriticalComponents(GameObject newInstance, Dictionary<System.Type, Component> preserved, string characterId)
        {
            foreach (var kvp in preserved)
            {
                var oldComp = kvp.Value;
                if (oldComp == null) continue;

                var newComp = newInstance.GetComponent(kvp.Key);
                if (newComp == null)
                {
                    newComp = newInstance.AddComponent(kvp.Key);
                }

                // Copy serialized fields using JsonUtility
                var json = JsonUtility.ToJson(oldComp);
                JsonUtility.FromJsonOverwrite(json, newComp);
            }

            // Ensure name is correct for finding
            newInstance.name = $"{characterId}_Instance";
        }

        private void ApplyMaterialOverrides(GameObject instance, Material[] overrides)
        {
            if (overrides == null || overrides.Length == 0) return;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length && i < overrides.Length; i++)
                {
                    if (overrides[i] != null)
                    {
                        mats[i] = overrides[i];
                    }
                }
                renderer.sharedMaterials = mats;
            }
        }

        private void ApplyAccessories(GameObject instance, GameObject[] accessories)
        {
            if (accessories == null || accessories.Length == 0) return;

            // Find or create "Accessories" child transform
            Transform accessoryParent = instance.transform.Find("Accessories");
            if (accessoryParent == null)
            {
                accessoryParent = new GameObject("Accessories").transform;
                accessoryParent.SetParent(instance.transform);
                accessoryParent.localPosition = Vector3.zero;
                accessoryParent.localRotation = Quaternion.identity;
            }
            else
            {
                // Clear existing accessories
                foreach (Transform child in accessoryParent)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (var accessory in accessories)
            {
                if (accessory != null)
                {
                    var accInstance = Instantiate(accessory, accessoryParent);
                    accInstance.name = accessory.name;
                }
            }
        }

        private void ApplyVFX(GameObject instance, ParticleSystem[] vfxPrefabs)
        {
            if (vfxPrefabs == null || vfxPrefabs.Length == 0) return;

            Transform vfxParent = instance.transform.Find("VFX");
            if (vfxParent == null)
            {
                vfxParent = new GameObject("VFX").transform;
                vfxParent.SetParent(instance.transform);
                vfxParent.localPosition = Vector3.zero;
                vfxParent.localRotation = Quaternion.identity;
            }
            else
            {
                foreach (Transform child in vfxParent)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (var vfx in vfxPrefabs)
            {
                if (vfx != null)
                {
                    var vfxInstance = Instantiate(vfx, vfxParent);
                    vfxInstance.name = vfx.name;
                    vfxInstance.Play();
                }
            }
        }

        /// <summary>
        /// Get all unlocked skins for a character
        /// </summary>
        public List<CharacterSkinData> GetUnlockedSkins(string characterId)
        {
            var all = GetSkinsForCharacter(characterId);
            var unlocked = new List<CharacterSkinData>();
            foreach (var skin in all)
            {
                if (IsUnlocked(skin))
                    unlocked.Add(skin);
            }
            return unlocked;
        }

        /// <summary>
        /// Get all locked skins for a character
        /// </summary>
        public List<CharacterSkinData> GetLockedSkins(string characterId)
        {
            var all = GetSkinsForCharacter(characterId);
            var locked = new List<CharacterSkinData>();
            foreach (var skin in all)
            {
                if (!IsUnlocked(skin))
                    locked.Add(skin);
            }
            return locked;
        }

        /// <summary>
        /// Force refresh - reload all skins from resources
        /// </summary>
        public void Refresh()
        {
            allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");
            BuildLookup();
            LoadEquippedSkins();
        }
    }
}