# 🎭 CueStrike — Character & Skin System Specification
> **Project:** CueStrike VR Billiards (AAA Unity, Meta Quest 2/3)  
> **Date:** 2026-08-05  
> **Status:** 📝 Specification Complete — Ready for Implementation  
> **Based on:** CHARACTER_SYSTEM_PLAN.md + CUESTRIKE_MASTER.md + Existing Assets

---

## 🎯 Executive Summary

ระบบตัวละคร + สกินครบวงจรสำหรับ **12 ตัวละคร** (10 ผู้เล่น + BoPanda มาสคอต + UncleNok AI Referee)  
รองรับ **4 ระดับความหายาก** (Common → Legendary) + **Seasonal/Event Skins** + **Unlock System**

---

## 👥 1. CHARACTER ROSTER (12 ตัว)

| # | ตัวละคร | ประเภท | Prefab Status | AAA FBX | Base Skin | Personality Archetype |
|---|---------|--------|---------------|---------|-----------|----------------------|
| 1 | **Somchay** | Player | ✅ Ready | ✅ AAA | Classic Thai Pool Shark | "The Local Legend" — Chill, witty, beer in hand |
| 2 | **MeiLing** | Player | ✅ Ready | ✅ AAA | Qipao + Cue Case | "Precision Princess" — Calculated, graceful, tea drinker |
| 3 | **Gentleman** | Player | ✅ Ready | ✅ AAA | Tuxedo + Pocket Watch | "The Aristocrat" — Polished, snooker purist, pinky out |
| 4 | **PanPan** | Player | ✅ Ready | ✅ AAA | Streetwear + Cap | "The Hustler" — Flashy, trash-talk, hip-hop vibe |
| 5 | **Finn** | Player | ✅ Ready | ✅ AAA | Hoodie + Headphones | "The Chill Pro" — Lo-fi beats, relaxed, zen focus |
| 6 | **KingFlex** | Player | ✅ Ready | ✅ AAA | Gold Chain + Tank Top | "The Showman" — Flex on 'em, celebration king |
| 7 | **Tusker** | Player | ✅ Ready | ✅ AAA | Safari Vest + Shorts | "The Gentle Giant" — Elephant memory, protective |
| 8 | **Phantom** | Player | ✅ Ready | ✅ AAA | Hooded Cloak + Mask | "The Shadow" — Mysterious, silent, appears/disappears |
| 9 | **Cassidy** | Player | ✅ Ready | ✅ AAA | Cowboy Hat + Duster | "The Drifter" — Western drawl, high noon energy |
| 10 | **Bones** | Player | ✅ Ready | ✅ AAA | Skeleton Suit + Glow | "The Undead" — Spooky, bone puns, Halloween year-round |
| 11 | **BoPanda** | Mascot 🐼 | ✅ Ready | ✅ AAA | Bamboo Hat + Bow Tie | "The Cheerleader" — Banter, popcorn, bamboo snacks |
| 12 | **UncleNok** | AI Referee 🐘 | ✅ Ready | ✅ AAA | Bowler Hat + Vest | "The Wise Judge" — Stern but fair, elephant wisdom |

> ✅ **All 12 AAA FBX + Prefabs exist** at `Assets/CueStrike/Models/AAA_Characters/` and `Assets/CueStrike/Prefabs/AAA_Characters/`

---

## 🎨 2. SKIN SYSTEM ARCHITECTURE

### 2.1 Skin Rarity Tiers

| Tier | Color | Name | Unlock Method | Visual Changes |
|------|-------|------|---------------|----------------|
| **0** | ⚪ Gray | **Default / Base** | Starting | Base model + 1 material |
| **1** | 🟢 Green | **Common** | Level 5 / 100 Coins | Texture swap (clothes color) |
| **2** | 🔵 Blue | **Rare** | Level 15 / 500 Coins | Texture + Accessory (hat/glasses) |
| **3** | 🟣 Purple | **Epic** | Level 30 / 2,000 Coins | New outfit mesh + VFX trail |
| **4** | 🟠 Orange | **Legendary** | Event / 10,000 Coins | Full remodel + Custom animations + Voice lines |

### 2.2 Skin Data Structure

