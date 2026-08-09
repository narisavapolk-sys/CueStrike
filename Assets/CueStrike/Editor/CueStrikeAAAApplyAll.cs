#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CueStrike.Editor
{
    /// <summary>
    /// CueStrike AAA — ONE BUTTON Apply All.
    /// Imports Blender-generated assets (FBX + PNG that were exported DIRECTLY into
    /// Assets/CueStrike/Models/AAA_Props and Assets/CueStrike/Textures), applies materials,
    /// decorates rooms, creates GrandArena crowd, and binds the 10 playable characters to Options.
    ///
    /// Usage: Tools → CueStrike → Apply → Apply All AAA
    /// Requires: Run BlenderScripts/create_all_aaa_master.py in Blender first.
    /// </summary>
    public static class CueStrikeAAAApplyAll
    {
        private const string ModelsDir = "Assets/CueStrike/Models/AAA_Props";
        private const string CharactersDir = "Assets/CueStrike/Models/AAA_Characters";
        private const string TexturesDir = "Assets/CueStrike/Textures";
        private const string PropsPrefabDir = "Assets/CueStrike/Prefabs/AAA_Props";
        private const string CharPrefabDir = "Assets/CueStrike/Prefabs/AAA_Characters";

        #region Menu
        /// <summary>
        /// BATCH-MODE entry point (for Unity -executeMethod). Skips popup dialogs
        /// so Apply All AAA can run headlessly on a CI/batch pipeline.
        /// Usage (batch): Unity.exe -batchmode -projectPath <proj> -executeMethod CueStrike.Editor.CueStrikeAAAApplyAll.ApplyAllAAABatch -quit
        /// </summary>
        public static void ApplyAllAAABatch()
        {
            ApplyAllAAA();
        }

        [MenuItem("Tools/CueStrike/Apply/Apply ALL AAA (Final Polish)")]
        public static void ApplyAllAAA()
        {
            if (!RunGuards()) return;

            // Remember the scene the user was in, restore it at the end
            string originalScenePath = EditorSceneManager.GetActiveScene().path;

            Debug.Log("[CueStrike AAA] ═══ Apply All AAA Started ═══");
            int steps = 0;

            // 1. Import Blender exports (FBX + PNG) if present
            steps += ImportBlenderExports();

            // 1.5 Convert FBX-embedded materials to URP/Lit (fixes pink materials)
            steps += ConvertAllFBXMaterialsToURP();

            // 2. Create materials from imported textures
            steps += CreateTexturedMaterials();

            // 3. Apply table textures to table prefabs
            steps += ApplyTableTexturesToPrefabs();

            // 4. Apply ball model if present
            steps += ApplyBallModel();

            // 5. Apply cue model if present
            steps += ApplyCueModel();

            // 6. Create prop prefabs from FBX
            steps += CreatePropPrefabs();

            // 7. Decorate room scenes with props
            steps += DecorateAllRooms();

            // 8. Create GrandArena crowd (lightweight)
            steps += CreateGrandArenaCrowd();

            // 9. Bind 10 playable characters to manager (no scene spawn)
            steps += BindCharactersToSystem();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 10. Run self-test
            bool testPassed = RunSelfTest();

            // Restore the user's original scene
            if (!string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[CueStrike AAA] ═══ Apply All Complete: {steps} steps executed, self-test {(testPassed ? "PASSED ✅" : "FAILED ❌")} ═══");

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "CueStrike AAA Apply All",
                    $"✅ Apply All AAA completed!\n\n" +
                    $"Steps executed: {steps}\n" +
                    $"Self-test: {(testPassed ? "ALL PASSED ✅" : "SOME FAILED ❌ (check console)")}\n\n" +
                    $"Blender assets were imported automatically.\n" +
                    $"Next: Open a room scene to see the AAA decorations!",
                    "OK"
                );
            }

            // Batch-mode exit code so CI can detect success/failure.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(testPassed ? 0 : 1);
            }
        }
        #endregion

        #region Menu: Fix Pink Materials
        /// <summary>
        /// Converts ALL FBX-embedded materials (balls, cue, props, crowd) from
        /// Blender's built-in Standard shader to "Universal Render Pipeline/Lit".
        /// Built-in Standard shaders render as PINK/MAGENTA in a URP project.
        /// </summary>
        [MenuItem("Tools/CueStrike/Fix/Fix Pink Materials (URP Conversion)")]
        public static void FixPinkMaterialsMenu()
        {
            if (!RunGuards()) return;

            Debug.Log("[CueStrike AAA] ═══ Fix Pink Materials Started ═══");
            int converted = ConvertAllFBXMaterialsToURP();

            // Re-apply converted materials to prefabs (cue, balls, props)
            int updated = 0;
            updated += ApplyBallModel();
            updated += ApplyCueModel();
            updated += CreatePropPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CueStrike AAA] ═══ Fix Pink Materials Complete: {converted} materials converted, {updated} prefabs updated ═══");
            EditorUtility.DisplayDialog(
                "CueStrike Fix Pink Materials",
                $"✅ Fix Pink Materials completed!\n\n" +
                $"Materials converted to URP/Lit: {converted}\n" +
                $"Prefabs refreshed: {updated}\n\n" +
                $"If objects are STILL pink, check:\n" +
                $"1. The material Asset shader is 'Universal Render Pipeline/Lit'\n" +
                $"2. The FBX was re-imported after conversion",
                "OK"
            );
        }
        #endregion

        #region URP Material Conversion (Pink Material Fix)
        /// <summary>
        /// Extracts FBX-embedded materials/textures and converts them to URP/Lit.
        /// Blender exports materials with the built-in Standard shader, which renders
        /// as PINK/MAGENTA in a URP project. This fixes that by converting them.
        /// </summary>
        private static int ConvertAllFBXMaterialsToURP()
        {
            string[] fbxFiles = Directory.GetFiles(ModelsDir, "*.fbx", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(CharactersDir, "*.fbx", SearchOption.TopDirectoryOnly))
                .ToArray();
            if (fbxFiles.Length == 0) return 0;

            string fbxMaterialsDir = "Assets/CueStrike/Materials/AAA/FBX";
            string fbxTexturesDir = "Assets/CueStrike/Textures/AAA_FBX";
            EnsureFolder(fbxMaterialsDir);
            EnsureFolder(fbxTexturesDir);

            string urpLit = "Universal Render Pipeline/Lit";
            Shader urpLitShader = Shader.Find(urpLit);
            if (urpLitShader == null)
            {
                Debug.LogWarning("[CueStrike AAA] URP/Lit shader not found — project may not use URP. Skipping conversion.");
                return 0;
            }

            int converted = 0;
            foreach (var fbx in fbxFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(fbx);
                ModelImporter importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (importer == null) continue;

                // 1) Extract embedded textures so they become real .png assets
                try { importer.ExtractTextures(fbxTexturesDir); }
                catch (Exception ex) { Debug.LogWarning($"[CueStrike AAA] ExtractTextures failed for {fileName}: {ex.Message}"); }

                importer.SaveAndReimport();
                AssetDatabase.Refresh();

                // 2) Extract embedded materials so they become editable .mat assets.
                // NOTE: Unity 6 removed ModelImporter.ExtractMaterials(), so we manually
                // clone the FBX's embedded materials out into standalone .mat assets.
                // Step 3 below then converts them to URP/Lit to fix pink materials.
                string modelMatDir = $"{fbxMaterialsDir}/{fileName}";
                EnsureFolder(modelMatDir);
                int extracted = ExtractMaterialsManually(fbx, modelMatDir);

                AssetDatabase.Refresh();
                if (extracted > 0)
                    Debug.Log($"[CueStrike AAA] Manually extracted {extracted} embedded materials from {fileName} → {modelMatDir}");

                // 3) Now find the extracted .mat assets and convert their shaders
                string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { modelMatDir });
                if (matGuids.Length == 0)
                    matGuids = AssetDatabase.FindAssets("t:Material", new[] { fbxMaterialsDir });

                foreach (var guid in matGuids)
                {
                    string matPath = AssetDatabase.GUIDToAssetPath(guid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) continue;
                    if (mat.shader != null && mat.shader.name == urpLit) continue; // already URP

                    // Save property values BEFORE swapping shader
                    Texture mainTex = mat.mainTexture;
                    Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
                    float smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f;
                    float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                    Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                    // Swap to URP/Lit
                    mat.shader = urpLitShader;

                    // Re-apply supported properties
                    Texture tex = mainTex;
                    if (tex != null)
                    {
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                    }
                    if (bumpMap != null && mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", bumpMap);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

                    EditorUtility.SetDirty(mat);
                    converted++;
                    Debug.Log($"[CueStrike AAA] Converted '{mat.name}' → URP/Lit");
                }
            }

            // 4) Also scan ALL project materials that still use Standard/legacy shaders and convert them
            converted += ConvertStandardMaterialsInProject(urpLitShader);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return converted;
        }

        /// <summary>Converts any Standard/Built-in shader material under Assets/CueStrike/Materials to URP/Lit.</summary>
        private static int ConvertStandardMaterialsInProject(Shader urpLitShader)
        {
            int converted = 0;
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/CueStrike/Materials" });
            foreach (var guid in matGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) continue;
                if (mat.shader == null) continue;
                if (mat.shader.name == "Universal Render Pipeline/Lit") continue;
                if (mat.shader.name.Contains("Universal Render Pipeline")) continue;

                Texture mainTex = mat.mainTexture;
                Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
                float smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f;
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                mat.shader = urpLitShader;

                if (mainTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                }
                if (bumpMap != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"[CueStrike AAA] Converted project material '{mat.name}' → URP/Lit");
            }
            return converted;
        }

        /// <summary>Loads the extracted material assets for a given FBX model.</summary>
        private static Material[] LoadFBXMaterials(string fbxPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(fbxPath);
            string modelMatDir = $"Assets/CueStrike/Materials/AAA/FBX/{fileName}";
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { modelMatDir });
            if (matGuids.Length == 0)
            {
                // Fall back to materials embedded in the FBX itself
                return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().ToArray();
            }
            return matGuids.Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                           .Where(m => m != null)
                           .ToArray();
        }
        #endregion

        #region Guards
        private static bool RunGuards()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (Application.isBatchMode)
                {
                    Debug.LogError("[CueStrike AAA] Blocked: Apply All cannot run during Play Mode.");
                    return false;
                }
                EditorUtility.DisplayDialog("Blocked", "Exit Play Mode first.", "OK");
                return false;
            }
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (Application.isBatchMode)
                {
                    // Batch mode: auto-save without a dialog.
                    EditorSceneManager.SaveOpenScenes();
                    return true;
                }
                bool save = EditorUtility.DisplayDialog("Unsaved Changes", "Save current scene before Apply All?", "Save", "Cancel");
                if (!save) return false;
                EditorSceneManager.SaveOpenScenes();
            }
            return true;
        }
        #endregion

        #region Step 1: Import Blender Exports
        private static int ImportBlenderExports()
        {
            int imported = 0;
            EnsureFolder(ModelsDir);
            EnsureFolder(TexturesDir);
            EnsureFolder(PropsPrefabDir);

            // FBX models already exported directly into Assets/CueStrike/Models/AAA_Props
            // by the Blender master script. Just refresh so Unity imports them.
            string[] fbxFiles = Directory.GetFiles(ModelsDir, "*.fbx", SearchOption.TopDirectoryOnly);
            if (fbxFiles.Length > 0)
            {
                Debug.Log($"[CueStrike AAA] Found {fbxFiles.Length} FBX models: {string.Join(", ", fbxFiles.Select(Path.GetFileName))}");
                imported += fbxFiles.Length;
            }

            // PNG textures already exported directly into Assets/CueStrike/Textures
            string[] pngFiles = Directory.GetFiles(TexturesDir, "*.png", SearchOption.TopDirectoryOnly);
            if (pngFiles.Length > 0)
            {
                Debug.Log($"[CueStrike AAA] Found {pngFiles.Length} PNG textures: {string.Join(", ", pngFiles.Select(Path.GetFileName))}");
                imported += pngFiles.Length;
            }

            AssetDatabase.Refresh();
            return imported;
        }
        #endregion

        #region Step 2: Create Textured Materials
        private static int CreateTexturedMaterials()
        {
            int created = 0;
            EnsureFolder("Assets/CueStrike/Materials/AAA");

            string[] textureNames =
            {
                "Felt_Snooker_Green", "Felt_Pool_Blue", "Felt_Snooker_Green_Normal", "Felt_Pool_Blue_Normal",
                "Cushion_Rubber", "Wood_Dark_Walnut", "Wood_Light_Oak", "Pocket_Leather", "Diamond_Marker_Ivory"
            };

            foreach (var texName in textureNames)
            {
                string texPath = $"{TexturesDir}/{texName}.png";
                if (!File.Exists(texPath)) continue;

                string matPath = $"Assets/CueStrike/Materials/AAA/{texName}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null) continue;

                // Normal maps → _BumpMap + NORMALMAP keyword
                if (texName.Contains("_Normal"))
                {
                    mat.SetTexture("_BumpMap", tex);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_NormalMap", tex);
                }
                else
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                }

                // Physical property presets per texture
                SetMaterialPresets(mat, texName);
                EditorUtility.SetDirty(mat);
                created++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CueStrike AAA] Created/updated {created} textured materials.");
            return created;
        }

        private static void SetMaterialPresets(Material mat, string name)
        {
            if (name.Contains("Felt"))
            {
                mat.SetFloat("_Smoothness", 0.1f);
                mat.SetFloat("_Metallic", 0f);
            }
            else if (name.Contains("Cushion"))
            {
                mat.SetFloat("_Smoothness", 0.3f);
                mat.SetFloat("_Metallic", 0f);
            }
            else if (name.Contains("Wood"))
            {
                mat.SetFloat("_Smoothness", 0.6f);
                mat.SetFloat("_Metallic", 0.05f);
            }
            else if (name.Contains("Leather"))
            {
                mat.SetFloat("_Smoothness", 0.2f);
                mat.SetFloat("_Metallic", 0f);
            }
            else if (name.Contains("Diamond"))
            {
                mat.SetFloat("_Smoothness", 0.8f);
                mat.SetFloat("_Metallic", 0.2f);
            }
        }
        #endregion

        #region Step 3: Apply Table Textures To Prefabs
        private static int ApplyTableTexturesToPrefabs()
        {
            int updated = 0;
            string[] tablePrefabPaths =
            {
                "Assets/CueStrike/Prefabs/Tables/SnookerTable_12ft_Placeholder.prefab",
                "Assets/CueStrike/Prefabs/Tables/PoolTable_Placeholder.prefab",
                "Assets/CueStrike/Prefabs/Tables/PoolTable_8ft.prefab",
                "Assets/CueStrike/Prefabs/Tables/SnookerTable.prefab",
                "Assets/CueStrike/Prefabs/Tables/PoolTable.prefab",
                "Assets/CueStrike/Prefabs/Tables/ChinesePoolTable.prefab"
            };

            var tableGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/CueStrike/Prefabs/Tables" });
            var paths = tableGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)).Distinct().ToList();
            paths.AddRange(tablePrefabPaths);

            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                bool changed = false;

                // Apply felt to TableSurface / Bed / Felt named children
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                {
                    string lname = child.name.ToLower();
                    Renderer rend = child.GetComponent<Renderer>();
                    if (rend == null) continue;

                    if (lname.Contains("surface") || lname.Contains("bed") || lname.Contains("felt") || lname.Contains("cloth"))
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/AAA/Felt_Snooker_Green.mat");
                        if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/AAA/Felt_Pool_Blue.mat");
                        if (mat != null) { rend.sharedMaterial = mat; changed = true; }
                    }
                    else if (lname.Contains("rail") || lname.Contains("cushion"))
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/AAA/Cushion_Rubber.mat");
                        if (mat != null) { rend.sharedMaterial = mat; changed = true; }
                    }
                    else if (lname.Contains("leg") || lname.Contains("frame") || lname.Contains("wood"))
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/AAA/Wood_Dark_Walnut.mat");
                        if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/AAA/Wood_Light_Oak.mat");
                        if (mat != null) { rend.sharedMaterial = mat; changed = true; }
                    }
                    else if (lname.Contains("pocket"))
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/AAA/Pocket_Leather.mat");
                        if (mat != null) { rend.sharedMaterial = mat; changed = true; }
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                    updated++;
                }
                GameObject.DestroyImmediate(instance);
            }

            Debug.Log($"[CueStrike AAA] Applied AAA table textures to {updated} table prefabs.");
            return updated;
        }
        #endregion

        #region Step 4: Apply Ball Model
        private static int ApplyBallModel()
        {
            string fbxPath = $"{ModelsDir}/CueStrike_PoolBalls_AAA.fbx";
            if (!File.Exists(fbxPath)) return 0;

            // The FBX contains all 16 balls. Map them to the runtime ball material system.
            // Existing CueStrikeBall prefab uses BallIdentity + per-ball material swapping at runtime.
            Debug.Log("[CueStrike AAA] Ball FBX found. Runtime BallManager will use embedded materials per BallIdentity ID.");
            return 1;
        }
        #endregion

        #region Step 5: Apply Cue Model
        private static int ApplyCueModel()
        {
            string fbxPath = $"{ModelsDir}/CueStrike_Cue_AAA.fbx";
            if (!File.Exists(fbxPath)) return 0;

            string cuePrefabPath = "Assets/CueStrike/Prefabs/CueStrikeCue_AAA.prefab";
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxPrefab == null) return 0;

            if (File.Exists(cuePrefabPath))
            {
                AssetDatabase.DeleteAsset(cuePrefabPath);
            }
            PrefabUtility.SaveAsPrefabAsset(fbxPrefab, cuePrefabPath);
            Debug.Log("[CueStrike AAA] Created AAA cue prefab from Blender FBX: " + cuePrefabPath);

            // Also update existing cue prefab mesh if present
            string existingPath = "Assets/CueStrike/Prefabs/CueStrikeCue.prefab";
            if (File.Exists(existingPath))
            {
                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(existingPath);
                if (existing != null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
                    MeshFilter mf = instance.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(fbxPath);
                        if (mesh != null) mf.sharedMesh = mesh;
                    }
                    Renderer rend = instance.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Material[] mats = LoadFBXMaterials(fbxPath);
                        if (mats.Length > 0) rend.sharedMaterials = mats;
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, existingPath);
                    GameObject.DestroyImmediate(instance);
                }
            }
            return 2;
        }
        #endregion

        #region Step 6: Create Prop Prefabs From FBX
        private static int CreatePropPrefabs()
        {
            int created = 0;
            EnsureFolder(PropsPrefabDir);
            EnsureFolder(CharPrefabDir);

            string[] fbxFiles = Directory.GetFiles(ModelsDir, "*.fbx", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(CharactersDir, "*.fbx", SearchOption.TopDirectoryOnly))
                .ToArray();
            foreach (var fbx in fbxFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(fbx);
                // Skip balls + cue — handled separately
                if (fileName.Contains("PoolBalls") || fileName.Contains("Cue_AAA")) continue;

                // Character FBX → prefab under Prefabs/AAA_Characters; room props → AAA_Props
                bool isCharacter = fbx.StartsWith(CharactersDir, StringComparison.OrdinalIgnoreCase);
                string prefabDir = isCharacter ? CharPrefabDir : PropsPrefabDir;

                GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (fbxPrefab == null) continue;

                string prefabPath = $"{prefabDir}/{fileName}.prefab";
                if (!File.Exists(prefabPath))
                {
                    PrefabUtility.SaveAsPrefabAsset(fbxPrefab, prefabPath);
                    created++;
                    Debug.Log($"[CueStrike AAA] Created prop prefab: {prefabPath}");
                }
                else
                {
                    // Refresh existing prefab from latest FBX
                    AssetDatabase.DeleteAsset(prefabPath);
                    PrefabUtility.SaveAsPrefabAsset(fbxPrefab, prefabPath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            return created;
        }
        #endregion

        #region Step 7: Decorate All Room Scenes
        private static int DecorateAllRooms()
        {
            int decorated = 0;

            // Scene name → prop placement definitions
            var roomScenes = new Dictionary<string, RoomDecoration>
            {
                { "AAA_RoomDAY", new RoomDecoration("LuxuryChandelier", new Vector3(0f, 4.2f, 0f)) },
                { "Industrial_Room", new RoomDecoration("IndustrialLamp", new Vector3(0f, 3.8f, 0f)) },
                { "Luxury_Room", new RoomDecoration("LuxuryChandelier", new Vector3(0f, 4.2f, 0f)) },
                { "SpaceNebula_Room", new RoomDecoration("SpaceConsole", new Vector3(2f, 0f, -2f)) },
                { "ZenDojo_Room", new RoomDecoration("ZenLantern", new Vector3(0f, 0f, 0f)) },
                { "WarpFantasy_Room", new RoomDecoration("WarpPortalArch", new Vector3(-2.5f, 0f, -3f)) },
                { "Cyberpunk_Room", new RoomDecoration("NeonSign_Strike", new Vector3(0f, 3.5f, -4f)) },
                { "GrandArena", new RoomDecoration("CrowdDummy", Vector3.zero) }
            };

            foreach (var kvp in roomScenes)
            {
                string scenePath = FindScenePath(kvp.Key);
                if (string.IsNullOrEmpty(scenePath)) continue;

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject root = new GameObject("AAA_BlenderDecor");
                root.transform.position = Vector3.zero;

                bool placed = false;
                if (kvp.Key != "GrandArena")
                {
                    placed = PlaceDecorProp(root.transform, kvp.Value.PrefabName, kvp.Value.Position);
                }

                // Extra props per room
                placed |= PlaceSecondaryProps(root.transform, kvp.Key);

                if (placed)
                {
                    EditorSceneManager.SaveScene(scene);
                    decorated++;
                    Debug.Log($"[CueStrike AAA] Decorated scene: {kvp.Key}");
                }
                else
                {
                    GameObject.DestroyImmediate(root);
                }
            }
            return decorated;
        }

        private static bool PlaceDecorProp(Transform parent, string prefabName, Vector3 position)
        {
            string prefabPath = $"{PropsPrefabDir}/{prefabName}.prefab";
            if (!File.Exists(prefabPath)) return false;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;

            // Check if already placed (avoid duplicates on re-run)
            string containerName = "AAA_BlenderDecor";
            var existingRoot = GameObject.Find(containerName);
            Transform existing = existingRoot != null ? existingRoot.transform.Find(prefabName) : null;
            if (existing != null) return true; // Already placed

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefabName;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            return true;
        }

        private static bool PlaceSecondaryProps(Transform parent, string sceneName)
        {
            bool placed = false;
            switch (sceneName)
            {
                case "Luxury_Room":
                    placed |= PlaceDecorProp(parent, "BarBottleSet", new Vector3(-2f, 0.8f, 2f));
                    placed |= PlaceDecorProp(parent, "HoloScreen", new Vector3(2.5f, 1.5f, -2f));
                    break;
                case "Cyberpunk_Room":
                    placed |= PlaceDecorProp(parent, "BarBottleSet", new Vector3(-2f, 0.8f, 2f));
                    placed |= PlaceDecorProp(parent, "HoloScreen", new Vector3(2.5f, 1.5f, -2f));
                    break;
                case "Industrial_Room":
                    placed |= PlaceDecorProp(parent, "BarBottleSet", new Vector3(-2f, 0.8f, 2f));
                    break;
                case "SpaceNebula_Room":
                    placed |= PlaceDecorProp(parent, "HoloScreen", new Vector3(2.5f, 1.5f, -2f));
                    break;
                case "WarpFantasy_Room":
                    placed |= PlaceDecorProp(parent, "HoloScreen", new Vector3(2.5f, 1.5f, -2f));
                    break;
            }
            return placed;
        }

        private static string FindScenePath(string sceneName)
        {
            string[] sceneGuids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            foreach (var g in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(path).Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return null;
        }
        #endregion

        #region Step 8: GrandArena Crowd (Lightweight)
        private static int CreateGrandArenaCrowd()
        {
            // Find GrandArena scene
            string scenePath = FindScenePath("GrandArena");
            if (string.IsNullOrEmpty(scenePath)) return 0;

            string crowdPrefabPath = $"{PropsPrefabDir}/CrowdDummy.prefab";
            if (!File.Exists(crowdPrefabPath)) return 0;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Already placed?
            var existingCrowd = GameObject.Find("AAA_Crowd_System");
            if (existingCrowd != null)
            {
                EditorSceneManager.SaveScene(scene);
                return 1; // Already exists
            }

            GameObject crowdRoot = new GameObject("AAA_Crowd_System");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(crowdPrefabPath);
            if (prefab == null)
            {
                GameObject.DestroyImmediate(crowdRoot);
                return 0;
            }

            // Find the arena table (for seating ring placement)
            Vector3 center = Vector3.zero;
            GameObject table = GameObject.FindGameObjectWithTag("Table");
            if (table != null) center = table.transform.position;

            // Place crowd in 3 rings — lightweight, no physics, no AI
            int count = 0;
            System.Random rng = new System.Random(42);
            for (int ring = 1; ring <= 3; ring++)
            {
                int ringCount = ring * 14; // ring1=14, ring2=28, ring3=42 → total 84
                float radius = 5f + ring * 2.5f;
                float yOffset = 0.5f + ring * 0.5f;

                for (int i = 0; i < ringCount; i++)
                {
                    float ang = (i / (float)ringCount) * Mathf.PI * 2f;
                    float x = center.x + Mathf.Cos(ang) * radius;
                    float z = center.z + Mathf.Sin(ang) * radius;

                    // Jitter for natural look
                    x += (float)(rng.NextDouble() - 0.5) * 1.2f;
                    z += (float)(rng.NextDouble() - 0.5) * 1.2f;
                    float y = yOffset + (float)(rng.NextDouble() - 0.5) * 0.3f;

                    GameObject dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    dummy.name = $"Spectator_{count:000}";
                    dummy.transform.position = new Vector3(x, y, z);
                    dummy.transform.rotation = Quaternion.Euler(0, ang * Mathf.Rad2Deg + 180f, 0);
                    dummy.transform.SetParent(crowdRoot.transform, true);

                    // Random scale variety
                    float s = 0.85f + (float)(rng.NextDouble() - 0.5) * 0.3f;
                    dummy.transform.localScale = Vector3.one * s;
                    count++;
                }
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[CueStrike AAA] Created GrandArena crowd: {count} lightweight spectators (3 rings, no physics/AI).");
            return count;
        }
        #endregion

        #region Step 9: Bind 10 Characters To System
        private static int BindCharactersToSystem()
        {
            try
            {
                // Batch-safe: create/update the 10 CharacterData ScriptableObject assets first.
                CueStrikeCharacterSetup.CreateCharacterDataAssets();

                // Interactive path only: also create manager + selector UI in the current scene.
                if (!Application.isBatchMode)
                {
                    CueStrikeCharacterSetup.SetupCharacterSystem();
                }
                return 10;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CueStrike AAA] Character system already set up or has partial state: {ex.Message}");
                return 0;
            }
        }
        #endregion

        #region Self-Test
        private static bool RunSelfTest()
        {
            bool allPass = true;
            int pass = 0, fail = 0;

            // Test 1: All 10 playable characters' ability classes exist.
            // NOTE: Real class names are per-character (FinnAquaRush, KingFlexBlingBling, ...),
            // NOT "Ability_*" — that was an outdated naming scheme.
            string[] abilityTypes =
            {
                "CueStrike.Characters.Somchay.SomchayAbilityController",      // Somchay
                "CueStrike.Characters.MeiLing.MeiLingAbilityController",      // MeiLing
                "CueStrike.Characters.Gentleman.GentlemanAbilityController",  // Gentleman
                "CueStrike.Characters.BoPanda.BoPandaHypeEngine",             // PanPan (Bo Panda)
                "CueStrike.Characters.Finn.FinnAquaRush",                     // Finn
                "CueStrike.Characters.KingFlex.KingFlexBlingBling",           // KingFlex
                "CueStrike.Characters.Tusker.TuskerGentlemansMemory",         // Tusker
                "CueStrike.Characters.PanPan.PanPanZenStance",                // PanPan Zen
                "CueStrike.Characters.Phantom.PhantomSpectralSight",          // Phantom
                "CueStrike.Characters.Cassidy.CassidyQuickDraw",              // Cassidy
                "CueStrike.Characters.Bones.BonesXRayVision"                  // Bones
            };

            foreach (var t in abilityTypes)
            {
                if (FindTypeInAllAssemblies(t) != null) { pass++; }
                else { Debug.LogError($"[CueStrike AAA SelfTest] MISSING ability: {t}"); fail++; allPass = false; }
            }

            // Test 2: Texture materials created
            string[] requiredMats =
            {
                "Assets/CueStrike/Materials/AAA/Felt_Snooker_Green.mat",
                "Assets/CueStrike/Materials/AAA/Felt_Pool_Blue.mat",
                "Assets/CueStrike/Materials/AAA/Wood_Dark_Walnut.mat"
            };
            foreach (var m in requiredMats)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(m) != null) { pass++; }
                else { Debug.LogError($"[CueStrike AAA SelfTest] MISSING material: {m}"); fail++; allPass = false; }
            }

            // Test 3: No pink materials — all materials must use URP shaders
            string[] allMatGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/CueStrike/Materials" });
            foreach (var guid in allMatGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) continue;
                if (mat.shader == null) continue;
                if (!mat.shader.name.Contains("Universal Render Pipeline"))
                {
                    Debug.LogError($"[CueStrike AAA SelfTest] PINK MATERIAL DETECTED: {matPath} uses '{mat.shader.name}' (not URP)");
                    fail++; allPass = false;
                }
                else
                {
                    pass++;
                }
            }

            // Test 4: Playable character FBX models + prefabs exist (per doc checklist #3-4)
            string[] characterNames =
            {
                "Somchay", "MeiLing", "Gentleman", "PanPan", "Finn",
                "KingFlex", "Tusker", "Phantom", "Cassidy", "Bones"
            };
            foreach (var c in characterNames)
            {
                if (File.Exists($"{CharactersDir}/{c}_AAA.fbx")) pass++;
                else { Debug.LogError($"[CueStrike AAA SelfTest] MISSING character FBX: {c}_AAA.fbx"); fail++; allPass = false; }

                if (File.Exists($"{CharPrefabDir}/{c}_AAA.prefab")) pass++;
                else { Debug.LogError($"[CueStrike AAA SelfTest] MISSING character prefab: {c}_AAA.prefab"); fail++; allPass = false; }
            }

            Debug.Log($"[CueStrike AAA SelfTest] Result: {pass} PASS, {fail} FAIL");
            return allPass;
        }

        /// <summary>
        /// Searches ALL loaded assemblies (Assembly-CSharp runtime scripts) for a type.
        /// Type.GetType(name) only finds types in the CALLING assembly (the editor
        /// assembly), so script types must be searched manually.
        /// </summary>
        private static Type FindTypeInAllAssemblies(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = assembly.GetType(typeName, false);
                if (t != null) return t;
            }
            return null;
        }
        #endregion

        #region Helpers
        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// Unity 6 removed ModelImporter.ExtractMaterials(). This manually clones the
        /// FBX's embedded materials out into standalone .mat assets so the URP/Lit
        /// conversion step below can edit them as regular material assets.
        /// </summary>
        private static int ExtractMaterialsManually(string fbxPath, string outputDir)
        {
            EnsureFolder(outputDir);

            // Load all embedded materials from the imported FBX asset
            Material[] embeddedMats = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().ToArray();
            if (embeddedMats.Length == 0)
            {
                Debug.LogWarning($"[CueStrike AAA] No embedded materials found in {fbxPath}. " +
                                 $"Check that the FBX 'Materials' import mode is 'Import' (not 'None').");
                return 0;
            }

            int extracted = 0;
            foreach (Material src in embeddedMats)
            {
                if (src == null) continue;

                // Detect material instance name vs asset name
                string matName = src.name;
                string safeName = string.Join("_", matName.Split(Path.GetInvalidFileNameChars()));

                // Ensure unique .mat path
                string matPath = $"{outputDir}/{safeName}.mat";
                int dup = 1;
                while (File.Exists(matPath))
                {
                    matPath = $"{outputDir}/{safeName}_{dup}.mat";
                    dup++;
                }

                // If a material with this name already exists in the folder, reuse it
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (existing != null && existing.shader != null && existing.shader.name.Contains("Universal Render Pipeline"))
                {
                    continue; // already converted
                }

                // Clone the embedded material into a real .mat asset
                try
                {
                    Material clone = new Material(src);
                    clone.name = safeName;
                    AssetDatabase.CreateAsset(clone, matPath);
                    extracted++;
                    Debug.Log($"[CueStrike AAA] Extracted embedded material '{safeName}' → {matPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CueStrike AAA] Failed to extract material '{safeName}' from {fbxPath}: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            return extracted;
        }
        #endregion

        #region Data Class
        private class RoomDecoration
        {
            public string PrefabName;
            public Vector3 Position;

            public RoomDecoration(string prefabName, Vector3 position)
            {
                PrefabName = prefabName;
                Position = position;
            }
        }
        #endregion
    }
}
#endif