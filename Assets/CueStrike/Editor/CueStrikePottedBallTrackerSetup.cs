#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility: places CueStrikePottedBallTracker into the active scene.
/// Menu: CueStrike -> Setup -> Add Potted Ball Tracker to Scene
/// </summary>
public static class CueStrikePottedBallTrackerSetup
{
    private const string TrackerObjectName = "CueStrike_PottedBallTracker";

    [MenuItem("CueStrike/Setup/Add Potted Ball Tracker to Scene")]
    public static void AddTrackerToScene()
    {
        // Check for duplicate
        var existing = GameObject.Find(TrackerObjectName);
        if (existing != null)
        {
            var existingComp = existing.GetComponent<CueStrike.Gameplay.CueStrikePottedBallTracker>();
            if (existingComp != null)
            {
                EditorUtility.DisplayDialog("Already Exists",
                    "CueStrikePottedBallTracker is already in the scene on '" + existing.name + "'.\nNo action needed.", "OK");
                Selection.activeGameObject = existing;
                return;
            }
            Undo.AddComponent<CueStrike.Gameplay.CueStrikePottedBallTracker>(existing);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = existing;
            ShowSuccess(existing.name);
            return;
        }

        // Prefer attaching to GameManager
        GameObject host = GameObject.Find("GameManager")
                       ?? GameObject.Find("CueStrikeGameManager")
                       ?? GameObject.Find("CueStrike_GameManager");

        if (host != null)
        {
            if (host.GetComponent<CueStrike.Gameplay.CueStrikePottedBallTracker>() == null)
                Undo.AddComponent<CueStrike.Gameplay.CueStrikePottedBallTracker>(host);
        }
        else
        {
            host = new GameObject(TrackerObjectName);
            Undo.RegisterCreatedObjectUndo(host, "Create CueStrike_PottedBallTracker");
            Undo.AddComponent<CueStrike.Gameplay.CueStrikePottedBallTracker>(host);
        }

        Selection.activeGameObject = host;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        ShowSuccess(host.name);
    }

    private static void ShowSuccess(string objectName)
    {
        EditorUtility.DisplayDialog("Potted Ball Tracker Added",
            "CueStrikePottedBallTracker added to '" + objectName + "'.\n\n" +
            "The tracker will:\n" +
            "  - Record every potted ball per player\n" +
            "  - Auto-detect Snooker / 8-Ball / 9-Ball mode\n" +
            "  - Push live updates to Scoreboard and HUD\n\n" +
            "Press Ctrl+S to save the scene.", "OK");
    }
}
#endif