```csharp
// Assets/CueStrike/Characters/Skins/CharacterSkinData.cs
[CreateAssetMenu(menuName = "CueStrike/Skins/Character Skin")]
public class CharacterSkinData : ScriptableObject
{
    public string skinId;              // "somchay_summer_2024"
    public string characterId;         // "somchay"
    public SkinRarity rarity;          // Common, Rare, Epic, Legendary
    public string displayName;         // "Songkran Splash"
    public string description;         // "Celebrate Thai New Year in style"
    public Sprite icon;                // UI icon (256x256)
    public GameObject skinPrefab;      // Full prefab with meshes/materials
    public Material[] overrideMaterials; // For texture-swap skins
    public GameObject[] accessoryObjects; // Hats, glasses, etc.
    public ParticleSystem[] vfxEffects;   // Trail, aura, etc.
    public AudioClip[] voiceLines;        // Skin-specific lines
    public int unlockLevel;               // Level requirement
    public int unlockCost;                // Coin cost
    public bool isSeasonal;               // Time-limited
    public SeasonalEvent eventType;       // Songkran, Halloween, Xmas, etc.
    public AnimationClip[] customAnimations; // Legendary only
}
```

### 2.3 Skin Categories per Character

Each character gets **8-12 skins** across categories:

| Category | Skins per Char | Examples |
|----------|----------------|----------|
| **Base/Default** | 1 | Original design |
| **Color Variants (Common)** | 3 | Red/Blue/Gold outfit recolors |
| **Themed (Rare)** | 2 | "Tournament Pro", "Casual Friday" |
| **Cultural/Seasonal (Epic)** | 2 | Songkran, Halloween, Christmas, Lunar New Year |
| **Legendary/Event** | 1-2 | "World Champion", "Dev Exclusive", "Anniversary" |

**Total: ~100 skins across 12 characters**

---

## 🏗️ 3. TECHNICAL IMPLEMENTATION

### 3.1 Folder Structure

```
Assets/CueStrike/
├── Characters/
│   ├── Skins/
│   │   ├── CharacterSkinData.cs          # ScriptableObject definition
│   │   ├── SkinManager.cs                # Runtime skin switching
│   │   ├── SkinUnlockManager.cs          # Progression + unlock logic
│   │   ├── SkinPreviewUI.cs              # Character select preview
│   │   └── Resources/Skins/              # All SkinData assets
│   │       ├── Somchay/
│   │       ├── MeiLing/
│   │       └── ... (12 folders)
│   ├── CharacterSelector.cs              # Multiplayer character pick
│   └── PlayerCharacterManager.cs         # Existing - extend for skins
├── Models/AAA_Characters/                # Base FBX (12 chars)
├── Prefabs/AAA_Characters/               # Base Prefabs (12 chars)
├── Prefabs/Skins/                        # Skin variant prefabs
│   ├── Somchay_Default.prefab
│   ├── Somchay_Songkran_Epic.prefab
│   └── ...
└── Editor/
    ├── SkinSetup.cs                      # Batch create skin assets
    ├── SkinBuilder.cs                    # Build skin prefabs from base
    └── SkinSelfTest.cs                   # Validate all skins
```

### 3.2 Core Scripts

#### A. `CharacterSkinData.cs` — ScriptableObject
```csharp
namespace CueStrike.Characters.Skins
{
    public enum SkinRarity { Common, Rare, Epic, Legendary }
    public enum SeasonalEvent { None, Songkran, Halloween, Christmas, LunarNewYear, Anniversary, Summer, DevExclusive }

    [CreateAssetMenu(fileName = "Skin_", menuName = "CueStrike/Skins/Character Skin")]
    public class CharacterSkinData : ScriptableObject
    {
        [Header("Identity")]
        public string skinId;
        public string characterId;
        public SkinRarity rarity;
        public SeasonalEvent eventType;
        
        [Header("UI")]
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        
        [Header("Visual")]
        public GameObject skinPrefab;           // Full replacement prefab
        public Material[] materialOverrides;    // For simple texture swaps
        public GameObject[] accessories;        // Hats, glasses, props
        public ParticleSystem[] vfxPrefabs;     // Trails, auras
        
        [Header("Audio")]
        public AudioClip[] voiceLines;          // Skin-specific banter
        
        [Header("Unlock")]
        public int unlockLevel = 0;
        public int unlockCost = 0;
        public bool isSeasonal = false;
        public System.DateTime seasonalStart;
        public System.DateTime seasonalEnd;
        
        [Header("Legendary Only")]
        public AnimationClip[] customAnimations;
        public RuntimeAnimatorController animatorOverride;
    }
}
```

