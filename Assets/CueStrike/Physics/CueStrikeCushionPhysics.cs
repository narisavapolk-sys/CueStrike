using UnityEngine;

namespace CueStrike.Physics
{
    /// <summary>
    /// Implements realistic cushion sidespin deflection (Spin-to-Cushion Friction Transfer)
    /// for billiard/snooker balls.
    /// Attaches to Cushion colliders and applies lateral deflection forces dynamically
    /// based on ball spin. Automatically initializes and binds itself to all cushion colliders in the scene.
    /// </summary>
    public class CueStrikeCushionPhysics : MonoBehaviour
    {
        [Header("Physics Settings")]
        [Tooltip("How strongly sidespin (English) deflects the ball off the cushion.")]
        public float spinDeflectionFactor = 0.22f;

        [Tooltip("How much of the ball's spin is lost/transferred to the cushion during impact.")]
        [Range(0f, 1f)]
        public float spinLossFactor = 0.35f;

        /// <summary>
        /// Automatically finds and equips all cushion colliders in the active scene.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEquipAllCushions()
        {
            var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var col in colliders)
            {
                string nameLower = col.gameObject.name.ToLower();
                if (nameLower.Contains("cushion") || nameLower.Contains("rail") || col.gameObject.CompareTag("Cushion"))
                {
                    if (col.gameObject.GetComponent<CueStrikeCushionPhysics>() == null)
                    {
                        col.gameObject.AddComponent<CueStrikeCushionPhysics>();
                        count++;
                    }
                }
            }
            Debug.Log($"[CueStrike Physics] Automatically equipped {count} cushions with spin-deflection physics.");
        }

        private void OnCollisionEnter(Collision collision)
        {
            var rb = collision.rigidbody;
            if (rb == null) return;

            // Confirm it is a billiard ball
            if (collision.collider.CompareTag("Ball") || collision.collider.name.Contains("Ball"))
            {
                ApplySidespinDeflection(rb, collision);
            }
        }

        private void ApplySidespinDeflection(Rigidbody rb, Collision collision)
        {
            if (collision.contactCount == 0) return;

            Vector3 contactNormal = collision.contacts[0].normal;
            
            // Calculate tangent vector along the face of the cushion
            Vector3 tangent = Vector3.Cross(contactNormal, Vector3.up).normalized;

            // Get sidespin (rotation around Y-axis)
            float sidespin = rb.angularVelocity.y;

            if (Mathf.Abs(sidespin) > 0.05f)
            {
                // Apply a lateral velocity change along the tangent based on spin strength and direction
                Vector3 deflectionImpulse = tangent * sidespin * spinDeflectionFactor;
                rb.AddForce(deflectionImpulse, ForceMode.VelocityChange);

                // Transfer spin energy: reduce the Y angular velocity
                float newSpin = sidespin * (1f - spinLossFactor);
                rb.angularVelocity = new Vector3(rb.angularVelocity.x, newSpin, rb.angularVelocity.z);

                Debug.Log($"[CueStrike Physics] Applied sidespin deflection: Impulse={deflectionImpulse.magnitude:F3}, Remaining Spin={newSpin:F2}");
            }
        }
    }
}
