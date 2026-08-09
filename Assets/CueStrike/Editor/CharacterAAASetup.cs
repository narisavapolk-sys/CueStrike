using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Animations.Rigging;
using System.IO;
using System.Collections.Generic;

public class CharacterAAASetup : EditorWindow
{
    private const string CharactersRoot = "Assets/CueStrike/Characters";
    private const string ExportedFBX = "Assets/CueStrike/Models/Somchay_AAA.fbx";

    [MenuItem("CueStrike/Character System/Setup All AAA Characters")]
    public static void ShowWindow()
    {
        GetWindow<CharacterAAASetup>("AAA Character Setup");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Generate All Character Prefabs"))
        {
            GenerateAllPrefabs();
        }
    }

    private static void GenerateAllPrefabs()
    {
        // Validate that the base FBX exists
        if (!File.Exists(ExportedFBX))
        {
            Debug.LogError($"Base FBX not found at {ExportedFBX}. Please export at least one character using Blender script first.");
            return;
        }

        // Load the base FBX as a GameObject
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExportedFBX);
        if (basePrefab == null)
        {
            Debug.LogError("Failed to load base FBX as GameObject.");
            return;
        }

        // Ensure the model import settings are Humanoid
        SetHumanoidImportSettings(ExportedFBX);

        // Roster per CHARACTER_SYSTEM_PLAN.md — build prefabs only for these folders
        string[] roster =
        {
            "Somchay", "MeiLing", "Gentleman",
            "PanPan", "Finn", "KingFlex", "Tusker", "Phantom", "Cassidy", "Bones",
            "UncleNok", "BoPanda"
        };

        foreach (var characterName in roster)
        {
            string dir = Path.Combine(CharactersRoot, characterName);
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[CharacterAAASetup] Skipping {characterName} — folder not found at {dir}");
                continue;
            }

            // Create a new instance from the base prefab
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = characterName + "_Prefab";

            // Assign texture if image exists
            AssignCharacterTexture(instance, dir);

            // Add Rig Builder and Rig with constraints
            AddRigging(instance);

            // Save as prefab
            var prefabPath = Path.Combine(CharactersRoot, characterName, characterName + "_Prefab.prefab");
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Debug.Log($"Created prefab for {characterName} at {prefabPath}");

            // Clean up temporary scene object
            DestroyImmediate(instance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All character prefabs generated.");
    }

    private static void SetHumanoidImportSettings(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.Log($"Set Humanoid import settings for {fbxPath}");
        }
    }

    private static void AssignCharacterTexture(GameObject characterRoot, string characterDir)
    {
        // Look for a texture file (png or jpeg) inside the character folder
        var texFiles = Directory.GetFiles(characterDir, "*.png");
        if (texFiles.Length == 0)
            texFiles = Directory.GetFiles(characterDir, "*.jpeg");
        if (texFiles.Length == 0)
            texFiles = Directory.GetFiles(characterDir, "*.jpg");
        if (texFiles.Length == 0) return;

        var texturePath = texFiles[0];
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogWarning($"Could not load texture at {texturePath}");
            return;
        }

        // Create a simple material using URP Lit shader
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.mainTexture = texture;
        mat.name = characterRoot.name + "_Mat";

        // Save material asset next to the prefab
        var matPath = Path.Combine(Path.GetDirectoryName(characterDir), characterRoot.name + "_Mat.mat");
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();

        // Apply material to all renderers in the hierarchy
        var renderers = characterRoot.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.sharedMaterial = mat;
        }
    }

    private static void AddRigging(GameObject characterRoot)
    {
        // Add Rig Builder component
        var rigBuilder = characterRoot.AddComponent<RigBuilder>();

        // Create a child object to hold the rig
        var rigGO = new GameObject("Rig");
        rigGO.transform.SetParent(characterRoot.transform);
        var rig = rigGO.AddComponent<Rig>();

        // Two‑Bone IK for left hand
        var leftHandIK = new GameObject("LeftHand_IK");
        leftHandIK.transform.SetParent(rigGO.transform);
        var leftIK = leftHandIK.AddComponent<TwoBoneIKConstraint>();
        leftIK.data.root = FindBone(characterRoot.transform, "leftArm");
        leftIK.data.mid = FindBone(characterRoot.transform, "leftForeArm");
        leftIK.data.tip = FindBone(characterRoot.transform, "leftHand");
        leftIK.data.target = FindBone(characterRoot.transform, "LeftHand_IKTarget"); // placeholder, will be created later if missing
        leftIK.data.hint = FindBone(characterRoot.transform, "leftElbow");

        // Two‑Bone IK for right hand
        var rightHandIK = new GameObject("RightHand_IK");
        rightHandIK.transform.SetParent(rigGO.transform);
        var rightIK = rightHandIK.AddComponent<TwoBoneIKConstraint>();
        rightIK.data.root = FindBone(characterRoot.transform, "rightArm");
        rightIK.data.mid = FindBone(characterRoot.transform, "rightForeArm");
        rightIK.data.tip = FindBone(characterRoot.transform, "rightHand");
        rightIK.data.target = FindBone(characterRoot.transform, "RightHand_IKTarget");
        rightIK.data.hint = FindBone(characterRoot.transform, "rightElbow");

            }

    private static Transform FindBone(Transform root, string boneName)
    {
        var child = root.Find(boneName);
        if (child != null) return child;
        // Search recursively
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains(boneName.ToLower()))
                return t;
        }
        Debug.LogWarning($"Bone {boneName} not found in {root.name}");
        return null;
    }
}