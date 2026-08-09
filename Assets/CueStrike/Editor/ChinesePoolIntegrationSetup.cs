using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CueStrike.Gameplay.Rules;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor setup tool for Chinese Pool integration with WPA Rules Manager.
    /// Adds Apply menu item + Self-Test as required by CUESTRIKE_MASTER.md §5.
    /// </summary>
    public static class ChinesePoolIntegrationSetup
    {
        #region Apply Menu Items

        [MenuItem("Tools/CueStrike/Apply/Setup Chinese Pool")]
        public static void SetupChinesePoolIntegration()
        {
            // GUARD: Play Mode
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike] Cannot setup Chinese Pool integration while in Play Mode. Stop the game first.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            // GUARD: Unsaved changes
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[CueStrike] Setup cancelled by user (unsaved changes).");
                return;
            }

            // Find or create WPARulesManager
            var wpaMgr = Object.FindFirstObjectByType<CueStrikeWPARulesManager>();
            if (wpaMgr == null)
            {
                var go = new GameObject("CueStrikeWPARulesManager");
                wpaMgr = go.AddComponent<CueStrikeWPARulesManager>();
                Undo.RegisterCreatedObjectUndo(go, "Setup Chinese Pool");
                Debug.Log("[CueStrike] Created CueStrikeWPARulesManager (auto-adds ChinesePoolRuleset).");
            }

            // Ensure ChinesePoolRuleset component exists on it
            var cpRuleset = wpaMgr.GetComponent<CueStrikeChinesePoolRuleset>();
            if (cpRuleset == null)
            {
                cpRuleset = Undo.AddComponent<CueStrikeChinesePoolRuleset>(wpaMgr.gameObject);
                Debug.Log("[CueStrike] Added CueStrikeChinesePoolRuleset to WPARulesManager.");
            }

            // Find or create ChinesePoolGameManager
            var cpMgr = Object.FindFirstObjectByType<ChinesePoolGameManager>();
            if (cpMgr == null)
            {
                var cpGO = new GameObject("ChinesePoolGameManager");
                cpMgr = cpGO.AddComponent<ChinesePoolGameManager>();
                Undo.RegisterCreatedObjectUndo(cpGO, "Setup Chinese Pool");
                Debug.Log("[CueStrike] Created ChinesePoolGameManager.");
            }

            // Auto-wire ball setup if exists
            if (cpMgr.ballSetup == null)
            {
                cpMgr.ballSetup = Object.FindFirstObjectByType<ChinesePoolBallSetup>();
                if (cpMgr.ballSetup == null)
                    Debug.LogWarning("[CueStrike] ChinesePoolBallSetup not found in scene. Assign manually.");
            }

            // Scene dirty marking
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CueStrike] Chinese Pool integration setup complete. Mode = ChinesePool now available in WPARulesManager.");
            EditorUtility.DisplayDialog("CueStrike", "Chinese Pool integration setup complete!\n\n" +
                "Created/Wired:\n" +
                "• CueStrikeChinesePoolRuleset\n" +
                "• ChinesePoolGameManager\n\n" +
                "GameMode.ChinesePool is now selectable.", "OK");
        }

        [MenuItem("Tools/CueStrike/Debug/Test ChinesePool Integration")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test 1: WPARulesManager exists
            var wpaMgr = Object.FindFirstObjectByType<CueStrikeWPARulesManager>();
            if (wpaMgr == null)
            {
                Debug.LogError("FAIL: CueStrikeWPARulesManager not found in scene. Run 'Setup Chinese Pool' first.");
                pass = false;
            }
            else
            {
                Debug.Log("[SelfTest] WPARulesManager found.");
            }

            // Test 2: ChinesePoolRuleset component exists
            if (wpaMgr != null)
            {
                var cpRuleset = wpaMgr.GetComponent<CueStrikeChinesePoolRuleset>();
                if (cpRuleset == null)
                {
                    Debug.LogError("FAIL: CueStrikeChinesePoolRuleset not found on WPARulesManager.");
                    pass = false;
                }
                else
                {
                    Debug.Log("[SelfTest] CueStrikeChinesePoolRuleset found on WPARulesManager.");
                }
            }

            // Test 3: ChinesePoolGameManager exists
            var cpMgr = Object.FindFirstObjectByType<ChinesePoolGameManager>();
            if (cpMgr == null)
            {
                Debug.LogError("FAIL: ChinesePoolGameManager not found in scene.");
                pass = false;
            }
            else
            {
                Debug.Log("[SelfTest] ChinesePoolGameManager found.");
            }

            // Test 4: GameMode enum contains ChinesePool
            var modes = System.Enum.GetNames(typeof(CueStrikeWPARulesManager.GameMode));
            bool hasChinesePool = System.Array.IndexOf(modes, "ChinesePool") >= 0;
            if (!hasChinesePool)
            {
                Debug.LogError("FAIL: GameMode enum missing 'ChinesePool' value.");
                pass = false;
            }
            else
            {
                Debug.Log("[SelfTest] GameMode.ChinesePool is available.");
            }

            // Test 5: Mode switching works (compile-time check via types)
            // Just log the available modes
            Debug.Log($"[SelfTest] Available game modes: {string.Join(", ", modes)}");

            if (pass)
            {
                Debug.Log("ChinesePool Integration SELF-TEST PASSED — Ready for human verify.");
                EditorUtility.DisplayDialog("Self-Test Passed", "All Chinese Pool integration checks passed!", "OK");
            }
            else
            {
                Debug.LogWarning("ChinesePool Integration SELF-TEST FAILED — See errors above.");
                EditorUtility.DisplayDialog("Self-Test FAILED", "Some checks failed. See Console for details.", "OK");
            }
        }

        #endregion
    }
}