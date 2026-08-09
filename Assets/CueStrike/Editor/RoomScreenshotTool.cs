using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RoomScreenshotTool
{
    static readonly string[] Scenes = {
        "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity",
        "Assets/CueStrike/Scenes/Cyberpunk/Cyberpunk_Room.unity",
        "Assets/CueStrike/Scenes/GrandArena/GrandArena_Room.unity",
        "Assets/CueStrike/Scenes/Industrial/Industrial_Room.unity",
        "Assets/CueStrike/Scenes/Luxury/Luxury_Room.unity",
        "Assets/CueStrike/Scenes/SpaceNebula/SpaceNebula_Room.unity",
        "Assets/CueStrike/Scenes/WarpFantasy/WarpFantasy_Room.unity",
        "Assets/CueStrike/Scenes/ZenDojo/ZenDojo_Room.unity"
    };

    [MenuItem("Tools/CueStrike/Dev/Capture Room Screenshots")]
    public static void CaptureAll()
    {
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "RoomScreenshots");
        Directory.CreateDirectory(dir);

        foreach (string scenePath in Scenes)
        {
            if (!File.Exists(scenePath)) { Debug.LogError("[RoomShot] Missing: " + scenePath); continue; }
            EditorSceneManager.OpenScene(scenePath);
            Camera cam = Camera.main;
            if (cam == null) { cam = Object.FindObjectOfType<Camera>(); }
            if (cam == null) { Debug.LogError("[RoomShot] No camera in " + scenePath); continue; }

            string name = Path.GetFileNameWithoutExtension(scenePath).Replace(" ", "_");
            string path = Path.Combine(dir, name + ".png");

            RenderTexture rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(tex);
            Debug.Log("[RoomShot] Saved " + path);
        }

        AssetDatabase.Refresh();
        Debug.Log("[RoomShot] ALL DONE!");
    }
}