#### B. `SkinManager.cs` — Runtime Switching
```csharp
namespace CueStrike.Characters.Skins
{
    public class SkinManager : MonoBehaviour
    {
        public static SkinManager Instance { get; private set; }
        
        [SerializeField] private CharacterSkinData[] allSkins;
        private Dictionary<string, List<CharacterSkinData>> _skinsByCharacter = new();
        private Dictionary<string, CharacterSkinData> _equippedSkins = new(); // charId -> skin
        
        private void Awake()
        {
            Instance = this;
            BuildLookup();
            LoadEquippedSkins();
        }
        
        private void BuildLookup()
        {
            foreach (var skin in allSkins)
            {
                if (!_skinsByCharacter.ContainsKey(skin.characterId))
                    _skinsByCharacter[skin.characterId] = new List<CharacterSkinData>();
                _skinsByCharacter[skin.characterId].Add(skin);
            }
            // Sort by rarity
            foreach (var list in _skinsByCharacter.Values)
                list.Sort((a, b) => a.rarity.CompareTo(b.rarity));
        }
        
        public List<CharacterSkinData> GetSkinsForCharacter(string characterId)
            => _skinsByCharacter.TryGetValue(characterId, out var list) ? list : new List<CharacterSkinData>();
        
        public CharacterSkinData GetEquippedSkin(string characterId)
            => _equippedSkins.TryGetValue(characterId, out var skin) ? skin : null;
        
        public bool EquipSkin(string characterId, string skinId)
        {
            var skins = GetSkinsForCharacter(characterId);
            var skin = skins.Find(s => s.skinId == skinId);
            if (skin == null) return false;
            
            if (!IsUnlocked(skin)) return false;
            
            _equippedSkins[characterId] = skin;
            SaveEquippedSkins();
            ApplySkinToCharacter(characterId, skin);
            return true;
        }
        
        public bool IsUnlocked(CharacterSkinData skin)
        {
            if (skin.rarity == SkinRarity.Common) return true; // Base always unlocked
            // Check PlayerPrefs / SaveSystem
            return PlayerPrefs.GetInt($"skin_unlocked_{skin.skinId}", 0) == 1;
        }
        
        public void UnlockSkin(CharacterSkinData skin)
        {
            PlayerPrefs.SetInt($"skin_unlocked_{skin.skinId}", 1);
            PlayerPrefs.Save();
            OnSkinUnlocked?.Invoke(skin);
        }
        
        private void ApplySkinToCharacter(string characterId, CharacterSkinData skin)
        {
            // Find character instance in scene
            var charObj = GameObject.Find($"{characterId}_Instance");
            if (charObj == null) return;
            
            // Option A: Full prefab swap (for Epic/Legendary)
            if (skin.skinPrefab != null)
            {
                var newChar = Instantiate(skin.skinPrefab, charObj.transform.position, charObj.transform.rotation);
                newChar.name = $"{characterId}_Instance";
                Destroy(charObj);
                // Re-attach scripts (IK, RCA, etc.)
                ReattachComponents(newChar, characterId);
            }
            // Option B: Material/Accessory overlay (for Common/Rare)
            else
            {
                ApplyMaterialOverrides(charObj, skin.materialOverrides);
                ApplyAccessories(charObj, skin.accessories);
                ApplyVFX(charObj, skin.vfxPrefabs);
            }
        }
        
        public event Action<CharacterSkinData> OnSkinUnlocked;
        public event Action<string, CharacterSkinData> OnSkinEquipped; // charId, skin
    }
}
```

#### C. `SkinUnlockManager.cs` — Progression
```csharp
namespace CueStrike.Characters.Skins
{
    public class SkinUnlockManager : MonoBehaviour
    {
        [Header("Unlock Conditions")]
        public int[] levelThresholds = { 5, 15, 30, 50 }; // Common, Rare, Epic, Legendary
        public int[] coinCosts = { 100, 500, 2000, 10000 };
        
        public void CheckLevelUpUnlocks(int newLevel)
        {
            var allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");
            foreach (var skin in allSkins)
            {
                if (skin.unlockLevel <= newLevel && !SkinManager.Instance.IsUnlocked(skin))
                {
                    SkinManager.Instance.UnlockSkin(skin);
                    ShowUnlockNotification(skin);
                }
            }
        }
        
        public bool TryPurchaseSkin(CharacterSkinData skin)
        {
            if (SkinManager.Instance.IsUnlocked(skin)) return true;
            
            int coins = PlayerPrefs.GetInt("player_coins", 0);
            if (coins >= skin.unlockCost)
            {
                PlayerPrefs.SetInt("player_coins", coins - skin.unlockCost);
                SkinManager.Instance.UnlockSkin(skin);
                return true;
            }
            return false;
        }
        
        public void GrantSeasonalSkin(SeasonalEvent eventType)
        {
            var allSkins = Resources.LoadAll<CharacterSkinData>("Skins/");
            foreach (var skin in allSkins)
            {
                if (skin.eventType == eventType && skin.isSeasonal)
                {
                    SkinManager.Instance.UnlockSkin(skin);
                }
            }
        }
    }
}
```

