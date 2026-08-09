#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Imports Blender-generated AAA assets (FBX models + PNG textures)
/// into the CueStrike project and applies them to prefabs.
///
/// Usage: Tools → CueStrike → Blender Assets → Import & Apply
/// </summary>
public static class ImportBlenderAssets
{
    private const string BlenderExportDir = "BlenderScripts/Exports";
    private const string TextureDir = "Assets/CueStrike/Textures";
    private const string ModelDir = "Assets/CueStrike/Models";
    private const string MaterialDir = "Assets/CueStrike/Materials";

    [MenuItem("Tools/CueStrike/Blender Assets/Import All From Blender Exports")]
    public static void ImportAllBlenderAssets()
    {
        // Ensure target directories exist
        EnsureDirectory(ModelDir);
        EnsureDirectory(TextureDir);

        int imported = 0;

        // 1. Import FBX models
        imported += ImportFBX("CueStrike_PoolBalls_AAA.fbx", ModelDir + "/PoolBalls_AAA.fbx");
        imported += ImportFBX("CueStrike_Cue_AAA.fbx", ModelDir + "/Cue_AAA.fbx");
        imported += ImportFBX("Somchay_AAA.fbx", ModelDir + "/Somchay_AAA.fbx");

        // 2. Import PNG textures from BlenderScripts/Exports/Textures/ (legacy) and BlenderScripts/Exports/ (direct)
        string[] texDirs = { Path.Combine(BlenderExportDir, "Textures"), BlenderExportDir };
        bool texturesFound = false;
        foreach (string texExportDir in texDirs)
        {
            if (Directory.Exists(texExportDir))
            {
                foreach (string file in Directory.GetFiles(texExportDir, "*.png"))
                {
                    string fileName = Path.GetFileName(file);
                    string dest = TextureDir + "/" + fileName;
                    if (ImportTexture(file, dest))
                    {
                        imported++;
                        texturesFound = true;
                    }
                }
            }
        }
        if (!texturesFound)
        {
            Debug.LogWarning("[ImportBlenderAssets] No PNG textures found in Blender exports!");
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ImportBlenderAssets] ✅ Imported {imported} Blender assets. Now run Tools → CueStrike → Blender Assets → Apply to Prefabs");
    }

    [MenuItem("Tools/CueStrike/Blender Assets/Apply Character Model Only")]
    public static void ApplyCharacterModel()
    {
        string fbxPath = ModelDir + "/Somchay_AAA.fbx";
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxPrefab == null)
        {
            Debug.LogError("[ImportBlenderAssets] Somchay FBX not found at: " + fbxPath + " — run Import All first!");
            return;
        }

