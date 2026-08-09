using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace CueStrike.Characters.Editor
{
    /// <summary>
    /// Editor tool for setting up character prefabs with ability controllers,
    /// IK targets, and materials. One-click character configuration.
    /// </summary>
    public class CharacterSetupEditor : UnityEditor.EditorWindow
    {
        private GameObject _selectedPrefab;
        private CharacterData _characterData;
        private string _abilityType = "";
        private bool _findIKTargets = true;
        private bool _setupMaterials = true;
        private Vector2 _scrollPos;

        // Ability type lookup
        private readonly Dictionary<string, System.Type> _abilityTypes = new Dictionary<string, System.Type>
        {
            { "BoPandaHypeEngine", typeof(BoPanda.BoPandaHypeEngine) },
            { "FinnAquaRush", typeof(Finn.FinnAquaRush) },
            { "KingFlexBlingBling", typeof(KingFlex.KingFlexBlingBling) },
            { "TuskerGentlemansMemory", typeof(Tusker.TuskerGentlemansMemory) },
            { "PanPanZenStance", typeof(PanPan.PanPanZenStance) },
            { "PhantomSpectralSight", typeof(Phantom.PhantomSpectralSight) },
            { "CassidyQuickDraw", typeof(Cassidy.CassidyQuickDraw) },
            { "BonesXRayVision", typeof(Bones.BonesXRayVision) },
            { "SomchayAbilityController", null }, // Special case - doesn't implement ICharacterAbility
            { "MeiLingAbilityController", null },
            { "GentlemanAbilityController", null }
        };

        [MenuItem("CueStrike/Character Setup Editor", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<CharacterSetupEditor>("Character Setup");
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawPrefabSection();
            DrawCharacterDataSection();
            DrawAbilitySection();
            DrawSetupOptions();
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("CueStrike Character Setup", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Configure character prefabs with abilities, IK, and materials.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
        }

        private void DrawPrefabSection()
        {
            EditorGUILayout.LabelField("Character Prefab", EditorStyles.boldLabel);
            _selectedPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Prefab", _selectedPrefab, typeof(GameObject), false);

            if (_selectedPrefab != null)
            {
                EditorGUILayout.HelpBox(
                    $"Selected: {_selectedPrefab.name}\nPath: {AssetDatabase.GetAssetPath(_selectedPrefab)}",
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);
        }

        private void DrawCharacterDataSection()
        {
            EditorGUILayout.LabelField("Character Data (Optional)", EditorStyles.boldLabel);
            _characterData = (CharacterData)EditorGUILayout.ObjectField(
                "Data Asset", _characterData, typeof(CharacterData), false);

            if (_characterData != null)
            {
                EditorGUILayout.LabelField("Name:", _characterData.characterName);
                EditorGUILayout.LabelField("Ability:", _characterData.abilityControllerType);

                if (GUILayout.Button("Select Prefab from Data"))
                {
                    _selectedPrefab = _characterData.characterPrefab;
                }
            }

            EditorGUILayout.Space(5);
        }

        private void DrawAbilitySection()
        {
            EditorGUILayout.LabelField("Ability Controller", EditorStyles.boldLabel);

            // Ability type dropdown
            string[] abilityNames = new List<string>(_abilityTypes.Keys).ToArray();
            int currentIndex = System.Array.IndexOf(abilityNames, _abilityType);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("Ability Type", currentIndex, abilityNames);
            if (newIndex != currentIndex)
                _abilityType = abilityNames[newIndex];

            // Custom type input
            EditorGUILayout.LabelField("Or type custom class name:", EditorStyles.miniLabel);
            _abilityType = EditorGUILayout.TextField(_abilityType);

            EditorGUILayout.Space(5);
        }

        private void DrawSetupOptions()
        {
            EditorGUILayout.LabelField("Setup Options", EditorStyles.boldLabel);
            _findIKTargets = EditorGUILayout.Toggle("Auto-find IK Targets", _findIKTargets);
            _setupMaterials = EditorGUILayout.Toggle("Setup Materials", _setupMaterials);

            EditorGUILayout.Space(5);
        }

        private void DrawActionButtons()
        {
            GUI.enabled = _selectedPrefab != null;

            if (GUILayout.Button("Setup Character Prefab", GUILayout.Height(30)))
            {
                SetupCharacter();
            }

            if (_characterData != null && _selectedPrefab != null)
            {
                if (GUILayout.Button("Create CharacterData Asset", GUILayout.Height(25)))
                {
                    CreateCharacterDataAsset();
                }
            }

            GUI.enabled = true;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup All Characters in Scene"))
            {
                SetupAllInScene();
            }

            if (GUILayout.Button("Create All Missing Directories"))
            {
                CreateMissingDirectories();
            }

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Setup selected character prefab
        /// </summary>
        private void SetupCharacter()
        {
            if (_selectedPrefab == null) return;

            string path = AssetDatabase.GetAssetPath(_selectedPrefab);
            GameObject instance = PrefabUtility.LoadPrefabContents(path);

            if (instance == null)
            {
                Debug.LogError("[CharacterSetup] Failed to load prefab contents!");
                return;
            }

            bool modified = false;

            // 1. Add ability controller
            if (!string.IsNullOrEmpty(_abilityType) && _abilityTypes.ContainsKey(_abilityType))
            {
                System.Type type = _abilityTypes[_abilityType];
                if (type != null)
                {
                    var existing = instance.GetComponent(type);
                    if (existing == null)
                    {
                        instance.AddComponent(type);
                        modified = true;
                        Debug.Log($"[CharacterSetup] Added {_abilityType} to {_selectedPrefab.name}");
                    }
                }
            }

            // 2. Find IK targets
            if (_findIKTargets)
            {
                AddIKTargets(instance, ref modified);
            }

            // 3. Apply from CharacterData
            if (_characterData != null && _setupMaterials)
            {
                if (_characterData.characterMaterial != null)
                {
                    var renderers = instance.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                    {
                        r.sharedMaterial = _characterData.characterMaterial;
                    }
                    modified = true;
                    Debug.Log($"[CharacterSetup] Applied material to {_selectedPrefab.name}");
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Debug.Log($"[CharacterSetup] Saved prefab: {_selectedPrefab.name}");
            }
            else
            {
                Debug.Log("[CharacterSetup] No changes needed.");
            }

            PrefabUtility.UnloadPrefabContents(instance);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Add IK target transforms if not present
        /// </summary>
        private void AddIKTargets(GameObject instance, ref bool modified)
        {
            // Look for hand bones
            Transform leftHand = FindChildByName(instance.transform, "LeftHand");
            Transform rightHand = FindChildByName(instance.transform, "RightHand");

            // Create IK targets if needed
            if (leftHand != null && instance.transform.Find("LeftHand_IK") == null)
            {
                GameObject ikTarget = new GameObject("LeftHand_IK");
                ikTarget.transform.SetParent(instance.transform, false);
                ikTarget.transform.position = leftHand.position;
                ikTarget.transform.rotation = leftHand.rotation;
                modified = true;
                Debug.Log("[CharacterSetup] Created LeftHand_IK");
            }

            if (rightHand != null && instance.transform.Find("RightHand_IK") == null)
            {
                GameObject ikTarget = new GameObject("RightHand_IK");
                ikTarget.transform.SetParent(instance.transform, false);
                ikTarget.transform.position = rightHand.position;
                ikTarget.transform.rotation = rightHand.rotation;
                modified = true;
                Debug.Log("[CharacterSetup] Created RightHand_IK");
            }
        }

        /// <summary>
        /// Find child transform by name (recursive)
        /// </summary>
        private Transform FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                Transform result = FindChildByName(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Create CharacterData asset from current settings
        /// </summary>
        private void CreateCharacterDataAsset()
        {
            if (_characterData == null || _selectedPrefab == null) return;

            string path = AssetDatabase.GetAssetPath(_selectedPrefab);
            string dir = System.IO.Path.GetDirectoryName(path);
            string assetPath = $"{dir}/{_selectedPrefab.name}_Data.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Overwrite?", $"CharacterData already exists at:\n{assetPath}\n\nOverwrite?", "Yes", "No"))
                    return;
            }

            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterName = _selectedPrefab.name;
            data.characterPrefab = _selectedPrefab;
            data.abilityControllerType = _abilityType;

            // Fill from _characterData if set
            if (_characterData != null)
            {
                data.subtitle = _characterData.subtitle;
                data.description = _characterData.description;
                data.characterMaterial = _characterData.characterMaterial;
                data.portrait = _characterData.portrait;
                data.cardColor = _characterData.cardColor;
                data.abilityDescription = _characterData.abilityDescription;
                data.voiceClip = _characterData.voiceClip;
                data.abilitySound = _characterData.abilitySound;
            }

            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(data);
            Debug.Log($"[CharacterSetup] Created CharacterData: {assetPath}");
        }

        /// <summary>
        /// Setup all characters currently in scene
        /// </summary>
        private void SetupAllInScene()
        {
            var characters = FindObjectsOfType<PlayerCharacterManager>();
            if (characters.Length == 0)
            {
                EditorUtility.DisplayDialog("No Manager", "No PlayerCharacterManager found in scene!", "OK");
                return;
            }

            foreach (var manager in characters)
            {
                foreach (var data in manager.availableCharacters)
                {
                    if (data?.characterPrefab != null)
                    {
                        _selectedPrefab = data.characterPrefab;
                        _characterData = data;
                        _abilityType = data.abilityControllerType;
                        SetupCharacter();
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[CharacterSetup] All characters in scene setup complete!");
        }

        /// <summary>
        /// Create all missing character directories
        /// </summary>
        private void CreateMissingDirectories()
        {
            string basePath = "Assets/CueStrike/Characters";
            string[] dirs = {
                "BoPanda", "Finn", "KingFlex", "Tusker",
                "PanPan", "Phantom", "Cassidy", "Bones",
                "Somchay", "MeiLing", "Gentleman", "Editor"
            };

            foreach (var dir in dirs)
            {
                string fullPath = $"{basePath}/{dir}";
                if (!AssetDatabase.IsValidFolder(fullPath))
                {
                    System.IO.Directory.CreateDirectory(fullPath);
                    AssetDatabase.Refresh();
                    Debug.Log($"[CharacterSetup] Created directory: {fullPath}");
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Directories", "All character directories created!", "OK");
        }
    }
}