---

## 🎪 4. SKIN PREVIEW & CHARACTER SELECTION UI

### 4.1 Character Select Screen Flow

```
┌─────────────────────────────────────────────────────────────┐
│  CHARACTER SELECT                          [Coins: 1,250]   │
├─────────────────────────────────────────────────────────────┤
│  [Somchay]  [MeiLing]  [Gentleman]  [PanPan]  [Finn]  [>]  │
│       ▼                                                      │
│  ┌─────────────┐  ┌─────────────────────────────────────┐  │
│  │  3D Preview │  │ SKINS                                │  │
│  │  (Rotatable)│  │ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐  │  │
│  │             │  │ │Base│ │Red │ │Blue│ │Song│ │World│  │  │
│  │   [Model]   │  │ │ 🔓 │ │ 🔓 │ │ 🔒 │ │kran│ │Champ│  │  │
│  │             │  │ │Def │ │Com │ │Com │ │Epic│ │Leg │  │  │
│  └─────────────┘  │ └────┘ └────┘ └────┘ └────┘ └────┘  │  │
│                   │ [Equip] [Preview] [Buy: 2,000🪙]       │  │
│  Stats:           │                                        │  │
│  🎱 8-Ball: 73%   │  Rarity: Epic (Purple)                 │  │
│  🎱 9-Ball: 68%   │  Unlock: Level 30 or 2,000 Coins       │  │
│  🎱 Snooker: 45%  │  Event: Songkran 2024                  │  │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 `SkinPreviewUI.cs` — Component
```csharp
namespace CueStrike.Characters.Skins
{
    public class SkinPreviewUI : MonoBehaviour
    {
        [Header("References")]
        public Transform previewAnchor;
        public Camera previewCamera;
        public RenderTexture previewTexture;
        public RawImage previewDisplay;
        
        [Header("UI")]
        public Transform skinGridParent;
        public GameObject skinSlotPrefab;
        public TextMeshProUGUI skinNameText;
        public TextMeshProUGUI skinRarityText;
        public TextMeshProUGUI skinDescriptionText;
        public Button equipButton;
        public Button buyButton;
        public TextMeshProUGUI costText;
        
        private GameObject _currentPreviewModel;
        private CharacterSkinData _selectedSkin;
        private string _currentCharacterId;
        
        public void ShowCharacter(string characterId)
        {
            _currentCharacterId = characterId;
            var skins = SkinManager.Instance.GetSkinsForCharacter(characterId);
            PopulateSkinGrid(skins);
            SelectSkin(skins[0]); // Select base skin
        }
        
        private void PopulateSkinGrid(List<CharacterSkinData> skins)
        {
            // Clear existing
            foreach (Transform child in skinGridParent) Destroy(child.gameObject);
            
            foreach (var skin in skins)
            {
                var slot = Instantiate(skinSlotPrefab, skinGridParent);
                var icon = slot.GetComponentInChildren<RawImage>();
                icon.texture = AssetPreview.GetAssetPreview(skin.icon);
                
                // Rarity border color
                var border = slot.transform.Find("RarityBorder").GetComponent<Image>();
                border.color = GetRarityColor(skin.rarity);
                
                // Lock overlay
                var lockObj = slot.transform.Find("LockOverlay").gameObject;
                lockObj.SetActive(!SkinManager.Instance.IsUnlocked(skin));
                
                var btn = slot.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectSkin(skin));
            }
        }
        
        private void SelectSkin(CharacterSkinData skin)
        {
            _selectedSkin = skin;
            
            // Update info panel
            skinNameText.text = skin.displayName;
            skinRarityText.text = skin.rarity.ToString();
            skinRarityText.color = GetRarityColor(skin.rarity);
            skinDescriptionText.text = skin.description;
            
            // Update buttons
            bool unlocked = SkinManager.Instance.IsUnlocked(skin);
            bool equipped = SkinManager.Instance.GetEquippedSkin(_currentCharacterId)?.skinId == skin.skinId;
            
            equipButton.gameObject.SetActive(unlocked && !equipped);
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(() => EquipSelectedSkin());
            
            buyButton.gameObject.SetActive(!unlocked && skin.unlockCost > 0);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => BuySelectedSkin());
            costText.text = $"{skin.unlockCost} 🪙";
            
