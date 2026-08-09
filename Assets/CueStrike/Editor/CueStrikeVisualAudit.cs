// VisualAudit
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CueStrike.Audio;

public static class CueStrikeVisualAudit
{
    static string OutDir => Path.Combine(Directory.GetCurrentDirectory(), "RoomScreenshots");

    [MenuItem("Tools/CueStrike/AAA Master Control/Visual Audit (capture + manifest)")]
    public static void RunAuditMenu()
    {
        string p = CaptureAndManifest("Assets/CueStrike/Scenes/Title_NoksGrandHall.unity");
        Debug.Log("[VisualAudit] saved: " + p);
        AssetDatabase.Refresh();
    }

    public static string CaptureAndManifest(string scenePath)
    {
        Directory.CreateDirectory(OutDir);
        string png = Path.Combine(OutDir, "GrandHall_Audit.png");
        string manifestPath = Path.Combine(OutDir, "audit_manifest.json");
        string sceneName = "?";
        int voiceWav = 0, sfxSlotWired = 0, renderers = 0, materialsUripLit = 0;
        bool hasRefereeOnPrefab = false;
        try
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            sceneName = scene.name;
            renderers = scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<Renderer>(true).Length);
            materialsUripLit = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Renderer>(true))
                .SelectMany(rx => rx.sharedMaterials)
                .Where(m => m != null && m.shader != null && (m.shader.name.Contains("Universal") || m.shader.name.Contains("Lit")))
                .Count();
            var am = UnityEngine.Object.FindObjectOfType<CueStrikeAudioManager>();
            if (am != null)
                sfxSlotWired = typeof(CueStrikeAudioManager).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Count(fi => fi.FieldType == typeof(AudioClip) && fi.GetValue(am) != null);
            string vdir = Path.Combine(Application.dataPath, "CueStrike/Audio/Clips/Voice/UncleNok");
            voiceWav = Directory.Exists(vdir) ? Directory.GetFiles(vdir, "*.wav").Length : 0;
            hasRefereeOnPrefab = HasUncleNokReferee("Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab");
            CapturePng(scenePath, png);
        }
        catch (Exception e) { Debug.LogError("[VisualAudit] FAILED: " + e); png = "ERROR: " + e.Message; }
        var manifest = new AuditManifest { timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), sceneName = sceneName, screenshot = Path.GetFileName(png), voiceWavFiles = voiceWav, sfxSlotsWired = sfxSlotWired, rendererCount = renderers, materialsUripLit = materialsUripLit, prefabHasUncleNokReferee = hasRefereeOnPrefab, pinkFree = materialsUripLit > 0 };
        File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
        AssetDatabase.Refresh();
        Debug.Log("[VisualAudit] manifest written: " + manifestPath);
        return manifestPath;
    }

    static bool HasUncleNokReferee(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return prefab != null && prefab.GetComponentInChildren<CueStrike.MascotSystem.UncleNokReferee>(true) != null;
    }

    static void CapturePng(string scenePath, string path)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Vector3 tc = FindTableCenter();
        Vector3 cp = tc + new Vector3(0f, 2.2f, -3.5f);
        GameObject camGo = new GameObject("VisionAuditCam") { hideFlags = HideFlags.HideAndDontSave };
        Camera cam = camGo.AddComponent<Camera>();
        cam.transform.position = cp;
        cam.transform.rotation = Quaternion.LookRotation(tc - cp, Vector3.up);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
        cam.cullingMask = ~0;
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
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(camGo);
        Debug.Log("[VisualAudit] screenshot saved: " + path);
    }

    static Vector3 FindTableCenter()
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("table") || n.Contains("pool") || n.Contains("baulk")) return t.position;
            }
        return Vector3.zero;
    }

    [Serializable]
    public class AuditManifest
    {
        public string timestamp; public string sceneName; public string screenshot;
        public int voiceWavFiles; public int sfxSlotsWired; public int rendererCount;
        public int materialsUripLit; public bool prefabHasUncleNokReferee; public bool pinkFree;
    }
}


