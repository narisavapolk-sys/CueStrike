using CueStrike.Characters.Skins;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor Tool: Build Epic/Legendary skin prefabs from base character prefabs
    /// Menu: Tools/CueStrike/Skins/Build All Skin Prefabs
    /// </summary>
    public class SkinBuilder
    {
        private const string MENU_PATH = "Tools/CueStrike/Skins/Build All Skin Prefabs";
        private const string BASE_PREFAB_PATH = "Assets/CueStrike/Prefabs/AAA_Characters";
        private const string OUTPUT_PREFAB_PATH = "Assets/CueStrike/Prefabs/Skins";

        [MenuItem(MENU_PATH, priority = 201)]
        public static void BuildAllSkinPrefabs()
        {
            // 3-Layer Guard
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Blocked", "Cannot build prefabs while in Play Mode.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // Find all skin data assets
            string[] guids = AssetDatabase.FindAssets("t:CharacterSkinData", new[] { "Assets/CueStrike/Characters/Skins/Resources" });
            
            int built = 0;
            int skipped = 0;
            var errors = new List<string>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var skinData = AssetDatabase.LoadAssetAtPath<CharacterSkinData>(path);

                if (skinData == null) continue;

                // Only build prefabs for Epic and Legendary skins that don't have one yet
                if (skinData.rarity >= SkinRarity.Epic && skinData.skinPrefab == null)
                {
                    try
                    {
                        if (BuildSkinPrefab(skinData))
                        {
                            built++;
                        }
                        else
                        {
                            skipped++;
                            errors.Add($"{skinData.skinId}: Base prefab not found");
                        }
                    }
                    catch (System.Exception e)
                    {
                        errors.Add($"{skinData.skinId}: {e.Message}");
                        Debug.LogError($"[SkinBuilder] Failed to build {skinData.skinId}: {e.Message}");
                    }
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message = $"Built {built} Epic/Legendary skin prefabs. Skipped: {skipped}";
            if (errors.Count > 0)
            {
                message += $"\n\nErrors:\n" + string.Join("\n", errors);
            }

            EditorUtility.DisplayDialog("Skin Builder Complete", message, "OK");
        }

        private static bool BuildSkinPrefab(CharacterSkinData skinData)
        {
            // Load base character prefab
            string charName = skinData.characterId.ToTitleCase();
            string basePrefabPath = $"{BASE_PREFAB_PATH}/{charName}_AAA.prefab";
            
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (basePrefab == null)
            {
                // Try alternative naming
                string[] altPaths = new[]
                {
                    $"{BASE_PREFAB_PATH}/{charName}.prefab",
                    $"Assets/CueStrike/Prefabs/Characters/{charName}.prefab",
                    $"Assets/CueStrike/Prefabs/AAA_Characters/{charName}_Base.prefab"
                };
                
                foreach (var altPath in altPaths)
                {
                    basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(altPath);
                    if (basePrefab != null) break;
                }
            }

            if (basePrefab == null)
            {
                Debug.LogWarning($"[SkinBuilder] Base prefab not found for {skinData.characterId} at {basePrefabPath}");
                return false;
            }

            // Instantiate and modify
            var instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            if (instance == null) return false;

            instance.name = skinData.skinId;

            // Apply material overrides
            if (skinData.materialOverrides != null && skinData.materialOverrides.Length > 0)
            {
                ApplyMaterialOverrides(instance, skinData.materialOverrides);
            }

            // Add accessories
            if (skinData.accessories != null && skinData.accessories.Length > 0)
            {
                AddAccessories(instance, skinData.accessories);
            }

            // Add VFX
            if (skinData.vfxPrefabs != null && skinData.vfxPrefabs.Length > 0)
            {
                AddVFX(instance, skinData.vfxPrefabs);
            }

            // Apply animator override if present
            if (skinData.animatorOverride != null)
            {
                var animator = instance.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = skinData.animatorOverride;
                }
            }

            // Save as new prefab
            string outputPath = $"{OUTPUT_PREFAB_PATH}/{skinData.skinId}.prefab";
            EnsureFolderExists(OUTPUT_PREFAB_PATH);

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
            UnityEngine.Object.DestroyImmediate(instance);

            if (prefab == null)
            {
                Debug.LogError($"[SkinBuilder] Failed to save prefab: {outputPath}");
                return false;
            }

            // Update skinData reference
            skinData.skinPrefab = prefab;
            EditorUtility.SetDirty(skinData);

            Debug.Log($"✅ [SkinBuilder] Built skin prefab: {outputPath}");
            return true;
        }

        private static void ApplyMaterialOverrides(GameObject instance, Material[] overrides)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length && i < overrides.Length; i++)
                {
                    if (overrides[i] != null)
                    {
                        mats[i] = overrides[i];
                    }
                }
                renderer.sharedMaterials = mats;
            }
        }

        private static void AddAccessories(GameObject instance, GameObject[] accessories)
        {
            // Find or create "Accessories" parent
            Transform accessoryParent = instance.transform.Find("Accessories");
            if (accessoryParent == null)
            {
                accessoryParent = new GameObject("Accessories").transform;
                accessoryParent.SetParent(instance.transform);
                accessoryParent.localPosition = Vector3.zero;
                accessoryParent.localRotation = Quaternion.identity;
            }
            else
            {
                // Clear existing
                for (int i = accessoryParent.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(accessoryParent.GetChild(i).gameObject);
                }
            }

            foreach (var accessory in accessories)
            {
                if (accessory != null)
                {
                    var accInstance = PrefabUtility.InstantiatePrefab(accessory) as GameObject;
                    if (accInstance != null)
                    {
                        accInstance.transform.SetParent(accessoryParent);
                        accInstance.name = accessory.name;
                    }
                }
            }
        }

        private static void AddVFX(GameObject instance, ParticleSystem[] vfxPrefabs)
        {
            Transform vfxParent = instance.transform.Find("VFX");
            if (vfxParent == null)
            {
                vfxParent = new GameObject("VFX").transform;
                vfxParent.SetParent(instance.transform);
                vfxParent.localPosition = Vector3.zero;
                vfxParent.localRotation = Quaternion.identity;
            }
            else
            {
                for (int i = vfxParent.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(vfxParent.GetChild(i).gameObject);
                }
            }

            foreach (var vfx in vfxPrefabs)
            {
                if (vfx != null)
                {
                    var vfxInstance = PrefabUtility.InstantiatePrefab(vfx) as GameObject;
                    if (vfxInstance != null)
                    {
                        vfxInstance.transform.SetParent(vfxParent);
                        vfxInstance.name = vfx.name;
                    }
                }
            }
        }

        private static void EnsureFolderExists(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        [MenuItem(MENU_PATH, validate = true)]
        public static bool ValidateBuildAllSkinPrefabs()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}