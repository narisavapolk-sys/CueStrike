#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Applies the AAA Blender-generated Somchay character model into the scene,
/// replacing any Capsule/placeholder "Body" objects, and provides a debug menu
/// to verify proportions against the reference.
///
/// Menus:
///   Tools → CueStrike → AAA Character → Apply to Scene
///   Tools → CueStrike → Debug → Test AAA Character
/// </summary>
public static class ApplyAAACharacter
{
    private const string ModelDir = "Assets/CueStrike/Models";
    private const string CharacterPrefabPath = "Assets/CueStrike/Prefabs/Somchay_AAA.prefab";

    // Standard human proportions (metres) — matches typical Unity CC/glTF human scales
    private const float TARGET_HEIGHT = 1.80f;      // total height
    private const float TARGET_SHOULDER_WIDTH = 0.45f;
    private const float TARGET_ARM_LENGTH = 0.65f;
    private const float TARGET_LEG_LENGTH = 0.85f;
    private const float TOLERANCE = 0.15f;          // ±15% is acceptable for stylised AAA

    // ─────────────────────────────────────────────────────────────
    // MENU 1: Apply the character into the active scene
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/CueStrike/AAA Character/Apply to Scene")]
    public static void ApplyToScene()
    {
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/CueStrike/Models/Somchay_AAA.fbx");
        if (fbxPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "AAA Character",
                "Somchay FBX model not found!\n\n" +
                "1. Run: Tools → CueStrike → Blender Assets → Import All From Blender Exports\n" +
                "2. Then: Tools → CueStrike → Blender Assets → Apply Character Model Only",
                "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.name))
        {
            EditorUtility.DisplayDialog("AAA Character", "Open a scene first before applying.", "OK");
            return;
        }

        int replaced = 0;
        int created = 0;

        // ── 1) Find and replace any placeholder "Body"/"Capsule" objects ──
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Search recursively for placeholder objects
            var results = SearchForPlaceholderBody(root);
            foreach (GameObject placeholder in results)
            {
                // Replace placeholder with the AAA character instance
                GameObject charInstance = (GameObject)PrefabUtility.InstantiatePrefab(GetOrCreateCharacterPrefab());
                if (charInstance == null) continue;

                // Match position/rotation of the placeholder
                charInstance.transform.SetPositionAndRotation(
                    placeholder.transform.position, placeholder.transform.rotation);
                charInstance.transform.localScale = Vector3.one * 0.01f; // FBX is 1 unit = 1cm from Blender; fix on import

                // Copy parent if any
                if (placeholder.transform.parent != null)
                    charInstance.transform.SetParent(placeholder.transform.parent, true);

                charInstance.name = "Somchay_AAA";
                replaced++;
                Debug.Log($"[ApplyAAACharacter] ✅ Replaced '{placeholder.name}' with Somchay_AAA at {placeholder.transform.position}");
            }
        }

        // ── 2) If nothing was replaced, just place a fresh character ──
        if (replaced == 0)
        {
            GameObject charPrefab = GetOrCreateCharacterPrefab();
            if (charPrefab != null)
            {
                GameObject charInstance = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
                if (charInstance != null)
                {
                    charInstance.name = "Somchay_AAA";
                    charInstance.transform.position = Vector3.zero;
                    created++;
                    Debug.Log("[ApplyAAACharacter] ✅ No placeholder found — placed fresh Somchay_AAA at origin.");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        string msg = $"AAA Character applied!\n\nReplaced placeholders: {replaced}\nCreated new: {created}";
        Debug.Log($"[ApplyAAACharacter] {msg}");
        EditorUtility.DisplayDialog("AAA Character", msg, "OK");
    }

    // ─────────────────────────────────────────────────────────────
    // MENU 2: Debug — Test AAA Character & verify proportions
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/CueStrike/Debug/Test AAA Character")]
    public static void TestAAACharacter()
    {
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/CueStrike/Models/Somchay_AAA.fbx");
        if (fbxPrefab == null)
        {
            Debug.LogError("[TestAAACharacter] ❌ Somchay FBX not found at Assets/CueStrike/Models/Somchay_AAA.fbx — import it first!");
            EditorUtility.DisplayDialog("Test AAA Character", "Somchay FBX not found. Run Import All + Apply Character Model first.", "OK");
            return;
        }

        // Find the character in the scene, or instantiate a test copy
        GameObject charRoot = GameObject.Find("Somchay_AAA");
        bool isTemporary = false;
        if (charRoot == null)
        {
            GameObject charPrefab = GetOrCreateCharacterPrefab();
            if (charPrefab == null)
            {
                Debug.LogError("[TestAAACharacter] ❌ Could not create character prefab.");
                return;
            }
            charRoot = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
            charRoot.name = "Somchay_AAA_Test";
            charRoot.transform.position = Vector3.zero;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            isTemporary = true;
            Debug.Log("[TestAAACharacter] ℹ Created temporary test instance at origin.");
        }

        // ── Measure proportions from renderer bounds ──
        Bounds total = GetCombinedBounds(charRoot);
        float heightCM = total.size.y;  // FBX from Blender uses CM (1 unit = 1cm)
        float heightM  = heightCM / 100f;
        float shoulderWCM = total.size.x;
        float shoulderWM = shoulderWCM / 100f;

        // Visual helpers
        Debug.DrawLine(total.min, total.min + Vector3.up * total.size.y, Color.green, 30f);
        Debug.DrawLine(total.min, total.min + Vector3.right * total.size.x, Color.red, 30f);

        string result = BuildProportionReport(charRoot, heightM, shoulderWM);

        // Show the report
        Debug.Log(result);
        EditorUtility.DisplayDialog("Test AAA Character — Somchay", result, "OK");

        // Clean up temporary test instance
        if (isTemporary && charRoot != null)
        {
            Object.DestroyImmediate(charRoot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private static string BuildProportionReport(GameObject root, float heightM, float shoulderW)
    {
        float heightRatio = heightM / TARGET_HEIGHT;
        float shoulderRatio = shoulderW / TARGET_SHOULDER_WIDTH;

        string heightStatus = WithinTolerance(heightRatio) ? "✅ OK (เปรียบเทียบกับอ้างอิง 1.80m)" : "⚠️ เบี่ยงเบนจากอ้างอิง";
        string shoulderStatus = WithinTolerance(shoulderRatio) ? "✅ OK (เปรียบเทียบกับอ้างอิง 0.45m)" : "⚠️ เบี่ยงเบนจากอ้างอิง";

        return $"═══ Somchay AAA Character Report ═══\n\n" +
               $"ความสูง: {heightM:F2} m  {heightStatus}\n" +
               $"ความกว้างไหล่: {shoulderW:F2} m  {shoulderStatus}\n\n" +
               $"HR = {heightRatio:F2}  SWR = {shoulderRatio:F2}\n" +
               $"Tolerance: ±{TOLERANCE:P0}\n\n" +
               $"Renderer count: {root.GetComponentsInChildren<Renderer>(true).Length}\n" +
               $"Bones: {root.GetComponentsInChildren<Transform>(true).Length} transforms\n" +
               $"Prefab: {CharacterPrefabPath}";
    }

    private static bool WithinTolerance(float ratio) => Mathf.Abs(1f - ratio) <= TOLERANCE;

    private static Bounds GetCombinedBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private static GameObject GetOrCreateCharacterPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
        if (prefab != null) return prefab;

        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/CueStrike/Models/Somchay_AAA.fbx");
        if (fbx == null) return null;

        // Create a prefab asset from the FBX
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Prefabs"))
            AssetDatabase.CreateFolder("Assets/CueStrike", "Prefabs");
        prefab = PrefabUtility.SaveAsPrefabAsset(fbx, CharacterPrefabPath);
        Debug.Log($"[ApplyAAACharacter] ✅ Created character prefab: {CharacterPrefabPath}");
        AssetDatabase.Refresh();
        return prefab;
    }

    /// <summary>Recursively finds placeholder objects that should be replaced by the AAA character.</summary>
    private static System.Collections.Generic.List<GameObject> SearchForPlaceholderBody(GameObject obj)
    {
        var found = new System.Collections.Generic.List<GameObject>();
        SearchPlaceholderRecursive(obj.transform, found);
        return found;
    }

    private static void SearchPlaceholderRecursive(Transform t, System.Collections.Generic.List<GameObject> found)
    {
        string name = t.name.ToLowerInvariant();
        // Heuristic: placeholder capsule/body objects
        bool isBody = name.Contains("body") && t.childCount == 0;
        bool isCapsule = name.Contains("capsule") && t.childCount == 0;
        bool isCharacterPlaceholder = name.Contains("characterplaceholder") || name.Contains("char_placeholder");

        if (isBody || isCapsule || isCharacterPlaceholder)
            found.Add(t.gameObject);

        foreach (Transform child in t)
            SearchPlaceholderRecursive(child, found);
    }
}
#endif