using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CueStrike.NoirMemory;
using CueStrike.NoirMemory.RCA;

namespace CueStrike.Editor.NoirMemory
{
    /// <summary>
    /// Editor tool for wiring the RCA (Real Cue Adapter) system to Noir Memory.
    /// Creates the bridge, wires events, and provides self-test.
    /// </summary>
    public static class RCANoirSetup
    {
        #region Setup

        [MenuItem("Tools/CueStrike/Setup/Wire RCA to Noir Memory")]
        public static void WireRCAToNoirMemory()
        {
            // Guard 1: Block in Play Mode
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike Setup] Cannot wire RCA while in Play Mode.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            // Guard 2: Prompt unsaved changes
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[CueStrike Setup] Setup cancelled by user (unsaved changes).");
                return;
            }

            // Guard 3: Check scene has suitable objects
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.name))
            {
                Debug.LogError("[CueStrike Setup] No active scene.");
                EditorUtility.DisplayDialog("Setup FAILED", "No active scene.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();

            try
            {
                // Find or create CueStrikeRCANoirBridge
                var bridge = Object.FindFirstObjectByType<CueStrikeRCANoirBridge>();
                if (bridge == null)
                {
                    var bridgeGO = new GameObject("CueStrikeRCANoirBridge");
                    Undo.RegisterCreatedObjectUndo(bridgeGO, "Create RCA Bridge");
                    bridge = bridgeGO.AddComponent<CueStrikeRCANoirBridge>();
                    Debug.Log("[CueStrike Setup] Created CueStrikeRCANoirBridge.");
                }
                else
                {
                    Debug.Log("[CueStrike Setup] Found existing CueStrikeRCANoirBridge.");
                }

                // Find or create NoirMemoryGameController
                var controller = Object.FindFirstObjectByType<NoirMemoryGameController>();
                if (controller == null)
                {
                    var ctrlGO = new GameObject("NoirMemoryGameController");
                    Undo.RegisterCreatedObjectUndo(ctrlGO, "Create Game Controller");
                    controller = ctrlGO.AddComponent<NoirMemoryGameController>();
                    Debug.Log("[CueStrike Setup] Created NoirMemoryGameController.");
                }
                else
                {
                    Debug.Log("[CueStrike Setup] Found existing NoirMemoryGameController.");
                }

                // Wire bridge to controller via SerializedObject
                var serializedCtrl = new SerializedObject(controller);
                serializedCtrl.FindProperty("rcaBridge").objectReferenceValue = bridge;
                serializedCtrl.ApplyModifiedProperties();

                // Wire puzzle manager if exists
                var puzzleMgr = Object.FindFirstObjectByType<NoirMemoryPuzzleManager>();
                if (puzzleMgr != null)
                {
                    serializedCtrl.FindProperty("puzzleManager").objectReferenceValue = puzzleMgr;
                    serializedCtrl.ApplyModifiedProperties();
                    Debug.Log("[CueStrike Setup] Wired NoirMemoryPuzzleManager to GameController.");
                }
                else
                {
                    Debug.LogWarning("[CueStrike Setup] No NoirMemoryPuzzleManager found. GameController will use dummy mode.");
                }

                // Wire results screen if exists
                var resultsScreen = Object.FindFirstObjectByType<NoirMemoryResultsScreen>();
                if (resultsScreen != null)
                {
                    serializedCtrl.FindProperty("resultsScreen").objectReferenceValue = resultsScreen;
                    serializedCtrl.ApplyModifiedProperties();
                    Debug.Log("[CueStrike Setup] Wired NoirMemoryResultsScreen to GameController.");
                }

                // Set up dummy mode for offline testing
                serializedCtrl.FindProperty("enableDummyMode").boolValue = true;
                serializedCtrl.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[CueStrike Setup] RCA to Noir Memory wired successfully.");
                EditorUtility.DisplayDialog("CueStrike", "RCA to Noir Memory wired successfully!\n\n" +
                    "• CueStrikeRCANoirBridge (created/found)\n" +
                    "• NoirMemoryGameController (created/found)\n" +
                    "• Events wired: Bridge -> GameController\n" +
                    "• Dummy mode: ON (no hardware required)", "OK");
            }
            finally
            {
                Undo.CollapseUndoOperations(groupIndex);
            }
        }

        #endregion
    }
}