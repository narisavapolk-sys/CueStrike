using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.Gameplay;
using CueStrike.Gameplay.ChinesePool;
using CueStrike.MascotSystem;

namespace CueStrike.EditorTools
{
    /// <summary>R44 — wires BallPottedTracker into the Chinese Pool game loop.</summary>
    public static class PocketGameLoopSetup
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity";
        private const string RootName = "PocketGameLoop";

        [MenuItem("Tools/CueStrike/Gameplay/160. Setup Pocket Game Loop")]
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
            var tracker = Object.FindAnyObjectByType<BallPottedTracker>(FindObjectsInactive.Include);
            var gm = Object.FindAnyObjectByType<ChinesePoolGameManager>(FindObjectsInactive.Include);
            var setup = Object.FindAnyObjectByType<ChinesePoolBallSetup>(FindObjectsInactive.Include);
            var bo = Object.FindAnyObjectByType<BoReferee>(FindObjectsInactive.Include);
            if (tracker == null || gm == null || setup == null)
            {
                Debug.LogError("[PocketGameLoop] Missing tracker, GameManager, or BallSetup.");
                return false;
            }
            var root = GameObject.Find(RootName) ?? new GameObject(RootName);
            var bridge = root.GetComponent<PocketGameLoopBridge>() ?? root.AddComponent<PocketGameLoopBridge>();
            var so = new SerializedObject(bridge);
            so.FindProperty("_tracker").objectReferenceValue = tracker;
            so.FindProperty("_gameManager").objectReferenceValue = gm;
            so.FindProperty("_ballSetup").objectReferenceValue = setup;
            if (bo != null) so.FindProperty("_boReferee").objectReferenceValue = bo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return SelfTest();
        }

        public static bool SelfTest()
        {
            var bridge = Object.FindAnyObjectByType<PocketGameLoopBridge>(FindObjectsInactive.Include);
            bool ok = bridge != null;
            if (bridge != null)
            {
                var so = new SerializedObject(bridge);
                ok &= so.FindProperty("_tracker").objectReferenceValue != null;
                ok &= so.FindProperty("_gameManager").objectReferenceValue != null;
                ok &= so.FindProperty("_ballSetup").objectReferenceValue != null;
            }
            Debug.Log($"[Self-Test] Pocket Game Loop: {(ok ? "PASS 5/5" : "FAIL")}");
            return ok;
        }
    }
}
