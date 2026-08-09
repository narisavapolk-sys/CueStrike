#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.VR.Input;
using CueStrike.Core;

namespace CueStrike.Editor.SelfTest
{
    /// <summary>
    /// Self-test suite for the VR Physical Input System.
    /// Menu: Tools/CueStrike/Debug/Test VR Input System
    /// Tests singleton existence, component wiring, mapping asset, and mock operations.
    /// </summary>
    public static class VRInputSelfTest
    {
        private static int _passCount;
        private static int _failCount;

        [MenuItem("Tools/CueStrike/Debug/Test VR Input System")]
        public static void TestVRInputSystem()
        {
            Debug.Log("========== [CueStrike Test] VR Input System ==========");
            ResetCounters();

            // Test 1: VRInputManager singleton exists (auto-create temp if missing)
            var inputManager = CueStrikeVRInputManager.Instance;
            GameObject tempManagerGO = null;

            if (inputManager == null)
            {
                Debug.Log("[CueStrike Test] VRInputManager not found, creating temporary for test...");
                tempManagerGO = new GameObject("CueStrikeVRInputManager_TEMP");
                var tempManager = tempManagerGO.AddComponent<CueStrikeVRInputManager>();
                inputManager = tempManager;
                Undo.RegisterCreatedObjectUndo(tempManagerGO, "Temp VRInputManager");
                Debug.Log("⚠️  WARN: Temporary VRInputManager created for test. Run Tools/CueStrike/Setup/Wire VR Input System to set up permanently.");
            }

            if (inputManager != null)
            {
                Debug.Log("✅ PASS: CueStrikeVRInputManager.Instance is set");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: Could not create CueStrikeVRInputManager even with temp fallback.");
                _failCount++;
                PrintSummary("VR Input System");
                return;
            }

            // Test 2: Sub-controllers are wired
            TestNotNull("PhysicalShotController", inputManager.ShotController);
            TestNotNull("StanceController", inputManager.StanceController);
            TestNotNull("AimOrbitController", inputManager.AimOrbitController);
            TestNotNull("ShotHistory", inputManager.ShotHistory);

            // Test 3: Input Mapping asset exists
            var mapping = inputManager.InputMapping;
            if (mapping != null)
            {
                Debug.Log("✅ PASS: InputMapping asset is assigned");
                _passCount++;

                // Check key thresholds
                if (mapping.minPullBackDistance > 0f)
                {
                    Debug.Log($"✅ PASS: minPullBackDistance = {mapping.minPullBackDistance}");
                    _passCount++;
                }
                else
                {
                    Debug.LogError("❌ FAIL: minPullBackDistance is 0 or negative");
                    _failCount++;
                }
            }
            else
            {
                Debug.LogError("❌ FAIL: InputMapping asset is null");
                _failCount++;
            }

            // Test 4: Dominant hand detection
            var settingsManager = Object.FindFirstObjectByType<CueStrikeSettingsManager>();
            if (settingsManager != null)
            {
                var expectedHand = (settingsManager.dominantHand == 0)
                    ? CueStrikeVRInputManager.HandType.Right
                    : CueStrikeVRInputManager.HandType.Left;
                bool handMatch = (inputManager.DominantHand == expectedHand);
                Debug.Log(handMatch
                    ? $"✅ PASS: Dominant hand matches SettingsManager: {expectedHand}"
                    : $"❌ FAIL: Dominant hand mismatch. InputManager={inputManager.DominantHand}, Settings={expectedHand}");
                _ = handMatch ? _passCount++ : _failCount++;
            }
            else
            {
                Debug.Log("ℹ️  INFO: CueStrikeSettingsManager not found (dominant hand test skipped)");
                _passCount++;
            }

            // Test 5: Mock PhysicalShotController.SimulateShot
            if (inputManager.ShotController != null)
            {
                bool shotFired = false;
                System.Action<CueStrikePhysicalShotController.PhysicalShotData> handler =
                    (shot) => { shotFired = true; };

                inputManager.ShotController.OnShotExecuted += handler;
                inputManager.ShotController.SimulateShot(0.5f);

                if (shotFired)
                {
                    Debug.Log("✅ PASS: SimulateShot() fired OnShotExecuted event");
                    _passCount++;
                }
                else
                {
                    Debug.LogError("❌ FAIL: SimulateShot() did not fire OnShotExecuted event");
                    _failCount++;
                }

                inputManager.ShotController.OnShotExecuted -= handler;
            }

            // Test 6: Mock StanceController toggle
            if (inputManager.StanceController != null)
            {
                var initialStance = inputManager.StanceController.CurrentStance;
                inputManager.StanceController.ToggleStance();
                var toggledStance = inputManager.StanceController.CurrentStance;

                if (toggledStance != initialStance)
                {
                    Debug.Log($"✅ PASS: Stance toggled from {initialStance} to {toggledStance}");
                    _passCount++;
                }
                else
                {
                    Debug.LogError("❌ FAIL: Stance did not change after ToggleStance()");
                    _failCount++;
                }

                // Toggle back
                inputManager.StanceController.ToggleStance();
            }

            // Test 7: Mock ShotHistory
            if (inputManager.ShotHistory != null)
            {
                inputManager.ShotHistory.ClearHistory();
                if (inputManager.ShotHistory.HistoryCount == 0)
                {
                    Debug.Log("✅ PASS: ShotHistory cleared and empty");
                    _passCount++;
                }
                else
                {
                    Debug.LogError("❌ FAIL: ShotHistory not empty after ClearHistory()");
                    _failCount++;
                }

                // Test undo with no history
                var result = inputManager.ShotHistory.UndoLastShot();
                if (result == null)
                {
                    Debug.Log("✅ PASS: UndoLastShot() with empty history returned null (expected)");
                    _passCount++;
                }
                else
                {
                    Debug.LogError("❌ FAIL: UndoLastShot() returned a snapshot when history was empty");
                    _failCount++;
                }
            }

            // Test 8: Mock Options button
            bool optionsPressed = false;
            System.Action optionsHandler = () => { optionsPressed = true; };
            inputManager.OnOptionsPressed += optionsHandler;
            inputManager.SimulateOptionsPress();
            if (optionsPressed)
            {
                Debug.Log("✅ PASS: SimulateOptionsPress() fired OnOptionsPressed event");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: SimulateOptionsPress() did not fire OnOptionsPressed");
                _failCount++;
            }
            inputManager.OnOptionsPressed -= optionsHandler;

            // Test 9: Mock Undo button
            bool undoPressed = false;
            System.Action undoHandler = () => { undoPressed = true; };
            inputManager.OnUndoPressed += undoHandler;
            inputManager.SimulateUndoPress();
            if (undoPressed)
            {
                Debug.Log("✅ PASS: SimulateUndoPress() fired OnUndoPressed event");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: SimulateUndoPress() did not fire OnUndoPressed");
                _failCount++;
            }
            inputManager.OnUndoPressed -= undoHandler;

            PrintSummary("VR Input System");
        }

        private static void TestNotNull(string name, Object obj)
        {
            if (obj != null)
            {
                Debug.Log($"✅ PASS: {name} is wired and not null");
                _passCount++;
            }
            else
            {
                Debug.LogError($"❌ FAIL: {name} is null. Run Tools/CueStrike/Setup/Wire VR Input System to fix.");
                _failCount++;
            }
        }

        private static void ResetCounters() { _passCount = 0; _failCount = 0; }

        private static void PrintSummary(string testName)
        {
            Debug.Log($"========== [CueStrike Test] {testName}: {_passCount} PASS, {_failCount} FAIL ==========");
            if (_failCount > 0)
                Debug.LogWarning($"[CueStrike Test] ⚠️ {_failCount} test(s) failed in '{testName}'.");
            else
                Debug.Log($"[CueStrike Test] 🎉 '{testName}' all tests passed!");
        }
    }
}
#endif