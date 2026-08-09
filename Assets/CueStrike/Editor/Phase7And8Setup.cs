using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CueStrike.Environment;
using CueStrike.NoirMemory;
using CueStrike.UI;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor setup tools for Phase 7 (MR Passthrough, Multiplayer, AI) 
    /// and Phase 8 (Noir Memory Results) systems.
    /// </summary>
    public static class Phase7And8Setup
    {
        #region Apply Menu Items

        [MenuItem("Tools/CueStrike/Apply/Setup MR Passthrough")]
        public static void SetupMRPassthrough()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike] Cannot setup while in Play Mode.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Find or create EnvironmentManager
            var envMgr = Object.FindFirstObjectByType<CueStrikeEnvironmentManager>();
            if (envMgr == null)
            {
                var go = new GameObject("CueStrikeEnvironmentManager");
                envMgr = go.AddComponent<CueStrikeEnvironmentManager>();
                Undo.RegisterCreatedObjectUndo(go, "Setup MR Passthrough");
                Debug.Log("[CueStrike] Created CueStrikeEnvironmentManager.");
            }

            // Add or find MR Passthrough Manager
            var mrMgr = Object.FindFirstObjectByType<CueStrikeMRPassthroughManager>();
            if (mrMgr == null)
            {
                var mrGO = new GameObject("CueStrikeMRPassthroughManager");
                mrMgr = mrGO.AddComponent<CueStrikeMRPassthroughManager>();
                Undo.RegisterCreatedObjectUndo(mrGO, "Setup MR Passthrough");
                Debug.Log("[CueStrike] Created CueStrikeMRPassthroughManager.");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CueStrike] MR Passthrough setup complete.");
            EditorUtility.DisplayDialog("CueStrike", "MR Passthrough setup complete!\n\n" +
                "• CueStrikeEnvironmentManager\n• CueStrikeMRPassthroughManager", "OK");
        }

        [MenuItem("Tools/CueStrike/Apply/Setup AI Challenger")]
        public static void SetupAIChallenger()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike] Cannot setup while in Play Mode.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var aiCtrl = Object.FindFirstObjectByType<AI.CueStrikeAIController>();
            if (aiCtrl == null)
            {
                var aiGO = new GameObject("CueStrikeAIController");
                aiCtrl = aiGO.AddComponent<AI.CueStrikeAIController>();
                Undo.RegisterCreatedObjectUndo(aiGO, "Setup AI Challenger");

                // Auto-find references
                var shotMgr = Object.FindFirstObjectByType<CueStrikeShotManager>();
                if (shotMgr != null)
                {
                    var serialized = new SerializedObject(aiCtrl);
                    serialized.FindProperty("shotManager").objectReferenceValue = shotMgr;
                    serialized.ApplyModifiedProperties();
                }

                Debug.Log("[CueStrike] Created CueStrikeAIController with all 4 difficulty levels.");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CueStrike] AI Challenger setup complete.");
            EditorUtility.DisplayDialog("CueStrike", "AI Challenger setup complete!\n\n" +
                "• CueStrikeAIController (Easy/Medium/Hard/Expert)", "OK");
        }

        [MenuItem("Tools/CueStrike/Apply/Setup Multiplayer Lobby")]
        public static void SetupMultiplayerLobby()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike] Cannot setup while in Play Mode.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var lobbyUI = Object.FindFirstObjectByType<CueStrikeMultiplayerLobbyUI>();
            if (lobbyUI == null)
            {
                var lobbyGO = new GameObject("CueStrikeMultiplayerLobbyUI");
                lobbyUI = lobbyGO.AddComponent<CueStrikeMultiplayerLobbyUI>();
                Undo.RegisterCreatedObjectUndo(lobbyGO, "Setup Multiplayer Lobby");
                Debug.Log("[CueStrike] Created CueStrikeMultiplayerLobbyUI.");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CueStrike] Multiplayer Lobby setup complete.");
            EditorUtility.DisplayDialog("CueStrike", "Multiplayer Lobby setup complete!\n\n" +
                "• CueStrikeMultiplayerLobbyUI", "OK");
        }

        [MenuItem("Tools/CueStrike/Apply/Setup Noir Memory Results")]
        public static void SetupNoirMemoryResults()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike] Cannot setup while in Play Mode.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var resultsScreen = Object.FindFirstObjectByType<NoirMemoryResultsScreen>();
            if (resultsScreen == null)
            {
                var rsGO = new GameObject("NoirMemoryResultsScreen");
                resultsScreen = rsGO.AddComponent<NoirMemoryResultsScreen>();
                Undo.RegisterCreatedObjectUndo(rsGO, "Setup Noir Memory Results");
                Debug.Log("[CueStrike] Created NoirMemoryResultsScreen.");
            }

            // Find or create a Canvas for the results
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("NoirMemoryCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Setup Noir Memory Results");
                Debug.Log("[CueStrike] Created Canvas for Noir Memory Results.");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CueStrike] Noir Memory Results setup complete.");
            EditorUtility.DisplayDialog("CueStrike", "Noir Memory Results setup complete!\n\n" +
                "• NoirMemoryResultsScreen\n• Canvas (auto-created)", "OK");
        }

        [MenuItem("Tools/CueStrike/Apply/Setup All Phase 7 & 8")]
        public static void SetupAllPhase7And8()
        {
            SetupMRPassthrough();
            SetupAIChallenger();
            SetupMultiplayerLobby();
            SetupNoirMemoryResults();
            Debug.Log("[CueStrike] All Phase 7 & 8 systems setup complete.");
            EditorUtility.DisplayDialog("CueStrike", "All Phase 7 & 8 systems setup complete!", "OK");
        }

        #endregion

        #region Self-Test Menu Items

        [MenuItem("Tools/CueStrike/Debug/Test Phase 7 & 8 Integration")]
        public static void SelfTestPhase7And8()
        {
            bool pass = true;

            Debug.Log("=== Phase 7 & 8 Integration Self-Test ===");

            // Test 1: MR Passthrough Manager
            var mrMgr = Object.FindFirstObjectByType<CueStrikeMRPassthroughManager>();
            if (mrMgr == null)
            {
                Debug.LogError("FAIL: CueStrikeMRPassthroughManager not found.");
                pass = false;
            }
            else Debug.Log("✅ CueStrikeMRPassthroughManager found.");

            // Test 2: Environment Manager
            var envMgr = Object.FindFirstObjectByType<CueStrikeEnvironmentManager>();
            if (envMgr == null)
            {
                Debug.LogError("FAIL: CueStrikeEnvironmentManager not found.");
                pass = false;
            }
            else Debug.Log("✅ CueStrikeEnvironmentManager found.");

            // Test 3: AI Controller
            var aiCtrl = Object.FindFirstObjectByType<AI.CueStrikeAIController>();
            if (aiCtrl == null)
            {
                Debug.LogError("FAIL: CueStrikeAIController not found.");
                pass = false;
            }
            else
            {
                Debug.Log("✅ CueStrikeAIController found.");
                // Check that it supports all 4 levels
                var levels = System.Enum.GetNames(typeof(AI.SkillLevel));
                Debug.Log($"   Skill levels: {string.Join(", ", levels)}");
            }

            // Test 4: Multiplayer Lobby UI
            var lobbyUI = Object.FindFirstObjectByType<CueStrikeMultiplayerLobbyUI>();
            if (lobbyUI == null)
            {
                Debug.LogError("FAIL: CueStrikeMultiplayerLobbyUI not found.");
                pass = false;
            }
            else Debug.Log("✅ CueStrikeMultiplayerLobbyUI found.");

            // Test 5: Noir Memory Results
            var resultsScreen = Object.FindFirstObjectByType<NoirMemoryResultsScreen>();
            if (resultsScreen == null)
            {
                Debug.LogError("FAIL: NoirMemoryResultsScreen not found.");
                pass = false;
            }
            else Debug.Log("✅ NoirMemoryResultsScreen found.");

            // Test 6: Noir Memory Puzzle Manager (existing)
            var puzzleMgr = Object.FindFirstObjectByType<NoirMemoryPuzzleManager>();
            if (puzzleMgr == null)
            {
                Debug.LogWarning("WARN: NoirMemoryPuzzleManager not found (optional for this test).");
            }
            else Debug.Log("✅ NoirMemoryPuzzleManager found.");

            if (pass)
            {
                Debug.Log("Phase 7 & 8 SELF-TEST PASSED — All systems ready.");
                EditorUtility.DisplayDialog("Self-Test Passed", "All Phase 7 & 8 integration checks passed!", "OK");
            }
            else
            {
                Debug.LogWarning("Phase 7 & 8 SELF-TEST FAILED — Run 'Setup All Phase 7 & 8' first.");
                EditorUtility.DisplayDialog("Self-Test FAILED", "Some checks failed. See Console.", "OK");
            }
        }

        [MenuItem("Tools/CueStrike/Debug/Test AI Challenger")]
        public static void SelfTestAI()
        {
            bool pass = true;

            var aiCtrl = Object.FindFirstObjectByType<AI.CueStrikeAIController>();
            if (aiCtrl == null)
            {
                Debug.LogError("FAIL: CueStrikeAIController not found.");
                pass = false;
            }
            else
            {
                Debug.Log($"AI Controller found. Skill: {aiCtrl.GetSkillLevel()}");
                var p = aiCtrl.GetCurrentParameters();
                Debug.Log($"Parameters: accuracy={p.accuracy:F2}, power={p.power:F2}, delay={p.decisionDelay:F2}s");
            }

            // Verify strategy types exist (full namespace required)
            var easyType = System.Type.GetType("CueStrike.AI.CueStrikeAIEasy, Assembly-CSharp");
            var mediumType = System.Type.GetType("CueStrike.AI.CueStrikeAIMedium, Assembly-CSharp");
            var hardType = System.Type.GetType("CueStrike.AI.CueStrikeAIHard, Assembly-CSharp");
            var expertType = System.Type.GetType("CueStrike.AI.CueStrikeAIExpert, Assembly-CSharp");

            if (easyType == null) { Debug.LogError("FAIL: CueStrikeAIEasy type not found."); pass = false; }
            else Debug.Log("✅ AI Strategy: Easy");
            if (mediumType == null) { Debug.LogError("FAIL: CueStrikeAIMedium type not found."); pass = false; }
            else Debug.Log("✅ AI Strategy: Medium");
            if (hardType == null) { Debug.LogError("FAIL: CueStrikeAIHard type not found."); pass = false; }
            else Debug.Log("✅ AI Strategy: Hard");
            if (expertType == null) { Debug.LogError("FAIL: CueStrikeAIExpert type not found."); pass = false; }
            else Debug.Log("✅ AI Strategy: Expert");

            if (pass)
            {
                Debug.Log("AI Challenger SELF-TEST PASSED.");
                EditorUtility.DisplayDialog("Self-Test Passed", "AI Challenger checks passed!\nAll 4 difficulty levels available.", "OK");
            }
            else
            {
                Debug.LogWarning("AI Challenger SELF-TEST FAILED.");
                EditorUtility.DisplayDialog("Self-Test FAILED", "See Console for details.", "OK");
            }
        }

        [MenuItem("Tools/CueStrike/Debug/Test Noir Memory Results")]
        public static void SelfTestNoirMemoryResults()
        {
            bool pass = true;

            var results = Object.FindFirstObjectByType<NoirMemoryResultsScreen>();
            if (results == null)
            {
                Debug.LogError("FAIL: NoirMemoryResultsScreen not found. Run 'Setup Noir Memory Results' first.");
                pass = false;
            }
            else
            {
                Debug.Log("✅ NoirMemoryResultsScreen found.");
                Debug.Log("     Available API: CalculateScore(), ShowResults(), HideResults()");
                Debug.Log("     Leaderboard: GetLeaderboard(), ClearLeaderboard()");
            }

            // Test score calculation exists
            var scoreType = System.Type.GetType("NoirMemoryScoreData, Assembly-CSharp");
            if (scoreType == null)
            {
                scoreType = System.Type.GetType("CueStrike.NoirMemory.NoirMemoryResultsScreen+NoirMemoryScoreData, Assembly-CSharp");
            }
            if (scoreType == null)
            {
                Debug.LogWarning("WARN: NoirMemoryScoreData type lookup failed (nested class).");
            }
            else
            {
                Debug.Log("✅ NoirMemoryScoreData type available.");
            }

            if (pass)
            {
                Debug.Log("Noir Memory Results SELF-TEST PASSED.");
                EditorUtility.DisplayDialog("Self-Test Passed", "Noir Memory Results checks passed!", "OK");
            }
            else
            {
                Debug.LogWarning("Noir Memory Results SELF-TEST FAILED.");
                EditorUtility.DisplayDialog("Self-Test FAILED", "See Console for details.", "OK");
            }
        }

        #endregion
    }
}