using UnityEngine;

namespace CueStrike.Physics
{
    /// <summary>
    /// Manages table pass-through (Sim Mode toggle).
    /// If Sim Mode is OFF, outer table colliders (rails, frame, legs) are set to triggers
    /// so the player can walk through them like in Miracle Pool, while the play felt surface
    /// remains solid so billiard balls never fall through the table.
    /// </summary>
    public class CueStrikeTablePassThrough : MonoBehaviour
    {
        public static CueStrikeTablePassThrough Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ApplyColliderMode();
        }

        /// <summary>
        /// Reads Sim Mode state from PlayerPrefs and toggles table outer colliders.
        /// </summary>
        public void ApplyColliderMode()
        {
            bool isSimMode = PlayerPrefs.GetInt("CueStrike_SimMode", 1) == 1;
            Debug.Log($"[CueStrike Physics] Applying Sim Mode Table Collisions: {isSimMode}");

            // Find all colliders in the scene
            var allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (var col in allColliders)
            {
                string name = col.gameObject.name.ToLower();

                // Toggle frame, rails, and cushion borders, but NEVER the play surface felt (TableSurface)
                if (name.Contains("rail") || name.Contains("cushion") || name.Contains("leg") || 
                    name.Contains("pocket") || name.Contains("tableframe") || name.Contains("wood"))
                {
                    // If Sim Mode is ON: colliders are solid (isTrigger = false)
                    // If Sim Mode is OFF: colliders are triggers (isTrigger = true) so player can walk through
                    col.isTrigger = !isSimMode;
                }
            }
        }
    }
}
