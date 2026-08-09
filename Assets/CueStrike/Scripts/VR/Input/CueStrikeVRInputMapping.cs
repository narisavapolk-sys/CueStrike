using UnityEngine;
using UnityEngine.InputSystem;

namespace CueStrike.VR.Input
{
    /// <summary>
    /// Input Mapping Data for CueStrike VR Physical Input System.
    /// Maps physical VR interactions (grip, buttons, thumbsticks) to game actions.
    /// Uses XR Interaction Toolkit InputActionReference for cross-platform compatibility.
    /// Create via Assets/Create/CueStrike/VR Input Mapping
    /// </summary>
    [CreateAssetMenu(fileName = "VRInputMapping", menuName = "CueStrike/VR Input Mapping")]
    public class CueStrikeVRInputMapping : ScriptableObject
    {
        [Header("Dominant Hand (Cue Hand) — Right by default")]
        [Tooltip("Grip button: Hold to hold the cue. Should use XR Controller Select Action.")]
        public InputActionReference gripAction;

        [Tooltip("Primary button (X on left, A on right): Toggle Options UI.")]
        public InputActionReference optionsButtonAction;

        [Tooltip("Secondary button (Y on left, B on right): Undo last shot.")]
        public InputActionReference undoButtonAction;

        [Header("Non-Dominant Hand (Off Hand)")]
        [Tooltip("Grip on off-hand: Hold + Thumbstick to aim-orbit around cue ball.")]
        public InputActionReference offHandGripAction;

        [Tooltip("Thumbstick click (L3/R3): Toggle stance Standing ↔ Crouching.")]
        public InputActionReference stanceToggleAction;

        [Header("Thumbstick")]
        [Tooltip("Primary thumbstick 2D axis: Orbit/rotate camera around table.")]
        public InputActionReference orbitStickAction;

        [Tooltip("Secondary thumbstick Y-axis: Adjust crouch distance from cue ball.")]
        public InputActionReference stanceDistanceStickAction;

        [Header("Physics Thresholds")]
        [Tooltip("Minimum pull-back distance (meters) to register a charge.")]
        [Range(0.01f, 0.3f)]
        public float minPullBackDistance = 0.05f;

        [Tooltip("Minimum forward thrust velocity (m/s) to register a shot.")]
        [Range(0.1f, 1.0f)]
        public float minShotVelocity = 0.3f;

        [Tooltip("Maximum shot power (clamp).")]
        [Range(1f, 20f)]
        public float maxShotPower = 10f;

        [Tooltip("Power multiplier: pullBack * thrustVelocity * this.")]
        [Range(0.1f, 5f)]
        public float powerMultiplier = 1.5f;

        [Tooltip("Haptic amplitude during charge.")]
        [Range(0f, 1f)]
        public float hapticChargeAmplitude = 0.3f;

        [Tooltip("Haptic amplitude on shot execute.")]
        [Range(0f, 1f)]
        public float hapticShotAmplitude = 0.7f;

        [Tooltip("Haptic amplitude on ball impact.")]
        [Range(0f, 1f)]
        public float hapticImpactAmplitude = 0.5f;

        [Header("Stance")]
        [Tooltip("Minimum crouch distance from cue ball (meters).")]
        [Range(0.3f, 0.8f)]
        public float crouchDistanceMin = 0.3f;

        [Tooltip("Maximum crouch distance from cue ball (meters).")]
        [Range(0.8f, 2.0f)]
        public float crouchDistanceMax = 1.5f;

        [Tooltip("Default crouch distance (meters).")]
        [Range(0.3f, 1.5f)]
        public float crouchDistanceDefault = 0.8f;

        [Header("Aim Orbit")]
        [Tooltip("Smooth rotation speed for aim orbit (degrees per thumbstick unit).")]
        [Range(10f, 180f)]
        public float aimOrbitSpeed = 90f;

        [Tooltip("Smooth rotation speed for table orbit (degrees per thumbstick unit).")]
        [Range(10f, 180f)]
        public float tableOrbitSpeed = 60f;
    }
}