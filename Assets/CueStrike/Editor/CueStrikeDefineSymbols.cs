#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Automatically manages script define symbols for CueStrike.
/// If Normcore is in manifest.json, defines CUESTRIKE_NORMCORE.
/// </summary>
[InitializeOnLoad]
public static class CueStrikeDefineSymbols
{
    private const string Symbol = "CUESTRIKE_NORMCORE";

    [MenuItem("CueStrike/Multiplayer/Enable Multiplayer Sync Code")]
    public static void EnableMultiplayer()
    {
        SetSymbolActive(true);
        EditorUtility.DisplayDialog("Multiplayer Enabled",
            "CUESTRIKE_NORMCORE scripting define symbol has been added.\n\n" +
            "Unity will now compile the Normcore Multiplayer features.", "OK");
    }

    [MenuItem("CueStrike/Multiplayer/Disable Multiplayer Sync Code")]
    public static void DisableMultiplayer()
    {
        SetSymbolActive(false);
        EditorUtility.DisplayDialog("Multiplayer Disabled",
            "CUESTRIKE_NORMCORE scripting define symbol has been removed.\n\n" +
            "Multiplayer scripts will compile as offline stub classes.", "OK");
    }

    public static void SetSymbolActive(bool active)
    {
        BuildTargetGroup[] targetGroups = new[]
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS
        };

        foreach (var group in targetGroups)
        {
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var defineList = defines.Split(';').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).ToList();

            if (active)
            {
                if (!defineList.Contains(Symbol))
                {
                    defineList.Add(Symbol);
                    string newDefines = string.Join(";", defineList);
                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
                    Debug.Log($"[CueStrike Define] Added {Symbol} to {group}");
                }
            }
            else
            {
                if (defineList.Contains(Symbol))
                {
                    defineList.Remove(Symbol);
                    string newDefines = string.Join(";", defineList);
                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
                    Debug.Log($"[CueStrike Define] Removed {Symbol} from {group}");
                }
            }
        }
    }
}
#endif
