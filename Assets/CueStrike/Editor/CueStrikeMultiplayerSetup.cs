#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

#if CUESTRIKE_NORMCORE
using Normal.Realtime;
#endif

/// <summary>
/// Editor utility to set up the required Normcore multiplayer components in the scene.
/// Menu: CueStrike → Generate → Set Up Multiplayer in MainMenu
/// </summary>
public static class CueStrikeMultiplayerSetup
{
    private const string ScenePath = "Assets/CueStrike/Scenes/MainMenu.unity";

    [MenuItem("CueStrike/Generate/Set Up Multiplayer in MainMenu")]
    public static void SetupMultiplayer()
    {
        // Guard: Cannot run in Play Mode
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Multiplayer Setup] Cannot setup while in Play Mode. Please exit Play Mode first.");
            EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
            return;
        }

        // 1. Open the MainMenu scene
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Multiplayer Setup] Could not open scene: {ScenePath}");
            return;
        }

        // 2. Find or create the CueStrikeMultiplayer GameObject
        var mpGO = GameObject.Find("CueStrikeMultiplayer");
        if (mpGO == null)
        {
            mpGO = new GameObject("CueStrikeMultiplayer");
            Debug.Log("[Multiplayer Setup] Created new 'CueStrikeMultiplayer' GameObject.");
        }

        // 3. Add & configure components
#if CUESTRIKE_NORMCORE
        // Add Realtime component (Core Normcore connection)
        var realtime = mpGO.GetComponent<Realtime>();
        if (realtime == null)
        {
            realtime = mpGO.AddComponent<Realtime>();
            Debug.Log("[Multiplayer Setup] Added 'Realtime' component.");
        }

        // Add CueStrikeNormcoreManager (matchmaking controller)
        var manager = mpGO.GetComponent<CueStrike.Multiplayer.CueStrikeNormcoreManager>();
        if (manager == null)
        {
            manager = mpGO.AddComponent<CueStrike.Multiplayer.CueStrikeNormcoreManager>();
            manager.defaultRoomName = "CueStrike_Lobby";
            Debug.Log("[Multiplayer Setup] Added 'CueStrikeNormcoreManager' component.");
        }

        // Add RealtimeAvatarManager (handles VR Head/Hands sync)
        var avatarManager = mpGO.GetComponent<RealtimeAvatarManager>();
        if (avatarManager == null)
        {
            avatarManager = mpGO.AddComponent<RealtimeAvatarManager>();
            
            // Try to find a player avatar prefab in the project
            string[] guids = AssetDatabase.FindAssets("PlayerAvatar t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (avatarPrefab != null)
                {
                    avatarManager.localAvatarPrefab = avatarPrefab;
                    Debug.Log($"[Multiplayer Setup] Auto-assigned local avatar prefab: {path}");
                }
            }
            Debug.Log("[Multiplayer Setup] Added 'RealtimeAvatarManager' component.");
        }

        // Add CueStrikeVoiceManager for in-game VR voice chat
        var voiceManager = mpGO.GetComponent<CueStrike.Multiplayer.CueStrikeVoiceManager>();
        if (voiceManager == null)
        {
            voiceManager = mpGO.AddComponent<CueStrike.Multiplayer.CueStrikeVoiceManager>();
            Debug.Log("[Multiplayer Setup] Added 'CueStrikeVoiceManager' component.");
        }

        // Add CueStrikeGameSync to sync turn order and scores
        var gameSync = mpGO.GetComponent<CueStrike.Multiplayer.CueStrikeGameSync>();
        if (gameSync == null)
        {
            gameSync = mpGO.AddComponent<CueStrike.Multiplayer.CueStrikeGameSync>();
            Debug.Log("[Multiplayer Setup] Added 'CueStrikeGameSync' component.");
        }
#else
        // Fallback info component when Normcore package is not compiled yet
        var fallback = mpGO.GetComponent<CueStrikeFallbackInfo>();
        if (fallback == null)
        {
            fallback = mpGO.AddComponent<CueStrikeFallbackInfo>();
            fallback.message = "Normcore SDK is not present or CUESTRIKE_NORMCORE is not defined.\n" +
                               "Please import Normcore, and CUESTRIKE_NORMCORE will be auto-defined, " +
                               "then run this setup again to wire the network components.";
        }
#endif

        // 4. Save and dirty check
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Multiplayer Configured",
            "Multiplayer synchronization components successfully set up in MainMenu.unity!\n\n" +
            "IMPORTANT: Please paste your App Key into the Realtime component in the Inspector:\n" +
            "d614c57d-03b2-4eeb-a309-8ed6b2e4b806\n\n" +
            "Components Added:\n" +
            "  • Realtime (Normcore Core)\n" +
            "  • CueStrikeNormcoreManager (Matchmaker)\n" +
            "  • RealtimeAvatarManager (VR Tracking Sync)\n" +
            "  • CueStrikeVoiceManager (Voice Chat Controls)\n" +
            "  • CueStrikeGameSync (Turns and Score Sync)",
            "OK");
    }
}

#if !CUESTRIKE_NORMCORE
public class CueStrikeFallbackInfo : MonoBehaviour
{
    [TextArea(4, 10)]
    public string message;
}
#endif

#endif
