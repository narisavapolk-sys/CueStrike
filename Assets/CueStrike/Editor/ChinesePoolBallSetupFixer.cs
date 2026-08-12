using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.Editor
{
    /// <summary>
    /// R38 — ChinesePool BallSetup Fixer (AAA_RoomDAY)
    /// เพิ่ม ChinesePoolBallSetup component ลงฉาก + assign prefabs (Pool_CueBall/01/08/09)
    /// + assign ChinesePoolGameManager.ballSetup — แก้ Vision audit blocker:
    /// "Cannot start frame — ChinesePoolBallSetup is null" → เกมไม่เริ่ม → AI ยิงไม่ได้.
    ///
    /// Idempotent: รันซ้ำไม่สร้างซ้ำ / skip ถ้ามีครบ. Self-test + batchmode พร้อม.
    /// </summary>
    public static class ChinesePoolBallSetupFixer
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity";
        private const string SetupGOName = "ChinesePoolBallSetup";

        private const string BallsRoot = "Assets/CueStrike/Prefabs/Balls/Pool/";
        private const string CueBallPrefab = BallsRoot + "Pool_CueBall.prefab";
        private const string RedBallPrefab = BallsRoot + "Pool_Ball_01.prefab";
        private const string YellowBallPrefab = BallsRoot + "Pool_Ball_09.prefab";
        private const string BlackBallPrefab = BallsRoot + "Pool_Ball_08.prefab";

        [MenuItem("Tools/CueStrike/AI/120. Fix ChinesePool BallSetup (AAA_RoomDAY)")]
        public static void SetupFromMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[BallSetupFix] Cannot run in Play Mode.");
                return;
            }

            bool ok = Run();
            Debug.Log(ok ? "[BallSetupFix] ✅ Setup complete — balls will spawn, Practice AI can shoot now."
                          : "[BallSetupFix] ❌ Setup failed — see errors above.");
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
                Debug.LogError($"[BallSetupFix] Cannot open scene: {ScenePath}");
                return false;
            }

            bool pass = true;

            // 1. Ensure ChinesePoolBallSetup component
            var setup = Object.FindAnyObjectByType<ChinesePoolBallSetup>();
            if (setup == null)
            {
                var go = GameObject.Find(SetupGOName);
                if (go == null) go = new GameObject(SetupGOName);
                setup = go.AddComponent<ChinesePoolBallSetup>();
                Debug.Log("[BallSetupFix] ChinesePoolBallSetup created.");
            }
            else
            {
                Debug.Log("[BallSetupFix] ChinesePoolBallSetup already present — idempotent skip.");
            }

            // 2. Assign prefabs (ถ้ายังว่าง)
            var so = new SerializedObject(setup);
            AssignPrefab(so, "cueBallPrefab", CueBallPrefab);
            AssignPrefab(so, "redBallPrefab", RedBallPrefab);
            AssignPrefab(so, "yellowBallPrefab", YellowBallPrefab);
            AssignPrefab(so, "blackBallPrefab", BlackBallPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            // 3. Wire GameManager.ballSetup
            var gm = Object.FindAnyObjectByType<ChinesePoolGameManager>();
            if (gm != null)
            {
                var gmSo = new SerializedObject(gm);
                var prop = gmSo.FindProperty("ballSetup");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = setup;
                    gmSo.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[BallSetupFix] Wired GameManager.ballSetup.");
                }
                else if (prop != null)
                {
                    Debug.Log("[BallSetupFix] GameManager.ballSetup already assigned — idempotent skip.");
                }
                else
                {
                    Debug.LogWarning("[BallSetupFix] ballSetup field not found on GameManager.");
                    pass = false;
                }
            }
            else
            {
                Debug.LogWarning("[BallSetupFix] ChinesePoolGameManager not found in scene.");
                pass = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BallSetupFix] Scene saved: {ScenePath}");

            bool selfTest = RunSelfTest();
            return pass && selfTest;
        }

        private static void AssignPrefab(SerializedObject so, string field, string path)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[BallSetupFix] Field '{field}' not found.");
                return;
            }
            if (prop.objectReferenceValue != null)
            {
                Debug.Log($"[BallSetupFix] {field} already assigned — idempotent skip.");
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                prop.objectReferenceValue = prefab;
                Debug.Log($"[BallSetupFix] Assigned {field} = {path}.");
            }
            else
            {
                Debug.LogWarning($"[BallSetupFix] Prefab not found: {path}");
            }
        }

        [MenuItem("Tools/CueStrike/Debug/Test ChinesePool BallSetup Fix")]
        public static void TestFromMenu()
        {
            bool ok = RunSelfTest();
            Debug.Log(ok ? "[Self-Test] ChinesePool BallSetup Fix: ALL PASS" : "[Self-Test] ChinesePool BallSetup Fix: SOME FAILED");
        }

        public static bool RunSelfTest()
        {
            bool pass = true;

            var setup = Object.FindAnyObjectByType<ChinesePoolBallSetup>();
            pass &= Check("ChinesePoolBallSetup exists", setup != null);

            if (setup != null)
            {
                var so = new SerializedObject(setup);
                pass &= Check("cueBallPrefab assigned", so.FindProperty("cueBallPrefab")?.objectReferenceValue != null);
                pass &= Check("redBallPrefab assigned", so.FindProperty("redBallPrefab")?.objectReferenceValue != null);
                pass &= Check("yellowBallPrefab assigned", so.FindProperty("yellowBallPrefab")?.objectReferenceValue != null);
                pass &= Check("blackBallPrefab assigned", so.FindProperty("blackBallPrefab")?.objectReferenceValue != null);
            }

            var gm = Object.FindAnyObjectByType<ChinesePoolGameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var p = so.FindProperty("ballSetup");
                pass &= Check("GameManager.ballSetup assigned", p != null && p.objectReferenceValue != null);
            }
            else
            {
                pass &= Check("GameManager.ballSetup assigned", false);
            }

            Debug.Log($"[Self-Test] ChinesePool BallSetup Fix: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }

        private static bool Check(string name, bool condition)
        {
            Debug.Log($"[Self-Test] {name}: {(condition ? "✅" : "❌")}");
            return condition;
        }
    }
}
