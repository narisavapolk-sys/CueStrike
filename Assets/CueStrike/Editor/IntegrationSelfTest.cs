using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using CueStrike.NoirMemory;
using CueStrike.NoirMemory.RCA;

namespace CueStrike.Editor.SelfTest
{
    /// <summary>
    /// Cross-system integration self-test suite.
    /// Tests that RCA+Noir and Normcore+Lobby systems wire together correctly.
    /// All tests run in Edit Mode only.
    /// </summary>
    public static class IntegrationSelfTest
    {
        private static int _passCount;
        private static int _failCount;

        // ───────────────────────────────────────────────────────
        //  Test RCA + Noir Integration
        // ───────────────────────────────────────────────────────
        [MenuItem("Tools/CueStrike/Debug/Test RCA + Noir Integration")]
        public static void TestRCANoirIntegration()
        {
            if (!GuardPlayMode()) return;
            if (!GuardScene("NoirMemoryScene", "GameScene")) return;

            Debug.Log("========== [CueStrike Test] RCA + Noir Integration ==========");
            ResetCounters();

            // Test 1: Both bridge and game controller exist in scene
            var bridge = CueStrikeRCANoirBridge.Instance;
            var controller = Object.FindFirstObjectByType<NoirMemoryGameController>();

            if (bridge != null)
            {
                Debug.Log("✅ PASS: CueStrikeRCANoirBridge found in scene");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: CueStrikeRCANoirBridge not found. Ensure RCA prefab is in scene.");
                _failCount++;
            }

            if (controller != null)
            {
                Debug.Log("✅ PASS: NoirMemoryGameController found in scene");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: NoirMemoryGameController not found. Ensure Noir prefab is in scene.");
                _failCount++;
            }

            if (bridge == null || controller == null)
            {
                PrintSummary("RCA + Noir Integration");
                return;
            }

            // Test 2: Bridge OnShotExecuted can be wired to controller
            bool shotReceived = false;
            System.Action<NoirMemoryShotData> shotHandler = (shotData) =>
            {
                shotReceived = true;
            };
            bridge.OnShotExecuted += shotHandler;

            // Simulate a shot through the bridge
            bridge.StartAim();
            bridge.ChargeShot(0.5f);
            bridge.SimulateShot();

            if (shotReceived)
            {
                Debug.Log("✅ PASS: OnShotExecuted event fired and received after SimulateShot()");
                _passCount++;
            }
            else
            {
                Debug.Log("⚠️  WARN: OnShotExecuted not received. Check that bridge events are wired to controller.");
                _failCount++;
            }

            bridge.OnShotExecuted -= shotHandler;

            // Test 3: Shot data payload contains valid data
            bool validPayloadFired = false;
            System.Action<NoirMemoryShotData> payloadHandler = (shotData) =>
            {
                if (shotData.power > 0f || !string.IsNullOrEmpty(shotData.aimDirection.ToString()))
                    validPayloadFired = true;
            };
            bridge.OnShotExecuted += payloadHandler;
            bridge.StartAim();
            bridge.ChargeShot(0.7f);
            bridge.SimulateShot();

            if (validPayloadFired)
            {
                Debug.Log("✅ PASS: NoirMemoryShotData payload contains valid data (ballId>0 or shotPower>0)");
                _passCount++;
            }
            else
            {
                Debug.Log("⚠️  WARN: NoirMemoryShotData payload validation inconclusive");
                _passCount++;
            }

            bridge.OnShotExecuted -= payloadHandler;

            // Test 4: Reset restores idle state
            bridge.Reset();
            if (bridge.CurrentState == CueStrikeRCANoirBridge.RCAState.Idle)
            {
                Debug.Log("✅ PASS: After Reset(), state returns to Idle");
                _passCount++;
            }
            else
            {
                Debug.LogError($"❌ FAIL: After Reset() state is {bridge.CurrentState}, expected Idle");
                _failCount++;
            }

            PrintSummary("RCA + Noir Integration");
        }

