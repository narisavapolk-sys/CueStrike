using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// Implements Inverse Kinematics (IK) posture assistance for the player avatar.
    /// This script automatically adjusts the avatar's spine to a "pro stance" when aiming.
    /// </summary>
    public class CueStrikeIKAssist : MonoBehaviour
    {
        [Header("IK Settings")]
        [Tooltip("Reference to the player avatar's spine bone.")]
        public Transform spineBone;
        [Tooltip("The target angle for the spine bend in degrees (e.g., 45 for bending down).")]
        [Range(0f, 90f)]
        public float targetSpineBendAngle = 45f;
        [Tooltip("Speed at which the spine interpolates to the target angle.")]
        public float interpolationSpeed = 5f;
        [Tooltip("Maximum distance between CueTip and CueBall to trigger IK assist.")]
        public float cueingDistanceThreshold = 0.5f;

        [Header("Dependencies")]
        [Tooltip("Reference to the CueTip Transform.")]
        public Transform cueTip;
        [Tooltip("Reference to the CueBall Transform.")]
        public Transform cueBall;
        [Tooltip("Optional: Reference to the RigBuilder component for Animation Rigging.")]
        public RigBuilder rigBuilder;

        private Quaternion _initialSpineRotation;
        private bool _isIKActive = false;

        void Awake()
        {
            if (spineBone == null)
            {
                Debug.LogError("[CueStrikeIKAssist] Spine bone not assigned!", this);
                enabled = false;
                return;
            }
            _initialSpineRotation = spineBone.localRotation;

            if (rigBuilder == null)
            {
                rigBuilder = GetComponentInParent<RigBuilder>();
                if (rigBuilder == null)
                {
                    Debug.LogWarning("[CueStrikeIKAssist] RigBuilder not found. Animation Rigging IK might not function correctly.", this);
                }
            }
        }

        void Update()
        {
            if (cueTip == null || cueBall == null)
            {
                // Debug.LogWarning("[CueStrikeIKAssist] CueTip or CueBall not assigned. IK assist disabled.");
                SetIKState(false);
                return;
            }

            float distance = Vector3.Distance(cueTip.position, cueBall.position);
            bool shouldBeIKActive = distance < cueingDistanceThreshold;

            SetIKState(shouldBeIKActive);

            if (_isIKActive)
            {
                ApplyIKAssist();
            }
            else
            {
                ResetSpineRotation();
            }
        }

        private void SetIKState(bool active)
        {
            if (_isIKActive != active)
            {
                _isIKActive = active;
                // If using RigBuilder, enable/disable the appropriate Rig here
                if (rigBuilder != null)
                {
                    // Assuming you have a specific Rig for posture assist
                    // Example: rigBuilder.GetComponent<YourPostureRigScript>().enabled = active;
                    // For now, we'll just handle spine rotation directly.
                }
            }
        }

        private void ApplyIKAssist()
        {
            if (spineBone == null) return;

            // Calculate the target rotation for the spine
            // Assuming spine bends forward around its local X-axis
            Quaternion targetRotation = _initialSpineRotation * Quaternion.Euler(targetSpineBendAngle, 0, 0);

            // Smoothly interpolate to the target rotation
            spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, targetRotation, Time.deltaTime * interpolationSpeed);
        }

        private void ResetSpineRotation()
        {
            if (spineBone == null) return;

            // Smoothly interpolate back to the initial rotation
            spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, _initialSpineRotation, Time.deltaTime * interpolationSpeed);
        }

        // You might want to add methods here to handle "Sitting Mode"/Disabled accessibility options
        // e.g., public void EnableIK(bool enable) { _isIKActive = enable; }
    }
}