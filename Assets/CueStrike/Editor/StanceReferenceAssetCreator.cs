using UnityEngine;
using UnityEditor;
using CueStrike.Characters;

/// <summary>
/// Creates a StanceReferenceData asset in the project.
/// Usage: CueStrike → Character System → Create Stance Reference Asset
/// </summary>
public static class StanceReferenceAssetCreator
{
    private const string AssetPath = "Assets/CueStrike/Characters/StanceReference.asset";

    [MenuItem("CueStrike/Character System/Create Stance Reference Asset")]
    public static void CreateAsset()
    {
        // If the asset already exists, just select it
        var existing = AssetDatabase.LoadAssetAtPath<StanceReferenceData>(AssetPath);
        if (existing != null)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = existing;
            Debug.Log($"StanceReference asset already exists at {AssetPath}");
            return;
        }

        // Create a new ScriptableObject instance
        var asset = ScriptableObject.CreateInstance<StanceReferenceData>();
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the newly created asset
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log($"StanceReference asset created at {AssetPath}");
    }
}