            // Spawn preview model
            SpawnPreviewModel(skin);
        }
        
        private void SpawnPreviewModel(CharacterSkinData skin)
        {
            if (_currentPreviewModel) Destroy(_currentPreviewModel);
            
            GameObject prefab = skin.skinPrefab;
            if (prefab == null)
            {
                // Fallback: load base character + apply overrides
                prefab = Resources.Load<GameObject>($"Characters/Prefabs/{_currentCharacterId}_Base");
            }
            
            _currentPreviewModel = Instantiate(prefab, previewAnchor);
            _currentPreviewModel.transform.localPosition = Vector3.zero;
            _currentPreviewModel.transform.localRotation = Quaternion.Euler(0, 180, 0);
            _currentPreviewModel.transform.localScale = Vector3.one;
            
            // Apply skin overrides if not full prefab
            if (skin.skinPrefab == null)
            {
                SkinManager.Instance.ApplySkinToCharacter(_currentCharacterId, skin);
            }
            
            // Start idle animation
            var anim = _currentPreviewModel.GetComponent<Animation>();
            if (anim) anim.Play("Idle");
        }
        
        private Color GetRarityColor(SkinRarity rarity)
        {
            return rarity switch
            {
                SkinRarity.Common => Color.green,
                SkinRarity.Rare => Color.blue,
                SkinRarity.Epic => new Color(0.6f, 0.2f, 0.8f), // Purple
                SkinRarity.Legendary => new Color(1f, 0.5f, 0f), // Orange
                _ => Color.gray
            };
        }
    }
}
```

---

## 🛠️ 5. EDITOR TOOLS (Apply Buttons)

### 5.1 `SkinSetup.cs` — Batch Create Skin Assets
```csharp
// Assets/CueStrike/Editor/SkinSetup.cs
namespace CueStrike.Editor
{
    public class SkinSetup
    {
        [MenuItem("Tools/CueStrike/Skins/Create All Skin Data Assets", priority = 200)]
        public static void CreateAllSkinData()
        {
            // 3-Layer Guard
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            
            string[] characterIds = { "somchay", "meiling", "gentleman", "panpan", "finn", 
                                      "kingflex", "tusker", "phantom", "cassidy", "bones",
                                      "bopanda", "unclenok" };
            
            int created = 0;
            foreach (var charId in characterIds)
            {
                created += CreateSkinsForCharacter(charId);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", $"Created {created} SkinData assets", "OK");
        }
        
        private static int CreateSkinsForCharacter(string charId)
        {
            int count = 0;
            string folder = $"Assets/CueStrike/Characters/Skins/Resources/Skins/{charId.ToTitleCase()}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
            
            // Define skin templates per character
            var skins = GetSkinTemplates(charId);
            
            foreach (var template in skins)
            {
                string assetPath = $"{folder}/Skin_{template.skinId}.asset";
                if (AssetDatabase.LoadAssetAtPath<CharacterSkinData>(assetPath)) continue;
                
                var skinData = ScriptableObject.CreateInstance<CharacterSkinData>();
                // Populate from template
                skinData.skinId = template.skinId;
                skinData.characterId = charId;
                skinData.rarity = template.rarity;
                skinData.displayName = template.displayName;
                skinData.description = template.description;
                skinData.unlockLevel = template.unlockLevel;
                skinData.unlockCost = template.unlockCost;
                skinData.isSeasonal = template.isSeasonal;
                skinData.eventType = template.eventType;
                
                AssetDatabase.CreateAsset(skinData, assetPath);
                count++;
            }
            return count;
        }
        
        private static SkinTemplate[] GetSkinTemplates(string charId)
        {
            // Base skin (always)
            var list = new List<SkinTemplate>
            {
                new SkinTemplate { skinId = $"{charId}_default", displayName = "Default", 
                    rarity = SkinRarity.Common, unlockLevel = 0, unlockCost = 0 }
            };
            
            // Color variants (Common)
            list.AddRange(new[] { "red", "blue", "gold" }.Select(c => new SkinTemplate
            {
                skinId = $"{charId}_{c}",
                displayName = $"{c.ToTitleCase()} Variant",
                rarity = SkinRarity.Common,
                unlockLevel = 5,
                unlockCost = 100
            }));
            
            // Themed (Rare)
            list.AddRange(new[] { "tournament", "casual" }.Select(t => new SkinTemplate
            {
                skinId = $"{charId}_{t}",
                displayName = $"{t.ToTitleCase()} Style",
                rarity = SkinRarity.Rare,
                unlockLevel = 15,
                unlockCost = 500
            }));
            
            // Seasonal (Epic) - character specific
            var seasonal = GetSeasonalSkins(charId);
            list.AddRange(seasonal);
            
            // Legendary
            list.Add(new SkinTemplate
            {
                skinId = $"{charId}_world_champion",
                displayName = "World Champion",
                rarity = SkinRarity.Legendary,
                unlockLevel = 50,
                unlockCost = 10000,
                isSeasonal = false
            });
            
            return list.ToArray();
        }
        
        private static SkinTemplate[] GetSeasonalSkins(string charId)
        {
            // Character-specific seasonal themes
            var themes = new Dictionary<string, (SeasonalEvent, string)[]>
            {
                ["somchay"] = new[] { (SeasonalEvent.Songkran, "Songkran Splash"), (SeasonalEvent.Summer, "Beach Vibes") },
                ["meiling"] = new[] { (SeasonalEvent.LunarNewYear, "Lunar Elegance"), (SeasonalEvent.Spring, "Cherry Blossom") },
                ["gentleman"] = new[] { (SeasonalEvent.Christmas, "Victorian Christmas"), (SeasonalEvent.Anniversary, "Founder's Tailcoat") },
                ["panpan"] = new[] { (SeasonalEvent.Halloween, "Neon Ghost"), (SeasonalEvent.Summer, "Street Festival") },
                ["finn"] = new[] { (SeasonalEvent.Winter, "Cozy Hoodie"), (SeasonalEvent.Anniversary, "Lo-Fi Anniversary") },
                ["kingflex"] = new[] { (SeasonalEvent.Summer, "Gold Summer"), (SeasonalEvent.Halloween, "Spooky Flex") },
                ["tusker"] = new[] { (SeasonalEvent.Christmas, "Santa's Helper"), (SeasonalEvent.LunarNewYear, "Lucky Elephant") },
                ["phantom"] = new[] { (SeasonalEvent.Halloween, "True Phantom"), (SeasonalEvent.DevExclusive, "Shadow Dev") },
                ["cassidy"] = new[] { (SeasonalEvent.Summer, "Desert Bloom"), (SeasonalEvent.Halloween, "Grim Reaper") },
                ["bones"] = new[] { (SeasonalEvent.Halloween, "Bone King"), (SeasonalEvent.Christmas, "Skeleton Santa") },
                ["bopanda"] = new[] { (SeasonalEvent.Songkran, "Water Festival Panda"), (SeasonalEvent.Christmas, "Santa Panda") },
                ["unclenok"] = new[] { (SeasonalEvent.Anniversary, "Golden Judge"), (SeasonalEvent.DevExclusive, "Dev Referee") }
            };
            
            if (!themes.TryGetValue(charId, out var charThemes)) return new SkinTemplate[0];
            
            return charThemes.Select(t => new SkinTemplate
            {
                skinId = $"{charId}_{t.Item1.ToString().ToLower()}",
                displayName = t.Item2,
                rarity = SkinRarity.Epic,
                unlockLevel = 30,
                unlockCost = 2000,
                isSeasonal = true,
                eventType = t.Item1
            }).ToArray();
        }
        
        private class SkinTemplate
        {
            public string skinId;
            public string displayName;
            public SkinRarity rarity;
            public int unlockLevel;
            public int unlockCost;
            public bool isSeasonal;
            public SeasonalEvent eventType;
            public string description;
        }
    }
}
```

### 5.2 `SkinBuilder.cs` — Build Skin Prefabs from Base
```csharp
// Assets/CueStrike/Editor/SkinBuilder.cs
namespace CueStrike.Editor
{
    public class SkinBuilder
    {
        [MenuItem("Tools/CueStrike/Skins/Build All Skin Prefabs", priority = 201)]
        public static void BuildAllSkinPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            
            var allSkins = AssetDatabase.FindAssets("t:CharacterSkinData", new[] { "Assets/CueStrike/Characters/Skins/Resources" });
            int built = 0;
            
