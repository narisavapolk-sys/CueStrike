using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.VR;

namespace CueStrike.Editor
{
    /// <summary>
    /// Builds the Boot scene (Scene 0) and binds VRStartup (Quest optimization)
    /// to a "BootManager" GameObject, per the design documented in VRStartup.cs:
    /// "Attaches to the Boot Scene (Scene 0) via the 'NARI CUE STRIKE' editor menu."
    ///
    /// Menu:  Tools → NARI CUE STRIKE → Build Boot Scene (VRStartup)
    /// Batch: Unity.exe -batchmode -quit -projectPath ... -executeMethod CueStrike.Editor.BootSceneBuilder.BuildBootScene
    /// </summary>
    public static class BootSceneBuilder
    {
        private const string BootScenePath = "Assets/CueStrike/Scenes/Boot.unity";
        private const string DefaultNextScene = "Title_NoksGrandHall";

        [MenuItem("Tools/NARI CUE STRIKE/Build Boot Scene (VRStartup)")]
        public static void BuildBootScene()
        {
            // 3-layer guard (skipped in batchmode so -executeMethod does not hang on dialogs)
            if (!Application.isBatchMode)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorUtility.DisplayDialog("NARI CUE STRIKE", "Stop Play Mode before building the Boot Scene.", "OK");
                    return;
                }

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return; // User cancelled
                }
            }

            // Create the Boot scene with a BootManager GameObject bound to VRStartup + BootSceneLoader
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootManager = new GameObject("BootManager");
            bootManager.AddComponent<VRStartup>();
            var loader = bootManager.AddComponent<BootSceneLoader>();
            loader.nextSceneName = DefaultNextScene;

            string scenesDir = Path.GetDirectoryName(BootScenePath);
            if (!string.IsNullOrEmpty(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            EditorSceneManager.SaveScene(scene, BootScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddBootAsSceneZero();
            Debug.Log($"[BootSceneBuilder] Boot scene created: {BootScenePath} (VRStartup + BootSceneLoader -> {DefaultNextScene})");
        }

        private static void AddBootAsSceneZero()
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            if (scenes.Any(s => string.Equals(s.path, BootScenePath, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.Log("[BootSceneBuilder] Boot scene already in Build Settings - no change.");
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(BootScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[BootSceneBuilder] Boot scene added as Scene 0 in Build Settings.");
        }
    }
}
