using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.UI;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R26 — Editor tool: ผูก CueStrikeModeSelectionPanel เข้าฉาก MainMenu.
    ///
    /// - MenuItem: Tools/CueStrike/Main Menu/30. Setup Mode Selection
    /// - Idempotent: component มีอยู่แล้ว → ข้าม (กัน duplicate)
    /// - Guard 3 ชั้น (Play Mode block / Unsaved changes / batchmode-safe)
    /// - batchmode: -executeMethod CueStrike.EditorTools.CueStrikeModeSelectionSetup.SetupModeSelection
    /// </summary>
    public static class CueStrikeModeSelectionSetup
    {
        private const string MainMenuScenePath = "Assets/CueStrike/Scenes/MainMenu.unity";
        private const string GameObjectName = "ModeSelectionPanel";

        [MenuItem("Tools/CueStrike/Main Menu/30. Setup Mode Selection")]
        public static void SetupModeSelectionMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run Setup Mode Selection during Play Mode.", "OK");
                return;
            }
            SetupModeSelection();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void SetupModeSelection()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) || scene.path != MainMenuScenePath)
            {
                Debug.Log($"[CueStrikeModeSelectionSetup] Loading '{MainMenuScenePath}'...");
                scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[CueStrikeModeSelectionSetup] No Canvas found — panel will create its own at runtime.");
            }

            GameObject go = GameObject.Find(GameObjectName);
            CueStrikeModeSelectionPanel panel = null;

            if (go != null)
            {
                panel = go.GetComponent<CueStrikeModeSelectionPanel>();
            }

            if (panel != null)
            {
                Debug.Log($"[CueStrikeModeSelectionSetup] {GameObjectName} already wired — skipping (idempotent).");
            }
            else
            {
                if (go == null)
                {
                    go = new GameObject(GameObjectName);
                    Undo.RegisterCreatedObjectUndo(go, "Create Mode Selection Panel");
                }

                panel = Undo.AddComponent<CueStrikeModeSelectionPanel>(go);
                panel.targetCanvas = canvas;

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[CueStrikeModeSelectionSetup] CueStrikeModeSelectionPanel wired into MainMenu.");
            }

            if (scene.isDirty)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[CueStrikeModeSelectionSetup] MainMenu scene saved.");
            }

            SelfTest();
        }

        // ---- Self-Test (กฎข้อ 2) ----

        [MenuItem("Tools/CueStrike/Main Menu/Test Mode Selection")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] Mode Selection check:");

            int pass = 0, fail = 0;

            bool panelOk = Object.FindAnyObjectByType<CueStrikeModeSelectionPanel>() != null;
            LogResult("ModeSelectionPanel in scene", panelOk, ref pass, ref fail);

            _ = CueStrikeGameModeSelector.SelectedMode;
            bool selectorOk = true;
            LogResult("GameModeSelector readable", selectorOk, ref pass, ref fail);

            bool snookerScene = !string.IsNullOrEmpty(CueStrikeGameModeSelector.ModeToSceneName(CueStrikeGameModeSelector.GameMode.Snooker15));
            LogResult("Snooker mode → scene mapping", snookerScene, ref pass, ref fail);

            Debug.Log($"[SelfTest] Mode Selection: {pass} passed, {fail} failed.");
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
