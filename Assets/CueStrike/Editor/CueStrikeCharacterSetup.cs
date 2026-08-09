using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor tool: Setup Complete Character System with one click.
    /// Creates CharacterManager, 10 CharacterData ScriptableObjects, and Selector UI.
    /// </summary>
    public static class CueStrikeCharacterSetup
    {
        #region Menu Items
        [MenuItem("Tools/CueStrike/Apply/Setup Character System")]
        public static void SetupCharacterSystem()
        {
            if (!RunGuards()) return;

            Debug.Log("[CueStrikeCharacterSetup] === Starting Character System Setup ===");

            try
            {
                // 1. Create Character Manager
                GameObject charMgr = CreateCharacterManager();
                Undo.RegisterCreatedObjectUndo(charMgr, "Create Character Manager");

                // 2. Create Character Data ScriptableObjects
                List<Characters.CueStrikeCharacterData> characterDataList = CreateAllCharacterData();

                // 3. Assign to manager
                var managerComp = charMgr.GetComponent<Characters.CueStrikeCharacterManager>();
                if (managerComp != null)
                {
                    var soField = managerComp.GetType().GetField("_allCharacters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    soField?.SetValue(managerComp, characterDataList);
                    Debug.Log($"[Setup] Assigned {characterDataList.Count} characters to manager.");
                }

                // 4. Create Character Selector UI
                GameObject selectorUI = CreateCharacterSelectorUI();
                Undo.RegisterCreatedObjectUndo(selectorUI, "Create Character Selector UI");

                // 5. Mark scene dirty
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                Debug.Log("[CueStrikeCharacterSetup] === Setup Complete ===");
                Debug.Log($"[CueStrikeCharacterSetup] Created: CharacterManager, {characterDataList.Count} CharacterData assets, Selector UI");
                Debug.Log("[CueStrikeCharacterSetup] Next: Assign portraits, prefabs, and materials in Inspector.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CueStrikeCharacterSetup] Setup failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Setup Failed", ex.Message, "OK");
            }
        }

        /// <summary>
        /// Creates/updates the 10 playable CharacterData assets WITHOUT touching scenes
        /// (batch-safe — no scene operations, no dialogs).
        /// </summary>
        public static List<Characters.CueStrikeCharacterData> CreateCharacterDataAssets()
        {
            return CreateAllCharacterData();
        }

        /// <summary>
        /// Batchmode-safe entry point: creates/updates all CharacterData assets.
        /// Run via: Unity -batchmode -executeMethod CueStrike.Editor.CueStrikeCharacterSetup.ApplyCharacterRosterBatch
        /// </summary>
        public static void ApplyCharacterRosterBatch()
        {
            Debug.Log("[CueStrikeCharacterSetup] === Batch: Applying Character Roster ===");
            try
            {
                var characterDataList = CreateAllCharacterData();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[CueStrikeCharacterSetup] === Batch Complete: {characterDataList.Count} CharacterData assets ready ===");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CueStrikeCharacterSetup] Batch failed: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/CueStrike/Debug/Test All Character Abilities")]
        public static void TestAllCharacterAbilities()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Play Mode Blocked", "Exit Play Mode before running self-test.", "OK");
                return;
            }

            Debug.Log("[CueStrikeCharacterSetup] === Character Self-Test Started ===");
            bool allPass = true;
            int passCount = 0;
            int failCount = 0;

            // Test 1: Character Manager
            var manager = UnityEngine.Object.FindFirstObjectByType<Characters.CueStrikeCharacterManager>();
            if (manager != null && manager.RunSelfTest()) { passCount++; } else { failCount++; allPass = false; }

            // Test 2: Selector UI
            var selector = UnityEngine.Object.FindFirstObjectByType<UI.CueStrikeCharacterSelectorUI>();
            if (selector != null && selector.RunSelfTest()) { passCount++; } else { failCount++; allPass = false; }

            // Test 3: Test each ability type exists (REAL class names — see CHARACTER_SYSTEM_PLAN.md)
            string[] abilityTypes = new string[]
            {
                "CueStrike.Characters.Somchay.SomchayAbilityController",
                "CueStrike.Characters.MeiLing.MeiLingAbilityController",
                "CueStrike.Characters.Gentleman.GentlemanAbilityController",
                "CueStrike.Characters.BoPanda.BoPandaHypeEngine",
                "CueStrike.Characters.Finn.FinnAquaRush",
                "CueStrike.Characters.KingFlex.KingFlexBlingBling",
                "CueStrike.Characters.Tusker.TuskerGentlemansMemory",
                "CueStrike.Characters.PanPan.PanPanZenStance",
                "CueStrike.Characters.Phantom.PhantomSpectralSight",
                "CueStrike.Characters.Cassidy.CassidyQuickDraw",
                "CueStrike.Characters.Bones.BonesXRayVision"
            };

            foreach (string typeName in abilityTypes)
            {
                Type t = Type.GetType(typeName);
                if (t != null)
                {
                    Debug.Log($"[Self-Test] Ability type found: {typeName}");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"[Self-Test] Ability type MISSING: {typeName}");
                    failCount++;
                    allPass = false;
                }
            }

            Debug.Log($"[CueStrikeCharacterSetup] === Result: {passCount} PASS, {failCount} FAIL ===");
            EditorUtility.DisplayDialog(
                "Character Self-Test",
                allPass ? $"All tests passed! ({passCount} total)" : $"Some tests failed. ({passCount}/{passCount + failCount})\nCheck Console.",
                "OK"
            );
        }
        #endregion

        #region Guards
        private static bool RunGuards()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Blocked", "Exit Play Mode first.", "OK");
                return false;
            }
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                bool save = EditorUtility.DisplayDialog("Unsaved Changes", "Save before setup?", "Save", "Cancel");
                if (!save) return false;
                EditorSceneManager.SaveOpenScenes();
            }
            return true;
        }
        #endregion

        #region Creation
        private static GameObject CreateCharacterManager()
        {
            const string name = "CueStrike_CharacterManager";
            GameObject existing = GameObject.Find(name);
            if (existing != null) return existing;

            GameObject obj = new GameObject(name);
            obj.AddComponent<Characters.CueStrikeCharacterManager>();
            Debug.Log("[Setup] Created CharacterManager.");
            return obj;
        }

        private static List<Characters.CueStrikeCharacterData> CreateAllCharacterData()
        {
            var list = new List<Characters.CueStrikeCharacterData>();

            // Clean up legacy assets that no longer match the roster (Bo = mascot, not playable; PanPan replaces Pandy)
            string[] legacyIds = { "pandy", "bopanda" };
            foreach (var legacyId in legacyIds)
            {
                string legacyPath = $"Assets/CueStrike/Config/Characters/Character_{legacyId}.asset";
                if (AssetDatabase.LoadAssetAtPath<Characters.CueStrikeCharacterData>(legacyPath) != null)
                {
                    AssetDatabase.DeleteAsset(legacyPath);
                    Debug.Log($"[Setup] Removed legacy character asset: {legacyId}");
                }
            }

            // Roster per CHARACTER_SYSTEM_PLAN.md: 10 playable characters (Bo = mascot, not playable)
            string[] ids = new string[] { "somchay", "meiling", "gentleman", "finn", "kingflex", "tusker", "panpan", "phantom", "cassidy", "bones" };
            string[] names = new string[] { "Somchay", "MeiLing", "Gentleman", "Finn", "King Flex", "Tusker", "PanPan", "Phantom", "Cassidy", "Bones" };
            // REAL ability class names (per CHARACTER_SYSTEM_PLAN.md + existing code).
            // NOTE: The old "Ability_*" names no longer exist — using them would create
            // CharacterData assets with missing script references.
            string[] abilities = new string[]
            {
                "CueStrike.Characters.Somchay.SomchayAbilityController",
                "CueStrike.Characters.MeiLing.MeiLingAbilityController",
                "CueStrike.Characters.Gentleman.GentlemanAbilityController",
                "CueStrike.Characters.Finn.FinnAquaRush",
                "CueStrike.Characters.KingFlex.KingFlexBlingBling",
                "CueStrike.Characters.Tusker.TuskerGentlemansMemory",
                "CueStrike.Characters.PanPan.PanPanZenStance",
                "CueStrike.Characters.Phantom.PhantomSpectralSight",
                "CueStrike.Characters.Cassidy.CassidyQuickDraw",
                "CueStrike.Characters.Bones.BonesXRayVision"
            };
            string[] descs = new string[]
            {
                "The veteran champion with iron discipline.",
                "Graceful precision with orbital trajectory sight.",
                "Elegant showman who shines on Century Breaks.",
                "Speed demon from the deep. Fast shots = big rewards.",
                "Rap royalty dripping in gold. Style is everything.",
                "Gentleman elephant with photographic memory.",
                "Zen master finding focus in stillness.",
                "Ghostly phantom seeing through reality.",
                "Quick-draw cowgirl with lightning reflexes.",
                "X-Ray skeleton revealing optimal paths."
            };

            for (int i = 0; i < ids.Length; i++)
            {
                string path = $"Assets/CueStrike/Config/Characters/Character_{ids[i]}.asset";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                // Re-runnable: update existing asset if present, otherwise create it.
                var data = AssetDatabase.LoadAssetAtPath<Characters.CueStrikeCharacterData>(path);
                bool isNew = data == null;
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<Characters.CueStrikeCharacterData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.characterId = ids[i];
                data.displayName = names[i];
                data.description = descs[i];
                // Display ability name = short class name (e.g. "BonesXRayVision") not full namespace
                string abilityFull = abilities[i];
                data.abilityName = abilityFull.Substring(abilityFull.LastIndexOf('.') + 1).Replace("_", " ");
                data.abilityScriptType = abilities[i];
                data.isUnlocked = true;
                EditorUtility.SetDirty(data);
                Debug.Log(isNew ? $"[Setup] Created CharacterData: {ids[i]}" : $"[Setup] Updated existing CharacterData: {ids[i]}");

                list.Add(data);
            }

            AssetDatabase.SaveAssets();
            return list;
        }

        private static GameObject CreateCharacterSelectorUI()
        {
            // Find or create canvas
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("CueStrike_UI_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                RectTransform rt = canvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(1920f, 1080f);
                rt.position = new Vector3(0f, 1.6f, 2f);
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            GameObject selector = new GameObject("CueStrike_CharacterSelector");
            selector.transform.SetParent(canvasObj.transform, false);
            selector.AddComponent<UI.CueStrikeCharacterSelectorUI>();
            selector.SetActive(false); // Hidden by default

            Debug.Log("[Setup] Created CharacterSelectorUI.");
            return selector;
        }
        #endregion
    }
}