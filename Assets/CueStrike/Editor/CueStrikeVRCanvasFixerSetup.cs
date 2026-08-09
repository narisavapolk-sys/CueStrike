#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.VR;

/// <summary>
/// Editor setup for VR Canvas Fixer.
/// Menu: CueStrike -> Setup -> Fix VR Canvas (Anti Head-Lock)
/// </summary>
public static class CueStrikeVRCanvasFixerSetup
{
    [MenuItem("CueStrike/Setup/Fix VR Canvas (Anti Head-Lock)")]
    public static void AddVRCanvasFixer()
    {
        const string hostName = "CueStrike_VRCanvasFixer";

        // Check duplicate
        var existing = GameObject.Find(hostName);
        if (existing != null)
        {
            var comp = existing.GetComponent<CueStrikeVRCanvasFixer>();
            if (comp != null)
            {
                EditorUtility.DisplayDialog("Already Exists",
                    "CueStrikeVRCanvasFixer is already in the scene on '" + existing.name + "'.", "OK");
                Selection.activeGameObject = existing;
                return;
            }
            Undo.AddComponent<CueStrikeVRCanvasFixer>(existing);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = existing;
            ShowSuccess(existing.name);
            return;
        }

        // Try attaching to XR Origin or GameManager first
        GameObject host = GameObject.Find("XR Origin")
                       ?? GameObject.Find("XR Rig")
                       ?? GameObject.Find("XROrigin")
                       ?? GameObject.Find("GameManager")
                       ?? GameObject.Find("CueStrikeGameManager");

        if (host != null)
        {
            if (host.GetComponent<CueStrikeVRCanvasFixer>() == null)
                Undo.AddComponent<CueStrikeVRCanvasFixer>(host);
        }
        else
        {
            host = new GameObject(hostName);
            Undo.RegisterCreatedObjectUndo(host, "Create CueStrike_VRCanvasFixer");
            Undo.AddComponent<CueStrikeVRCanvasFixer>(host);
        }

        Selection.activeGameObject = host;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        ShowSuccess(host.name);
    }

    private static void ShowSuccess(string name)
    {
        EditorUtility.DisplayDialog("VR Canvas Fixer Added",
            "CueStrikeVRCanvasFixer added to '" + name + "'.\n\n" +
            "At runtime it will:\n" +
            "  - Detach any Canvas parented to XR Camera\n" +
            "  - Force World Space render mode on all Canvases\n" +
            "  - Place UI at a fixed world position (not following head)\n" +
            "  - Add Graphic Raycaster if missing\n\n" +
            "Press Ctrl+S to save scene.", "OK");
    }
}
#endif
