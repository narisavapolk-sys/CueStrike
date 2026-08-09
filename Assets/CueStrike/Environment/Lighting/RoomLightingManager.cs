using UnityEngine;
using System.Collections.Generic;

namespace CueStrike.Environment.Lighting
{
    /// <summary>
    /// Manages lighting profiles for up to 8 rooms.
    /// Attach this component to a persistent GameObject (e.g., the Environment manager).
    /// Create 8 RoomLightingProfile assets and assign them in the inspector.
    /// Use SetRoom(string) or SetRoom(int) to switch lighting for a specific room.
    /// </summary>
    public class RoomLightingManager : MonoBehaviour
    {
        public static RoomLightingManager Instance { get; private set; }

        [Tooltip("Assign 8 lighting profiles – one per room.")]
        public RoomLightingProfile[] roomProfiles = new RoomLightingProfile[8];

        private RoomLightingProfile _currentProfile;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Initialise to first profile if any are set
            if (roomProfiles != null && roomProfiles.Length > 0 && roomProfiles[0] != null)
            {
                SetRoom(0);
            }
        }

        /// <summary>
        /// Switch lighting to the room at the given index (0‑7).
        /// </summary>
        public void SetRoom(int index)
        {
            if (roomProfiles == null || index < 0 || index >= roomProfiles.Length)
            {
                Debug.LogWarning($"[RoomLightingManager] Invalid room index {index}");
                return;
            }

            ApplyProfile(roomProfiles[index]);
        }

        /// <summary>
        /// Switch lighting to the room with the matching name.
        /// </summary>
        public void SetRoom(string roomName)
        {
            if (roomProfiles == null) return;

            foreach (var profile in roomProfiles)
            {
                if (profile != null && profile.roomName == roomName)
                {
                    ApplyProfile(profile);
                    return;
                }
            }

            Debug.LogWarning($"[RoomLightingManager] No lighting profile found for room \"{roomName}\"");
        }

        private void ApplyProfile(RoomLightingProfile profile)
        {
            if (profile == null) return;

            // Apply ambient settings
            RenderSettings.ambientLight = profile.ambientColor;
            RenderSettings.ambientIntensity = profile.ambientIntensity;

            // Disable lights from the previous profile
            if (_currentProfile != null)
            {
                if (_currentProfile.directionalLight != null)
                    _currentProfile.directionalLight.enabled = false;

                if (_currentProfile.extraLights != null)
                {
                    foreach (var l in _currentProfile.extraLights)
                        if (l != null) l.enabled = false;
                }
            }

            // Enable lights from the new profile
            if (profile.directionalLight != null)
                profile.directionalLight.enabled = true;

            if (profile.extraLights != null)
            {
                foreach (var l in profile.extraLights)
                    if (l != null) l.enabled = true;
            }

            _currentProfile = profile;
        }
    }
}