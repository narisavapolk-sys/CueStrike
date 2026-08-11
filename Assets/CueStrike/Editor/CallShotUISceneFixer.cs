using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.UI.ChinesePool;

namespace CueStrike.Editor
{
    /// <summary>
    /// Fixes scenes where CallShot_Panel has TWO ChinesePoolCallShotUI components:
    /// one fully wired and one empty (all refs = 0). The UIManager points at the
    /// empty one, so ShowCallShot silently no-ops and the panel never appears.
    ///
    /// Fix: destroy the empty duplicate (when a wired sibling exists on the same
    /// GameObject) and repoint ChinesePoolUIManager._callShotUI to the survivor.
    ///
    /// Menu:  Tools → CueStrike → Fix → Fix CallShot UI Duplicate Components
    /// Batch: Unity.exe -batchmode -quit -projectPath ... -executeMethod CueStrike.Editor.CallShotUISceneFixer.FixAllScenes
    /// </summary>
    public static class CallShotUISceneFixer
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity",
            "Assets/CueStrike/Scenes/Title_NoksGrandHall.unity",
        };

        [MenuItem("Tools/CueStrike/Fix/Fix CallShot UI Duplicate Components")]
        public static void FixAllScenes()
        {
            // 3-layer guard (skipped in batchmode so -executeMethod does not hang on dialogs)
            if (!Application.isBatchMode)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorUtility.DisplayDialog("CueStrike", "Stop Play Mode before running this.", "OK");
                    return;
                }

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return; // User cancelled
                }
            }

            foreach (string path in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                int removed = RemoveEmptyDuplicates();
                int repointed = RepointUIManager();

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[CallShotUISceneFixer] {path}: removed {removed} empty duplicate(s), repointed {repointed} UIManager(s)");
            }
        }

        /// <summary>
        /// Destroys ChinesePoolCallShotUI components whose _callShotPanel is null
        /// when the same GameObject hosts another instance with it assigned.
        /// </summary>
        private static int RemoveEmptyDuplicates()
        {
            int removed = 0;
            var all = Object.FindObjectsByType<ChinesePoolCallShotUI>(FindObjectsSortMode.None);

            foreach (var group in all.Where(c => c != null).GroupBy(c => c.gameObject))
            {
                var components = group.ToList();
                bool hasWired = components.Any(c => IsFieldAssigned(c, "_callShotPanel"));

                foreach (var component in components)
                {
                    if (hasWired && !IsFieldAssigned(component, "_callShotPanel"))
                    {
                        Object.DestroyImmediate(component);
                        removed++;
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// Points ChinesePoolUIManager._callShotUI at the first surviving wired
        /// component when its current reference was destroyed (or is null).
        /// </summary>
        private static int RepointUIManager()
        {
            int repointed = 0;
            var survivors = Object.FindObjectsByType<ChinesePoolCallShotUI>(FindObjectsSortMode.None)
                .Where(c => c != null && IsFieldAssigned(c, "_callShotPanel"))
                .ToArray();
            if (survivors.Length == 0) return 0;

            var managers = Object.FindObjectsByType<ChinesePoolUIManager>(FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (manager == null) continue;

                var so = new SerializedObject(manager);
                var prop = so.FindProperty("_callShotUI");
                if (prop == null || prop.objectReferenceValue != null) continue;

                prop.objectReferenceValue = survivors[0];
                so.ApplyModifiedProperties();
                repointed++;
            }

            return repointed;
        }

        private static bool IsFieldAssigned(Object target, string fieldName)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            return prop != null && prop.objectReferenceValue != null;
        }
    }
}
