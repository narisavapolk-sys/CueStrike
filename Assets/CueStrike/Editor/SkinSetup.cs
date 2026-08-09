using CueStrike.Characters.Skins;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor Tool: Batch create all CharacterSkinData assets for 12 characters
    /// Menu: Tools/CueStrike/Skins/Create All Skin Data Assets
    /// </summary>
    public class SkinSetup
    {
        private const string MENU_PATH = "Tools/CueStrike/Skins/Create All Skin Data Assets";
        private const string BASE_RESOURCES_PATH = "Assets/CueStrike/Characters/Skins/Resources/Skins";

        private static readonly string[] CharacterIds = 
        {
            "somchay", "meiling", "gentleman", "panpan", "finn",
            "kingflex", "tusker", "phantom", "cassidy", "bones",
            "bopanda", "unclenok"
        };

        [MenuItem(MENU_PATH, priority = 200)]
        public static void CreateAllSkinData()
        {
            // 3-Layer Guard
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Blocked", "Cannot create assets while in Play Mode.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            int totalCreated = 0;
            var errors = new List<string>();

            foreach (var charId in CharacterIds)
            {
                try
                {
                    int created = CreateSkinsForCharacter(charId);
                    totalCreated += created;
                    Debug.Log($"[SkinSetup] {charId}: Created {created} skin assets");
                }
                catch (System.Exception e)
                {
                    errors.Add($"{charId}: {e.Message}");
                    Debug.LogError($"[SkinSetup] Failed for {charId}: {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message = $"Successfully created {totalCreated} SkinData assets across {CharacterIds.Length} characters.";
            if (errors.Count > 0)
            {
                message += $"\n\nErrors ({errors.Count}):\n" + string.Join("\n", errors);
            }

            EditorUtility.DisplayDialog("Skin Setup Complete", message, "OK");
        }

        private static int CreateSkinsForCharacter(string charId)
        {
            int count = 0;
            string folder = $"{BASE_RESOURCES_PATH}/{charId.ToTitleCase()}";

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parentFolder = $"{BASE_RESOURCES_PATH}";
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    // Create base Skins folder
                    string charsFolder = "Assets/CueStrike/Characters/Skins/Resources";
                    if (!AssetDatabase.IsValidFolder(charsFolder))
                    {
                        AssetDatabase.CreateFolder("Assets/CueStrike/Characters/Skins", "Resources");
                    }
                    AssetDatabase.CreateFolder("Assets/CueStrike/Characters/Skins/Resources", "Skins");
                }
                AssetDatabase.CreateFolder(parentFolder, charId.ToTitleCase());
                AssetDatabase.Refresh();
            }

            // Get skin templates for this character
            var templates = GetSkinTemplates(charId);

            foreach (var template in templates)
            {
                string assetPath = $"{folder}/Skin_{template.skinId}.asset";

                // Skip if already exists
                if (AssetDatabase.LoadAssetAtPath<CharacterSkinData>(assetPath) != null)
                    continue;

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

                // Set seasonal dates if applicable
                if (template.isSeasonal && template.eventType != SeasonalEvent.None)
                {
                    SetSeasonalDates(skinData, template.eventType);
                }

                AssetDatabase.CreateAsset(skinData, assetPath);
                count++;
            }

            return count;
        }

        private static void SetSeasonalDates(CharacterSkinData skin, SeasonalEvent eventType)
        {
            var now = System.DateTime.UtcNow;
            int year = now.Year;

            switch (eventType)
            {
                case SeasonalEvent.Songkran:
                    skin.seasonalStart = new System.DateTime(year, 4, 13, 0, 0, 0, System.DateTimeKind.Utc);
                    skin.seasonalEnd = new System.DateTime(year, 4, 15, 23, 59, 59, System.DateTimeKind.Utc);
                    break;
                case SeasonalEvent.Halloween:
                    skin.seasonalStart = new System.DateTime(year, 10, 25, 0, 0, 0, System.DateTimeKind.Utc);
                    skin.seasonalEnd = new System.DateTime(year, 10, 31, 23, 59, 59, System.DateTimeKind.Utc);
                    break;
                case SeasonalEvent.Christmas:
                    skin.seasonalStart = new System.DateTime(year, 12, 20, 0, 0, 0, System.DateTimeKind.Utc);
                    skin.seasonalEnd = new System.DateTime(year, 12, 26, 23, 59, 59, System.DateTimeKind.Utc);
                    break;
                case SeasonalEvent.LunarNewYear:
                    // Approximate - would need proper lunar calendar
                    skin.seasonalStart = new System.DateTime(year, 1, 20, 0, 0, 0, System.DateTimeKind.Utc);
                    skin.seasonalEnd = new System.DateTime(year, 2, 20, 23, 59, 59, System.DateTimeKind.Utc);
                    break;
                case SeasonalEvent.Anniversary:
                    // Game launch anniversary - configurable
                    skin.seasonalStart = new System.DateTime(year, 8, 1, 0, 0, 0, System.DateTimeKind.Utc);
                    skin.seasonalEnd = new System.DateTime(year, 8, 14, 23, 59, 59, System.DateTimeKind.Utc);
                    break;
                case SeasonalEvent.Summer:
                    skin.seasonalStart = new System.DateTime(year, 6, 1, 0, 0, 0, System.DateTimeKind.Utc);
                    skin.seasonalEnd = new System.DateTime(year, 8, 31, 23, 59, 59, System.DateTimeKind.Utc);
                    break;
                case SeasonalEvent.DevExclusive:
                    // Always available for devs
                    skin.seasonalStart = System.DateTime.MinValue;
                    skin.seasonalEnd = System.DateTime.MaxValue;
                    break;
            }
        }

        private static SkinTemplate[] GetSkinTemplates(string charId)
        {
            var list = new List<SkinTemplate>();

            // ============================================================
            // BASE / DEFAULT SKIN (Always first, always unlocked)
            // ============================================================
            list.Add(new SkinTemplate
            {
                skinId = $"{charId}_default",
                displayName = "Default",
                description = "The original look for " + charId.ToTitleCase(),
                rarity = SkinRarity.Common,
                unlockLevel = 0,
                unlockCost = 0,
                isSeasonal = false
            });

            // ============================================================
            // COLOR VARIANTS (Common - Texture swaps only)
            // ============================================================
            var colorVariants = new (string id, string name, string desc)[]
            {
                ("red", "Crimson", "Bold red outfit variant"),
                ("blue", "Azure", "Cool blue outfit variant"),
                ("gold", "Gold", "Premium gold outfit variant"),
            };

            foreach (var cv in colorVariants)
            {
                list.Add(new SkinTemplate
                {
                    skinId = $"{charId}_{cv.id}",
                    displayName = cv.name,
                    description = cv.desc,
                    rarity = SkinRarity.Common,
                    unlockLevel = 5,
                    unlockCost = 100,
                    isSeasonal = false
                });
            }

            // ============================================================
            // THEMED SKINS (Rare - Texture + Accessory)
            // ============================================================
            var themedSkins = new (string id, string name, string desc)[]
            {
                ("tournament", "Tournament Pro", "Clean competition attire with sponsor patches"),
                ("casual", "Casual Friday", "Relaxed weekend wear"),
            };

            foreach (var ts in themedSkins)
            {
                list.Add(new SkinTemplate
                {
                    skinId = $"{charId}_{ts.id}",
                    displayName = ts.name,
                    description = ts.desc,
                    rarity = SkinRarity.Rare,
                    unlockLevel = 15,
                    unlockCost = 500,
                    isSeasonal = false
                });
            }

            // ============================================================
            // SEASONAL / CULTURAL SKINS (Epic - New outfit mesh + VFX)
            // Character-specific themes
            // ============================================================
            var seasonalThemes = GetSeasonalThemes(charId);
            foreach (var theme in seasonalThemes)
            {
                list.Add(new SkinTemplate
                {
                    skinId = $"{charId}_{theme.eventType.ToString().ToLower()}",
                    displayName = theme.displayName,
                    description = theme.description,
                    rarity = SkinRarity.Epic,
                    unlockLevel = 30,
                    unlockCost = 2000,
                    isSeasonal = true,
                    eventType = theme.eventType
                });
            }

            // ============================================================
            // LEGENDARY SKINS (Full remodel + Custom anim + Voice)
            // ============================================================
            list.Add(new SkinTemplate
            {
                skinId = $"{charId}_world_champion",
                displayName = "World Champion",
                description = "Exclusive champion skin with custom animations and voice lines",
                rarity = SkinRarity.Legendary,
                unlockLevel = 50,
                unlockCost = 10000,
                isSeasonal = false
            });

            // Dev exclusive legendary
            list.Add(new SkinTemplate
            {
                skinId = $"{charId}_dev_exclusive",
                displayName = "Developer Edition",
                description = "Exclusive dev-only skin with unique effects",
                rarity = SkinRarity.Legendary,
                unlockLevel = 99,
                unlockCost = 0,
                isSeasonal = true,
                eventType = SeasonalEvent.DevExclusive
            });

            return list.ToArray();
        }

        private static (SeasonalEvent eventType, string displayName, string description)[] GetSeasonalThemes(string charId)
        {
            // Character-specific seasonal themes
            var themes = new Dictionary<string, (SeasonalEvent, string, string)[]>
            {
                ["somchay"] = new[]
                {
                    (SeasonalEvent.Songkran, "Songkran Splash", "Celebrate Thai New Year with water festival gear"),
                    (SeasonalEvent.Summer, "Beach Vibes", "Tropical shirt, sunglasses, and flip-flops")
                },
                ["meiling"] = new[]
                {
                    (SeasonalEvent.LunarNewYear, "Lunar Elegance", "Traditional red qipao with gold embroidery"),
                    (SeasonalEvent.Spring, "Cherry Blossom", "Pink sakura-themed outfit with falling petals VFX")
                },
                ["gentleman"] = new[]
                {
                    (SeasonalEvent.Christmas, "Victorian Christmas", "Tailcoat with holly lapel, pocket watch shows 12:25"),
                    (SeasonalEvent.Anniversary, "Founder's Tailcoat", "Original designer suit with golden cufflinks")
                },
                ["panpan"] = new[]
                {
                    (SeasonalEvent.Halloween, "Neon Ghost", "Glow-in-the-dark streetwear with spectral trail VFX"),
                    (SeasonalEvent.Summer, "Street Festival", "Tank top, shorts, skateboard accessory")
                },
                ["finn"] = new[]
                {
                    (SeasonalEvent.Winter, "Cozy Hoodie", "Oversized knit hoodie, steam breath VFX"),
                    (SeasonalEvent.Anniversary, "Lo-Fi Anniversary", "Headphones with equalizer VFX, cassette tape charm")
                },
                ["kingflex"] = new[]
                {
                    (SeasonalEvent.Summer, "Gold Summer", "Shirtless with gold body paint, dollar sign chains"),
                    (SeasonalEvent.Halloween, "Spooky Flex", "Skeleton suit with glowing bones, pumpkin prop")
                },
                ["tusker"] = new[]
                {
                    (SeasonalEvent.Christmas, "Santa's Helper", "Red/green vest, jingle bell collar, candy cane"),
                    (SeasonalEvent.LunarNewYear, "Lucky Elephant", "Red/gold ceremonial blanket, fortune charms")
                },
                ["phantom"] = new[]
                {
                    (SeasonalEvent.Halloween, "True Phantom", "Fully translucent model, spectral shimmer VFX"),
                    (SeasonalEvent.DevExclusive, "Shadow Dev", "Dev hoodie, commits floating particles")
                },
                ["cassidy"] = new[]
                {
                    (SeasonalEvent.Summer, "Desert Bloom", "Floral duster, cactus flower hat band"),
                    (SeasonalEvent.Halloween, "Grim Reaper", "Tattered black cloak, scythe cue, skull VFX")
                },
                ["bones"] = new[]
                {
                    (SeasonalEvent.Halloween, "Bone King", "Crown of skulls, throne particles, royal cape"),
                    (SeasonalEvent.Christmas, "Skeleton Santa", "Santa hat on skull, gift sack, jingle VFX")
                },
                ["bopanda"] = new[]
                {
                    (SeasonalEvent.Songkran, "Water Festival Panda", "Bamboo water gun, floral lei, splash VFX"),
                    (SeasonalEvent.Christmas, "Santa Panda", "Santa suit, gift bag, 'Ho ho ho... bamboo!' voice")
                },
                ["unclenok"] = new[]
                {
                    (SeasonalEvent.Anniversary, "Golden Judge", "Gold-plated bowler, gavel with particle trail"),
                    (SeasonalEvent.DevExclusive, "Dev Referee", "Unity logo shirt, 'Bug Fixed' stamp prop")
                }
            };

            if (themes.TryGetValue(charId, out var charThemes))
            {
                return charThemes;
            }

            // Fallback generic themes
            return new[]
            {
                (SeasonalEvent.Halloween, "Halloween Special", "Spooky seasonal variant"),
                (SeasonalEvent.Christmas, "Winter Festive", "Holiday seasonal variant")
            };
        }

        [MenuItem(MENU_PATH, validate = true)]
        public static bool ValidateCreateAllSkinData()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private class SkinTemplate
        {
            public string skinId;
            public string displayName;
            public string description;
            public SkinRarity rarity;
            public int unlockLevel;
            public int unlockCost;
            public bool isSeasonal;
            public SeasonalEvent eventType;
        }
    }

    // Extension method for string title case
    public static class StringExtensions
    {
        public static string ToTitleCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }
    }
}