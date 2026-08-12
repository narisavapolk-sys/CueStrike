using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.AI;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.Editor
{
    /// <summary>
    /// R37 — ChinesePool AI Modifier Setup (AAA_RoomDAY)
    /// เพิ่ม ChinesePoolAIModifier component ลงฉาก + assign refs ให้
    /// ChinesePoolGameManager.aiModifier และ CueStrikePracticeAIBridge.aiModifier
    /// — แก้ Vision audit blocker: AI ยิงไม่ได้เพราะ modifier หายจากฉาก.
    ///
    /// Idempotent: รันซ้ำไม่สร้างซ้ำ / skip ถ้ามีครบ. Self-test + batchmode พร้อม.
    /// </summary>
    public static class ChinesePoolAIModifierSetup
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity";
        private const string ModifierGOName = "ChinesePoolAIModifier";

        [MenuItem("Tools/CueStrike/AI/110. Setup ChinesePool AI Modifier (AAA_RoomDAY)")]
        public static void SetupFromMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[ChinesePoolAI] Cannot run in Play Mode.");
                return;
            }

            bool ok = Run();
            Debug.Log(ok ? "[ChinesePoolAI] ✅ Setup complete — AI modifier wired, Practice AI will shoot now."
                          : "[ChinesePoolAI] ❌ Setup failed — see errors above.");
        }

        /// <summary>Batchmode entry point.</summary>
        public static void RunFromBatch()
        {
            bool ok = Run();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (scene == null || !scene.IsValid())
            {
                Debug.LogError($"[ChinesePoolAI] Cannot open scene: {ScenePath}");
                return false;
            }

            bool pass = true;

            // 1. Ensure ChinesePoolAIModifier component
            var modifier = Object.FindAnyObjectByType<ChinesePoolAIModifier>();
            if (modifier == null)
            {
                var go = GameObject.Find(ModifierGOName);
                if (go == null) go = new GameObject(ModifierGOName);
                modifier = go.AddComponent<ChinesePoolAIModifier>();
                Debug.Log("[ChinesePoolAI] ChinesePoolAIModifier created.");
            }
            else
            {
                Debug.Log("[ChinesePoolAI] ChinesePoolAIModifier already present — idempotent skip.");
            }

            // 2. Wire GameManager.aiModifier
            var gm = Object.FindAnyObjectByType<ChinesePoolGameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var prop = so.FindProperty("aiModifier");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = modifier;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[ChinesePoolAI] Wired GameManager.aiModifier.");
                }
                else if (prop != null)
                {
                    Debug.Log("[ChinesePoolAI] GameManager.aiModifier already assigned — idempotent skip.");
                }
                else
                {
                    Debug.LogWarning("[ChinesePoolAI] aiModifier field not found on GameManager.");
                    pass = false;
                }
            }
            else
            {
                Debug.LogWarning("[ChinesePoolAI] ChinesePoolGameManager not found in scene.");
                pass = false;
            }

            // 3. Wire Bridge.aiModifier
            var bridge = Object.FindAnyObjectByType<CueStrikePracticeAIBridge>();
            if (bridge != null)
            {
                var so = new SerializedObject(bridge);
                var prop = so.FindProperty("aiModifier");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = modifier;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[ChinesePoolAI] Wired PracticeAIBridge.aiModifier.");
                }
                else if (prop != null)
                {
                    Debug.Log("[ChinesePoolAI] PracticeAIBridge.aiModifier already assigned — idempotent skip.");
                }
                else
                {
                    Debug.LogWarning("[ChinesePoolAI] aiModifier field not found on bridge.");
                    pass = false;
                }
            }
            else
            {
                Debug.LogWarning("[ChinesePoolAI] CueStrikePracticeAIBridge not found in scene (Practice AI not wired).");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ChinesePoolAI] Scene saved: {ScenePath}");

            bool selfTest = RunSelfTest();
            return pass && selfTest;
        }

        [MenuItem("Tools/CueStrike/Debug/Test ChinesePool AI Modifier")]
        public static void TestFromMenu()
        {
            bool ok = RunSelfTest();
            Debug.Log(ok ? "[Self-Test] ChinesePool AI Modifier: ALL PASS" : "[Self-Test] ChinesePool AI Modifier: SOME FAILED");
        }

        public static bool RunSelfTest()
        {
            bool pass = true;
            pass &= Check("ChinesePoolAIModifier exists", Object.FindAnyObjectByType<ChinesePoolAIModifier>() != null);

            var gm = Object.FindAnyObjectByType<ChinesePoolGameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var p = so.FindProperty("aiModifier");
                pass &= Check("GameManager.aiModifier assigned", p != null && p.objectReferenceValue != null);
            }
            else
            {
                pass &= Check("GameManager.aiModifier assigned", false);
            }

            var bridge = Object.FindAnyObjectByType<CueStrikePracticeAIBridge>();
            if (bridge != null)
            {
                var so = new SerializedObject(bridge);
                var p = so.FindProperty("aiModifier");
                pass &= Check("Bridge.aiModifier assigned", p != null && p.objectReferenceValue != null);
            }
            else
            {
                Debug.Log("[Self-Test] Bridge.aiModifier assigned: ⚠️ (no bridge in scene)");
            }

            Debug.Log($"[Self-Test] ChinesePool AI Modifier: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }

        private static bool Check(string name, bool condition)
        {
            Debug.Log($"[Self-Test] {name}: {(condition ? "✅" : "❌")}");
            return condition;
        }
    }
}
