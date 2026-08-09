using UnityEngine;

namespace CueStrike.Environment.Lighting
{
    /// <summary>
    /// ScriptableObject that defines lighting settings for a single room.
    /// Create assets via Assets → Create → CueStrike → Room Lighting Profile.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomLightingProfile", menuName = "CueStrike/Room Lighting Profile", order = 10)]
    public class RoomLightingProfile : ScriptableObject
    {
        [Header("Room Identification")]
        [Tooltip("Friendly name for the room (e.g., \"Room1\").")]
        public string roomName = "Room";

        [Header("Ambient Settings")]
        public Color ambientColor = Color.white;
        [Range(0f, 8f)]
        public float ambientIntensity = 1f;

        [Header("Directional Light")]
        public Light directionalLight;

        [Header("Additional Lights")]
        public Light[] extraLights;
    }
}