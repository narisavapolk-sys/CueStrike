#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor script to setup exit/integration scripts in hub.unity.
/// Menu: CueStrike → Generate → Integrate Practice Hub Scene
/// </summary>
public static class PracticeHubSceneSetup
{
    private const string ScenePath = "Assets/hub.unity";

    [MenuItem("CueStrike/Generate/Integrate Practice Hub Scene")]
    public static void IntegratePracticeHub()
    {
        // Guard: Cannot run in Play Mode
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Practice Setup] Cannot setup while in Play Mode. Please exit Play Mode first.");
            EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Practice Setup] Could not open scene: {ScenePath}");
            return;
        }

        // Find or create a manager GameObject to hold our Exit button trigger
        var practiceExitGO = GameObject.Find("PracticeExitController");
        if (practiceExitGO == null)
        {
            practiceExitGO = new GameObject("PracticeExitController");
            practiceExitGO.AddComponent<CueStrike.Gameplay.CueStrikePracticeExit>();
            Debug.Log("[Practice Setup] Created PracticeExitController and attached CueStrikePracticeExit component.");
        }

        // Also ensure CueStrikePracticeManager exists in the scene
        var practiceMgr = GameObject.Find("CueStrikePracticeManager") ?? GameObject.Find("PracticeManager");
        if (practiceMgr == null)
        {
            practiceMgr = new GameObject("CueStrikePracticeManager");
            practiceMgr.AddComponent<CueStrike.Gameplay.CueStrikePracticeManager>();
            Debug.Log("[Practice Setup] Added CueStrikePracticeManager component to the scene.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Practice Hub Integrated",
            "Offline Practice Hub scene (hub.unity) successfully integrated!\n\n" +
            "Setups Done:\n" +
            "  • Added PracticeExitController (Floating world-space Exit Button)\n" +
            "  • Ensured CueStrikePracticeManager is present",
            "OK");
    }
}
#endif