        // Check if character prefab exists and update it
        string charPrefabPath = "Assets/CueStrike/Prefabs/Somchay_AAA.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Prefabs"))
            AssetDatabase.CreateFolder("Assets/CueStrike", "Prefabs");

        // Always re-create the prefab from the latest FBX
        if (File.Exists(charPrefabPath))
        {
            AssetDatabase.DeleteAsset(charPrefabPath);
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(fbxPrefab, charPrefabPath);
        Debug.Log("[ImportBlenderAssets] ✅ Created/Updated AAA character prefab: " + charPrefabPath);

        // Also assign URP/Lit materials to avoid pink materials
        AssignURPMaterialsToModel(fbxPrefab);
    }

    /// <summary>Ensures using URP/Lit shader on all renderers of the imported model to avoid pink materials.</summary>
    private static void AssignURPMaterialsToModel(GameObject modelRoot)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[ImportBlenderAssets] No renderers found on model: " + modelRoot.name);
            return;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            // Try URP/Unlit first (also renders correctly in URP) before falling back to legacy shaders.
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            // Keep using URP Unlit as last resort — Standard renders pink/magenta in URP.
            Debug.LogWarning("[ImportBlenderAssets] URP shader not found — using URP Unlit. " +
                "Make sure the Universal RP package is installed.");
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        // Load Somchay textures if they were imported
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/Somchay_Albedo.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/Somchay_Normal.png");
        Texture2D rough = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/Somchay_Roughness.png");

        int converted = 0;
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null)
                {
                    m = new Material(shader);
                    mats[i] = m;
                }
                else if (m.shader != shader)
                {
                    Material upgraded = new Material(shader);
                    upgraded.name = m.name + "_URP";
                    // Try to preserve base color if source had _BaseMap or _MainTex
                    if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                        upgraded.SetTexture("_BaseMap", m.GetTexture("_BaseMap"));
                    else if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null)
                        upgraded.SetTexture("_BaseMap", m.GetTexture("_MainTex"));
                    if (m.HasProperty("_Color"))
                        upgraded.SetColor("_BaseColor", m.GetColor("_Color"));
                    mats[i] = upgraded;
                }

                // Apply Somchay placeholder textures to URP properties
                if (mats[i].HasProperty("_BaseMap") && albedo != null)
                    mats[i].SetTexture("_BaseMap", albedo);
                if (mats[i].HasProperty("_BaseColor"))
                    mats[i].SetColor("_BaseColor", new Color(0.85f, 0.65f, 0.52f, 1f)); // skin tone
                if (mats[i].HasProperty("_BumpMap") && normal != null)
                {
                    mats[i].SetTexture("_BumpMap", normal);
                    mats[i].EnableKeyword("_NORMALMAP");
                }
                if (mats[i].HasProperty("_MetallicGlossMap") && rough != null)
                    mats[i].SetTexture("_MetallicGlossMap", rough);
            }
            r.sharedMaterials = mats;
            EditorUtility.SetDirty(r);
            converted++;
        }

        // Save materials as assets so they persist
        EnsureDirectory("Assets/CueStrike/Materials/Character");
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null && AssetDatabase.GetAssetPath(mats[j]) == "")
                {
                    string matPath = "Assets/CueStrike/Materials/Character/" + mats[j].name + ".mat";
                    AssetDatabase.CreateAsset(mats[j], matPath);
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ImportBlenderAssets] ✅ Assigned URP/Lit materials to {converted} renderer(s) on {modelRoot.name}");
    }

    [MenuItem("Tools/CueStrike/Blender Assets/Apply All to Prefabs")]
    public static void ApplyBlenderAssetsToPrefabs()
    {
        // 1. Apply pool ball FBX to ball prefab
        ApplyBallModel();

        // 2. Apply cue FBX to cue prefab
        ApplyCueModel();

        // 3. Apply table textures to materials
        ApplyTableTextures();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ImportBlenderAssets] ✅ All Blender assets applied to prefabs!");
    }

    [MenuItem("Tools/CueStrike/Blender Assets/Apply Ball Model Only")]
    public static void ApplyBallModel()
    {
        string fbxPath = ModelDir + "/PoolBalls_AAA.fbx";
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxPrefab == null)
        {
            Debug.LogError("[ImportBlenderAssets] Ball FBX not found at: " + fbxPath + " — run Import All first!");
            return;
        }

        // Find ball prefab
        string ballPrefabPath = "Assets/CueStrike/Prefabs/CueStrikeBall.prefab";
        GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ballPrefabPath);
        if (ballPrefab == null)
        {
            Debug.LogWarning("[ImportBlenderAssets] Ball prefab not found at: " + ballPrefabPath + " — creating new prefab from FBX.");
            ballPrefabPath = "Assets/CueStrike/Prefabs/CueStrikeBall_AAA.prefab";
            ballPrefab = PrefabUtility.SaveAsPrefabAsset(fbxPrefab, ballPrefabPath);
            Debug.Log("[ImportBlenderAssets] ✅ Created new ball prefab: " + ballPrefabPath);
            return;
        }

        Debug.Log("[ImportBlenderAssets] ✅ Ball FBX imported. Manual assignment needed per-ball in prefab.");
    }

    [MenuItem("Tools/CueStrike/Blender Assets/Apply Cue Model Only")]
    public static void ApplyCueModel()
    {
        string fbxPath = ModelDir + "/Cue_AAA.fbx";
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxPrefab == null)
        {
            Debug.LogError("[ImportBlenderAssets] Cue FBX not found at: " + fbxPath + " — run Import All first!");
            return;
        }

        // Create or update cue prefab
        string cuePrefabPath = "Assets/CueStrike/Prefabs/CueStrikeCue_AAA.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Prefabs"))
            AssetDatabase.CreateFolder("Assets/CueStrike", "Prefabs");

        // Check if existing cue prefab exists
        string existingCuePrefab = "Assets/CueStrike/Prefabs/CueStrikeCue.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(existingCuePrefab);
        if (existing != null)
        {
            // Update existing cue prefab with new model
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            
            // Replace mesh filter with FBX mesh
            MeshFilter mf = instance.GetComponent<MeshFilter>();
            if (mf == null)
                mf = instance.AddComponent<MeshFilter>();

            Mesh fbxMesh = AssetDatabase.LoadAssetAtPath<Mesh>(fbxPath);
            if (fbxMesh != null)
            {
                mf.sharedMesh = fbxMesh;
                EditorUtility.SetDirty(mf);
            }

            // Replace renderer materials with FBX materials
            Renderer rend = instance.GetComponent<Renderer>();
            if (rend == null)
                rend = instance.AddComponent<MeshRenderer>();

            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var newMaterialsList = new List<Material>();
            foreach (Object obj in allAssets)
            {
                if (obj is Material mat)
                    newMaterialsList.Add(mat);
            }
            Material[] newMaterials = newMaterialsList.ToArray();
            // Explicit cast to avoid CS0266
            if (newMaterials != null && newMaterials.Length > 0)
            {
                rend.sharedMaterials = newMaterials;
                EditorUtility.SetDirty(rend);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, existingCuePrefab);
            GameObject.DestroyImmediate(instance);
            Debug.Log("[ImportBlenderAssets] ✅ Updated existing cue prefab: " + existingCuePrefab);
        }
        else
        {
            // Create new prefab from FBX
            var prefab = PrefabUtility.SaveAsPrefabAsset(fbxPrefab, cuePrefabPath);
            Debug.Log("[ImportBlenderAssets] ✅ Created new cue prefab: " + cuePrefabPath);
        }
    }

    [MenuItem("Tools/CueStrike/Blender Assets/Apply Table Textures Only")]
    public static void ApplyTableTextures()
    {
        // Try to find and update existing materials with new textures
        string[] textureFiles = {
            "Felt_Snooker_Green",
            "Felt_Pool_Blue",
            "Cushion_Rubber",
            "Wood_Dark_Walnut",
            "Wood_Light_Oak",
            "Pocket_Leather",
            "Diamond_Marker_Ivory"
        };

        int updated = 0;
        foreach (string texName in textureFiles)
        {
            // Try to find a material with matching name
            string matPath = MaterialDir + "/" + texName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                // Check alternative names
                mat = FindMaterialByApproximateName(texName);
            }

            if (mat != null)
            {
                // Assign texture to material
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TextureDir + "/" + texName + ".png");

                // Also try with normal map suffix
                if (tex == null && texName.Contains("Felt"))
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        TextureDir + "/" + texName + "_Normal.png");
                    if (tex != null)
                    {
                        mat.SetTexture("_BumpMap", tex);
                        mat.EnableKeyword("_NORMALMAP");
                        EditorUtility.SetDirty(mat);
                        updated++;
                        Debug.Log($"[ImportBlenderAssets] ✓ Assigned normal map {texName}_Normal to {mat.name}");
                    }
                    continue;
                }

                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                    EditorUtility.SetDirty(mat);
                    updated++;
                    Debug.Log($"[ImportBlenderAssets] ✓ Assigned texture {texName} to {mat.name}");
                }
            }
            else
            {
                Debug.Log($"[ImportBlenderAssets] ⚠ No material found for texture: {texName}");
            }
        }

        Debug.Log($"[ImportBlenderAssets] ✅ Updated {updated} materials with new textures.");
    }

    // ─── Helpers ──────────────────────────────────────

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string[] parts = parent.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static int ImportFBX(string sourceFile, string destPath)
    {
        string source = Path.Combine(BlenderExportDir, sourceFile);
        if (!File.Exists(source))
        {
            Debug.LogWarning($"[ImportBlenderAssets] FBX not found: {source}");
            return 0;
        }
        File.Copy(source, destPath, overwrite: true);
        Debug.Log($"[ImportBlenderAssets] ✓ Imported: {sourceFile} → {destPath}");
        return 1;
    }

    private static bool ImportTexture(string sourceFile, string destPath)
    {
        if (!File.Exists(sourceFile))
            return false;
        File.Copy(sourceFile, destPath, overwrite: true);
        Debug.Log($"[ImportBlenderAssets] ✓ Imported texture: {Path.GetFileName(sourceFile)}");
        return true;
    }

    private static Material FindMaterialByApproximateName(string name)
    {
        string[] parts = name.Split('_');
        string searchTerm = parts.Length > 1 ? parts[^1] : parts[0];

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/CueStrike" });
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string matName = Path.GetFileNameWithoutExtension(path);
            if (matName.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }
        return null;
    }
}
#endif