#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

// Utility to generate physics materials and prefabs for CueStrike
[InitializeOnLoad]
public static class CueStrikeAssetCreator
{
    static CueStrikeAssetCreator()
    {
        EditorApplication.delayCall += EnsureAssets;
    }

    private static void EnsureAssets()
    {
        CreatePhysicsMaterials();
        CreateBallPrefab();
        CreateCuePrefab();
    }

    private static void CreatePhysicsMaterials()
    {
        var basePath = "Assets/CueStrike/Physics/Materials";
        if (!AssetDatabase.IsValidFolder(basePath))
        {
            AssetDatabase.CreateFolder("Assets/CueStrike/Physics", "Materials");
        }

        void Ensure(string name, float bounciness, float friction)
        {
            var path = basePath + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path) == null)
            {
                var mat = new PhysicsMaterial(name);
                mat.bounciness = bounciness;
                mat.dynamicFriction = friction;
                mat.staticFriction = friction;
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        Ensure("BallMaterial", 0.6f, 0.2f);
        Ensure("TableFelt", 0.02f, 0.9f);
        Ensure("Cushion", 0.5f, 0.6f);
        AssetDatabase.SaveAssets();
    }

    private static void CreateBallPrefab()
    {
        var prefabPath = "Assets/CueStrike/Prefabs/CueStrikeBall.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "CueStrikeBall";
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.17f;
        var phys = go.AddComponent<CueStrikeBallPhysics>();

        // ensure folder
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Prefabs"))
            AssetDatabase.CreateFolder("Assets/CueStrike", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);
    }

    private static void CreateCuePrefab()
    {
        var prefabPath = "Assets/CueStrike/Prefabs/CueStrikeCue.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        var go = new GameObject("CueStrikeCue");
        var cue = go.AddComponent<CueStrikeCue>();
        var tip = new GameObject("Tip");
        tip.transform.SetParent(go.transform, false);
        cue.tipTransform = tip.transform;

        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Prefabs"))
            AssetDatabase.CreateFolder("Assets/CueStrike", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);
    }
}
#endif