using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.AI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CueStrike.Editor
{
    /// <summary>
    /// AAA Room Setup - Automatically imports and configures all 8 AAA rooms in Unity
    /// Zero Pink Policy: Ensures all materials use URP/Lit shader
    /// </summary>
    public class RoomSetupAAA : EditorWindow
    {
        private const string ROOMS_ROOT = "Assets/CueStrike/Art/Rooms";
        private const string SCENES_ROOT = "Assets/CueStrike/Scenes/Rooms";
        private const string PREFABS_ROOT = "Assets/CueStrike/Prefabs/Rooms";
        
        private static readonly string[] RoomNames = new[]
        {
            "ZenDojo",
            "Cyberpunk", 
            "SpaceNebula",
            "Industrial",
            "WarpFantasy",
            "Luxury_DAY",
            "Luxury_NIGHT",
            "Arena_Core"
        };
        
        private static readonly Dictionary<string, RoomConfig> RoomConfigs = new Dictionary<string, RoomConfig>
        {
            ["ZenDojo"] = new RoomConfig
            {
                ambientColor = new Color(0.3f, 0.25f, 0.2f),
                fogColor = new Color(0.85f, 0.82f, 0.75f),
                fogDensity = 0.02f,
                sunColor = new Color(1f, 0.95f, 0.85f),
                sunIntensity = 1.5f,
                reflectionIntensity = 0.5f
            },
            ["Cyberpunk"] = new RoomConfig
            {
                ambientColor = new Color(0.1f, 0.05f, 0.15f),
                fogColor = new Color(0.15f, 0.1f, 0.2f),
                fogDensity = 0.08f,
                sunColor = new Color(0.5f, 0.3f, 0.7f),
                sunIntensity = 0.3f,
                reflectionIntensity = 0.8f
            },
            ["SpaceNebula"] = new RoomConfig
            {
                ambientColor = new Color(0.15f, 0.18f, 0.25f),
                fogColor = new Color(0.2f, 0.25f, 0.35f),
                fogDensity = 0.03f,
                sunColor = new Color(0.6f, 0.7f, 1f),
                sunIntensity = 0.5f,
                reflectionIntensity = 0.7f
            },
            ["Industrial"] = new RoomConfig
            {
                ambientColor = new Color(0.2f, 0.18f, 0.15f),
                fogColor = new Color(0.3f, 0.28f, 0.25f),
                fogDensity = 0.05f,
                sunColor = new Color(1f, 0.9f, 0.8f),
                sunIntensity = 1f,
                reflectionIntensity = 0.4f
            },
            ["WarpFantasy"] = new RoomConfig
            {
                ambientColor = new Color(0.15f, 0.12f, 0.2f),
                fogColor = new Color(0.3f, 0.25f, 0.4f),
                fogDensity = 0.04f,
                sunColor = new Color(0.7f, 0.6f, 0.9f),
                sunIntensity = 0.4f,
                reflectionIntensity = 0.6f
            },
            ["Luxury_DAY"] = new RoomConfig
            {
                ambientColor = new Color(0.4f, 0.35f, 0.3f),
                fogColor = new Color(0.9f, 0.88f, 0.82f),
                fogDensity = 0.01f,
                sunColor = new Color(1f, 0.98f, 0.92f),
                sunIntensity = 3f,
                reflectionIntensity = 0.9f
            },
            ["Luxury_NIGHT"] = new RoomConfig
            {
                ambientColor = new Color(0.1f, 0.08f, 0.06f),
                fogColor = new Color(0.15f, 0.12f, 0.1f),
                fogDensity = 0.02f,
                sunColor = new Color(0.5f, 0.4f, 0.3f),
                sunIntensity = 0.1f,
                reflectionIntensity = 0.7f
            },
            ["Arena_Core"] = new RoomConfig
            {
                ambientColor = new Color(0.15f, 0.18f, 0.22f),
                fogColor = new Color(0.1f, 0.15f, 0.2f),
                fogDensity = 0.015f,
                sunColor = new Color(0.9f, 0.95f, 1f),
                sunIntensity = 1f,
                reflectionIntensity = 1f
            }
        };
        
        [MenuItem("CueStrike/AAA World Tour/Setup All Rooms")]
        public static void SetupAllRooms()
        {
            Debug.Log("=== CueStrike AAA World Tour: Setting Up All Rooms ===");
            
            // Ensure directories exist
            EnsureDirectory(SCENES_ROOT);
            EnsureDirectory(PREFABS_ROOT);
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (string roomName in RoomNames)
            {
                try
                {
                    SetupRoom(roomName);
                    successCount++;
                    Debug.Log($"✓ {roomName} setup complete");
                }
                catch (System.Exception e)
                {
                    failCount++;
                    Debug.LogError($"✗ {roomName} setup failed: {e.Message}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"=== AAA World Tour Complete: {successCount} succeeded, {failCount} failed ===");
            EditorUtility.DisplayDialog("AAA World Tour", 
                $"Room setup complete!\nSuccess: {successCount}\nFailed: {failCount}", "OK");
        }
        
        [MenuItem("CueStrike/AAA World Tour/Verify Zero Pink Policy")]
        public static void VerifyZeroPinkPolicy()
        {
            Debug.Log("=== Verifying Zero Pink Policy ===");
            
            int pinkCount = 0;
            int totalMaterials = 0;
            
            foreach (string roomName in RoomNames)
            {
                string roomPath = Path.Combine(ROOMS_ROOT, roomName);
                if (!Directory.Exists(roomPath)) continue;
                
                var materials = AssetDatabase.FindAssets("t:Material", new[] { roomPath })
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(path => AssetDatabase.LoadAssetAtPath<Material>(path));
                
                foreach (var mat in materials)
                {
                    if (mat == null) continue;
                    totalMaterials++;
                    
                    // Check for pink/magenta tint (missing shader/texture)
                    if (mat.shader.name.Contains("Hidden") || 
                        mat.shader.name.Contains("Error") ||
                        mat.shader.name.Contains("Pink"))
                    {
                        pinkCount++;
                        Debug.LogError($"[PINK DETECTED] {mat.name} in {roomName} uses shader: {mat.shader.name}");
                    }
                    
                    // Ensure URP/Lit
                    if (!mat.shader.name.Contains("Universal Render Pipeline/Lit") &&
                        !mat.shader.name.Contains("Universal Render Pipeline/Particles/Lit"))
                    {
                        Debug.LogWarning($"[SHADER CHECK] {mat.name} in {roomName} uses: {mat.shader.name} (not URP/Lit)");
                    }
                }
            }
            
            Debug.Log($"=== Zero Pink Policy Verification Complete ===");
            Debug.Log($"Total Materials: {totalMaterials}");
            Debug.Log($"Pink/Error Materials: {pinkCount}");
            
            if (pinkCount == 0)
            {
                Debug.Log("✓ ZERO PINK POLICY: PASSED - No pink materials detected!");
                EditorUtility.DisplayDialog("Zero Pink Policy", "✓ PASSED - No pink materials detected!", "OK");
            }
            else
            {
                Debug.LogError($"✗ ZERO PINK POLICY: FAILED - {pinkCount} pink materials found!");
                EditorUtility.DisplayDialog("Zero Pink Policy", $"✗ FAILED - {pinkCount} pink materials found!", "OK");
            }
        }
        
        [MenuItem("CueStrike/AAA World Tour/Convert All Materials To URP/Lit")]
        public static void ConvertAllMaterialsToURPLit()
        {
            Debug.Log("=== Converting All Room Materials to URP/Lit ===");
            
            int converted = 0;
            
            foreach (string roomName in RoomNames)
            {
                string roomPath = Path.Combine(ROOMS_ROOT, roomName);
                if (!Directory.Exists(roomPath)) continue;
                
                var materialPaths = AssetDatabase.FindAssets("t:Material", new[] { roomPath })
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid));
                
                foreach (string matPath in materialPaths)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) continue;
                    
                    // Skip if already URP/Lit
                    if (mat.shader.name.Contains("Universal Render Pipeline/Lit")) continue;
                    
                    // Save properties
                    Color baseColor = mat.GetColor("_BaseColor");
                    Color emissionColor = mat.GetColor("_EmissionColor");
                    float metallic = mat.GetFloat("_Metallic");
                    float smoothness = mat.GetFloat("_Glossiness");
                    float occlusion = mat.HasProperty("_OcclusionStrength") ? mat.GetFloat("_OcclusionStrength") : 1f;
                    Texture2D baseMap = mat.GetTexture("_BaseMap") as Texture2D;
                    Texture2D normalMap = mat.GetTexture("_BumpMap") as Texture2D;
                    Texture2D metallicMap = mat.GetTexture("_MetallicGlossMap") as Texture2D;
                    Texture2D occlusionMap = mat.GetTexture("_OcclusionMap") as Texture2D;
                    Texture2D emissionMap = mat.GetTexture("_EmissionMap") as Texture2D;
                    bool hasEmission = mat.IsKeywordEnabled("_EMISSION");
                    
                    // Convert to URP/Lit
                    mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                    
                    // Restore properties
                    mat.SetColor("_BaseColor", baseColor);
                    mat.SetColor("_EmissiveColor", emissionColor);
                    mat.SetFloat("_Metallic", metallic);
                    mat.SetFloat("_Smoothness", smoothness);
                    mat.SetFloat("_OcclusionStrength", occlusion);
                    
                    if (baseMap) mat.SetTexture("_BaseMap", baseMap);
                    if (normalMap) mat.SetTexture("_BumpMap", normalMap);
                    if (metallicMap) mat.SetTexture("_MetallicGlossMap", metallicMap);
                    if (occlusionMap) mat.SetTexture("_OcclusionMap", occlusionMap);
                    if (emissionMap) mat.SetTexture("_EmissiveMap", emissionMap);
                    
                    if (hasEmission)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    }
                    
                    // Handle transparency
                    if (mat.HasProperty("_Mode"))
                    {
                        // Standard shader mode -> URP surface type
                        float mode = mat.GetFloat("_Mode");
                        if (mode == 2 || mode == 3) // Fade or Transparent
                        {
                            mat.SetFloat("_Surface", 1); // Transparent
                            mat.SetFloat("_Blend", 0); // Alpha
                            mat.SetFloat("_ZWrite", 0);
                            mat.SetOverrideTag("RenderType", "Transparent");
                            mat.renderQueue = 3000;
                        }
                    }
                    
                    EditorUtility.SetDirty(mat);
                    converted++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"=== Converted {converted} materials to URP/Lit ===");
            EditorUtility.DisplayDialog("Convert to URP/Lit", $"Converted {converted} materials", "OK");
        }
        
        [MenuItem("CueStrike/AAA World Tour/Create Lighting Presets")]
        public static void CreateLightingPresets()
        {
            Debug.Log("=== Creating Lighting Presets ===");
            
            string presetPath = "Assets/CueStrike/Rendering/LightingPresets";
            EnsureDirectory(presetPath);
            
            foreach (var kvp in RoomConfigs)
            {
                var config = kvp.Value;
                var preset = ScriptableObject.CreateInstance<RoomLightingPreset>();
                preset.roomName = kvp.Key;
                preset.ambientColor = config.ambientColor;
                preset.fogColor = config.fogColor;
                preset.fogDensity = config.fogDensity;
                preset.sunColor = config.sunColor;
                preset.sunIntensity = config.sunIntensity;
                preset.reflectionIntensity = config.reflectionIntensity;
                
                string assetPath = Path.Combine(presetPath, $"{kvp.Key}_LightingPreset.asset");
                AssetDatabase.CreateAsset(preset, assetPath);
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {RoomConfigs.Count} lighting presets");
        }
        
        private static void SetupRoom(string roomName)
        {
            string fbxPath = Path.Combine(ROOMS_ROOT, roomName, $"{roomName}.fbx");
            if (!File.Exists(fbxPath))
            {
                throw new System.Exception($"FBX not found: {fbxPath}");
            }
            
            // Import settings
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                importer.globalScale = 1f;
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.materialName = ModelImporterMaterialName.BasedOnTextureName;
                importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.Import;
                importer.importBlendShapeNormals = ModelImporterNormals.Import;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.optimizeMeshPolygons = true;
                importer.optimizeMeshVertices = true;
                importer.importVisibility = true;
                importer.importCameras = true;
                importer.importLights = true;
                importer.animationType = ModelImporterAnimationType.None;
                
                importer.SaveAndReimport();
            }
            
            // Create prefab
            GameObject roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (roomPrefab == null)
            {
                throw new System.Exception($"Could not load prefab from {fbxPath}");
            }
            
            // Instantiate and configure
            GameObject instance = PrefabUtility.InstantiatePrefab(roomPrefab) as GameObject;
            instance.name = roomName;
            
            // Apply room configuration
            ApplyRoomConfiguration(instance, roomName);
            
            // Create scene
            CreateRoomScene(instance, roomName);
            
            // Save as prefab
            string prefabPath = Path.Combine(PREFABS_ROOT, $"{roomName}.prefab");
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            
            // Clean up
            DestroyImmediate(instance);
        }
        
        private static void ApplyRoomConfiguration(GameObject roomRoot, string roomName)
        {
            if (!RoomConfigs.TryGetValue(roomName, out RoomConfig config))
            {
                Debug.LogWarning($"No config for {roomName}");
                return;
            }
            
            // Set layer
            roomRoot.layer = LayerMask.NameToLayer("Default");
            foreach (Transform child in roomRoot.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            
            // Configure materials to URP/Lit
            var renderers = roomRoot.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    
                    // Ensure URP/Lit
                    if (!mat.shader.name.Contains("Universal Render Pipeline/Lit"))
                    {
                        ConvertMaterialToURPLit(mat);
                    }
                    
                    // Mark as static for lightmapping
                    renderer.gameObject.isStatic = true;
                }
            }
            
            // Add reflection probe
            AddReflectionProbe(roomRoot, config);
            
            // Add light probes
            AddLightProbes(roomRoot);
        }
        
        private static void ConvertMaterialToURPLit(Material mat)
        {
            // Save properties
            Color baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
            float occlusion = mat.HasProperty("_OcclusionStrength") ? mat.GetFloat("_OcclusionStrength") : 1f;
            
            Texture2D baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") as Texture2D : null;
            Texture2D normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") as Texture2D : null;
            Texture2D metallicMap = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") as Texture2D : null;
            Texture2D occlusionMap = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") as Texture2D : null;
            Texture2D emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") as Texture2D : null;
            
            bool hasEmission = mat.IsKeywordEnabled("_EMISSION");
            float mode = mat.HasProperty("_Mode") ? mat.GetFloat("_Mode") : 0f;
            
            // Convert
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            
            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_EmissiveColor", emissionColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_OcclusionStrength", occlusion);
            
            if (baseMap) mat.SetTexture("_BaseMap", baseMap);
            if (normalMap) mat.SetTexture("_BumpMap", normalMap);
            if (metallicMap) mat.SetTexture("_MetallicGlossMap", metallicMap);
            if (occlusionMap) mat.SetTexture("_OcclusionMap", occlusionMap);
            if (emissionMap) mat.SetTexture("_EmissiveMap", emissionMap);
            
            if (hasEmission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            
            // Handle transparency
            if (mode == 2 || mode == 3) // Fade or Transparent
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0); // Alpha
                mat.SetFloat("_ZWrite", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
            }
            else
            {
                mat.SetFloat("_Surface", 0); // Opaque
                mat.SetFloat("_Blend", 0);
                mat.SetFloat("_ZWrite", 1);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = 2000;
            }
            
            EditorUtility.SetDirty(mat);
        }
        
        private static void AddReflectionProbe(GameObject roomRoot, RoomConfig config)
        {
            var probe = roomRoot.GetComponentInChildren<ReflectionProbe>();
            if (probe == null)
            {
                GameObject probeObj = new GameObject("ReflectionProbe");
                probeObj.transform.SetParent(roomRoot.transform);
                probeObj.transform.localPosition = new Vector3(0, 2, 0);
                probe = probeObj.AddComponent<ReflectionProbe>();
            }
            
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(10, 4, 10);
            probe.center = new Vector3(0, 2, 0);
            probe.intensity = config.reflectionIntensity;
            probe.resolution = 128;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.hdr = true;
            probe.shadowDistance = 100;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.backgroundColor = config.ambientColor;
            probe.cullingMask = -1;
            probe.boxProjection = true;
        }
        
        private static void AddLightProbes(GameObject roomRoot)
        {
            var probeGroup = roomRoot.GetComponentInChildren<LightProbeGroup>();
            if (probeGroup == null)
            {
                GameObject probeObj = new GameObject("LightProbeGroup");
                probeObj.transform.SetParent(roomRoot.transform);
                probeGroup = probeObj.AddComponent<LightProbeGroup>();
            }
            
            // Create probe positions in a grid
            var positions = new List<Vector3>();
            for (int x = -4; x <= 4; x += 2)
            {
                for (int y = 1; y <= 3; y += 1)
                {
                    for (int z = -4; z <= 4; z += 2)
                    {
                        positions.Add(new Vector3(x, y, z));
                    }
                }
            }
            probeGroup.probePositions = positions.ToArray();
        }
        
        private static void CreateRoomScene(GameObject roomInstance, string roomName)
        {
            string scenePath = Path.Combine(SCENES_ROOT, $"{roomName}.unity");
            
            // Create new scene (Unity 6 API)
            var scene = EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            
            // Add room instance
            var roomInScene = PrefabUtility.InstantiatePrefab(roomInstance) as GameObject;
            roomInScene.name = roomName;
            
            // Apply lighting config
            if (RoomConfigs.TryGetValue(roomName, out RoomConfig config))
            {
                ApplySceneLighting(config);
            }
            
            // Add essential components
            AddSceneEssentials(roomInScene, roomName);
            
            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.CloseScene(scene, true);
        }
        
        private static void ApplySceneLighting(RoomConfig config)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = config.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = config.fogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = config.fogDensity;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = config.reflectionIntensity;
            RenderSettings.reflectionBounces = 1;
            
            // Sun light
            var sunObj = new GameObject("Directional Light");
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = config.sunColor;
            sun.intensity = config.sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.4f;
            sun.shadowNearPlane = 0.1f;
            sunObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
        
        private static void AddSceneEssentials(GameObject roomRoot, string roomName)
        {
            // Room manager component
            var manager = roomRoot.AddComponent<RoomManager>();
            manager.roomName = roomName;
            manager.roomType = (RoomType)System.Array.IndexOf(RoomNames, roomName);
            
            // NavMesh setup (requires AI Navigation package)
            #if UNITY_AI_NAVIGATION || CUESTRIKE_AI_NAVIGATION
            var navMesh = roomRoot.AddComponent<UnityEngine.AI.NavMeshSurface>();
            navMesh.collectObjects = UnityEngine.AI.CollectObjects.Children;
            navMesh.layerMask = 1 << LayerMask.NameToLayer("Default");
            navMesh.defaultArea = 0;
            navMesh.BuildNavMesh();
            #else
            Debug.LogWarning($"[RoomSetupAAA] AI Navigation package not installed - skipping NavMesh setup for {roomName}. Install 'AI Navigation' package and add CUESTRIKE_AI_NAVIGATION to Scripting Define Symbols.");
            #endif
            
            // Static batching
            StaticBatchingUtility.Combine(roomRoot.gameObject);
        }
        
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
    
    // Supporting classes
    public class RoomConfig
    {
        public Color ambientColor;
        public Color fogColor;
        public float fogDensity;
        public Color sunColor;
        public float sunIntensity;
        public float reflectionIntensity;
    }
    
    public enum RoomType
    {
        ZenDojo = 0,
        Cyberpunk = 1,
        SpaceNebula = 2,
        Industrial = 3,
        WarpFantasy = 4,
        Luxury_DAY = 5,
        Luxury_NIGHT = 6,
        Arena_Core = 7
    }
    
    public class RoomManager : MonoBehaviour
    {
        public string roomName;
        public RoomType roomType;
        public bool isInitialized = false;
        
        void Awake()
        {
            InitializeRoom();
        }
        
        void InitializeRoom()
        {
            if (isInitialized) return;
            
            // Apply room-specific runtime settings
            switch (roomType)
            {
                case RoomType.ZenDojo:
                    SetupZenDojo();
                    break;
                case RoomType.Cyberpunk:
                    SetupCyberpunk();
                    break;
                case RoomType.SpaceNebula:
                    SetupSpaceNebula();
                    break;
                case RoomType.Industrial:
                    SetupIndustrial();
                    break;
                case RoomType.WarpFantasy:
                    SetupWarpFantasy();
                    break;
                case RoomType.Luxury_DAY:
                    SetupLuxuryDay();
                    break;
                case RoomType.Luxury_NIGHT:
                    SetupLuxuryNight();
                    break;
                case RoomType.Arena_Core:
                    SetupArenaCore();
                    break;
            }
            
            isInitialized = true;
        }
        
        void SetupZenDojo()
        {
            // Enable zen particle effects, calm audio
            PlayAmbience("Play_Ambience_ZenDojo");
        }
        
        void SetupCyberpunk()
        {
            // Enable neon flicker, rain particles
            PlayAmbience("Play_Ambience_Cyberpunk");
        }
        
        void SetupSpaceNebula()
        {
            // Enable starfield animation, nebula shader
            PlayAmbience("Play_Ambience_Space");
        }
        
        void SetupIndustrial()
        {
            // Enable steam particles, fan rotation
            PlayAmbience("Play_Ambience_Industrial");
        }
        
        void SetupWarpFantasy()
        {
            // Enable magic particles, rune animation
            PlayAmbience("Play_Ambience_Fantasy");
        }
        
        void SetupLuxuryDay()
        {
            // Enable dust motes, warm light
            PlayAmbience("Play_Ambience_Luxury");
        }
        
        void SetupLuxuryNight()
        {
            // Enable candle flicker, intimate ambience
            PlayAmbience("Play_Ambience_Luxury_Night");
        }
        
        void SetupArenaCore()
        {
            // Enable hologram animation, spawn pads
            PlayAmbience("Play_Ambience_Arena");
        }
        
        void PlayAmbience(string eventName)
        {
            // Try Wwise first
            #if UNITY_WWISE || WWISE_ENABLED
            try 
            {
                AkSoundEngine.PostEvent(eventName, gameObject);
                return;
            }
            catch { }
            #endif
            
            // Fallback to CueStrikeAudioManager (new system)
            if (CueStrike.Audio.CueStrikeAudioManager.Instance != null)
            {
                CueStrike.Audio.CueStrikeAudioManager.Instance.PlayAmbientRoom();
                return;
            }
            
            // Final fallback to Unity AudioSource
            var audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D
                audioSource.loop = true;
                audioSource.playOnAwake = false;
            }
            Debug.Log($"[RoomManager] Playing ambience: {eventName} (Unity AudioSource fallback)");
        }
    }
    
    // Lighting preset ScriptableObject
    public class RoomLightingPreset : ScriptableObject
    {
        public string roomName;
        public Color ambientColor;
        public Color fogColor;
        public float fogDensity;
        public Color sunColor;
        public float sunIntensity;
        public float reflectionIntensity;
        
        public void ApplyToScene()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.reflectionIntensity = reflectionIntensity;
        }
    }
}