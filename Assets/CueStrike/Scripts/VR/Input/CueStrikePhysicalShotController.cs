using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace CueStrike.VR.Input
{
    /// <summary>
    /// Physical shot controller using Hand-as-Cue paradigm.
    /// State machine: Idle → Aiming → Charged → Shooting → Resolving → Idle
    /// Tracks dominant hand controller transform for pull-back/thrust detection.
    /// </summary>
    public class CueStrikePhysicalShotController : MonoBehaviour
    {
        #region Enums
        public enum ShotState
        {
            Idle,
            Aiming,
            Charged,
            Shooting,
            Resolving
        }
        #endregion

        #region Events
        /// <summary>Fired when a physical shot is executed.</summary>
        public event Action<PhysicalShotData> OnShotExecuted;

        /// <summary>Fired when shot state changes.</summary>
        public event Action<ShotState> OnStateChanged;

        /// <summary>Fired during charge for haptic feedback.</summary>
        public event Action<float> OnChargeHapticRequested; // amplitude

        /// <summary>Fired on shot for haptic feedback.</summary>
        public event Action<float> OnShotHapticRequested; // amplitude

        /// <summary>Fired on ball impact for haptic feedback.</summary>
        public event Action<float> OnImpactHapticRequested; // amplitude
        #endregion

        #region Structs
        /// <summary>
        /// Data container for a completed physical shot.
        /// </summary>
        public struct PhysicalShotData
        {
            public Vector3 cueBallPosition;
            public Vector3 direction;
            public float power;         // 0-1 normalized
            public float pullBackDistance;
            public float thrustVelocity;
        }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CueStrikeVRInputMapping inputMapping;

        [Tooltip("The dominant hand interactor (holds the cue). Assigned by VRInputManager.")]
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor dominantHandInteractor;

        [Tooltip("Transform of the dominant hand controller. Assigned by VRInputManager.")]
        [SerializeField] private Transform dominantHandTransform;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = false;
        #endregion

        #region Private State
        private ShotState _currentState = ShotState.Idle;

        // Tracking
        private bool _gripHeld;
        private Vector3 _cueRestPosition;
        private Quaternion _cueRestRotation;
        private float _pullBackDistance;
        private float _thrustVelocity;
        private Vector3 _lastHandPosition;
        private Vector3 _shotDirection;

        // Timing
        private float _chargedStartTime;
        private float _maxChargeTime = 10f; // auto-cancel after 10s
        #endregion

        #region Properties
        public ShotState CurrentState => _currentState;
        public bool IsCharged => _currentState == ShotState.Charged;
        public bool IsAiming => _currentState == ShotState.Aiming;
        public float PullBackDistance => _pullBackDistance;
        public float ThrustVelocity => _thrustVelocity;
        public Vector3 ShotDirection => _shotDirection;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (inputMapping == null)
                inputMapping = Resources.Load<CueStrikeVRInputMapping>("VRInputMapping");
        }

        private void Update()
        {
            if (dominantHandTransform == null) return;
            if (inputMapping == null) return;

            // Read grip state
            bool gripPressed = IsActionPressed(inputMapping.gripAction);

            switch (_currentState)
            {
                case ShotState.Idle:
                    UpdateIdle(gripPressed);
                    break;
                case ShotState.Aiming:
                    UpdateAiming(gripPressed);
                    break;
                case ShotState.Charged:
                    UpdateCharged(gripPressed);
                    break;
                case ShotState.Shooting:
                    // Waiting for resolve callback
                    break;
                case ShotState.Resolving:
                    // Waiting for resolve callback
                    break;
            }

            _lastHandPosition = dominantHandTransform.position;
        }
        #endregion

        #region State Machine
        private void UpdateIdle(bool gripPressed)
        {
            if (gripPressed)
            {
                // Grip pressed — start aiming
                _cueRestPosition = dominantHandTransform.position;
                _cueRestRotation = dominantHandTransform.rotation;
                _pullBackDistance = 0f;
                _thrustVelocity = 0f;
                _shotDirection = dominantHandTransform.forward;

                TransitionTo(ShotState.Aiming);
                if (verboseLogging)
                    Debug.Log($"[PhysicalShot] Grip pressed → Aiming. Rest pos: {_cueRestPosition}");
            }
        }

        private void UpdateAiming(bool gripHeld)
        {
            if (!gripHeld)
            {
                // Released grip before charging — cancel
                TransitionTo(ShotState.Idle);
                if (verboseLogging)
                    Debug.Log("[PhysicalShot] Grip released during Aiming → Idle (cancelled)");
                return;
            }

            // Calculate pull-back distance (how far behind the rest position)
            Vector3 handPos = dominantHandTransform.position;
            Vector3 pullVector = _cueRestPosition - handPos;
            float pullBack = Vector3.Dot(pullVector, -(_cueRestRotation * Vector3.forward));

            if (pullBack < 0f) pullBack = 0f; // Only count backward movement
            _pullBackDistance = pullBack;

            // Shot direction = from current hand pos toward rest position, projected along rest forward
            Vector3 direction = (_cueRestPosition - handPos).normalized;
            if (direction.sqrMagnitude > 0.01f)
                _shotDirection = direction;
            else
                _shotDirection = dominantHandTransform.forward;

            if (_pullBackDistance >= inputMapping.minPullBackDistance)
            {
                // Charged!
                _chargedStartTime = Time.time;
                TransitionTo(ShotState.Charged);
                OnChargeHapticRequested?.Invoke(inputMapping.hapticChargeAmplitude);
                if (verboseLogging)
                    Debug.Log($"[PhysicalShot] Pull-back {_pullBackDistance:F3}m ≥ threshold → Charged");
            }

            if (verboseLogging && Time.frameCount % 30 == 0)
                Debug.Log($"[PhysicalShot] Aiming - pullBack: {_pullBackDistance:F3}m");
        }

        private void UpdateCharged(bool gripHeld)
        {
            if (!gripHeld)
            {
                // Released grip while charged — cancel shot, return to Idle
                _pullBackDistance = 0f;
                TransitionTo(ShotState.Idle);
                if (verboseLogging)
                    Debug.Log("[PhysicalShot] Grip released during Charged → Idle (cancelled)");
                return;
            }

            // Auto-cancel if charged too long
            if (Time.time - _chargedStartTime > _maxChargeTime)
            {
                _pullBackDistance = 0f;
                Debug.LogWarning("[PhysicalShot] Charge timeout — auto-cancelling");
                TransitionTo(ShotState.Idle);
                return;
            }

            // Calculate thrust velocity
            Vector3 handPos = dominantHandTransform.position;
            Vector3 velocity = (handPos - _lastHandPosition) / Time.deltaTime;
            float forwardVelocity = Vector3.Dot(velocity, _shotDirection);

            // Only count forward thrust
            if (forwardVelocity > 0f)
                _thrustVelocity = forwardVelocity;
            else
                _thrustVelocity = 0f;

            // Check if thrust exceeds threshold
            if (_thrustVelocity >= inputMapping.minShotVelocity)
            {
                ExecuteShot();
            }

            if (verboseLogging && Time.frameCount % 15 == 0)
                Debug.Log($"[PhysicalShot] Charged - pull: {_pullBackDistance:F3}m, thrust vel: {_thrustVelocity:F3}m/s");
        }
        #endregion

        #region Shot Execution
        private void ExecuteShot()
        {
            // Calculate power
            float rawPower = _pullBackDistance * _thrustVelocity * inputMapping.powerMultiplier;
            float normalizedPower = Mathf.Clamp01(rawPower / inputMapping.maxShotPower);
            float clampedPower = Mathf.Min(rawPower, inputMapping.maxShotPower);

            TransitionTo(ShotState.Shooting);

            // Build shot data
            var shotData = new PhysicalShotData
            {
                cueBallPosition = FindCueBallPosition(),
                direction = _shotDirection,
                power = normalizedPower,
                pullBackDistance = _pullBackDistance,
                thrustVelocity = _thrustVelocity
            };

            OnShotHapticRequested?.Invoke(inputMapping.hapticShotAmplitude);
            OnShotExecuted?.Invoke(shotData);

            if (verboseLogging)
            {
                Debug.Log($"[PhysicalShot] SHOT EXECUTED | power: {normalizedPower:F2} ({clampedPower:F1}), " +
                          $"pull: {_pullBackDistance:F3}m, thrust: {_thrustVelocity:F3}m/s, dir: {_shotDirection}");
            }

            // Reset tracking data
            _pullBackDistance = 0f;
            _thrustVelocity = 0f;
        }

        /// <summary>
        /// Called externally when the shot resolves (ball stops moving).
        /// Transitions back to Idle.
        /// </summary>
        public void ResolveShot()
        {
            if (_currentState == ShotState.Shooting || _currentState == ShotState.Resolving)
            {
                OnImpactHapticRequested?.Invoke(inputMapping.hapticImpactAmplitude);
                TransitionTo(ShotState.Idle);
                if (verboseLogging)
                    Debug.Log("[PhysicalShot] Shot resolved → Idle");
            }
        }

        /// <summary>
        /// Force reset to Idle state (e.g., on scene load).
        /// </summary>
        public void ForceReset()
        {
            _pullBackDistance = 0f;
            _thrustVelocity = 0f;
            _gripHeld = false;
            TransitionTo(ShotState.Idle);
        }
        #endregion

        #region Helpers
        private void TransitionTo(ShotState newState)
        {
            if (_currentState == newState) return;
            ShotState oldState = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(newState);

            if (verboseLogging)
                Debug.Log($"[PhysicalShot] State: {oldState} → {newState}");
        }

        private Vector3 FindCueBallPosition()
        {
            // Try to find cue ball by tag
            var cueBall = GameObject.FindGameObjectWithTag("CueBall");
            if (cueBall != null)
                return cueBall.transform.position;

            // Fallback: look for CueStrikeBall with ID 0
            var balls = FindObjectsByType<CueStrikeBall>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                if (ball.BallId == 0)
                    return ball.transform.position;
            }

            return Vector3.zero;
        }

        private static bool IsActionPressed(InputActionReference actionRef)
        {
            if (actionRef == null || actionRef.action == null) return false;
            try
            {
                return actionRef.action.IsPressed();
            }
            catch
            {
                return false;
            }
        }

        private static Vector2 ReadActionValue(InputActionReference actionRef)
        {
            if (actionRef == null || actionRef.action == null) return Vector2.zero;
            try
            {
                return actionRef.action.ReadValue<Vector2>();
            }
            catch
            {
                return Vector2.zero;
            }
        }
        #endregion

        #region Public Accessors for VRInputManager
        /// <summary>
        /// Assigns the dominant hand interactor and transform.
        /// Called by VRInputManager on setup.
        /// </summary>
        public void AssignDominantHand(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor interactor, Transform handTransform)
        {
            dominantHandInteractor = interactor;
            dominantHandTransform = handTransform;
        }

        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor GetDominantHandInteractor() => dominantHandInteractor;
        public Transform GetDominantHandTransform() => dominantHandTransform;
        #endregion

        #region Mock Testing
        /// <summary>
        /// Simulate a shot programmatically (for self-test and AI).
        /// </summary>
        public void SimulateShot(float power = 0.5f)
        {
            var shotData = new PhysicalShotData
            {
                cueBallPosition = FindCueBallPosition(),
                direction = Vector3.forward,
                power = Mathf.Clamp01(power),
                pullBackDistance = 0.1f,
                thrustVelocity = 1.0f
            };

            OnShotHapticRequested?.Invoke(inputMapping.hapticShotAmplitude);
            OnShotExecuted?.Invoke(shotData);

            if (verboseLogging)
                Debug.Log($"[PhysicalShot] SIMULATED SHOT | power: {shotData.power:F2}");
        }
        #endregion
    }
}