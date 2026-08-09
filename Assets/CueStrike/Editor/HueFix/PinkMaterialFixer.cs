// 🎨 "Pink Exorcist" — HueFix
// Rule 4 compliant: Editor-only tool. NEVER edits a .unity Scene asset directly.
//   - Coach applies via menu button (Tools → CueStrike → …), full Undo supported.
//   - Scan (Preview) is non-destructive and writes an evidence report so the
//     coach can review BEFORE applying (Vision workflow).
//   - Replaces only legacy/Built-in/HDRP/NULL-shader materials with URP/Lit;
//     custom URP shaders (felt, cloth, glass, …) are preserved.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class PinkMaterialFixer
{
    private const string MENU_PREFIX = "Tools/CueStrike/";
    private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";
    private static readonly HashSet<string> LegacyOrHdrpShaderNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Standard",
        "Standard Utilities",
        "HDRP/Lit",
        "HDRP/Unlit",
        "HDRP/Shader Graph",
        "HDRP/Unlit Shader Graph",
        "Particles/Standard Surface",
        "Legacy Shaders/Diffuse",
        "Mobile/Diffuse",
        "Mobile/Vertex Lit",
    };

    private static Shader s_UrpLit;
    private static Shader UrpLit
    {
        get
        {
            if (s_UrpLit == null)
            {
                s_UrpLit = Shader.Find(URP_LIT_SHADER) ?? Shader.Find("URP/Lit");
                if (s_UrpLit == null)
                    Debug.LogError("[PinkMaterialFixer] URP/Lit shader not found in this project.");
            }
            return s_UrpLit;
        }
    }

    [Serializable]
    private struct Entry
    {
        public string assetPath;
        public string materialName;
        public string oldShader;
        public string newShader;
        public bool applied;
    }

    // ─── Menu (coach presses these) ────────────────────────────────────────
    [MenuItem(MENU_PREFIX + "Scan Pink Materials (Preview)")]
    public static void ScanMenu()
    {
        RunScan();
    }

    [MenuItem(MENU_PREFIX + "Fix Pink Materials (Apply + Undo)")]
    public static void FixMenu()
    {
        if (!EditorUtility.DisplayDialog(
            "Pink Exorcist",
            "Apply URP/Lit fix to ALL pink/missing-shader materials? Undo supported (Edit → Undo).",
            "Apply", "Cancel"))
        {
            Debug.Log("[PinkMaterialFixer] cancelled by user.");
            return;
        }
        RunFix();
    }

    // ─── Batchmode entry points (−executeMethod) ─────────────────────────────
    public static void RunScan()
    {
        var changes = Collect(apply: false);
        var n = WriteReport("scan", changes);
        Debug.Log($"[PinkMaterialFixer] SCAN complete: {n} pink/missing-shader materials found (NONE changed). Report: {LastReportPath()}");
    }

    public static void RunFix()
    {
        var changes = Collect(apply: true);
        var n = WriteReport("fix", changes);
        Debug.Log($"[PinkMaterialFixer] FIX complete: {n} materials set to URP/Lit. Undo: Edit → Undo. Report: {LastReportPath()}");
    }


    // ─── Core ───────────────────────────────────────────────────────────────
        private static List<Entry> Collect(bool apply)
    {
        var urpLit = UrpLit;
        var changes = new List<Entry>();

        // (1) ALL Material assets in the project  (thorough: ห้ามชมพูแม้แต่จุดเดียว)
        foreach (string guid in AssetDatabase.FindAssets("t:Material"))
        {
                        TryAdd(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        // (2) Scene-instance materials not saved as assets (asset ones handled above)
        var scene = EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(scene.path))
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m)))
                            TryAdd(m);
                    }
                }
            }
        }

        void TryAdd(Material m)
        {
            if (!m) return;
            if (!IsPink(m)) return;
            string oldName = m.shader != null ? m.shader.name : "<NULL/missing>";
            var e = new Entry
            {
                assetPath = AssetDatabase.GetAssetPath(m),
                materialName = m.name,
                oldShader = oldName,
                newShader = urpLit != null ? urpLit.name : "<URP/Lit not found>",
                applied = apply && urpLit != null
            };
            if (apply && urpLit != null)
            {
                Undo.RecordObject(m, "Pink Exorcist: set URP/Lit");
                m.shader = urpLit;
                EditorUtility.SetDirty(m);
            }
            changes.Add(e);
        }

        if (apply)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        return changes;
    }

    private static bool IsPink(Material m)
    {
        if (m.shader == null) return true; // missing shader → Unity hot-pink fallback
        string n = m.shader.name;
        if (LegacyOrHdrpShaderNames.Contains(n)) return true;
        if (n.StartsWith("HDRP/", StringComparison.Ordinal)) return true;
        if (n.StartsWith("Standard ", StringComparison.Ordinal)) return true;
        return false;
    }

    private static string s_LastReport;
    private static string LastReportPath() => s_LastReport;

    private static int WriteReport(string tag, List<Entry> changes)
    {
        string dir = Path.Combine(Application.dataPath, "CueStrike/Editor/HueFix/Report");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, $"pink_report_{tag}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        s_LastReport = file;

        using (var sw = new StreamWriter(file, false))
        {
            sw.WriteLine("# Pink Exorcist — report [{0}] — {1}", tag, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sw.WriteLine("# count: {0}", changes.Count);
            sw.WriteLine("# columns: assetPath | materialName | oldShader -> newShader | applied");
            sw.WriteLine(new string('-', 70));
            foreach (var e in changes)
            {
                string line = string.Format("{0} | {1} | {2} -> {3} | applied={4}",
                    e.assetPath, e.materialName, e.oldShader, e.newShader, e.applied);
                sw.WriteLine(line);
                Debug.Log("[PinkMaterialFixer] " + line);
            }
        }
        return changes.Count;
    }
}