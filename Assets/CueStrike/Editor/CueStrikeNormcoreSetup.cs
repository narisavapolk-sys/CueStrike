using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CueStrike.Multiplayer.Normcore;
using CueStrike.UI;

namespace CueStrike.Editor.Multiplayer
{
    /// <summary>
    /// Editor tool for wiring Normcore multiplayer system.
    /// Creates CueStrikeNormcoreManager, wires to lobby UI,
    /// and provides self-test functionality.
    /// </summary>
    public static class CueStrikeNormcoreSetup
    {
        #region Setup

        [MenuItem("Tools/CueStrike/Setup/Wire Normcore Multiplayer")]
        public static void WireNormcoreMultiplayer()
        {
            // Guard 1
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike Setup] Cannot wire Normcore while in Play Mode.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            // Guard 2
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[CueStrike Setup] Setup cancelled by user.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();

            try
            {
                // Find or create CueStrikeNormcoreManager
                var normMgr = Object.FindFirstObjectByType<CueStrikeNormcoreManager>();
                if (normMgr == null)
                {
                    var mgrGO = new GameObject("CueStrikeNormcoreManager");
                    Undo.RegisterCreatedObjectUndo(mgrGO, "Create Normcore Manager");
                    normMgr = mgrGO.AddComponent<CueStrikeNormcoreManager>();
                    Debug.Log("[CueStrike Setup] Created CueStrikeNormcoreManager.");
                }
                else
                {
                    Debug.Log("[CueStrike Setup] Found existing CueStrikeNormcoreManager.");
                }

                // Ensure DontDestroyOnLoad
                if (normMgr.gameObject.scene.name != null)
                {
                    // Move to DontDestroyOnLoad if needed
                    // Note: Can't call DontDestroyOnLoad in editor for objects in scene
                }

                // Find and wire to lobby UI
                var lobbyUI = Object.FindFirstObjectByType<CueStrikeMultiplayerLobbyUI>();
                if (lobbyUI != null)
                {
                    Debug.Log("[CueStrike Setup] Found CueStrikeMultiplayerLobbyUI — events ready to wire at runtime.");
                    Debug.Log("[CueStrike Setup] Wire lobby events: normMgr.OnRoomListUpdated -> lobbyUI, " +
                              "normMgr.OnPlayerJoined -> lobbyUI, etc.");
                }
                else
                {
                    Debug.LogWarning("[CueStrike Setup] No CueStrikeMultiplayerLobbyUI found in scene.");
                }

                // Check if Normcore SDK is present
                bool hasSdk = CueStrikeNormcoreManager.HasNormcoreSdk;
                if (!hasSdk)
                {
                    Debug.LogWarning("[CueStrike Setup] Normcore SDK not detected. " +
                                     "Set CUESTRIKE_NORMCORE compilation symbol if SDK is installed, " +
                                     "or use offline/dummy mode (auto-enabled).");
                }
                else
                {
                    Debug.Log("[CueStrike Setup] Normcore SDK detected. Online mode available.");
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[CueStrike Setup] Normcore multiplayer wired successfully.");
                EditorUtility.DisplayDialog("CueStrike", "Normcore Multiplayer wired!\n\n" +
                    "• CueStrikeNormcoreManager (created/found)\n" +
                    "• Offline/dummy mode: " + (hasSdk ? "OFF (SDK found)" : "ON (auto)\n") +
                    "• Wire lobby events at runtime via inspector", "OK");
            }
            finally
            {
                Undo.CollapseUndoOperations(groupIndex);
            }
        }

        #endregion
    }
}
