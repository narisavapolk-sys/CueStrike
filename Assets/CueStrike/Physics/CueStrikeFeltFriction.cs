using UnityEngine;

namespace CueStrike.Physics
{
    /// <summary>
    /// Implements realistic felt friction and spin roll transition (Topspin / Backspin).
    /// Translates linear sliding and angular velocity mismatch into realistic rolling,
    /// enabling screw-backs (draw shots) and runs (follow shots) to behave like a real table.
    /// </summary>
    public class CueStrikeFeltFriction : MonoBehaviour
    {
        [Header("Friction Settings")]
        public float slidingFrictionFactor = 0.15f; // Coefficient of sliding friction on felt
        public float rollingFrictionFactor = 0.008f; // Coefficient of rolling friction on felt

        private Rigidbody _rb;
        private float _ballRadius = 0.02625f; // Standard snooker ball default
        private float _inertia;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                // Solid sphere moment of inertia: I = 2/5 * m * r^2
                _inertia = 0.4f * _rb.mass * _ballRadius * _ballRadius;
            }

            // Read table style radius standard
            bool isSnooker = PlayerPrefs.GetInt("CueStrike_TableStyle", 0) == 0;
            _ballRadius = isSnooker ? 0.02625f : 0.028575f;
        }

        /// <summary>
        /// Sets the friction multiplier from felt skin customization.
        /// </summary>
        public void SetFrictionMultiplier(float multiplier)
        {
            slidingFrictionFactor = Mathf.Max(0.01f, 0.15f * multiplier);
            rollingFrictionFactor = Mathf.Max(0.001f, 0.008f * multiplier);
        }

        /// <summary>
        /// Sets the roll speed multiplier from felt skin customization.
        /// </summary>
        public void SetRollSpeedMultiplier(float multiplier)
        {
            // Roll speed is inversely related to friction - lower friction = faster roll
            // This is handled by the friction multiplier above
        }

        /// <summary>
        /// Automatically equips all active balls in the scene with felt friction.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEquipAllBalls()
        {
            var rbs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var rb in rbs)
            {
                if (rb.gameObject.CompareTag("Ball") || rb.gameObject.name.Contains("Ball"))
                {
                    if (rb.gameObject.GetComponent<CueStrikeFeltFriction>() == null)
                    {
                        rb.gameObject.AddComponent<CueStrikeFeltFriction>();
                        count++;
                    }
                }
            }
            Debug.Log($"[CueStrike Physics] Automatically equipped {count} balls with felt friction & spin physics.");
        }

        private void FixedUpdate()
        {
            if (_rb == null || _rb.isKinematic) return;

            // Check if ball is touching the felt surface (downward raycast)
            if (UnityEngine.Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, _ballRadius + 0.005f))
            {
                string surfaceName = hit.collider.name.ToLower();
                if (surfaceName.Contains("surface") || surfaceName.Contains("felt") || surfaceName.Contains("table"))
                {
                    ResolveFeltFriction();
                }
            }
        }

        /// <summary>
        /// Calculates and applies sliding friction and torque corrections to transition into natural roll.
        /// </summary>
        private void ResolveFeltFriction()
        {
            // Vector pointing from ball center to felt contact point
            Vector3 contactOffset = Vector3.down * _ballRadius;

            // Calculate velocity at the contact point (linear velocity + angular velocity cross contactOffset)
            Vector3 relativeContactVelocity = _rb.linearVelocity + Vector3.Cross(_rb.angularVelocity, contactOffset);

            float slideSpeed = relativeContactVelocity.magnitude;

            if (slideSpeed > 0.01f)
            {
                // Ball is sliding/spinning: apply sliding friction opposite to sliding direction
                Vector3 frictionDir = -relativeContactVelocity.normalized;
                float frictionForce = slidingFrictionFactor * _rb.mass * 9.81f; // F = mu * m * g
                Vector3 frictionImpulse = frictionDir * frictionForce * Time.fixedDeltaTime;

                // Apply linear velocity change
                _rb.linearVelocity += frictionImpulse / _rb.mass;

                // Apply torque due to friction at the contact point (T = contactOffset x frictionForce)
                Vector3 frictionTorque = Vector3.Cross(contactOffset, frictionImpulse);
                _rb.angularVelocity += frictionTorque / _inertia;
            }
            else
            {
                // Natural Roll: apply standard rolling resistance
                if (_rb.linearVelocity.magnitude > 0.01f)
                {
                    float rollFriction = rollingFrictionFactor * _rb.mass * 9.81f;
                    Vector3 resistanceForce = -_rb.linearVelocity.normalized * rollFriction * Time.fixedDeltaTime;
                    _rb.linearVelocity += resistanceForce / _rb.mass;

                    // Match rotation to rolling speed exactly
                    Vector3 idealAngularVel = Vector3.Cross(_rb.linearVelocity, Vector3.up) / _ballRadius;
                    _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, idealAngularVel, Time.fixedDeltaTime * 15f);
                }
                else
                {
                    // Stop completely to prevent micro drifting
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}
