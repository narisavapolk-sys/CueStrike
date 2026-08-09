using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CueStrike.Physics;

namespace CueStrike.Customization
{
    /// <summary>
    /// CustomizationManager - Central manager for ball, felt, and cue skin customization.
    /// Handles loading, saving, and applying skins at runtime via PlayerPrefs/JSON.
    /// </summary>
    public class CustomizationManager : MonoBehaviour
    {
        public static CustomizationManager Instance { get; private set; }

        [Header("Skin Databases (Assign in Inspector)")]
        public List<BallSkinData> ballSkins = new List<BallSkinData>();
        public List<FeltSkinData> feltSkins = new List<FeltSkinData>();
        public List<CueSkinData> cueSkins = new List<CueSkinData>();

        [Header("Current Selection (Runtime)")]
        public string currentBallSkinId = "ball_classic";
        public string currentFeltSkinId = "felt_classic_green";
        public string currentCueSkinId = "cue_classic_ash";

        [Header("Persistence")]
        public bool useJsonFile = true; // true = JSON file, false = PlayerPrefs
        public string jsonFileName = "customization_save.json";

        // Events
        public event Action<BallSkinData> OnBallSkinChanged;
        public event Action<FeltSkinData> OnFeltSkinChanged;
        public event Action<CueSkinData> OnCueSkinChanged;

        // Cache
        private Dictionary<string, BallSkinData> ballSkinLookup;
        private Dictionary<string, FeltSkinData> feltSkinLookup;
        private Dictionary<string, CueSkinData> cueSkinLookup;

        private const string PrefKeyBall = "CueStrike_CurrentBallSkin";
        private const string PrefKeyFelt = "CueStrike_CurrentFeltSkin";
        private const string PrefKeyCue = "CueStrike_CurrentCueSkin";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildLookups();
            LoadSelection();
        }

        /// <summary>
        /// Builds dictionary lookups for fast skin retrieval by ID.
        /// </summary>
        public void BuildLookups()
        {
            ballSkinLookup = new Dictionary<string, BallSkinData>();
            foreach (var skin in ballSkins)
            {
                if (!string.IsNullOrEmpty(skin.skinId))
                    ballSkinLookup[skin.skinId] = skin;
            }

            feltSkinLookup = new Dictionary<string, FeltSkinData>();
            foreach (var skin in feltSkins)
            {
                if (!string.IsNullOrEmpty(skin.skinId))
                    feltSkinLookup[skin.skinId] = skin;
            }

            cueSkinLookup = new Dictionary<string, CueSkinData>();
            foreach (var skin in cueSkins)
            {
                if (!string.IsNullOrEmpty(skin.skinId))
                    cueSkinLookup[skin.skinId] = skin;
            }
        }

        /// <summary>
        /// Loads saved skin selection from PlayerPrefs or JSON file.
        /// </summary>
        public void LoadSelection()
        {
            if (useJsonFile)
            {
                LoadFromJson();
            }
            else
            {
                LoadFromPlayerPrefs();
            }

            // Apply loaded skins
            ApplyBallSkin(currentBallSkinId);
            ApplyFeltSkin(currentFeltSkinId);
            ApplyCueSkin(currentCueSkinId);
        }

        /// <summary>
        /// Saves current skin selection to PlayerPrefs or JSON file.
        /// </summary>
        public void SaveSelection()
        {
            if (useJsonFile)
            {
                SaveToJson();
            }
            else
            {
                SaveToPlayerPrefs();
            }
        }

        // ===== BALL SKIN =====

