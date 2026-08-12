using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike;
using CueStrike.AI;

namespace CueStrike.Editor
{
    /// <summary>
    /// R36 — Snooker AI Setup: เตรียม Snooker_Demo ให้ AI เล่นสนุกเกอร์ได้จริง.
    ///
    /// 1. สร้างโต๊ะ (พื้น + rails) ให้ลูกกลิ้งไม่ตก
    /// 2. สร้าง 6 pockets (มุม 4 + กลาง 2)
    /// 3. เพิ่ม Rigidbody + SphereCollider ให้ลูกทุกตัวที่ยังไม่มี
    /// 4. เพิ่ม CueStrikeSnookerAIBridge + assign refs
    ///
    /// Idempotent: รันซ้ำไม่สร้างซ้ำ / skip ถ้ามีครบ. Self-test + batchmode พร้อม.
    /// </summary>
    public static class SnookerAISetup
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/Snooker_Demo.unity";
        private const string BridgeGOName = "SnookerAI_Bridge";
        private const string TableName = "SnookerTable_Physics";
        private const string PocketsParentName = "SnookerPockets";

        // ตำแหน่งลูกใน scene: x ±1.2, z -0.9..1.5 → โต๊ะขยายออกอีกนิด
        private static readonly Vector3[] DefaultPocketPositions =
        {
            new Vector3(-1.35f, 0.38f, -1.25f), // มุมซ้ายล่าง
            new Vector3( 1.35f, 0.38f, -1.25f), // มุมขวาล่าง
            new Vector3(-1.35f, 0.38f,  1.85f), // มุมซ้ายบน
            new Vector3( 1.35f, 0.38f,  1.85f), // มุมขวาบน
            new Vector3( 0f,    0.38f, -1.25f), // กลางล่าง
            new Vector3( 0f,    0.38f,  1.85f), // กลางบน
        };

