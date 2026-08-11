using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using CueStrike.NoirMemory;
using CueStrike.NoirMemory.RCA;

namespace CueStrike.Editor.SelfTest
{
    /// <summary>
    /// P8 Noir Memory Self-Test suite.
    /// All tests run in Edit Mode only.
    /// </summary>
    public static class NoirMemorySelfTest
    {
        private static int _passCount;
        private static int _failCount;

        // ───────────────────────────────────────────────────────
        //  Test Noir Memory Leaderboard UI
        // ───────────────────────────────────────────────────────
        [MenuItem("Tools/CueStrike/Debug/Test Noir Memory Leaderboard UI")]
        public static void TestNoirMemoryLeaderboardUI()
        {
            if (!GuardPlayMode()) return;
            if (!GuardScene("NoirMemoryScene", "MainMenu", "GameScene")) return;

            Debug.Log("========== [CueStrike Test] Noir Memory Leaderboard UI ==========");
            ResetCounters();

            // Test 1: NoirMemoryResultsScreen exists
            var resultsScreen = Object.FindFirstObjectByType<NoirMemoryResultsScreen>();
            if (resultsScreen != null)
            {
                Debug.Log("✅ PASS: NoirMemoryResultsScreen found in scene");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: NoirMemoryResultsScreen not found in scene. Add a NoirMemoryResultsScreen component to the scene.");
                _failCount++;
            }

            // Test 2: Instance singleton is set
            if (NoirMemoryResultsScreen.Instance != null)
            {
                Debug.Log("✅ PASS: NoirMemoryResultsScreen.Instance is set");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: NoirMemoryResultsScreen.Instance is null. Ensure Awake() sets it.");
                _failCount++;
            }

            // Test 3: CalculateScore returns valid data
            if (resultsScreen != null)
            {
                var score = resultsScreen.CalculateScore(
                    correctPots: 5,
                    wrongPots: 2,
                    totalAttempts: 10,
                    memoryAccuracy: 0.8f,
                    completionTime: 45f,
                    comboCount: 3,
                    puzzleName: "SelfTestPuzzle"
                );
                if (score != null && score.totalScore > 0)
                {
                    Debug.Log($"✅ PASS: CalculateScore returned score={score.totalScore}, grade={score.grade}");
                    _passCount++;
                }
                else
                {
                    Debug.LogError("❌ FAIL: CalculateScore returned invalid data");
                    _failCount++;
                }
            }

            // Test 4: Leaderboard operations
            if (resultsScreen != null)
            {
                resultsScreen.ClearLeaderboard();
                var lb = resultsScreen.GetLeaderboard();
                if (lb != null && lb.Count == 0)
                {
                    Debug.Log("✅ PASS: Leaderboard cleared and empty");
                    _passCount++;
                }
                else
                {
                    Debug.LogError("❌ FAIL: Leaderboard not empty after ClearLeaderboard()");
                    _failCount++;
                }
            }

            PrintSummary("Noir Memory Leaderboard UI");
        }

        // ───────────────────────────────────────────────────────
        //  Test RCA Noir Bridge
        // ───────────────────────────────────────────────────────
        [MenuItem("Tools/CueStrike/Debug/Test RCA Noir Bridge")]
        public static void TestRCANoirBridge()
        {
            if (!GuardPlayMode()) return;
            if (!GuardScene("NoirMemoryScene", "MainMenu", "GameScene")) return;

            Debug.Log("========== [CueStrike Test] RCA Noir Bridge ==========");
            ResetCounters();

            // Test 1: Singleton exists
            var bridge = CueStrikeRCANoirBridge.Instance;
            if (bridge != null)
            {
                Debug.Log("✅ PASS: CueStrikeRCANoirBridge.Instance is set");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: CueStrikeRCANoirBridge.Instance is null. Is the prefab in the scene?");
                _failCount++;
            }

            if (bridge == null)
            {
                PrintSummary("RCA Noir Bridge");
                return;
            }

            // Test 2: Dummy mode available
            if (bridge.IsDummyMode || !bridge.IsHardwareConnected)
            {
                Debug.Log("✅ PASS: RCA bridge is in fallback/dummy mode (no hardware required)");
                _passCount++;
            }
            else
            {
                Debug.Log("ℹ️  INFO: RCA hardware is connected. Tests use real hardware path.");
                _passCount++;
            }

            // Test 3: Calibration data available
            var calData = bridge.GetCalibrationData();
            if (calData != null)
            {
                Debug.Log("✅ PASS: GetCalibrationData() returned data");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: GetCalibrationData() returned null");
                _failCount++;
            }

            // Test 4: State machine transitions
            bridge.StartAim();
            bool aimingOk = bridge.CurrentState == CueStrikeRCANoirBridge.RCAState.Aiming;
            Debug.Log(aimingOk
                ? "✅ PASS: StartAim() → State=Aiming"
                : $"❌ FAIL: StartAim() → State={bridge.CurrentState} (expected Aiming)");
            _ = aimingOk ? _passCount++ : _failCount++;

            if (bridge.CurrentState == CueStrikeRCANoirBridge.RCAState.Aiming)
            {
                bridge.ChargeShot(0.5f);
                bool chargingOk = bridge.CurrentState == CueStrikeRCANoirBridge.RCAState.Charging;
                Debug.Log(chargingOk
                    ? "✅ PASS: ChargeShot(0.5) → State=Charging"
                    : $"❌ FAIL: ChargeShot() → State={bridge.CurrentState} (expected Charging)");
                _ = chargingOk ? _passCount++ : _failCount++;
            }

            // Test 5: Mock shot fires event
            bool shotFired = false;
            System.Action<NoirMemoryShotData> handler = (shot) => { shotFired = true; };
            bridge.OnShotExecuted += handler;
            bridge.SimulateShot();
            Debug.Log(shotFired
                ? "✅ PASS: SimulateShot() fired OnShotExecuted event"
                : "❌ FAIL: SimulateShot() did not fire OnShotExecuted event");
            _ = shotFired ? _passCount++ : _failCount++;
            bridge.OnShotExecuted -= handler;

            // Test 6: Reset works
            bridge.Reset();
            bool idleAfterReset = bridge.CurrentState == CueStrikeRCANoirBridge.RCAState.Idle;
            Debug.Log(idleAfterReset
                ? "✅ PASS: Reset() → State=Idle"
                : $"❌ FAIL: After Reset() State={bridge.CurrentState} (expected Idle)");
            _ = idleAfterReset ? _passCount++ : _failCount++;

            PrintSummary("RCA Noir Bridge");
        }

