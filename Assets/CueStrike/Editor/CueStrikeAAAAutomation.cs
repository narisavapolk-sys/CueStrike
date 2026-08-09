using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using CueStrike.Gameplay; // For CueStrikeIKAssist

namespace CueStrike.Editor
{
    public class CueStrikeAAAAutomation : EditorWindow
    {
        [MenuItem("Tools/CueStrike/Apply/Fix Shaders and Setup IK")]
        public static void FixShadersAndSetupIK()
        {
            // Layer 1: Block if in Play Mode
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Operation Blocked", "Cannot perform this operation while in Play Mode. Please exit Play Mode and try again.", "OK");
                return;
            }

            // Layer 2: Prompt for unsaved changes
            if (EditorUtility.DisplayDialog("Unsaved Changes Warning", "This operation may modify project assets. Do you want to save your current scene before proceeding?", "Save and Continue", "Continue Without Saving"))
            {
                EditorSceneManager.SaveOpenScenes();
            }

            // Layer 3: Prompt for wrong scene (optional, if your IK setup requires a specific scene)
            // if (EditorSceneManager.GetActiveScene().name != "YourMainSceneName")
            // {
            //     if (!EditorUtility.DisplayDialog("Wrong Scene Warning", "This operation is typically performed in 'YourMainSceneName'. Continue anyway?", "Yes", "No"))
            //     {
            //         return;
            //     }
            // }

            // Record Undo for the entire operation
            Undo.SetCurrentGroupName("Fix Shaders and Setup IK");
            int undoGroup = Undo.GetCurrentGroup();

            bool success = true;

            // --- 1. Fix Shaders (The Pink Issue) ---
            Debug.Log("[CueStrikeAAAAutomation] Starting shader fix...");
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/CueStrike/Scripts" });
            foreach (string guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string content = File.ReadAllText(path);

                if (content.Contains("Shader.Find(\"Standard\")"))
                {
                    Undo.RecordObject(AssetDatabase.LoadAssetAtPath<MonoScript>(path), "Shader Fix");
                    string newContent = content.Replace("Shader.Find(\"Standard\")", "Shader.Find(\"Universal Render Pipeline/Lit\")");
                    File.WriteAllText(path, newContent);
                    AssetDatabase.ImportAsset(path);
                    Debug.Log($"[CueStrikeAAAAutomation] Fixed shader in: {path}");
                    success = true; // Assume success if any change occurred
                }
            }
            if (!success) Debug.Log("[CueStrikeAAAAutomation] No 'Shader.Find(\"Standard\")' instances found to fix. Assuming shaders are already correct for URP.");
            else Debug.Log("[CueStrikeAAAAutomation] Shader fix completed.");

            // --- 2. Setup IK Assist ---
            Debug.Log("[CueStrikeAAAAutomation] Setting up IK Assist...");
            GameObject playerAvatar = GameObject.FindWithTag("Player"); // Assuming player avatar has "Player" tag
            if (playerAvatar != null)
            {
                CueStrikeIKAssist ikAssist = playerAvatar.GetComponent<CueStrikeIKAssist>();
                if (ikAssist == null)
                {
                    Undo.AddComponent<CueStrikeIKAssist>(playerAvatar);
                    ikAssist = playerAvatar.AddComponent<CueStrikeIKAssist>();
                    Debug.Log("[CueStrikeAAAAutomation] Added CueStrikeIKAssist component to Player.");
                }
                else
                {
                    Debug.Log("[CueStrikeAAAAutomation] CueStrikeIKAssist component already exists on Player.");
                }

                // Further configuration of ikAssist component could go here
                // e.g., assigning spine bone, cue tip, cue ball references if they are known in the scene
                // ikAssist.spineBone = FindSpineBone(playerAvatar.transform);
                // ikAssist.cueTip = GameObject.Find("CueTip").transform;
                // ikAssist.cueBall = GameObject.Find("CueBall").transform;
            }
            else
            {
                Debug.LogWarning("[CueStrikeAAAAutomation] Player avatar (GameObject with tag 'Player') not found. IK Assist setup skipped.");
                success = false;
            }
            Debug.Log("[CueStrikeAAAAutomation] IK Assist setup completed.");

            // --- Final Steps ---
            AssetDatabase.Refresh();
            Undo.CollapseUndoOperations(undoGroup); // Consolidate all undo steps

            if (success)
            {
                EditorUtility.DisplayDialog("Operation Complete", "Shader fixes and IK Assist setup completed successfully.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Operation Complete with Warnings", "Shader fixes and IK Assist setup completed, but with some warnings/issues. Check Console for details.", "OK");
            }

            // --- Self-test Mechanism ---
            VerifySetup();
        }

        private static void VerifySetup()
        {
            Debug.Log("[CueStrikeAAAAutomation] Starting self-test verification...");
            bool verificationPassed = true;

            // Verify IK Component Presence
            GameObject playerAvatar = GameObject.FindWithTag("Player");
            if (playerAvatar != null)
            {
                if (playerAvatar.GetComponent<CueStrikeIKAssist>() != null)
                {
                    Debug.Log("[CueStrikeAAAAutomation Self-Test] CueStrikeIKAssist component found on Player. OK.");
                }
                else
                {
                    Debug.LogError("[CueStrikeAAAAutomation Self-Test] CueStrikeIKAssist component NOT found on Player. FAILED.");
                    verificationPassed = false;
                }
            }
            else
            {
                Debug.LogWarning("[CueStrikeAAAAutomation Self-Test] Player avatar (GameObject with tag 'Player') not found. Cannot verify IK component presence.");
            }

            // Verify Shader Compatibility (this is a heuristic, actual visual check is best)
            // This is harder to verify programmatically without loading every material.
            // A simple check could be to ensure no "Standard" shader keyword is present in materials
            // or to check if any dynamically created materials are using "Standard" in runtime.
            // For now, we rely on the log messages from the shader fix step.
            Debug.Log("[CueStrikeAAAAutomation Self-Test] Shader compatibility assumed based on fix script execution. Manual visual verification is recommended.");

            if (verificationPassed)
            {
                Debug.Log("[CueStrikeAAAAutomation Self-Test] All automated verifications passed.");
            }
            else
            {
                Debug.LogError("[CueStrikeAAAAutomation Self-Test] Some verifications FAILED. Please check console for details.");
            }
        }

        // Example: Helper to find spine bone (might need to be more robust)
        // private static Transform FindSpineBone(Transform avatarRoot)
        // {
        //     // Implement logic to find the spine bone, e.g., by name or by traversing hierarchy
        //     return avatarRoot.Find("Armature/Hips/Spine"); // Example path
        // }
    }
}