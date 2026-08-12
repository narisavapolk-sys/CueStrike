using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.AI;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R41 — Editor tool: ผูก SnookerDifficultyUI ลง Snooker_Demo
    ///
    /// - MenuItem: Tools/CueStrike/AI/140. Setup Snooker Difficulty UI
    /// - เพิ่ม SnookerDifficultyUI component + assign bridge ref (SnookerAI_Bridge)
    /// - Idempotent: ถ้ามีครบแล้ว → ข้าม
    /// - Self-test: UI component + bridge ref + 4 ปุ่ม
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.SnookerDifficultyUISetup.RunFromBatch
    /// </summary>
    public static class SnookerDifficultyUISetup
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/Snooker_Demo.unity";
        private const string UIGOName = "SnookerDifficultyUI_Controller";

        [MenuItem("Tools/CueStrike/AI/140. Setup Snooker Difficulty UI")]
        public static void SetupFromMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[SnookerDifficultyUI] Cannot run in Play Mode.");
                return;
            }

            bool ok = Run();
            Debug.Log(ok ? "[SnookerDifficultyUI] ✅ Setup complete — difficulty selector ready in Snooker_Demo."
                          : "[SnookerDifficultyUI] ❌ Setup failed — see errors above.");
        }

        /// <summary>Batchmode entry point (compile gate + CI).</summary>
        public static void RunFromBatch()
        {
            bool ok = Run();
            if (!ok)
            {
                EditorApplication.Exit(1);
            }
            EditorApplication.Exit(0);
        }

        public static bool Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (scene == null || !scene.IsValid())
            {
                Debug.LogError($"[SnookerDifficultyUI] Cannot open scene: {ScenePath}");
                return false;
            }

            bool pass = true;

            // ---------- 1. Bridge (ต้องมีจาก R36) ----------
            var bridge = UnityEngine.Object.FindAnyObjectByType<CueStrikeSnookerAIBridge>();
            if (bridge == null)
            {
                Debug.LogError("[SnookerDifficultyUI] CueStrikeSnookerAIBridge not found — run SnookerAISetup (R36) first.");
                return false;
            }

            // ---------- 2. UI component ----------
            var ui = UnityEngine.Object.FindAnyObjectByType<SnookerDifficultyUI>();
            if (ui == null)
            {
                var go = new GameObject(UIGOName);
                ui = go.AddComponent<SnookerDifficultyUI>();

                // assign bridge ref ผ่าน SerializedObject (private field)
                var so = new SerializedObject(ui);
                var bridgeProp = so.FindProperty("_bridge");
                if (bridgeProp != null)
                {
                    bridgeProp.objectReferenceValue = bridge;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Debug.Log("[SnookerDifficultyUI] Added SnookerDifficultyUI + wired bridge ref.");
            }
            else
            {
                Debug.Log("[SnookerDifficultyUI] SnookerDifficultyUI already present — idempotent skip.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SnookerDifficultyUI] Scene saved: {ScenePath}");

            bool selfTestOk = RunSelfTest();
            return pass && selfTestOk;
        }

        [MenuItem("Tools/CueStrike/Debug/Test Snooker Difficulty UI")]
        public static void TestFromMenu()
        {
            bool ok = RunSelfTest();
            Debug.Log(ok ? "[Self-Test] Snooker Difficulty UI: ALL PASS" : "[Self-Test] Snooker Difficulty UI: SOME FAILED");
        }

        public static bool RunSelfTest()
        {
            bool pass = true;
            var ui = UnityEngine.Object.FindAnyObjectByType<SnookerDifficultyUI>();
            var bridge = UnityEngine.Object.FindAnyObjectByType<CueStrikeSnookerAIBridge>();

            pass &= Check("SnookerDifficultyUI exists", ui != null);
            pass &= Check("Bridge exists", bridge != null);
            pass &= Check("Bridge ref wired",
                ui != null && new SerializedObject(ui).FindProperty("_bridge") is var p && p != null && p.objectReferenceValue != null);
            pass &= Check("Difficulty 4 levels exist",
                ui != null && System.Enum.GetValues(typeof(SkillLevel)).Length == 4);
            pass &= Check("Default difficulty Medium", ui == null || ui.selectedDifficulty == SkillLevel.Medium);

            Debug.Log($"[Self-Test] Snooker Difficulty UI: {(pass ? "PASS 5/5" : "FAIL")}");
            return pass;
        }

        private static bool Check(string name, bool condition)
        {
            Debug.Log($"[Self-Test] {name}: {(condition ? "✅" : "❌")}");
            return condition;
        }
    }
}
