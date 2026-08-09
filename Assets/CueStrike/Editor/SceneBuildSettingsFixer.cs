using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CueStrike.Editor
{
    /// <summary>
    /// Adds every scene under Assets/CueStrike/Scenes to EditorBuildSettings (enabled).
    /// Fixes the issue where only Title_NoksGrandHall was in the build, causing
    /// runtime scene loads (MainMenu, room scenes, Snooker_Demo) to fail in builds.
    ///
    /// Menu:  Tools → CueStrike → Fix → Add All Scenes to Build Settings
    /// Batch: Unity.exe -batchmode -quit -projectPath … -executeMethod CueStrike.Editor.SceneBuildSettingsFixer.FixBuildScenes
    /// </summary>
    public static class SceneBuildSettingsFixer
    {
        private const string ScenesRoot = "Assets/CueStrike/Scenes";

        [MenuItem("Tools/CueStrike/Fix/Add All Scenes to Build Settings")]
        public static void FixBuildScenes()
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

            if (!Directory.Exists(ScenesRoot))
            {
                Debug.LogError($"[SceneBuildSettingsFixer] Scenes folder not found: {ScenesRoot}");
                return;
            }

            string[] scenePaths = Directory.GetFiles(ScenesRoot, "*.unity", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (scenePaths.Length == 0)
            {
                Debug.LogError("[SceneBuildSettingsFixer] No .unity scenes found under " + ScenesRoot);
                return;
            }

            EditorBuildSettings.scenes = scenePaths
                .Select(p => new EditorBuildSettingsScene(p, true))
                .ToArray();

            Debug.Log($"[SceneBuildSettingsFixer] ✅ Added {scenePaths.Length} scenes to Build Settings:\n" +
                      string.Join("\n", scenePaths.Select(p => "  - " + p)));
        }
    }
}
