using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.MascotSystem;
using CueStrike.UI;

namespace CueStrike.EditorTools
{
    /// <summary>R43 — adds RefereeModeSwitcher + selector UI to Title lobby.</summary>
    public static class RefereeModeUISetup
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/Title_NoksGrandHall.unity";
        private const string RootName = "RefereeModeMenu";

        [MenuItem("Tools/CueStrike/Mascots/155. Setup Referee Mode Menu")]
        public static void SetupFromMenu() => Run();

        public static void RunFromBatch()
        {
            if (!Run()) EditorApplication.Exit(1);
            EditorApplication.Exit(0);
        }

        public static bool Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return false;
            var bo = Object.FindAnyObjectByType<BoRefereeEventBridge>(FindObjectsInactive.Include);
            if (bo == null) { Debug.LogError("[RefereeModeUI] BoRefereeEventBridge missing in Title lobby."); return false; }
            var root = GameObject.Find(RootName) ?? new GameObject(RootName);
            var switcher = root.GetComponent<RefereeModeSwitcher>() ?? root.AddComponent<RefereeModeSwitcher>();
            var switcherSo = new SerializedObject(switcher);
            switcherSo.FindProperty("_boBridge").objectReferenceValue = bo;
            switcherSo.ApplyModifiedPropertiesWithoutUndo();
            var ui = root.GetComponent<RefereeModeUI>() ?? root.AddComponent<RefereeModeUI>();
            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("_switcher").objectReferenceValue = switcher;
            uiSo.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return SelfTest();
        }

        public static bool SelfTest()
        {
            var switcher = Object.FindAnyObjectByType<RefereeModeSwitcher>(FindObjectsInactive.Include);
            var ui = Object.FindAnyObjectByType<RefereeModeUI>(FindObjectsInactive.Include);
            bool ok = switcher != null && ui != null && switcher.BoBridge != null && System.Enum.GetValues(typeof(RefereeModeSwitcher.Mode)).Length == 3;
            Debug.Log($"[Self-Test] Referee Mode UI: {(ok ? "PASS 4/4" : "FAIL")}");
            return ok;
        }
    }
}