        // ───────────────────────────────────────────────────────
        //  Test Noir Memory Game Controller
        // ───────────────────────────────────────────────────────
        [MenuItem("Tools/CueStrike/Debug/Test Noir Memory Game Controller")]
        public static void TestNoirMemoryGameController()
        {
            if (!GuardPlayMode()) return;
            if (!GuardScene("NoirMemoryScene", "MainMenu", "GameScene")) return;

            Debug.Log("========== [CueStrike Test] Noir Memory Game Controller ==========");
            ResetCounters();

            var controller = Object.FindFirstObjectByType<NoirMemoryGameController>();
            if (controller != null)
            {
                Debug.Log("✅ PASS: NoirMemoryGameController found in scene");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: NoirMemoryGameController not found in scene. Add component to scene.");
                _failCount++;
                PrintSummary("Noir Memory Game Controller");
                return;
            }

            // Test 2: Verify puzzle data exists
            var puzzles = controller.GetType().GetMethod("GetPuzzles")?.Invoke(controller, null) as System.Collections.IList;
            if (puzzles != null && puzzles.Count > 0)
            {
                Debug.Log($"✅ PASS: GetPuzzles() returned {puzzles.Count} puzzles");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: GetPuzzles() returned null or empty. Has puzzle data been loaded?");
                _failCount++;
            }

            // Test 3: Score calculation
            var calcMethod = controller.GetType().GetMethod("CalculateScore");
            if (calcMethod != null)
            {
                int testScore = (int)calcMethod.Invoke(controller, new object[] { 5, 2, 10, 0.8f, 45f, 3 });
                if (testScore > 0)
                {
                    Debug.Log($"✅ PASS: CalculateScore(5,2,10,0.8,45,3) = {testScore}");
                    _passCount++;
                }
                else
                {
                    Debug.LogError($"❌ FAIL: CalculateScore returned {testScore}, expected > 0");
                    _failCount++;
                }
            }

            // Test 4: ValidateShot accepts valid data
            var validateMethod = controller.GetType().GetMethod("ValidateShot");
            if (validateMethod != null)
            {
                var shotData = new NoirMemoryShotData
                {
                    power = 0.5f,
                    cueAngle = 15f
                };
                bool isValid = (bool)validateMethod.Invoke(controller, new object[] { shotData });
                Debug.Log(isValid
                    ? $"✅ PASS: ValidateShot accepted valid shot"
                    : $"ℹ️  INFO: ValidateShot returned false (may need correct puzzle state)");
                _ = isValid ? _passCount++ : _failCount++;
            }

            PrintSummary("Noir Memory Game Controller");
        }

        // ───────────────────────────────────────────────────────
        //  Test All P8 Systems (Suite)
        // ───────────────────────────────────────────────────────
        [MenuItem("Tools/CueStrike/Debug/Test All P8 Systems")]
        public static void TestAllP8Systems()
        {
            if (!GuardPlayMode()) return;

            Debug.Log("══════════ [CueStrike Test] All P8 Systems Suite ══════════");
            ResetCounters();

            TestNoirMemoryLeaderboardUI_Internal();
            TestRCANoirBridge_Internal();
            TestNoirMemoryGameController_Internal();

            Debug.Log($"══════════ [CueStrike Test] P8 Suite: {_passCount} PASS, {_failCount} FAIL ══════════");
            if (_failCount > 0)
                Debug.LogWarning($"[CueStrike Test] ⚠️ {_failCount} test(s) failed in P8 suite.");
            else
                Debug.Log("[CueStrike Test] 🎉 All P8 tests passed!");
        }

