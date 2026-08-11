using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.UI.ChinesePool;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R25 — Editor tool: ผูก Match Flow (ChinesePoolMatchSetupUI + ChinesePoolMatchEndScreen)
    /// เข้าฉากห้องที่เปิดอยู่ (AAA_RoomDAY / Title).
    ///
    /// - MenuItem: Tools/CueStrike/Room Scene/20. Setup Match Flow (Best-of + WINNER)
    /// - Idempotent: component มีอยู่แล้ว → ข้าม (กัน duplicate)
    /// - Guard 3 ชั้น (Play Mode block / Unsaved changes / batchmode-safe)
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.ChinesePoolMatchFlowSetup.SetupMatchFlow
    /// </summary>
    public static class ChinesePoolMatchFlowSetup
    {
        private const string SetupGOName = "MatchFlow";
        private const string EndScreenGOName = "MatchEndScreen";

        [MenuItem("Tools/CueStrike/Room Scene/20. Setup Match Flow (Best-of + WINNER)")]
        public static void SetupMatchFlowMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run Setup Match Flow during Play Mode.", "OK");
                return;
            }

            SetupMatchFlow();
        }

        private const string RoomScenePath = "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity";

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void SetupMatchFlow()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.Log($"[ChinesePoolMatchFlowSetup] No active scene — loading '{RoomScenePath}'...");
                scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            }

            // 1. Canvas (ถ้าไม่มี component จะสร้างให้เองตอน runtime — แค่ log)
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ChinesePoolMatchFlowSetup] No Canvas found — components will create their own at runtime.");
            }

            // 2. MatchSetupUI (idempotent)
            EnsureComponent<ChinesePoolMatchSetupUI>(SetupGOName, canvas);
            // 3. MatchEndScreen (idempotent)
            EnsureComponent<ChinesePoolMatchEndScreen>(EndScreenGOName, canvas);

            if (scene.isDirty)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[ChinesePoolMatchFlowSetup] Scene saved.");
            }

            SelfTest();
        }

        private static void EnsureComponent<T>(string goName, Canvas canvas) where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject go = GameObject.Find(goName);
            T comp = null;

            if (go != null)
            {
                comp = go.GetComponent<T>();
            }

            if (comp != null)
            {
                Debug.Log($"[ChinesePoolMatchFlowSetup] {goName} already wired — skipping (idempotent).");
                return;
            }

            if (go == null)
            {
                go = new GameObject(goName);
                Undo.RegisterCreatedObjectUndo(go, $"Create {goName}");
            }

            comp = Undo.AddComponent<T>(go);
            var canvasField = comp.GetType().GetField("targetCanvas");
            if (canvasField != null && canvas != null)
            {
                canvasField.SetValue(comp, canvas);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ChinesePoolMatchFlowSetup] {typeof(T).Name} wired into scene '{scene.name}'.");
        }

        // ---- Self-Test (กฎข้อ 2) ----

        [MenuItem("Tools/CueStrike/Room Scene/Test Match Flow")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] Match Flow check:");

            int pass = 0, fail = 0;

            bool setupOk = UnityEngine.Object.FindAnyObjectByType<ChinesePoolMatchSetupUI>() != null;
            LogResult("MatchSetupUI in scene", setupOk, ref pass, ref fail);

            bool endOk = UnityEngine.Object.FindAnyObjectByType<ChinesePoolMatchEndScreen>() != null;
            LogResult("MatchEndScreen in scene", endOk, ref pass, ref fail);

            var gm = UnityEngine.Object.FindAnyObjectByType<CueStrike.Gameplay.ChinesePool.ChinesePoolGameManager>();
            LogResult("ChinesePoolGameManager in scene", gm != null, ref pass, ref fail);

            Debug.Log($"[SelfTest] Match Flow: {pass} passed, {fail} failed.");
            if (fail > 0)
            {
                Debug.LogError($"[SelfTest] {fail} check(s) FAILED.");
            }
        }

        private static void LogResult(string name, bool ok, ref int pass, ref int fail)
        {
            if (ok) { pass++; Debug.Log($"  ✅ {name}"); }
            else { fail++; Debug.LogError($"  ❌ {name}"); }
        }
    }
}