            foreach (var guid in allSkins)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skinData = AssetDatabase.LoadAssetAtPath<CharacterSkinData>(path);
                
                if (skinData.rarity >= SkinRarity.Epic && skinData.skinPrefab == null)
                {
                    if (BuildSkinPrefab(skinData))
                        built++;
                }
            }
            
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Done", $"Built {built} Epic/Legendary skin prefabs", "OK");
        }
        
        private static bool BuildSkinPrefab(CharacterSkinData skinData)
        {
            // Load base character prefab
            string basePrefabPath = $"Assets/CueStrike/Prefabs/AAA_Characters/{skinData.characterId.ToTitleCase()}_AAA.prefab";
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (basePrefab == null) return false;
            
            // Instantiate and modify
            var instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            
            // Apply material overrides
            if (skinData.materialOverrides != null && skinData.materialOverrides.Length > 0)
            {
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length && i < skinData.materialOverrides.Length; i++)
                    {
                        if (skinData.materialOverrides[i] != null)
                            mats[i] = skinData.materialOverrides[i];
                    }
                    renderer.sharedMaterials = mats;
                }
            }
            
            // Add accessories
            if (skinData.accessories != null)
            {
                foreach (var accessory in skinData.accessories)
                {
                    if (accessory != null)
                    {
                        var accInstance = PrefabUtility.InstantiatePrefab(accessory) as GameObject;
                        accInstance.transform.SetParent(instance.transform);
                        // Position via accessory's local transform
                    }
                }
            }
            
            // Add VFX
            if (skinData.vfxPrefabs != null)
            {
                foreach (var vfx in skinData.vfxPrefabs)
                {
                    if (vfx != null)
                    {
                        var vfxInstance = PrefabUtility.InstantiatePrefab(vfx) as GameObject;
                        vfxInstance.transform.SetParent(instance.transform);
                    }
                }
            }
            
            // Save as new prefab
            string outputPath = $"Assets/CueStrike/Prefabs/Skins/{skinData.skinId}.prefab";
            string dir = System.IO.Path.GetDirectoryName(outputPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
            DestroyImmediate(instance);
            
            // Update skinData reference
            skinData.skinPrefab = prefab;
            EditorUtility.SetDirty(skinData);
            
            Debug.Log($"✅ Built skin prefab: {outputPath}");
            return true;
        }
    }
}
```

### 5.3 `SkinSelfTest.cs` — Validation
```csharp
// Assets/CueStrike/Editor/SkinSelfTest.cs
namespace CueStrike.Editor
{
    public class SkinSelfTest
    {
        [MenuItem("Tools/CueStrike/Skins/Validate All Skins", priority = 202)]
        public static void ValidateAllSkins()
        {
            var allSkins = AssetDatabase.FindAssets("t:CharacterSkinData", new[] { "Assets/CueStrike/Characters/Skins/Resources" });
            int pass = 0, fail = 0;
            var errors = new List<string>();
            
            foreach (var guid in allSkins)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skin = AssetDatabase.LoadAssetAtPath<CharacterSkinData>(path);
                
                if (ValidateSkin(skin, errors))
                    pass++;
                else
                    fail++;
            }
            