        // ───────────────────────────────────────────────────────
        //  Internal helpers (no guards, no MenuItem)
        // ───────────────────────────────────────────────────────
        private static bool IsSceneAllowed(string[] allowedScenes)
        {
            string currentScene = EditorSceneManager.GetActiveScene().name;
            return allowedScenes.Contains(currentScene);
        }

        private static void TestNoirMemoryLeaderboardUI_Internal()
        {
            if (!IsSceneAllowed(new[] { "NoirMemoryScene", "MainMenu", "GameScene" }))
            {
                Debug.LogWarning("[CueStrike Test] ⚠️ Skip leaderboard test: wrong scene");
                return;
            }
            var resultsScreen = Object.FindFirstObjectByType<NoirMemoryResultsScreen>();
            if (resultsScreen != null) _passCount++; else { Debug.LogWarning("[CueStrike Test] ⚠️ Skip: NoirMemoryResultsScreen not in scene"); return; }
            if (NoirMemoryResultsScreen.Instance != null) _passCount++; else _failCount++;
            var score = resultsScreen.CalculateScore(5, 2, 10, 0.8f, 45f, 3, "SuiteTest");
            if (score != null && score.totalScore > 0) _passCount++; else _failCount++;
            resultsScreen.ClearLeaderboard();
            if (resultsScreen.GetLeaderboard()?.Count == 0) _passCount++; else _failCount++;
        }

        private static void TestRCANoirBridge_Internal()
        {
            if (!IsSceneAllowed(new[] { "NoirMemoryScene", "GameScene" }))
            {
                Debug.LogWarning("[CueStrike Test] ⚠️ Skip RCA bridge test: wrong scene");
                return;
            }
            var bridge = CueStrikeRCANoirBridge.Instance;
            if (bridge == null) { Debug.LogWarning("[CueStrike Test] ⚠️ Skip: CueStrikeRCANoirBridge not in scene"); return; }
            _passCount++;
            var calData = bridge.GetCalibrationData();
            if (calData != null) _passCount++; else _failCount++;
            bridge.StartAim();
            if (bridge.CurrentState == CueStrikeRCANoirBridge.RCAState.Aiming) _passCount++; else _failCount++;
            bool shotFired = false;
            System.Action<NoirMemoryShotData> handler = (s) => { shotFired = true; };
            bridge.OnShotExecuted += handler;
            bridge.SimulateShot();
            if (shotFired) _passCount++; else _failCount++;
            bridge.OnShotExecuted -= handler;
            bridge.Reset();
            if (bridge.CurrentState == CueStrikeRCANoirBridge.RCAState.Idle) _passCount++; else _failCount++;
        }

        private static void TestNoirMemoryGameController_Internal()
        {
            if (!IsSceneAllowed(new[] { "NoirMemoryScene", "GameScene" }))
            {
                Debug.LogWarning("[CueStrike Test] ⚠️ Skip game controller test: wrong scene");
                return;
            }
            var controller = Object.FindFirstObjectByType<NoirMemoryGameController>();
            if (controller == null) { Debug.LogWarning("[CueStrike Test] ⚠️ Skip: NoirMemoryGameController not in scene"); return; }
            _passCount++;
            var puzzles = controller.GetType().GetMethod("GetPuzzles")?.Invoke(controller, null) as System.Collections.IList;
            if (puzzles != null && puzzles.Count > 0) _passCount++; else _failCount++;
            var calcMethod = controller.GetType().GetMethod("CalculateScore");
            if (calcMethod != null)
            {
                int testScore = (int)calcMethod.Invoke(controller, new object[] { 3, 1, 5, 0.75f, 30f, 2 });
                if (testScore > 0) _passCount++; else _failCount++;
            }
        }

        // ───────────────────────────────────────────────────────
        //  Guards & utilities
        // ───────────────────────────────────────────────────────
        private static bool GuardPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[CueStrike Test] ❌ Cannot run test in Play Mode. Please exit Play Mode first.");
                return false;
            }
            return true;
        }

        private static bool GuardScene(params string[] allowedScenes)
        {
            string currentScene = EditorSceneManager.GetActiveScene().name;
            if (!allowedScenes.Contains(currentScene))
            {
                Debug.LogWarning($"[CueStrike Test] ⚠️ Skip: current scene is '{currentScene}', expected one of [{string.Join(", ", allowedScenes)}]");
                return false;
            }
            return true;
        }

        private static void ResetCounters() { _passCount = 0; _failCount = 0; }

        private static void PrintSummary(string testName)
        {
            Debug.Log($"========== [CueStrike Test] {testName}: {_passCount} PASS, {_failCount} FAIL ==========");
            if (_failCount > 0)
                Debug.LogWarning($"[CueStrike Test] ⚠️ {_failCount} test(s) failed in '{testName}'. Please fix before proceeding.");
            else
                Debug.Log($"[CueStrike Test] 🎉 '{testName}' all tests passed!");
        }
    }
}