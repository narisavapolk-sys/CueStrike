using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CueStrike.VR.Input;

namespace CueStrike.Editor.Setup
{
    /// <summary>
    /// Editor tool for wiring up the CueStrike VR Input System.
    /// Modifies the scene to add required VR input components.
    /// </summary>
    public static class CueStrikeVRInputSetup
    {
        /// <summary>
        /// Finds all MeshRenderers in the current scene and assigns them to the XR Ray Interactor's
        /// render list so that the ray can interact with the meshes.
        /// </summary>
        [MenuItem("Tools/CueStrike/Setup/Wire VR Input System")]
        public static void SetupVRInput()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[CueStrike Setup] Cannot run setup during Play Mode. Please exit Play Mode first.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            var currentScene = EditorSceneManager.GetActiveScene();

            // Find the VRInputManager in the scene, or create one if it doesn't exist
            var vrInputManager = Object.FindFirstObjectByType<CueStrikeVRInputManager>();
            if (vrInputManager == null)
            {
                var go = new GameObject("CueStrikeVRInputManager");
                vrInputManager = go.AddComponent<CueStrikeVRInputManager>();
                Debug.Log("[CueStrike Setup] Created CueStrikeVRInputManager.");
            }
            else
            {
                Debug.Log("[CueStrike Setup] Found existing CueStrikeVRInputManager.");
            }

            // Find or create PhysicalShotController
            var physicalShotController = Object.FindFirstObjectByType<CueStrikePhysicalShotController>();
            if (physicalShotController == null)
            {
                var go = new GameObject("CueStrikePhysicalShotController");
                physicalShotController = go.AddComponent<CueStrikePhysicalShotController>();
                Debug.Log("[CueStrike Setup] Created CueStrikePhysicalShotController.");
            }
            else
            {
                Debug.Log("[CueStrike Setup] Found existing CueStrikePhysicalShotController.");
            }

            // Wire references via SerializedObject
            var serializedManager = new SerializedObject(vrInputManager);
            serializedManager.FindProperty("dominantHandController").objectReferenceValue = GameObject.Find("RightHand Controller");
            serializedManager.FindProperty("offHandController").objectReferenceValue = GameObject.Find("LeftHand Controller");
            serializedManager.FindProperty("physicalShotController").objectReferenceValue = physicalShotController;
            serializedManager.ApplyModifiedProperties();

            // Set up ray interactor mesh references
            var meshRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var interactors = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
            if (interactors.Length > 0)
            {
                foreach (var interactor in interactors)
                {
                    var serializedInteractor = new SerializedObject(interactor);
                    var renderList = serializedInteractor.FindProperty("m_RayOriginMask.m_Renderers");
                    renderList.ClearArray();
                    int index = 0;
                    foreach (var renderer in meshRenderers)
                    {
                        renderList.InsertArrayElementAtIndex(index);
                        renderList.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
                        index++;
                    }
                    serializedInteractor.ApplyModifiedProperties();
                    Debug.Log($"[CueStrike Setup] Wired {index} MeshRenderers to {interactor.name}.");
                }
            }
            else
            {
                Debug.LogWarning("[CueStrike Setup] No XRRayInteractors found in scene. Please add XR Interaction Manager setup.");
            }

            // Mark dirty and save
            EditorSceneManager.MarkSceneDirty(currentScene);
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("[CueStrike Setup] VR Input System wired successfully.");
            EditorUtility.DisplayDialog("CueStrike", "VR Input System wired successfully!\n\n" +
                "• CueStrikeVRInputManager (created/found)\n" +
                "• CueStrikePhysicalShotController (created/found)\n" +
                "• MeshRenderers wired to XRRayInteractors\n" +
                "• SaveOpenScenes skipped during Play Mode", "OK");
        }
    }
}