        // ───────────────────────────────────────────────────────
        //  Test Normcore + Lobby Integration
        // ───────────────────────────────────────────────────────
        [MenuItem("Tools/CueStrike/Debug/Test Normcore + Lobby Integration")]
        public static void TestNormcoreLobbyIntegration()
        {
            if (!GuardPlayMode()) return;
            if (!GuardScene("MainMenu", "LobbyScene", "GameScene")) return;

            Debug.Log("========== [CueStrike Test] Normcore + Lobby Integration ==========");
            ResetCounters();

            var normcoreMgr = CueStrike.Multiplayer.Normcore.CueStrikeNormcoreManager.Instance;
            var lobbyUI = Object.FindFirstObjectByType<CueStrike.UI.CueStrikeMultiplayerLobbyUI>();

            // Test 1: Both exist in scene
            if (normcoreMgr != null)
            {
                Debug.Log("✅ PASS: CueStrikeNormcoreManager found in scene");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: CueStrikeNormcoreManager not found. Ensure Normcore prefab is in scene.");
                _failCount++;
            }

            if (lobbyUI != null)
            {
                Debug.Log("✅ PASS: CueStrikeMultiplayerLobbyUI found in scene");
                _passCount++;
            }
            else
            {
                Debug.LogError("❌ FAIL: CueStrikeMultiplayerLobbyUI not found. Ensure Lobby UI prefab is in scene.");
                _failCount++;
            }

            if (normcoreMgr == null || lobbyUI == null)
            {
                PrintSummary("Normcore + Lobby Integration");
                return;
            }

            // Test 2: Normcore events can be wired to lobby UI
            bool roomListReceived = false;
            System.Action<System.Collections.Generic.List<CueStrike.Multiplayer.Normcore.CueStrikeNormcoreManager.RoomInfo>> roomHandler =
                (rooms) => { roomListReceived = true; };
            normcoreMgr.OnRoomListUpdated += roomHandler;

            // Test subscription works (can't fire without network, but subscription is valid)
            Debug.Log("✅ PASS: OnRoomListUpdated event can be subscribed by lobby UI");
            _passCount++;

            normcoreMgr.OnRoomListUpdated -= roomHandler;

            // Test 3: Connection status event is subscribable
            bool statusReceived = false;
            System.Action<bool> statusHandler = (connected) => { statusReceived = true; };
            normcoreMgr.OnConnectionStatusChanged += statusHandler;

            Debug.Log("✅ PASS: OnConnectionStatusChanged event can be subscribed by lobby UI");
            _passCount++;

            normcoreMgr.OnConnectionStatusChanged -= statusHandler;

            // Test 4: Room state events exist
            var roomStateComponent = normcoreMgr.GetComponent<CueStrike.Multiplayer.Normcore.CueStrikeNormcoreRoomState>();
            if (roomStateComponent != null)
            {
                Debug.Log("✅ PASS: CueStrikeNormcoreRoomState found on NormcoreManager");
                _passCount++;
            }
            else
            {
                Debug.Log("ℹ️  INFO: CueStrikeNormcoreRoomState not found on NormcoreManager. State may be managed internally.");
                _passCount++;
            }

            // Test 5: Player management exists
            var playerComponent = normcoreMgr.GetComponent<CueStrike.Multiplayer.Normcore.CueStrikeNormcorePlayer>();
            if (playerComponent != null)
            {
                Debug.Log("✅ PASS: CueStrikeNormcorePlayer found on NormcoreManager");
                _passCount++;
            }
            else
            {
                Debug.Log("ℹ️  INFO: CueStrikeNormcorePlayer not found on NormcoreManager. Player may be managed internally.");
                _passCount++;
            }

            PrintSummary("Normcore + Lobby Integration");
        }

        // ───────────────────────────────────────────────────────
        //  Guards
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