using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CueStrike.Editor
{
    /// <summary>
    /// Unity Editor Tool: สร้าง Title Screen Scene สำหรับ CueStrike VR
    /// Menu: Tools/CueStrike/Setup/Create Title Screen Scene
    /// </summary>
    public class TitleScreenSetup
    {
        private const string MENU_PATH = "Tools/CueStrike/Setup/Create Title Screen Scene";
        private const string SCENE_NAME = "TitleScreen";
        private const string SCENE_PATH = "Assets/CueStrike/Scenes/TitleScreen.unity";
        private const string FBX_PATH = "Assets/CueStrike/Models/TitleScreen/TitleScreen.fbx";
        private const string PREFAB_PATH = "Assets/CueStrike/Prefabs/TitleScreen/TitleScreen.prefab";

        [MenuItem(MENU_PATH, priority = 100)]
        public static void CreateTitleScreenScene()
        {
            // 3-Layer Guard
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Blocked", "Cannot create scene while in Play Mode.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // Check if FBX exists
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH))
            {
                EditorUtility.DisplayDialog("Missing FBX", 
                    $"Title Screen FBX not found at {FBX_PATH}\n\n" +
                    "Please run Blender script first:\n" +
                    "blender --background --python BlenderScripts/create_title_screen.py\n\n" +
                    "Then import the FBX to Unity.", "OK");
                return;
            }

            // Create scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SCENE_NAME;

            // Load FBX prefab
            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
            var titleScreenInstance = PrefabUtility.InstantiatePrefab(fbxAsset) as GameObject;
            titleScreenInstance.name = "TitleScreen_Root";
            titleScreenInstance.transform.position = Vector3.zero;
            titleScreenInstance.transform.rotation = Quaternion.identity;
            titleScreenInstance.transform.localScale = Vector3.one;

            // Setup materials to URP
            FixMaterialsToURP(titleScreenInstance);

            // Add AudioSource for title music
            var audioSource = titleScreenInstance.AddComponent<AudioSource>();
            audioSource.playOnAwake = true;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // 2D music
            audioSource.volume = 0.7f;
            // audioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/CueStrike/Audio/Music/TitleTheme.wav");

            // Add TitleScreenManager component
            var manager = titleScreenInstance.AddComponent<TitleScreenManager>();
            
            // Find and assign animation
            var anim = titleScreenInstance.GetComponentInChildren<Animation>();
            if (anim != null)
            {
                manager.titleAnimation = anim;
                // Set animation to loop
                foreach (AnimationState state in anim)
                {
                    state.wrapMode = WrapMode.Loop;
                }
                anim.Play();
            }

            // Setup lighting from FBX
            SetupSceneLighting();

            // Create Prefab
            var prefabDir = System.IO.Path.GetDirectoryName(PREFAB_PATH);
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                System.IO.Directory.CreateDirectory(prefabDir);
                AssetDatabase.Refresh();
            }
            
            var prefab = PrefabUtility.SaveAsPrefabAsset(titleScreenInstance, PREFAB_PATH);
            Debug.Log($"✅ TitleScreen Prefab saved to: {PREFAB_PATH}");

            // Save scene
            var sceneDir = System.IO.Path.GetDirectoryName(SCENE_PATH);
            if (!AssetDatabase.IsValidFolder(sceneDir))
            {
                System.IO.Directory.CreateDirectory(sceneDir);
                AssetDatabase.Refresh();
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"✅ TitleScreen Scene saved to: {SCENE_PATH}");

            // Add to Build Settings
            AddSceneToBuildSettings(SCENE_PATH, 0); // Index 0 = first scene

            EditorUtility.DisplayDialog("Success", 
                $"Title Screen created successfully!\n\n" +
                $"Scene: {SCENE_PATH}\n" +
                $"Prefab: {PREFAB_PATH}\n\n" +
                "Next steps:\n" +
                "1. Assign TitleTheme.wav to AudioSource\n" +
                "2. Hook up TitleScreenManager to UI buttons\n" +
                "3. Test in VR", "OK");
        }

        private static void FixMaterialsToURP(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            int fixedCount = 0;
            
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat != null && mat.shader.name.Contains("Standard") && !mat.shader.name.Contains("URP"))
                    {
                        // Try to find URP equivalent
                        var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                        if (urpShader != null)
                        {
                            var newMat = new Material(urpShader);
                            // Copy properties
                            if (mat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", mat.GetColor("_BaseColor"));
                            else if (mat.HasProperty("_Color")) newMat.SetColor("_BaseColor", mat.GetColor("_Color"));
                            
                            if (mat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", mat.GetFloat("_Metallic"));
                            if (mat.HasProperty("_Glossiness")) newMat.SetFloat("_Smoothness", 1f - mat.GetFloat("_Glossiness"));
                            else if (mat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", mat.GetFloat("_Smoothness"));
                            
                            if (mat.HasProperty("_EmissionColor")) newMat.SetColor("_EmissionColor", mat.GetColor("_EmissionColor"));
                            if (mat.HasProperty("_Emission")) newMat.EnableKeyword("_EMISSION");
                            
                            newMat.name = mat.name + "_URP";
                            materials[i] = newMat;
                            fixedCount++;
                        }
                    }
                }
                renderer.sharedMaterials = materials;
            }
            
            if (fixedCount > 0)
            {
                Debug.Log($"🔧 Fixed {fixedCount} materials to URP/Lit");
            }
        }

        private static void SetupSceneLighting()
        {
            // Ensure we have a Lighting Settings asset
            var lightingSettingsPath = "Assets/CueStrike/Environment/Lighting/TitleScreenLightingSettings.asset";
            var lightingSettingsDir = System.IO.Path.GetDirectoryName(lightingSettingsPath);
            if (!AssetDatabase.IsValidFolder(lightingSettingsDir))
            {
                System.IO.Directory.CreateDirectory(lightingSettingsDir);
                AssetDatabase.Refresh();
            }

            // Create LightingSettings if not exists
            var lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingSettingsPath);
            if (lightingSettings == null)
            {
                lightingSettings = new LightingSettings();
                AssetDatabase.CreateAsset(lightingSettings, lightingSettingsPath);
                AssetDatabase.SaveAssets();
            }

            // Apply to scene
            Lightmapping.lightingSettings = lightingSettings;
            
            // Generate lighting (optional - can be done manually)
            // Lightmapping.Bake();
        }

        private static void AddSceneToBuildSettings(string scenePath, int index)
        {
            var scenes = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            
            // Remove if already exists
            list.RemoveAll(s => s.path == scenePath);
            
            // Insert at index
            if (index >= 0 && index <= list.Count)
            {
                list.Insert(index, new EditorBuildSettingsScene(scenePath, true));
            }
            else
            {
                list.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"📋 Added {scenePath} to Build Settings at index {index}");
        }

        [MenuItem(MENU_PATH, validate = true)]
        public static bool ValidateCreateTitleScreenScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }

    /// <summary>
    /// Runtime component for Title Screen management
    /// </summary>
    public class TitleScreenManager : MonoBehaviour
    {
        public Animation titleAnimation;
        public AudioClip titleMusic;
        public float autoStartDelay = 0f;
        
        private AudioSource _audioSource;
        private bool _hasStarted = false;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (titleMusic != null && _audioSource != null)
            {
                _audioSource.clip = titleMusic;
            }
        }

        private void Start()
        {
            if (autoStartDelay > 0)
            {
                Invoke(nameof(StartTitleSequence), autoStartDelay);
            }
            else
            {
                StartTitleSequence();
            }
        }

        public void StartTitleSequence()
        {
            if (_hasStarted) return;
            _hasStarted = true;

            if (_audioSource != null && _audioSource.clip != null)
            {
                _audioSource.Play();
            }

            if (titleAnimation != null)
            {
                titleAnimation.Play();
            }
        }

        public void OnPlayButtonPressed()
        {
            // Transition to main menu or game scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void OnSettingsButtonPressed()
        {
            // Open settings overlay
            Debug.Log("Open Settings");
        }

        public void OnQuitButtonPressed()
        {
            #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}