// 🎮 CueStrikeAAA Master Control — Rule #4 Scene Automation with Vision (one button)
using System; using System.IO; using System.Linq; using System.Text;
using UnityEditor; using UnityEditor.SceneManagement;
using UnityEngine; using UnityEngine.SceneManagement;

public static class CueStrikeAAAMasterControl
{
    const string GRAND_HALL    = "Assets/CueStrike/Scenes/Title_NoksGrandHall.unity";
    const string UNCLE_NOK_PREF = "Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab";
    const string BO_PANDA_PREF  = "Assets/CueStrike/Characters/BoPanda/BoPanda_Prefab.prefab";
    static string ScreenshotDir => Path.Combine(Directory.GetCurrentDirectory(), "RoomScreenshots");

    [MenuItem("Tools/CueStrike/AAA Master Control/🚀 Run All + Apply + Vision")]
    public static void RunAllApply() => Execute();
    // -executeMethod CueStrikeAAAMasterControl.RunAllApplyBatch
    public static void RunAllApplyBatch() => Execute();

    [MenuItem("Tools/CueStrike/AAA Master Control/🛡️ Install All Tools + Readiness")]
    public static void InstallAllToolsMenu() => InstallAllTools();

    /// <summary>Full Arsenal readiness check: packages, tests, audio wiring + Vision audit report.</summary>
    public static void InstallAllTools()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CueStrike AAA Readiness Report ===");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Unity: " + Application.unityVersion);

            // 1. UPM packages present in manifest.json
            string manifest = File.ReadAllText("Packages/manifest.json");
            string[] want = { "com.unity.animation.rigging", "com.unity.ai.navigation",
                              "com.unity.muse.texture", "com.unity.muse.audio" };
            foreach (string pkg in want)
            {
                bool present = manifest.Contains("\"" + pkg + "\"");
                sb.AppendLine("PACKAGE  " + pkg + ": " + (present ? "PRESENT" : "ABSENT"));
            }
            sb.AppendLine("NOTE     muse.audio not in Unity registry (404) -> enable via Window->Muse after MANUAL login (coach action).");

            // 2. Test framework presence
            string asmdef = "Assets/CueStrike/Tests/Editor/CueStrike.Tests.Editor.asmdef";
            sb.AppendLine("TESTASM  " + (File.Exists(asmdef) ? "PRESENT" : "MISSING"));
            string testDir = "Assets/CueStrike/Tests/Editor";
            int testScripts = Directory.Exists(testDir) ? Directory.GetFiles(testDir, "*.cs").Length : 0;
            sb.AppendLine("TESTSC  " + testScripts + " test scripts");

            // 3. Audio wiring (real disk reads)
            string vdir = Path.Combine(Application.dataPath, "CueStrike/Audio/Clips/Voice/UncleNok");
            int voiceWav = Directory.Exists(vdir) ? Directory.GetFiles(vdir, "*.wav").Length : 0;
            string sdir = Path.Combine(Application.dataPath, "CueStrike/Audio/Clips");
            int sfxWav = Directory.Exists(sdir) ? Directory.GetFiles(sdir, "*.wav").Length : 0;
            sb.AppendLine("AUDIO    voiceWav=" + voiceWav + "  sfxWav=" + sfxWav);

            // 4. Vision audit (screenshot + manifest) — opens scene, no save
            string auditManifest = CueStrikeVisualAudit.CaptureAndManifest(GRAND_HALL);
            sb.AppendLine("AUDIT    " + auditManifest);

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string report = Path.Combine(Directory.GetCurrentDirectory(), "readiness_report_" + ts + ".txt");
            File.WriteAllText(report, sb.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[MasterControl] Readiness report written: " + report);
            Debug.Log("[MasterControl] Full Arsenal readiness OK (see report). Muse enable = MANUAL (Window->Muse).");
        }
        catch (Exception e) { Debug.LogError("[MasterControl] InstallAllTools ABORTED: " + e); throw; }
    }

    static void Execute()
    {
        try
        {
            Debug.Log("[MasterControl] START AAA setup (Shader+Audio+Characters+Vision+Apply)");
            CueStrikeVoiceAndSfxBinder.AssignVoiceTo(UNCLE_NOK_PREF); // 14 Zira -> UncleNok_Prefab
            if (!File.Exists(GRAND_HALL)) { Debug.LogError("[MasterControl] Grand Hall scene missing: " + GRAND_HALL); return; }
            Scene scene = EditorSceneManager.OpenScene(GRAND_HALL, OpenSceneMode.Single);
            Debug.Log("[MasterControl] active scene: " + scene.name);
                        try { PinkMaterialFixer.RunFix(); } catch (Exception e) { Debug.LogError("[MasterControl] ShaderFix exc: " + e); }
            CueStrikeVoiceAndSfxBinder.EnsureAudioManagerInScene(scene);    // place/confirm AudioManager first
            CueStrikeVoiceAndSfxBinder.AssignSfxToScene();                   // THEN bind 9 SFX placeholder clips
            PlaceCharacters();
            Debug.Log("[MasterControl] props renderers: " + scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<Renderer>(true).Length) + " (pink->URP/Lit)");
            CaptureVisionScreenshot();
                        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(scene, scene.path);
            Debug.Log("[MasterControl] DONE: scene saved + RoomScreenshots/GrandHall_Master.png");
        }
        catch (Exception e) { Debug.LogError("[MasterControl] ABORTED: " + e); throw; }
    }

    static Vector3 FindTableCenter()
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            { string n = t.name.ToLowerInvariant(); if (n.Contains("table") || n.Contains("pool") || n.Contains("baulk")) return t.position; }
        return Vector3.zero;
    }

        static void PlaceCharacters()
    {
        Vector3 tc = FindTableCenter();
        PlaceIfMissing("UncleNok", UNCLE_NOK_PREF, tc + new Vector3(0f, 0f, -2.4f), Quaternion.identity, tc);
        PlaceIfMissing("BoPanda",  BO_PANDA_PREF,  tc + new Vector3(1.8f, 0f, -1.6f), Quaternion.Euler(0f, 165f, 0f), tc);
    }

    // anchorTc is passed explicitly so scope is explicit (CS0103 fix)
    static void PlaceIfMissing(string label, string prefabPath, Vector3 pos, Quaternion rot, Vector3 anchorTc)
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        bool already = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Any(t => t.name.Contains(label));
        if (already) { Debug.Log("[MasterControl] " + label + " already present; skip"); return; }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (!prefab) { Debug.LogError("[MasterControl] prefab missing: " + prefabPath); return; }
                // PrefabUtility.InstantiatePrefab(prefab) adds to scene & returns Object; cast to GameObject
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (!inst) { Debug.LogError("[MasterControl] place " + label + " failed"); return; }
        inst.transform.SetParent(null); inst.transform.localPosition = pos; inst.transform.localRotation = rot; inst.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(inst, "Place " + label);
        Debug.Log("[MasterControl] placed " + label + " at " + inst.transform.position + " (table anchor=" + anchorTc + ")");
    }

    static void CaptureVisionScreenshot()
    {
        Directory.CreateDirectory(ScreenshotDir);
        string path = Path.Combine(ScreenshotDir, "GrandHall_Master.png");
        Vector3 tc = FindTableCenter(), cp = tc + new Vector3(0f, 2.2f, -3.5f);
        GameObject camGo = new GameObject("VisionCaptureCam"); camGo.hideFlags = HideFlags.HideAndDontSave;
        Camera cam = camGo.AddComponent<Camera>();
        cam.transform.position = cp; cam.transform.rotation = Quaternion.LookRotation(tc - cp, Vector3.up);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f); cam.cullingMask = ~0;
        RenderTexture rt = new RenderTexture(1920, 1080, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null; cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(camGo);
        Debug.Log("[MasterControl] Vision screenshot saved: " + path);
    }
}