        /// <summary>
        /// Sets the active ball skin by ID.
        /// </summary>
        public void SetBallSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId))
            {
                Debug.LogError("[CustomizationManager] Ball skin ID is null or empty");
                return;
            }

            if (!ballSkinLookup.ContainsKey(skinId))
            {
                Debug.LogError($"[CustomizationManager] Ball skin '{skinId}' not found in database");
                return;
            }

            var skin = ballSkinLookup[skinId];
            if (!skin.isUnlocked)
            {
                Debug.LogWarning($"[CustomizationManager] Ball skin '{skinId}' is locked");
                return;
            }

            currentBallSkinId = skinId;
            ApplyBallSkin(skinId);
            SaveSelection();
            Debug.Log($"[CustomizationManager] Ball skin changed to: {skin.skinName} ({skinId})");
        }

        /// <summary>
        /// Applies ball skin to all ball renderers in scene.
        /// </summary>
        public void ApplyBallSkin(string skinId)
        {
            if (!ballSkinLookup.TryGetValue(skinId, out var skin))
            {
                Debug.LogError($"[CustomizationManager] Cannot apply ball skin '{skinId}' - not found");
                return;
            }

            Material ballMat = skin.CreateMaterial();
            Material numberMat = skin.CreateNumberMaterial();

            // Find all ball renderers (tagged "Ball" or with Ball component)
            var ballRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int appliedCount = 0;

            foreach (var renderer in ballRenderers)
            {
                if (renderer.CompareTag("Ball") || renderer.name.Contains("Ball"))
                {
                    // Apply main ball material
                    var mats = renderer.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i].name.Contains("Ball") || mats[i].name.Contains("ball") || i == 0)
                        {
                            mats[i] = ballMat;
                        }
                        // Number decal material (usually second material)
                        else if (mats[i].name.Contains("Number") || mats[i].name.Contains("number") || i == 1)
                        {
                            mats[i] = numberMat;
                        }
                    }
                    renderer.materials = mats;
                    appliedCount++;
                }
            }

            OnBallSkinChanged?.Invoke(skin);
            Debug.Log($"[CustomizationManager] Applied ball skin '{skin.skinName}' to {appliedCount} ball renderers");
        }

        public BallSkinData GetBallSkin(string skinId)
        {
            ballSkinLookup.TryGetValue(skinId, out var skin);
            return skin;
        }

        public BallSkinData GetCurrentBallSkin()
        {
            return GetBallSkin(currentBallSkinId);
        }

        public List<BallSkinData> GetAllBallSkins()
        {
            return new List<BallSkinData>(ballSkins);
        }

        public List<BallSkinData> GetUnlockedBallSkins()
        {
            var list = new List<BallSkinData>();
            foreach (var skin in ballSkins)
            {
                if (skin.isUnlocked) list.Add(skin);
            }
            return list;
        }

        // ===== FELT SKIN =====

        /// <summary>
        /// Sets the active felt skin by ID.
        /// </summary>
        public void SetFeltSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId))
            {
                Debug.LogError("[CustomizationManager] Felt skin ID is null or empty");
                return;
            }

            if (!feltSkinLookup.ContainsKey(skinId))
            {
                Debug.LogError($"[CustomizationManager] Felt skin '{skinId}' not found in database");
                return;
            }

            var skin = feltSkinLookup[skinId];
            if (!skin.isUnlocked)
            {
                Debug.LogWarning($"[CustomizationManager] Felt skin '{skinId}' is locked");
                return;
            }

            currentFeltSkinId = skinId;
            ApplyFeltSkin(skinId);
            SaveSelection();
            Debug.Log($"[CustomizationManager] Felt skin changed to: {skin.skinName} ({skinId})");
        }

        /// <summary>
        /// Applies felt skin to table, cushions, and lines.
        /// </summary>
        public void ApplyFeltSkin(string skinId)
        {
            if (!feltSkinLookup.TryGetValue(skinId, out var skin))
            {
                Debug.LogError($"[CustomizationManager] Cannot apply felt skin '{skinId}' - not found");
                return;
            }

            Material feltMat = skin.CreateFeltMaterial();
            Material cushionMat = skin.CreateCushionMaterial();
            Material lineMat = skin.CreateLineMaterial();

            // Apply to table surface (tagged "TableFelt" or named "Felt")
            var feltRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int feltCount = 0, cushionCount = 0, lineCount = 0;

            foreach (var renderer in feltRenderers)
            {
                if (renderer.CompareTag("TableFelt") || renderer.name.Contains("Felt") || renderer.name.Contains("Cloth"))
                {
                    renderer.material = feltMat;
                    feltCount++;
                }
                else if (renderer.CompareTag("Cushion") || renderer.name.Contains("Cushion") || renderer.name.Contains("Rail"))
                {
                    renderer.material = cushionMat;
                    cushionCount++;
                }
                else if (renderer.CompareTag("TableLine") || renderer.name.Contains("Line") || renderer.name.Contains("Mark"))
                {
                    renderer.material = lineMat;
                    lineCount++;
                }
            }

            // Apply physics parameters
            var (friction, rollSpeed) = skin.GetPhysicsParameters();
            ApplyFeltPhysics(friction, rollSpeed);

            OnFeltSkinChanged?.Invoke(skin);
            Debug.Log($"[CustomizationManager] Applied felt skin '{skin.skinName}' - Felt: {feltCount}, Cushions: {cushionCount}, Lines: {lineCount}");
        }

        /// <summary>
        /// Applies physics parameters from felt skin to physics system.
        /// </summary>
        private void ApplyFeltPhysics(float frictionMultiplier, float rollSpeedMultiplier)
        {
            // Find and update CueStrikeFeltFriction component if exists
            var feltFriction = FindFirstObjectByType<CueStrikeFeltFriction>();
            if (feltFriction != null)
            {
                feltFriction.SetFrictionMultiplier(frictionMultiplier);
                feltFriction.SetRollSpeedMultiplier(rollSpeedMultiplier);
            }
        }

        public FeltSkinData GetFeltSkin(string skinId)
        {
            feltSkinLookup.TryGetValue(skinId, out var skin);
            return skin;
        }

        public FeltSkinData GetCurrentFeltSkin()
        {
            return GetFeltSkin(currentFeltSkinId);
        }

        public List<FeltSkinData> GetAllFeltSkins()
        {
            return new List<FeltSkinData>(feltSkins);
        }

        public List<FeltSkinData> GetUnlockedFeltSkins()
        {
            var list = new List<FeltSkinData>();
            foreach (var skin in feltSkins)
            {
                if (skin.isUnlocked) list.Add(skin);
            }
            return list;
        }

        // ===== CUE SKIN =====

        /// <summary>
        /// Sets the active cue skin by ID.
        /// </summary>
        public void SetCueSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId))
            {
                Debug.LogError("[CustomizationManager] Cue skin ID is null or empty");
                return;
            }

            if (!cueSkinLookup.ContainsKey(skinId))
            {
                Debug.LogError($"[CustomizationManager] Cue skin '{skinId}' not found in database");
                return;
            }

            var skin = cueSkinLookup[skinId];
            if (!skin.isUnlocked)
            {
                Debug.LogWarning($"[CustomizationManager] Cue skin '{skinId}' is locked");
                return;
            }

            currentCueSkinId = skinId;
            ApplyCueSkin(skinId);
            SaveSelection();
            Debug.Log($"[CustomizationManager] Cue skin changed to: {skin.skinName} ({skinId})");
        }

        /// <summary>
        /// Applies cue skin to the active cue in scene.
        /// </summary>
        public void ApplyCueSkin(string skinId)
        {
            if (!cueSkinLookup.TryGetValue(skinId, out var skin))
            {
                Debug.LogError($"[CustomizationManager] Cannot apply cue skin '{skinId}' - not found");
                return;
            }

            // Find active cue
            var cue = FindFirstObjectByType<CueStrikeCue>();
            if (cue != null)
            {
                skin.ApplyToCue(cue);
            }
            else
            {
                Debug.LogWarning("[CustomizationManager] No CueStrikeCue found in scene to apply skin");
            }

            OnCueSkinChanged?.Invoke(skin);
        }

        public CueSkinData GetCueSkin(string skinId)
        {
            cueSkinLookup.TryGetValue(skinId, out var skin);
            return skin;
        }

        public CueSkinData GetCurrentCueSkin()
        {
            return GetCueSkin(currentCueSkinId);
        }

        public List<CueSkinData> GetAllCueSkins()
        {
            return new List<CueSkinData>(cueSkins);
        }

        public List<CueSkinData> GetUnlockedCueSkins()
        {
            var list = new List<CueSkinData>();
            foreach (var skin in cueSkins)
            {
                if (skin.isUnlocked) list.Add(skin);
            }
            return list;
        }

        // ===== UNLOCK SYSTEM =====

        /// <summary>
        /// Unlocks a skin by ID (ball, felt, or cue).
        /// </summary>
        public bool UnlockSkin(string skinId)
        {
            if (ballSkinLookup.ContainsKey(skinId))
            {
                ballSkinLookup[skinId].isUnlocked = true;
                SaveSelection();
                return true;
            }
            if (feltSkinLookup.ContainsKey(skinId))
            {
                feltSkinLookup[skinId].isUnlocked = true;
                SaveSelection();
                return true;
            }
            if (cueSkinLookup.ContainsKey(skinId))
            {
                cueSkinLookup[skinId].isUnlocked = true;
                SaveSelection();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a skin is unlocked.
        /// </summary>
        public bool IsSkinUnlocked(string skinId)
        {
            if (ballSkinLookup.TryGetValue(skinId, out var ball)) return ball.isUnlocked;
            if (feltSkinLookup.TryGetValue(skinId, out var felt)) return felt.isUnlocked;
            if (cueSkinLookup.TryGetValue(skinId, out var cue)) return cue.isUnlocked;
            return false;
        }

        // ===== PERSISTENCE =====

        [Serializable]
        private class SaveData
        {
            public string ballSkinId;
            public string feltSkinId;
            public string cueSkinId;
            public List<string> unlockedBallSkins = new List<string>();
            public List<string> unlockedFeltSkins = new List<string>();
            public List<string> unlockedCueSkins = new List<string>();
        }

        private void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetString(PrefKeyBall, currentBallSkinId);
            PlayerPrefs.SetString(PrefKeyFelt, currentFeltSkinId);
            PlayerPrefs.SetString(PrefKeyCue, currentCueSkinId);

            // Save unlocked skins
            string unlockedBalls = string.Join(",", GetUnlockedIds(ballSkins));
            string unlockedFelts = string.Join(",", GetUnlockedIds(feltSkins));
            string unlockedCues = string.Join(",", GetUnlockedIds(cueSkins));

            PlayerPrefs.SetString("CueStrike_UnlockedBalls", unlockedBalls);
            PlayerPrefs.SetString("CueStrike_UnlockedFelts", unlockedFelts);
            PlayerPrefs.SetString("CueStrike_UnlockedCues", unlockedCues);

            PlayerPrefs.Save();
        }

        private void LoadFromPlayerPrefs()
        {
            currentBallSkinId = PlayerPrefs.GetString(PrefKeyBall, "ball_classic");
            currentFeltSkinId = PlayerPrefs.GetString(PrefKeyFelt, "felt_classic_green");
            currentCueSkinId = PlayerPrefs.GetString(PrefKeyCue, "cue_classic_ash");

            LoadUnlockedFromPlayerPrefs();
        }

        private void LoadUnlockedFromPlayerPrefs()
        {
            ApplyUnlockedFromString("CueStrike_UnlockedBalls", ballSkinLookup);
            ApplyUnlockedFromString("CueStrike_UnlockedFelts", feltSkinLookup);
            ApplyUnlockedFromString("CueStrike_UnlockedCues", cueSkinLookup);
        }

        private void ApplyUnlockedFromString(string prefKey, Dictionary<string, BallSkinData> lookup)
        {
            if (!PlayerPrefs.HasKey(prefKey)) return;
            string data = PlayerPrefs.GetString(prefKey);
            if (string.IsNullOrEmpty(data)) return;

            foreach (var id in data.Split(','))
            {
                if (lookup.TryGetValue(id, out var skin)) skin.isUnlocked = true;
            }
        }

        private void ApplyUnlockedFromString(string prefKey, Dictionary<string, FeltSkinData> lookup)
        {
            if (!PlayerPrefs.HasKey(prefKey)) return;
            string data = PlayerPrefs.GetString(prefKey);
            if (string.IsNullOrEmpty(data)) return;

            foreach (var id in data.Split(','))
            {
                if (lookup.TryGetValue(id, out var skin)) skin.isUnlocked = true;
            }
        }

        private void ApplyUnlockedFromString(string prefKey, Dictionary<string, CueSkinData> lookup)
        {
            if (!PlayerPrefs.HasKey(prefKey)) return;
            string data = PlayerPrefs.GetString(prefKey);
            if (string.IsNullOrEmpty(data)) return;

            foreach (var id in data.Split(','))
            {
                if (lookup.TryGetValue(id, out var skin)) skin.isUnlocked = true;
            }
        }

        private List<string> GetUnlockedIds(List<BallSkinData> skins)
        {
            var ids = new List<string>();
            foreach (var s in skins) if (s.isUnlocked) ids.Add(s.skinId);
            return ids;
        }

        private List<string> GetUnlockedIds(List<FeltSkinData> skins)
        {
            var ids = new List<string>();
            foreach (var s in skins) if (s.isUnlocked) ids.Add(s.skinId);
            return ids;
        }

        private List<string> GetUnlockedIds(List<CueSkinData> skins)
        {
            var ids = new List<string>();
            foreach (var s in skins) if (s.isUnlocked) ids.Add(s.skinId);
            return ids;
        }

        private void SaveToJson()
        {
            var data = new SaveData
            {
                ballSkinId = currentBallSkinId,
                feltSkinId = currentFeltSkinId,
                cueSkinId = currentCueSkinId,
                unlockedBallSkins = GetUnlockedIds(ballSkins),
                unlockedFeltSkins = GetUnlockedIds(feltSkins),
                unlockedCueSkins = GetUnlockedIds(cueSkins)
            };

            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, jsonFileName);
            File.WriteAllText(path, json);
        }

        private void LoadFromJson()
        {
            string path = Path.Combine(Application.persistentDataPath, jsonFileName);
            if (!File.Exists(path))
            {
                LoadFromPlayerPrefs(); // Fallback
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);

                currentBallSkinId = data.ballSkinId;
                currentFeltSkinId = data.feltSkinId;
                currentCueSkinId = data.cueSkinId;

                // Apply unlocked
                foreach (var id in data.unlockedBallSkins)
                    if (ballSkinLookup.TryGetValue(id, out var b)) b.isUnlocked = true;
                foreach (var id in data.unlockedFeltSkins)
                    if (feltSkinLookup.TryGetValue(id, out var f)) f.isUnlocked = true;
                foreach (var id in data.unlockedCueSkins)
                    if (cueSkinLookup.TryGetValue(id, out var c)) c.isUnlocked = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomizationManager] Failed to load JSON: {e.Message}");
                LoadFromPlayerPrefs();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only self-test for CustomizationManager.
        /// Run via: Tools/CueStrike/Debug/Test CustomizationManager
        /// </summary>
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test CustomizationManager")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test 1: Create instance
            var go = new GameObject("CustomizationManager_Test");
            var manager = go.AddComponent<CustomizationManager>();

            // Create test skins
            var ballSkin = ScriptableObject.CreateInstance<BallSkinData>();
            ballSkin.skinId = "ball_test";
            ballSkin.skinName = "Test Ball";
            ballSkin.isUnlocked = true;

            var feltSkin = ScriptableObject.CreateInstance<FeltSkinData>();
            feltSkin.skinId = "felt_test";
            feltSkin.skinName = "Test Felt";
            feltSkin.isUnlocked = true;

            var cueSkin = ScriptableObject.CreateInstance<CueSkinData>();
            cueSkin.skinId = "cue_test";
            cueSkin.skinName = "Test Cue";
            cueSkin.isUnlocked = true;

            manager.ballSkins.Add(ballSkin);
            manager.feltSkins.Add(feltSkin);
            manager.cueSkins.Add(cueSkin);

            manager.BuildLookups();

            // Test 2: GetBallSkin
            var retrievedBall = manager.GetBallSkin("ball_test");
            if (retrievedBall == null || retrievedBall.skinId != "ball_test")
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: GetBallSkin not working");
                pass = false;
            }

            // Test 3: GetFeltSkin
            var retrievedFelt = manager.GetFeltSkin("felt_test");
            if (retrievedFelt == null || retrievedFelt.skinId != "felt_test")
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: GetFeltSkin not working");
                pass = false;
            }

            // Test 4: GetCueSkin
            var retrievedCue = manager.GetCueSkin("cue_test");
            if (retrievedCue == null || retrievedCue.skinId != "cue_test")
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: GetCueSkin not working");
                pass = false;
            }

            // Test 5: SetBallSkin
            manager.SetBallSkin("ball_test");
            if (manager.currentBallSkinId != "ball_test")
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: SetBallSkin not working");
                pass = false;
            }

            // Test 6: SetFeltSkin
            manager.SetFeltSkin("felt_test");
            if (manager.currentFeltSkinId != "felt_test")
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: SetFeltSkin not working");
                pass = false;
            }

            // Test 7: SetCueSkin
            manager.SetCueSkin("cue_test");
            if (manager.currentCueSkinId != "cue_test")
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: SetCueSkin not working");
                pass = false;
            }

            // Test 8: Unlock system
            var lockedBall = ScriptableObject.CreateInstance<BallSkinData>();
            lockedBall.skinId = "ball_locked";
            lockedBall.skinName = "Locked Ball";
            lockedBall.isUnlocked = false;
            manager.ballSkins.Add(lockedBall);
            manager.BuildLookups();

            if (manager.IsSkinUnlocked("ball_locked"))
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: Locked skin should not be unlocked");
                pass = false;
            }

            manager.UnlockSkin("ball_locked");
            if (!manager.IsSkinUnlocked("ball_locked"))
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: UnlockSkin not working");
                pass = false;
            }

            // Test 9: Save/Load PlayerPrefs
            manager.useJsonFile = false;
            manager.SaveSelection();
            manager.LoadSelection();
            if (manager.currentBallSkinId != "ball_test")
            {
                UnityEngine.Debug.LogError("[CustomizationManager SelfTest] FAIL: PlayerPrefs save/load not working");
                pass = false;
            }

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(ballSkin);
            UnityEngine.Object.DestroyImmediate(feltSkin);
            UnityEngine.Object.DestroyImmediate(cueSkin);
            UnityEngine.Object.DestroyImmediate(lockedBall);

            if (pass)
            {
                UnityEngine.Debug.Log("[CustomizationManager SelfTest] ✅ ALL TESTS PASSED — Ready for human verify");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[CustomizationManager SelfTest] ⚠️ TESTS FAILED — Fix before proceeding");
            }
        }
#endif
    }
}