        [MenuItem("Tools/CueStrike/Snooker/100. Setup Snooker AI (Snooker_Demo)")]
        public static void SetupFromMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[SnookerAI] Cannot run in Play Mode.");
                return;
            }
            bool ok = Run();
            Debug.Log(ok ? "[SnookerAI] ✅ Setup complete — Snooker AI ready (physics + pockets + bridge)."
                          : "[SnookerAI] ❌ Setup failed — see errors above.");
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
                Debug.LogError($"[SnookerAI] Cannot open scene: {ScenePath}");
                return false;
            }

            bool pass = true;
            pass &= CreateTable();
            pass &= CreatePockets();
            pass &= EnsureBallPhysics();
            var bridge = EnsureBridge();
            pass &= bridge != null;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SnookerAI] Scene saved: {ScenePath}");

            bool selfTest = RunSelfTest();
            return pass && selfTest;
        }

        // ---------- 1. Table ----------

        private static bool CreateTable()
        {
            var existing = GameObject.Find(TableName);
            if (existing != null)
            {
                Debug.Log("[SnookerAI] Table already present — idempotent skip.");
                return true;
            }

            var table = new GameObject(TableName);
            table.transform.position = new Vector3(0f, 0.38f, 0.3f);

            // พื้นโต๊ะ (baize) — BoxCollider เป็นพื้นให้ลูกกลิ้ง
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Table_Bed";
            bed.transform.SetParent(table.transform, false);
            bed.transform.localScale = new Vector3(2.9f, 0.02f, 3.3f);
            bed.transform.localPosition = Vector3.zero;
            var bedRb = bed.GetComponent<Rigidbody>();
            if (bedRb != null) Object.DestroyImmediate(bedRb);
            bed.GetComponent<Collider>().isTrigger = false;

            // Rails (ขอบโต๊ะ 4 ด้าน) — กันลูกตก
            CreateRail(table.transform, "Rail_Top", new Vector3(0f, 0.04f, 1.66f), new Vector3(2.95f, 0.08f, 0.08f));
            CreateRail(table.transform, "Rail_Bottom", new Vector3(0f, 0.04f, -1.06f), new Vector3(2.95f, 0.08f, 0.08f));
            CreateRail(table.transform, "Rail_Left", new Vector3(-1.47f, 0.04f, 0.3f), new Vector3(0.08f, 0.08f, 2.8f));
            CreateRail(table.transform, "Rail_Right", new Vector3(1.47f, 0.04f, 0.3f), new Vector3(0.08f, 0.08f, 2.8f));

            Debug.Log("[SnookerAI] Table created (bed + 4 rails).");
            return true;
        }

        private static void CreateRail(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = pos;
            rail.transform.localScale = scale;
            var rb = rail.GetComponent<Rigidbody>();
            if (rb != null) Object.DestroyImmediate(rb);
        }

        // ---------- 2. Pockets ----------

        private static bool CreatePockets()
        {
            var parent = GameObject.Find(PocketsParentName);
            if (parent != null)
            {
                Debug.Log("[SnookerAI] Pockets already present — idempotent skip.");
                return true;
            }

            parent = new GameObject(PocketsParentName);
            foreach (var pos in DefaultPocketPositions)
            {
                var pocket = new GameObject("Pocket");
                pocket.transform.SetParent(parent.transform, false);
                pocket.transform.position = pos;
                var col = pocket.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.14f;
            }
            Debug.Log($"[SnookerAI] Created {DefaultPocketPositions.Length} pockets.");
            return true;
        }

        // ---------- 3. Ball physics ----------

        private static bool EnsureBallPhysics()
        {
            int fixedCount = 0;
            var identities = Object.FindObjectsByType<BallIdentity>(FindObjectsSortMode.None);
            foreach (var identity in identities)
            {
                if (identity == null) continue;
                var go = identity.gameObject;

                if (go.GetComponent<Collider>() == null)
                {
                    go.AddComponent<SphereCollider>();
                    fixedCount++;
                }
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = go.AddComponent<Rigidbody>();
                    rb.mass = 0.14f;
                    rb.drag = 0.35f;
                    rb.angularDrag = 0.6f;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    fixedCount++;
                }
            }
            Debug.Log($"[SnookerAI] Ball physics ensured ({identities.Length} balls, {fixedCount} fixes).");
            return identities.Length > 0;
        }

        // ---------- 4. Bridge ----------

        private static CueStrikeSnookerAIBridge EnsureBridge()
        {
            var bridge = Object.FindFirstObjectByType<CueStrikeSnookerAIBridge>();
            if (bridge != null)
            {
                Debug.Log("[SnookerAI] Bridge already present — idempotent skip.");
            }
            else
            {
                var go = new GameObject(BridgeGOName);
                bridge = go.AddComponent<CueStrikeSnookerAIBridge>();
                Debug.Log("[SnookerAI] Bridge added.");
            }

            var so = new SerializedObject(bridge);

            // ruleset
            var ruleset = Object.FindFirstObjectByType<CueStrikeWBPSRuleset>();
            if (ruleset != null)
            {
                var p = so.FindProperty("ruleset");
                if (p != null && p.objectReferenceValue == null) p.objectReferenceValue = ruleset;
            }

            // pocket positions
            var pocketsProp = so.FindProperty("pocketPositions");
            if (pocketsProp != null && pocketsProp.arraySize == 0)
            {
                pocketsProp.arraySize = DefaultPocketPositions.Length;
                for (int i = 0; i < DefaultPocketPositions.Length; i++)
                {
                    pocketsProp.GetArrayElementAtIndex(i).vector3Value = DefaultPocketPositions[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[SnookerAI] Bridge configured: ruleset={ruleset != null}, pockets={pocketsProp?.arraySize ?? 0}.");
            return bridge;
        }

        // ---------- Self-test ----------

        [MenuItem("Tools/CueStrike/Debug/Test Snooker AI")]
        public static void TestFromMenu()
        {
            bool ok = RunSelfTest();
            Debug.Log(ok ? "[Self-Test] Snooker AI: ALL PASS" : "[Self-Test] Snooker AI: SOME FAILED");
        }

        public static bool RunSelfTest()
        {
            bool pass = true;
            pass &= Check("SnookerAIBridge exists", Object.FindFirstObjectByType<CueStrikeSnookerAIBridge>() != null);
            pass &= Check("WBPS ruleset exists", Object.FindFirstObjectByType<CueStrikeWBPSRuleset>() != null);
            pass &= Check("Pockets exist (≥6)", Object.FindObjectsByType<SphereCollider>(FindObjectsSortMode.None).Length >= 6);
            pass &= Check("Balls have Rigidbody", CountRigidbodyBalls() >= 20);

            var bridge = Object.FindFirstObjectByType<CueStrikeSnookerAIBridge>();
            if (bridge != null)
            {
                var so = new SerializedObject(bridge);
                var p = so.FindProperty("pocketPositions");
                pass &= Check("Bridge pocket positions set", p != null && p.arraySize >= 6);
                var r = so.FindProperty("ruleset");
                pass &= Check("Bridge ruleset assigned", r != null && r.objectReferenceValue != null);
            }

            Debug.Log($"[Self-Test] Snooker AI: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }

        private static int CountRigidbodyBalls()
        {
            int count = 0;
            foreach (var identity in Object.FindObjectsByType<BallIdentity>(FindObjectsSortMode.None))
            {
                if (identity != null && identity.GetComponent<Rigidbody>() != null) count++;
            }
            return count;
        }

        private static bool Check(string name, bool condition)
        {
            Debug.Log($"[Self-Test] {name}: {(condition ? "✅" : "❌")}");
            return condition;
        }
    }
}
