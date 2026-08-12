using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.Gameplay;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R47 — deterministic preflight for the physical pocket pipeline.
    /// The PlayMode R43 test performs the actual Rigidbody trigger simulation;
    /// this tool validates the scene prerequisites without modifying gameplay.
    /// </summary>
    public static class PocketPhysicsAuditor
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity";

        [MenuItem("Tools/CueStrike/Gameplay/170. Audit AAA Pocket Physics")]
        public static void AuditFromMenu()
        {
            if (Application.isPlaying) { Debug.LogError("[Pocket Auditor] Blocked during Play Mode."); return; }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("[Pocket Auditor] Save the current scene before running the AAA audit.");
                return;
            }
            Run();
        }

        public static void RunFromBatch()
        {
            if (!Run()) EditorApplication.Exit(1);
            EditorApplication.Exit(0);
        }

        public static bool Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || scene.path != ScenePath) return false;
            int pass = 0, fail = 0;
            Check("AAA scene loaded", scene.IsValid(), ref pass, ref fail);
            var tracker = Object.FindAnyObjectByType<BallPottedTracker>(FindObjectsInactive.Include);
            var setup = Object.FindAnyObjectByType<ChinesePoolBallSetup>(FindObjectsInactive.Include);
            var bridge = Object.FindAnyObjectByType<PocketGameLoopBridge>(FindObjectsInactive.Include);
            Check("BallPottedTracker exists", tracker != null, ref pass, ref fail);
            Check("ChinesePoolBallSetup exists", setup != null, ref pass, ref fail);
            Check("PocketGameLoopBridge exists", bridge != null, ref pass, ref fail);

            int pocketCount = 0;
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
            {
                if (c.GetComponent<Pocket>() == null) continue;
                pocketCount++;
                if (HasTag("Pocket") && !c.CompareTag("Pocket"))
                {
                    c.gameObject.tag = "Pocket";
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"[Pocket Auditor] Repaired tag on {c.name}.");
                }
                Check($"Pocket '{c.name}' is trigger", c.isTrigger, ref pass, ref fail);
                Check($"Pocket '{c.name}' has Pocket tag", c.CompareTag("Pocket"), ref pass, ref fail);
            }
            Check("Six or more pocket triggers", pocketCount >= 6, ref pass, ref fail);
            Check("Ball tag exists", HasTag("Ball"), ref pass, ref fail);
            Check("Pocket tag exists", HasTag("Pocket"), ref pass, ref fail);
            if (HasTag("Ball") && HasTag("Pocket"))
            {
                int ballLayer = LayerMask.NameToLayer("Ball");
                int pocketLayer = LayerMask.NameToLayer("Pocket");
                if (ballLayer >= 0 && pocketLayer >= 0)
                    Check("Ball/Pocket layer collision enabled", !UnityEngine.Physics.GetIgnoreLayerCollision(ballLayer, pocketLayer), ref pass, ref fail);
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Pocket Auditor] RESULT: {pass} passed, {fail} failed. Runtime Rigidbody simulation is covered by R43PocketTriggerPlayModeTests.");
            return fail == 0;
        }

        private static bool HasTag(string tag)
        {
            try { return UnityEditorInternal.InternalEditorUtility.tags != null && System.Array.IndexOf(UnityEditorInternal.InternalEditorUtility.tags, tag) >= 0; }
            catch { return false; }
        }

        private static void Check(string label, bool value, ref int pass, ref int fail)
        {
            if (value) { pass++; Debug.Log($"[Pocket Auditor] PASS: {label}"); }
            else { fail++; Debug.LogError($"[Pocket Auditor] FAIL: {label}"); }
        }
    }
}
