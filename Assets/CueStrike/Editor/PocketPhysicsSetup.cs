using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.Gameplay;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R43 — Editor tool: เพิ่ม pocket detection + ฟิสิกส์หลุมใน AAA_RoomDAY
    ///
    /// - เพิ่ม tags "Ball" + "Pocket" ใน TagManager
    /// - สร้าง pocket 6 จุด (SphereCollider trigger + Pocket.cs) บนโต๊ะ AAA Table 12ft
    /// - เพิ่ม BallPottedTracker + assign pocket positions
    /// - Idempotent: รันซ้ำ skip + self-test + batchmode
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.PocketPhysicsSetup.RunFromBatch
    /// </summary>
    public static class PocketPhysicsSetup
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity";
        private const string PocketsGOName = "CueStrike_Pockets";
        private const string TrackerGOName = "BallPottedTracker";

        // ตำแหน่งหลุมบนโต๊ะ AAA (scale 4×8, origin (0,0.4,0)) — มุม 4 + กลางขอบสั้น 2
        private static readonly Vector3[] PocketPositions =
        {
            new Vector3(-1.8f, 0.42f, -3.5f), // มุมซ้ายล่าง
            new Vector3( 1.8f, 0.42f, -3.5f), // มุมขวาล่าง
            new Vector3(-1.8f, 0.42f,  3.5f), // มุมซ้ายบน
            new Vector3( 1.8f, 0.42f,  3.5f), // มุมขวาบน
            new Vector3( 0f,   0.42f, -3.5f), // กลางล่าง
            new Vector3( 0f,   0.42f,  3.5f), // กลางบน
        };

        [MenuItem("Tools/CueStrike/Gameplay/150. Setup AAA Pocket Detection")]
        public static void SetupFromMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[PocketPhysics] Cannot run in Play Mode.");
                return;
            }

            bool ok = Run();
            Debug.Log(ok ? "[PocketPhysics] ✅ Setup complete — balls can drop into pockets in AAA_RoomDAY."
                          : "[PocketPhysics] ❌ Setup failed — see errors above.");
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
            bool pass = true;

            pass &= EnsureTags();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (scene == null || !scene.IsValid())
            {
                Debug.LogError($"[PocketPhysics] Cannot open scene: {ScenePath}");
                return false;
            }

            // 1. Pocket group (6 pockets)
            var pocketsGroup = GameObject.Find(PocketsGOName);
            if (pocketsGroup == null)
            {
                pocketsGroup = new GameObject(PocketsGOName);
                pocketsGroup.transform.position = Vector3.zero;
                for (int i = 0; i < PocketPositions.Length; i++)
                {
                    CreatePocket(pocketsGroup.transform, $"Pocket_{i + 1}", PocketPositions[i]);
                }
                Debug.Log($"[PocketPhysics] Created {PocketPositions.Length} pockets on AAA table.");
            }
            else
            {
                Debug.Log("[PocketPhysics] Pockets already present — idempotent skip.");
            }

            // 2. BallPottedTracker
            var tracker = UnityEngine.Object.FindAnyObjectByType<BallPottedTracker>();
            if (tracker == null)
            {
                var trackerGO = new GameObject(TrackerGOName);
                tracker = trackerGO.AddComponent<BallPottedTracker>();
                tracker.SetPocketPositions(PocketPositions);
                tracker.SetBallTransforms(FindBallTransforms());
                tracker.StartTracking();
                Debug.Log("[PocketPhysics] Added BallPottedTracker + pocket positions + ball transforms.");
            }
            else
            {
                Debug.Log("[PocketPhysics] BallPottedTracker already present — idempotent skip.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PocketPhysics] Scene saved: {ScenePath}");

            bool selfTestOk = RunSelfTest();
            return pass && selfTestOk;
        }

        private static void CreatePocket(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.18f;

            go.AddComponent<Pocket>();
        }

        private static Transform[] FindBallTransforms()
        {
            var balls = GameObject.FindGameObjectsWithTag("Ball");
            var transforms = new Transform[Mathf.Max(balls.Length, 15)];
            foreach (var ball in balls)
            {
                var id = ball.GetComponent<ChinesePoolBallIdentifier>();
                if (id == null) continue;
                if (id.ballId >= 1 && id.ballId <= 15)
                {
                    transforms[id.ballId - 1] = ball.transform;
                }
            }
            return transforms;
        }

        private static bool EnsureTags()
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null || tagManager.Length == 0)
            {
                Debug.LogError("[PocketPhysics] Cannot load TagManager.");
                return false;
            }

            var so = new SerializedObject(tagManager[0]);
            var tagsProp = so.FindProperty("tags");
            if (tagsProp == null)
            {
                Debug.LogError("[PocketPhysics] tags property not found on TagManager.");
                return false;
            }

            bool changed = false;
            if (!HasTag(tagsProp, "Ball"))
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Ball";
                changed = true;
                Debug.Log("[PocketPhysics] Added tag 'Ball'.");
            }
            if (!HasTag(tagsProp, "Pocket"))
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Pocket";
                changed = true;
                Debug.Log("[PocketPhysics] Added tag 'Pocket'.");
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.Log("[PocketPhysics] Tags already present — idempotent skip.");
            }

            return true;
        }

        private static bool HasTag(SerializedProperty tagsProp, string tag)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return true;
            }
            return false;
        }

        [MenuItem("Tools/CueStrike/Debug/Test AAA Pocket Detection")]
        public static void TestFromMenu()
        {
            bool ok = RunSelfTest();
            Debug.Log(ok ? "[Self-Test] AAA Pocket Detection: ALL PASS" : "[Self-Test] AAA Pocket Detection: SOME FAILED");
        }

        public static bool RunSelfTest()
        {
            bool pass = true;
            var tracker = UnityEngine.Object.FindAnyObjectByType<BallPottedTracker>();
            var pockets = UnityEngine.Object.FindObjectsByType<Pocket>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            pass &= Check("Tags Ball+Pocket exist", TagExists("Ball") && TagExists("Pocket"));
            pass &= Check("BallPottedTracker present", tracker != null);
            pass &= Check("Pockets present (>= 4)", pockets != null && pockets.Length >= 4);
            pass &= Check("Pockets are triggers", pockets != null && Array.TrueForAll(pockets, p => {
                var col = p.GetComponent<SphereCollider>();
                return col != null && col.isTrigger;
            }));

            Debug.Log($"[Self-Test] AAA Pocket Detection: {(pass ? "PASS 4/4" : "FAIL")}");
            return pass;
        }

        private static bool TagExists(string tag)
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null || tagManager.Length == 0) return false;
            var so = new SerializedObject(tagManager[0]);
            var tagsProp = so.FindProperty("tags");
            if (tagsProp == null) return false;
            return HasTag(tagsProp, tag);
        }

        private static bool Check(string name, bool condition)
        {
            Debug.Log($"[Self-Test] {name}: {(condition ? "✅" : "❌")}");
            return condition;
        }
    }
}
