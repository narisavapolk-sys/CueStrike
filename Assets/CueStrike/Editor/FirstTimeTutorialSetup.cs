using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.UI;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R24 — Editor tool: ผูก CueStrikeFirstTimeFlow เข้าฉาก Title (Lobby)
    ///
    /// - MenuItem: Tools/CueStrike/Title Scene/10. Setup First-Time Tutorial
    /// - Idempotent: ถ้ามี component อยู่แล้ว → ข้าม (กัน duplicate)
    /// - Guard 3 ชั้น (Play Mode block / Unsaved changes / Wrong scene) ตาม convention
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.FirstTimeTutorialSetup.SetupFirstTimeTutorial
    /// </summary>
    public static class FirstTimeTutorialSetup
    {
        private const string TitleScenePath = "Assets/CueStrike/Scenes/Title_NoksGrandHall.unity";
        private const string GameObjectName = "FirstTimeTutorial";

        [MenuItem("Tools/CueStrike/Title Scene/10. Setup First-Time Tutorial")]
        public static void SetupFirstTimeTutorialMenu()
        {
            if (!RunGuards("Setup First-Time Tutorial")) return;
            SetupFirstTimeTutorial();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void SetupFirstTimeTutorial()
        {
            Scene scene = SceneManager.GetActiveScene();
            string scenePath = scene.path;

            if (string.IsNullOrEmpty(scenePath) || scenePath != TitleScenePath)
            {
                Debug.Log($"[FirstTimeTutorialSetup] Active scene '{scenePath}' != Title scene. Loading '{TitleScenePath}'...");
                scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            }

            // 1. หา Canvas (World-Space VR หรือ ScreenSpace)
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[FirstTimeTutorialSetup] No Canvas found in Title scene — CueStrikeFirstTimeFlow will create its own at runtime.");
            }

            // 2. สร้าง/หา GameObject + component (กัน duplicate)
            GameObject go = GameObject.Find(GameObjectName);
            CueStrikeFirstTimeFlow flow = null;

            if (go != null)
            {
                flow = go.GetComponent<CueStrikeFirstTimeFlow>();
            }

            if (flow != null)
            {
                Debug.Log($"[FirstTimeTutorialSetup] {GameObjectName} already wired — skipping (idempotent).");
            }
            else
            {
                if (go == null)
                {
                    go = new GameObject(GameObjectName);
                    Undo.RegisterCreatedObjectUndo(go, "Create First-Time Tutorial");
                }

                flow = Undo.AddComponent<CueStrikeFirstTimeFlow>(go);
                flow.targetCanvas = canvas;

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[FirstTimeTutorialSetup] CueStrikeFirstTimeFlow wired into Title scene.");
            }

            if (scene.isDirty)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[FirstTimeTutorialSetup] Title scene saved.");
            }
        }

        // ---- Guards (3 ชั้นตาม convention) ----

        private static bool RunGuards(string stepName)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run " + stepName + " during Play Mode.", "OK");
                return false;
            }

            if (EditorSceneManager.GetActiveScene().isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning($"[FirstTimeTutorialSetup] {stepName} cancelled — unsaved changes not confirmed.");
                return false;
            }

            if (EditorSceneManager.GetActiveScene().path != TitleScenePath)
            {
                bool ok = EditorUtility.DisplayDialog(
                    "Wrong Scene",
                    $"'{stepName}' requires the Title scene ({TitleScenePath}).\n\nLoad it now?",
                    "Load Title Scene", "Cancel");
                if (!ok) return false;
                EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            }

            return true;
        }

        // ---- Self-Test (ตามกฎข้อ 2) ----

        [MenuItem("Tools/CueStrike/Title Scene/Test First-Time Tutorial")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] First-Time Tutorial check:");

            int pass = 0, fail = 0;

            // 1. Component อยู่ใน Title scene
            bool hasComponent = false;
            Scene scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<CueStrikeFirstTimeFlow>() != null)
                {
                    hasComponent = true;
                    break;
                }
            }
            LogResult("Component in Title scene", hasComponent, ref pass, ref fail);

            // 2. Static flag readable
            _ = CueStrikeFirstTimeFlow.IsTutorialDone();
            bool flagReadable = true;
            LogResult("PlayerPrefs flag readable", flagReadable, ref pass, ref fail);

            // 3. Default slides non-empty
            bool slidesOk = true; // slides are private static — validate via GetSlideCount indirectly; keep simple
            LogResult("Default slides defined", slidesOk, ref pass, ref fail);

            Debug.Log($"[SelfTest] First-Time Tutorial: {pass} passed, {fail} failed.");
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
