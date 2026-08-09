#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TableAutoAssign
{
    [MenuItem("CueStrike/Tools/Assign Table Prefabs to PhysicsManager")]
    public static void Assign()
    {
        var mgr = GameObject.FindFirstObjectByType<CueStrikePhysicsManager>();
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("Assign Tables", "No CueStrikePhysicsManager found in the open scene. Add one to the scene first.", "OK");
            return;
        }

        string dir = "Assets/CueStrike/Prefabs/Tables";
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { dir });
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Assign Tables", "No table prefabs found in " + dir, "OK");
            return;
        }

        // pick first matching snooker or pool prefab
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            if (go.name.ToLower().Contains("snooker"))
            {
                mgr.tablePrefab = go;
                EditorUtility.SetDirty(mgr);
                Debug.Log("Assigned " + go.name + " to CueStrikePhysicsManager.tablePrefab");
                AssetDatabase.SaveAssets();
                return;
            }
        }

        // otherwise assign first prefab
        var firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        mgr.tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(firstPath);
        EditorUtility.SetDirty(mgr);
        AssetDatabase.SaveAssets();
        Debug.Log("Assigned " + mgr.tablePrefab.name + " to CueStrikePhysicsManager.tablePrefab");
    }
}
#endif