            string report = $"Skin Validation Report\nPass: {pass} | Fail: {fail}\n\n";
            if (errors.Count > 0)
                report += "Errors:\n" + string.Join("\n", errors);
            
            EditorUtility.DisplayDialog(fail == 0 ? "All Pass" : "Validation Failed", report, "OK");
            
            if (fail > 0)
                Debug.LogError(report);
            else
                Debug.Log(report);
        }
        
        private static bool ValidateSkin(CharacterSkinData skin, List<string> errors)
        {
            bool ok = true;
            string prefix = $"[{skin.skinId}] ";
            
            if (string.IsNullOrEmpty(skin.skinId)) { errors.Add(prefix + "Missing skinId"); ok = false; }
            if (string.IsNullOrEmpty(skin.characterId)) { errors.Add(prefix + "Missing characterId"); ok = false; }
            if (string.IsNullOrEmpty(skin.displayName)) { errors.Add(prefix + "Missing displayName"); ok = false; }
            if (skin.icon == null) { errors.Add(prefix + "Missing icon"); ok = false; }
            
            // Epic/Legendary must have prefab or material overrides
            if (skin.rarity >= SkinRarity.Epic)
            {
                if (skin.skinPrefab == null && (skin.materialOverrides == null || skin.materialOverrides.Length == 0))
                {
                    errors.Add(prefix + "Epic/Legendary skin missing prefab AND material overrides");
                    ok = false;
                }
            }
            
            // Seasonal skins need date range
            if (skin.isSeasonal)
            {
                if (skin.seasonalStart == default || skin.seasonalEnd == default)
                {
                    errors.Add(prefix + "Seasonal skin missing date range");
                    ok = false;
                }
            }
            
            // Check unlock cost matches rarity
            int expectedCost = skin.rarity switch
            {
                SkinRarity.Common => 0,
                SkinRarity.Rare => 500,
                SkinRarity.Epic => 2000,
                SkinRarity.Legendary => 10000,
                _ => 0
            };
            if (skin.unlockCost != expectedCost && skin.rarity != SkinRarity.Common)
            {
                errors.Add(prefix + $"Unlock cost {skin.unlockCost} doesn't match rarity {skin.rarity} (expected {expectedCost})");
                ok = false;
            }
            
            return ok;
        }
    }
}
```

---

## 📦 6. IMPLEMENTATION ROADMAP

| Phase | Task | Duration | Dependencies |
|-------|------|----------|--------------|
| **1** | Create `CharacterSkinData.cs` + `SkinManager.cs` + `SkinUnlockManager.cs` | 1 day | None |
| **2** | Run `SkinSetup.cs` → Generate all ~100 SkinData assets | 30 min | Phase 1 |
| **3** | Create material variants (Common/Rare texture swaps) | 2 days | Art assets |
| **4** | Build Epic/Legendary prefabs via `SkinBuilder.cs` | 1 day | Phase 3 |
| **5** | Implement `SkinPreviewUI.cs` + Character Select Scene | 2 days | Phase 1 |
| **6** | Integrate with `PlayerCharacterManager.cs` + Multiplayer sync | 1 day | Phase 5 |
| **7** | Run `SkinSelfTest.cs` → Fix all validation errors | 30 min | Phase 4 |
| **8** | Seasonal event system + Calendar integration | 1 day | Phase 6 |

**Total: ~8 days for full skin system**

---

## 🎯 7. IMMEDIATE ACTION ITEMS

### For You (พี่โม่ง) — Unity Editor Actions:
1. **Run Blender Title Screen** → `blender --background --python BlenderScripts/create_title_screen.py`
2. **Import FBX** → Drag to `Assets/CueStrike/Models/TitleScreen/`
3. **Click Unity Menu:** `Tools/CueStrike/Setup/Create Title Screen Scene`
4. **Click Unity Menu:** `Tools/CueStrike/Skins/Create All Skin Data Assets`
5. **Click Unity Menu:** `Tools/CueStrike/Skins/Validate All Skins`

### For Dev Agent (AI) — Code Tasks:
1. ✅ Create `CharacterSkinData.cs`, `SkinManager.cs`, `SkinUnlockManager.cs`
2. ✅ Create `SkinSetup.cs`, `SkinBuilder.cs`, `SkinSelfTest.cs` (Editor)
3. ✅ Create `SkinPreviewUI.cs` + Character Select scene setup
4. ⏳ Create material variants for 12 characters (Common/Rare)
5. ⏳ Build Epic/Legendary prefabs with custom meshes

---

## 📁 8. FILE CHECKLIST

### Runtime Scripts (Assets/CueStrike/Characters/Skins/)
- [x] `CharacterSkinData.cs` — ScriptableObject definition
- [x] `SkinManager.cs` — Runtime skin switching + equip logic
- [x] `SkinUnlockManager.cs` — Level/coin/seasonal unlocks
- [ ] `SkinPreviewUI.cs` — Character select preview UI
- [ ] `CharacterSelector.cs` — Multiplayer character pick sync

### Editor Scripts (Assets/CueStrike/Editor/)
- [x] `SkinSetup.cs` — Batch create SkinData assets
- [x] `SkinBuilder.cs` — Build Epic/Legendary prefabs
- [x] `SkinSelfTest.cs` — Validate all skin data
- [ ] `CharacterSelectSceneSetup.cs` — Create character select scene

### Data Assets (Assets/CueStrike/Characters/Skins/Resources/Skins/)
- [ ] 12 character folders × ~8-10 skins = ~100 `.asset` files

### Prefabs (Assets/CueStrike/Prefabs/Skins/)
- [ ] Base prefabs (12) — already exist in AAA_Characters
- [ ] Epic/Legendary variant prefabs (~24)

---

## 💡 9. STRATEGIC NOTES

1. **Start Simple:** Common/Rare = material swaps only (fast, low memory)
2. **Epic/Legendary = Full Prefabs** with custom meshes, VFX, animations
3. **Seasonal = Time-gated** — Auto-unlock during event, relock after (or keep if purchased)
4. **Multiplayer Sync:** SkinId synced via Normcore — remote players see correct skin
5. **Performance:** Skin prefabs pooled at startup; swap via `SkinManager.ApplySkinToCharacter()`
6. **Monetization Ready:** Coin costs mapped to rarity; easy to add IAP for coin packs

---

## 🗣️ 10. CHARACTER VOICE LINES (Skin-Specific)

Each Legendary skin gets **3-5 unique voice lines**:

| Character | Legendary Skin | Sample Lines |
|-----------|---------------|--------------|
| Somchay | World Champion | "World champion energy!", "Somchay in the house!", "Champions drink Chang!" |
| MeiLing | Lunar Empress | "Fortune favors the precise", "The dragon awakens", "Perfection is a journey" |
| PanPan | Neon Ghost | "Catch me if you can!", "Glow up!", "Street legend activated" |
| BoPanda | Santa Panda | "Ho ho ho... bamboo!", "Merry Christmas from the panda!", "Who's been naughty? *munches bamboo*" |

---

*Document Version: 2026-08-05 v1.0 | Spec Complete | Ready for Implementation*
*Next: Run Editor tools → Generate assets → Build prefabs → Integrate UI*