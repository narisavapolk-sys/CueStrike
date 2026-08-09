#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class CreateSkinsAndApply
{
    [MenuItem("CueStrike/Tools/Apply AAA Materials & Prefab Upgrades")]
    public static void ApplyAAAMaterials()
    {
        string matDir = "Assets/CueStrike/Materials";
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike")) AssetDatabase.CreateFolder("Assets", "CueStrike");
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder("Assets/CueStrike", "Materials");

        // 1. Shaders Setup
        Shader feltShader = Shader.Find("Custom/URP/FeltShader");
        Shader cyberGridShader = Shader.Find("Custom/URP/CyberGridFelt");
        Shader ballShader = Shader.Find("Custom/URP/ClearCoatBall");
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

        if (feltShader == null || cyberGridShader == null || ballShader == null || urpLitShader == null)
        {
            Debug.LogError("CueStrike AAA: Shaders not found! Ensure FeltShader, CyberGridFelt, and ClearCoatBall are compiled.");
            return;
        }

        // 2. Felt Materials (3-4 variations)
        // Option 1: Green Velvet
        Material feltSnooker = GetOrCreateMaterial(matDir + "/Felt_Snooker.mat", feltShader);
        feltSnooker.SetColor("_BaseColor", new Color(0.02f, 0.22f, 0.04f, 1f));
        feltSnooker.SetColor("_FuzzColor", new Color(0.1f, 0.58f, 0.2f, 1f));
        feltSnooker.SetFloat("_FuzzPower", 3.5f);
        feltSnooker.SetFloat("_BumpScale", 0.35f);
        EditorUtility.SetDirty(feltSnooker);

        // Option 2: Royal Blue
        Material feltPool = GetOrCreateMaterial(matDir + "/Felt_Pool.mat", feltShader);
        feltPool.SetColor("_BaseColor", new Color(0.04f, 0.12f, 0.28f, 1f));
        feltPool.SetColor("_FuzzColor", new Color(0.15f, 0.35f, 0.7f, 1f));
        feltPool.SetFloat("_FuzzPower", 3.8f);
        feltPool.SetFloat("_BumpScale", 0.35f);
        EditorUtility.SetDirty(feltPool);

        // Option 3: Cyber Grid
        Material feltCyber = GetOrCreateMaterial(matDir + "/Felt_Cyber_Grid.mat", cyberGridShader);
        feltCyber.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.06f, 1f));
        feltCyber.SetColor("_GridColor", new Color(0.0f, 0.85f, 1.0f, 1f));
        feltCyber.SetFloat("_GridFrequency", 18.0f);
        feltCyber.SetFloat("_LineWidth", 0.04f);
        feltCyber.SetColor("_FuzzColor", new Color(0.05f, 0.1f, 0.25f, 1f));
        feltCyber.SetFloat("_FuzzPower", 5.0f);
        EditorUtility.SetDirty(feltCyber);

        // Option 4: Burgundy Red
        Material feltBurgundy = GetOrCreateMaterial(matDir + "/Felt_Burgundy.mat", feltShader);
        feltBurgundy.SetColor("_BaseColor", new Color(0.32f, 0.02f, 0.05f, 1f));
        feltBurgundy.SetColor("_FuzzColor", new Color(0.68f, 0.08f, 0.12f, 1f));
        feltBurgundy.SetFloat("_FuzzPower", 3.4f);
        feltBurgundy.SetFloat("_BumpScale", 0.35f);
        EditorUtility.SetDirty(feltBurgundy);

        // Update Snooker Table folder felt if it exists
        string aaaFeltPath = "Assets/Snooker Table/AAA_Cloth_Green.mat";
        Material aaaFelt = AssetDatabase.LoadAssetAtPath<Material>(aaaFeltPath);
        if (aaaFelt != null)
        {
            aaaFelt.shader = feltShader;
            aaaFelt.SetColor("_BaseColor", new Color(0.02f, 0.22f, 0.04f, 1f));
            aaaFelt.SetColor("_FuzzColor", new Color(0.1f, 0.58f, 0.2f, 1f));
            aaaFelt.SetFloat("_FuzzPower", 3.5f);
            aaaFelt.SetFloat("_BumpScale", 0.35f);
            EditorUtility.SetDirty(aaaFelt);
        }

        // 3. Wood Materials (High Gloss AAA Mahogany)
        Material woodTable = GetOrCreateMaterial(matDir + "/Wood_Table.mat", urpLitShader);
        woodTable.SetColor("_BaseColor", new Color(0.38f, 0.18f, 0.08f, 1f));
        woodTable.SetFloat("_Smoothness", 0.85f);
        woodTable.SetFloat("_Metallic", 0.05f);
        EditorUtility.SetDirty(woodTable);

        string aaaWoodPath = "Assets/Snooker Table/AAA_Wood_Mahogany.mat";
        Material aaaWood = AssetDatabase.LoadAssetAtPath<Material>(aaaWoodPath);
        if (aaaWood != null)
        {
            aaaWood.shader = urpLitShader;
            aaaWood.SetColor("_BaseColor", new Color(0.38f, 0.18f, 0.08f, 1f));
            aaaWood.SetFloat("_Smoothness", 0.88f);
            aaaWood.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(aaaWood);
        }

        // 4. Ball Materials (3-4 variations)
        // Option 1: Classic Resin
        Material ballClassic = GetOrCreateMaterial(matDir + "/Ball_Classic.mat", ballShader);
        ballClassic.SetColor("_BaseColor", Color.white);
        ballClassic.SetFloat("_ClearCoat", 1.0f);
        ballClassic.SetFloat("_ClearCoatRoughness", 0.015f);
        ballClassic.SetFloat("_ReflectionIntensity", 0.85f);
        EditorUtility.SetDirty(ballClassic);

        // Backward compatibility for Ball_Material.mat (alias to classic)
        Material ballMat = GetOrCreateMaterial(matDir + "/Ball_Material.mat", ballShader);
        ballMat.SetColor("_BaseColor", Color.white);
        ballMat.SetFloat("_ClearCoat", 1.0f);
        ballMat.SetFloat("_ClearCoatRoughness", 0.015f);
        ballMat.SetFloat("_ReflectionIntensity", 0.85f);
        EditorUtility.SetDirty(ballMat);

        // Option 2: Neon Cyber
        Material ballNeon = GetOrCreateMaterial(matDir + "/Ball_Neon.mat", urpLitShader);
        ballNeon.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.08f, 1f));
        ballNeon.SetFloat("_Smoothness", 0.95f);
        ballNeon.SetFloat("_Metallic", 0.2f);
        ballNeon.EnableKeyword("_EMISSION");
        ballNeon.SetColor("_EmissionColor", new Color(0.0f, 0.7f, 1.0f, 1f) * 1.5f); // glowing cyan edge lines
        EditorUtility.SetDirty(ballNeon);

        // Option 3: Gold Marble
        Material ballGold = GetOrCreateMaterial(matDir + "/Ball_Gold_Marble.mat", urpLitShader);
        ballGold.SetColor("_BaseColor", new Color(1.0f, 0.82f, 0.35f, 1f)); // luxury gold tint
        ballGold.SetFloat("_Smoothness", 0.92f);
        ballGold.SetFloat("_Metallic", 1.0f); // highly metallic
        EditorUtility.SetDirty(ballGold);

        // Option 4: Reflective Holo
        Material ballHolo = GetOrCreateMaterial(matDir + "/Ball_Holo.mat", urpLitShader);
        ballHolo.SetColor("_BaseColor", new Color(0.4f, 0.9f, 1.0f, 0.35f));
        ballHolo.SetFloat("_Smoothness", 0.98f);
        ballHolo.SetFloat("_Metallic", 0.1f);
        EditorUtility.SetDirty(ballHolo);

        // 4.5 Generate Individual Numbered Ball Materials for Resources
        string resDir = "Assets/Resources/CueStrike/BallMaterials";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CueStrike")) AssetDatabase.CreateFolder("Assets/Resources", "CueStrike");
        if (!AssetDatabase.IsValidFolder(resDir)) AssetDatabase.CreateFolder("Assets/Resources/CueStrike", "BallMaterials");

        // Generate Ball_0 to Ball_15
        for (int i = 0; i <= 15; i++)
        {
            Material bMat = GetOrCreateMaterial(resDir + "/Ball_" + i + ".mat", ballShader);
            bMat.SetColor("_BaseColor", Color.white);
            bMat.SetFloat("_ClearCoat", 1.0f);
            bMat.SetFloat("_ClearCoatRoughness", 0.015f);
            bMat.SetFloat("_ReflectionIntensity", 0.85f);
            
            // Assign the generated texture
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CueStrike/Textures/ball_" + i + ".png");
            if (tex != null)
            {
                bMat.SetTexture("_BaseMap", tex);
            }
            EditorUtility.SetDirty(bMat);
        }

        // Generate Snooker Colored Materials
        // Red
        Material ballRed = GetOrCreateMaterial(resDir + "/Ball_Red.mat", ballShader);
        ballRed.SetColor("_BaseColor", new Color(0.85f, 0.05f, 0.05f, 1f));
        ballRed.SetFloat("_ClearCoat", 1.0f);
        ballRed.SetFloat("_ClearCoatRoughness", 0.015f);
        ballRed.SetFloat("_ReflectionIntensity", 0.85f);
        EditorUtility.SetDirty(ballRed);

        // Pink
        Material ballPink = GetOrCreateMaterial(resDir + "/Ball_Pink.mat", ballShader);
        ballPink.SetColor("_BaseColor", new Color(0.95f, 0.4f, 0.6f, 1f));
        ballPink.SetFloat("_ClearCoat", 1.0f);
        ballPink.SetFloat("_ClearCoatRoughness", 0.015f);
        ballPink.SetFloat("_ReflectionIntensity", 0.85f);
        EditorUtility.SetDirty(ballPink);

        // Black
        Material ballBlack = GetOrCreateMaterial(resDir + "/Ball_Black.mat", ballShader);
        ballBlack.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.08f, 1f));
        ballBlack.SetFloat("_ClearCoat", 1.0f);
        ballBlack.SetFloat("_ClearCoatRoughness", 0.015f);
        ballBlack.SetFloat("_ReflectionIntensity", 0.85f);
        EditorUtility.SetDirty(ballBlack);

        // Blue
        Material ballBlue = GetOrCreateMaterial(resDir + "/Ball_Blue.mat", ballShader);
        ballBlue.SetColor("_BaseColor", new Color(0.05f, 0.25f, 0.85f, 1f));
        ballBlue.SetFloat("_ClearCoat", 1.0f);
        ballBlue.SetFloat("_ClearCoatRoughness", 0.015f);
        ballBlue.SetFloat("_ReflectionIntensity", 0.85f);
        EditorUtility.SetDirty(ballBlue);

        // 5. Apply Felt and Wood to Table Prefabs
        string tableDir = "Assets/CueStrike/Prefabs/Tables";
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { tableDir });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var ts = instance.transform.Find("TableSurface");
            if (ts != null)
            {
                var rend = ts.GetComponent<Renderer>();
                if (rend != null)
                {
                    if (prefab.name.ToLower().Contains("snooker")) rend.sharedMaterial = feltSnooker;
                    else rend.sharedMaterial = feltPool;
                    EditorUtility.SetDirty(rend);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            GameObject.DestroyImmediate(instance);
        }

        // 6. Apply Ball Material & Trail Component to Ball Prefab
        string ballPrefabPath = "Assets/CueStrike/Prefabs/CueStrikeBall.prefab";
        var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ballPrefabPath);
        if (ballPrefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
            var rend = instance.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = ballClassic;
                EditorUtility.SetDirty(rend);
            }
            
            // Wire CueStrikeBallTrail dynamically to ball prefab
            if (instance.GetComponent<CueStrikeBallTrail>() == null)
            {
                instance.AddComponent<CueStrikeBallTrail>();
            }

            // Remove any dangling script references before saving (avoids "missing script" error)
            RemoveMissingScripts(instance);

            PrefabUtility.SaveAsPrefabAsset(instance, ballPrefabPath);
            GameObject.DestroyImmediate(instance);
        }

        // 7. Configure All Character Prefabs with CueIKController dynamically
        string charDir = "Assets/CueStrike/Characters";
        if (AssetDatabase.IsValidFolder(charDir))
        {
            var charGuids = AssetDatabase.FindAssets("t:Prefab", new[] { charDir });
            foreach (var g in charGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance.GetComponent<CueStrikeCue>() == null)
                {
                    instance.AddComponent<CueStrikeCue>();
                    EditorUtility.SetDirty(instance);
                }

                // Clear any missing script components left over from moved/renamed scripts
                RemoveMissingScripts(instance);

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                GameObject.DestroyImmediate(instance);
            }
        }

        // 8. Replace wrong SnookerTable in the active scene automatically
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject oldTable = null;
        foreach (var rootGO in activeScene.GetRootGameObjects())
        {
            if (rootGO.name.ToLower().Contains("snookertable"))
            {
                oldTable = rootGO;
                break;
            }
        }

        if (oldTable != null && !oldTable.transform.gameObject.CompareTag("Untagged") && oldTable.GetComponentInChildren<Pocket>() == null)
        {
            // If it doesn't have our corrected pockets, replace it!
            Vector3 oldPos = oldTable.transform.position;
            Quaternion oldRot = oldTable.transform.rotation;
            
            // Delete old table
            GameObject.DestroyImmediate(oldTable);
            
            // Instantiate correct Snooker Table prefab
            var correctPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CueStrike/Prefabs/Tables/CueStrikeTable_Snooker12ft.prefab");
            if (correctPrefab != null)
            {
                var newTable = (GameObject)PrefabUtility.InstantiatePrefab(correctPrefab);
                newTable.name = "SnookerTable_12ft_WPBSA";
                newTable.transform.position = oldPos;
                newTable.transform.rotation = oldRot;
                
                // Mark scene dirty so user can save
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log("CueStrike AAA: Replaced incorrect Snooker table in active scene with correct leg-supported WPBSA prefab.");
            }
        }
        else if (oldTable != null && !oldTable.name.Equals("SnookerTable_12ft_WPBSA"))
        {
            // Just ensure name matches SnookerTable_12ft_WPBSA for AiderBridge/AutoSave
            oldTable.name = "SnookerTable_12ft_WPBSA";
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CueStrike AAA: Upgraded multiple felt variations, ball skins, and attached dynamic ball trails to prefabs.");
    }

    /// <summary>
    /// Removes any MonoBehaviour components that reference a script that's no longer
    /// available (missing .cs or broken GUID). This must run BEFORE SaveAsPrefabAsset,
    /// otherwise Unity throws: "You are trying to save a Prefab with a missing script."
    /// </summary>
    static void RemoveMissingScripts(GameObject root)
    {
        if (root == null) return;

        // Process this GameObject and all children recursively
        var allTransforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t == null) continue;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }

        // Also clear along the root object itself
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
    }

    static Material GetOrCreateMaterial(string path, Shader shader)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.shader = shader;
        }
        return m;
    }
}
#endif