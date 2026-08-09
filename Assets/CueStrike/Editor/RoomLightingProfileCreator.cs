using UnityEditor;
using UnityEngine;
using CueStrike.Environment.Lighting;

namespace CueStrike.Editor
{
    public static class RoomLightingProfileCreator
    {
        private const string ProfileFolder = "Assets/CueStrike/Environment/Lighting/Profiles";

        [MenuItem("Tools/CueStrike/Create Lighting Profiles")]
        public static void CreateProfiles()
        {
            // Ensure the folder exists
            if (!AssetDatabase.IsValidFolder(ProfileFolder))
            {
                AssetDatabase.CreateFolder("Assets/CueStrike/Environment/Lighting", "Profiles");
            }

            for (int i = 1; i <= 8; i++)
            {
                string assetPath = $"{ProfileFolder}/Room{i}Profile.asset";
                if (AssetDatabase.LoadAssetAtPath<RoomLightingProfile>(assetPath) != null)
                {
                    Debug.Log($"[RoomLightingProfileCreator] Profile already exists: {assetPath}");
                    continue;
                }

                RoomLightingProfile profile = ScriptableObject.CreateInstance<RoomLightingProfile>();
                profile.roomName = $"Room{i}";
                profile.ambientColor = Color.white;
                profile.ambientIntensity = 1f;
                // directionalLight and extraLights can be assigned manually in the inspector

                AssetDatabase.CreateAsset(profile, assetPath);
                Debug.Log($"[RoomLightingProfileCreator] Created {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}