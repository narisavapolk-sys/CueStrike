//
// CueStrikeStrikeRealism — Translates cue-strike input (trigger force, stance offset, aim) into realistic cue-ball physics
// Created by Nari for P'Mong | 2026-07-19
// Phase 1: Physics Realism — enhances existing CueStrikeBallPhysics without overlapping
//
using UnityEngine;

namespace CueStrike.Physics
{
    /// <summary>
    /// Sits between the VR cue controller and the Rigidbody of the cue ball.
    /// Computes velocity + spin from physical strike parameters and applies them
    /// via a single impulse, leaving trajectory/rolling to existing physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CueStrikeStrikeRealism : MonoBehaviour
    {
        // ---------------- PHYSICAL CONSTANTS (WPBSA-spec) ----------------
        private const float kBallMassKg = 0.170f;      // standard snooker ball
        private const float kBallRadiusM = 0.02625f;
        private const float kG = 9.81f;

        // ---------------- TUNABLE ----------------
        [Header("Strike Mapping")]
        [Tooltip("Max trigger pull (0..1) → strike speed in m/s.")]
        public float maxStrikeSpeed = 9.0f;
        [Tooltip("Speed multiplier when full power shot is active.")]
        public float powerShotBoost = 1.35f;

        [Header("Spin / English")]
        [Range(0f, 4f)] public float spinStrength = 1.4f;
        [Tooltip("Top spin intensity curve (follow).")]
        [Range(0f, 3f)] public float topSpinFactor = 1.6f;
        [Range(0f, 3f)] public float backSpinFactor = 1.6f;
        [Range(0f, 3f)] public float sideSpinFactor = 1.2f;

        [Header("Realism")]
        [Tooltip("Slight random aim scatter when strike speed high (kick).")]
        [Range(0f, 0.05f)] public float kickScatter = 0.012f;
        [Tooltip("Mis-cue probability at extreme spin + low speed.")]
        [Range(0f, 0.3f)] public float miscueProbability = 0.08f;

        // ---------------- STATE ----------------
        private Rigidbody _rb;
        private bool _powerShotActive = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.mass = kBallMassKg;
                _rb.useGravity = true;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        // ---------------- PUBLIC API ----------------

        /// <summary>
        /// Apply a realistic strike. Aim direction (normalized, world-space).
        /// strikePoint = contact point on cue ball (-1..1 offset from center along strike plane).
        /// strikePoint.x = side (English), y = top (-)/back (+).
        /// triggerPull 0..1.
        /// </summary>
        public void Strike(Vector3 aimDir, Vector2 strikePoint, float triggerPull)
        {
            if (_rb == null) return;

            triggerPull = Mathf.Clamp01(triggerPull);
            float speed = triggerPull * maxStrikeSpeed * (_powerShotActive ? powerShotBoost : 1f);

            // Mis-cue: random contact offset when spin extreme + low speed
            bool miscue = false;
            float offsetMag = strikePoint.magnitude;
            if (offsetMag > 0.85f && speed < 2.5f && Random.value < miscueProbability)
            {
                miscue = true;
                speed *= 0.3f;
                aimDir = Quaternion.Euler(
                    Random.Range(-20f, 20f),
                    Random.Range(-20f, 20f),
                    0f) * aimDir;
            }

            // Linear velocity
            Vector3 v = aimDir.normalized * speed;

            // Slight kick scatter (kick sound realism)
            if (speed > 6f && kickScatter > 0f)
                v += Random.insideUnitSphere * kickScatter * speed;

            _rb.linearVelocity = v;   // impulse immediately in unity 6 using linearVelocity

            // Angular velocity (spin) from contact point
            // right = perpendicular to aim in horizontal plane
            Vector3 right = Vector3.Cross(Vector3.up, aimDir).normalized;
            Vector3 up = Vector3.up;

            // side spin (English) -> rotation around Y
            float sideSpin = strikePoint.x * sideSpinFactor * spinStrength * (speed / maxStrikeSpeed);
            Vector3 angVel = up * sideSpin;

            // top spin (follow) -> rotation around right (positive = forward rolling faster)
            float topSpin = -strikePoint.y * topSpinFactor * spinStrength * (speed / maxStrikeSpeed);
            // back spin (draw) -> -strikePoint.y > 0 => negative topSpin slows down / pulls back
            angVel += right * topSpin;

            _rb.angularVelocity = angVel;

            // Store power shot flag to clear
            _powerShotActive = false;

            if (miscue) UnityEngine.Debug.Log("[CueStrike] MIS-CUE! Warning");
        }

        /// <summary>Flag the next strike as a power shot (boosted).</summary>
        public void ArmPowerShot() { _powerShotActive = true; }

        /// <summary>Predict stop distance using friction (m) — for AI / aim assist.</summary>
        public float PredictStopDistance(float currentSpeed, float frictionCoef = 0.18f)
        {
            // v² = u² - 2·μ·g·s  =>  s = u² / (2 μ g)
            return (currentSpeed * currentSpeed) / (2f * frictionCoef * kG);
        }
    }
}