using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CueStrike.VR.Input
{
    /// <summary>
    /// Controls camera orbit modes:
    /// - Aim Orbit: Hold off-hand grip + thumbstick X → orbit around cue ball (for aiming)
    /// - Table Orbit: Thumbstick X only (no off-hand grip) → orbit around table center
    /// Uses SmoothDamp for smooth rotation transitions.
    /// </summary>
    public class CueStrikeAimOrbitController : MonoBehaviour
    {
        #region Events
        /// <summary>Fired when aim orbit angle changes (degrees around cue ball).</summary>
        public event Action<float> OnAimAngleChanged;

        /// <summary>Fired when table orbit angle changes (degrees around table).</summary>
        public event Action<float> OnTableOrbitAngleChanged;
        #endregion

        #region Enums
        public enum OrbitMode
        {
            None,
            AimOrbit,    // Around cue ball
            TableOrbit   // Around table center
        }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CueStrikeVRInputMapping inputMapping;

        [Tooltip("XR Origin transform. Assigned by VRInputManager.")]
        [SerializeField] private Transform xrOriginTransform;

        [Tooltip("Camera transform (eye level). Assigned by VRInputManager.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Settings")]
        [SerializeField] private float orbitSmoothTime = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = false;
        #endregion

        #region Private State
        private OrbitMode _currentMode = OrbitMode.None;
        private float _currentAimAngle;
        private float _currentTableAngle;
        private float _aimVelocity;
        private float _tableVelocity;
        private bool _wasOffHandGripHeld;
        #endregion

        #region Properties
        public OrbitMode CurrentOrbitMode => _currentMode;
        public float CurrentAimAngle => _currentAimAngle;
        public float CurrentTableAngle => _currentTableAngle;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (inputMapping == null)
                inputMapping = Resources.Load<CueStrikeVRInputMapping>("VRInputMapping");
        }

        private void Update()
        {
            if (inputMapping == null || xrOriginTransform == null) return;

            // 1. Determine orbit mode
            bool offHandGripHeld = IsActionPressed(inputMapping.offHandGripAction);
            Vector2 stickValue = ReadStickValue(inputMapping.orbitStickAction);

            // Mode transition
            if (offHandGripHeld && Mathf.Abs(stickValue.x) > 0.1f)
            {
                if (_currentMode != OrbitMode.AimOrbit)
                {
                    _currentMode = OrbitMode.AimOrbit;
                    if (verboseLogging) Debug.Log("[AimOrbit] Mode: AimOrbit (grip held + thumbstick)");
                }
            }
            else if (!offHandGripHeld && Mathf.Abs(stickValue.x) > 0.1f)
            {
                if (_currentMode != OrbitMode.TableOrbit)
                {
                    _currentMode = OrbitMode.TableOrbit;
                    if (verboseLogging) Debug.Log("[AimOrbit] Mode: TableOrbit (thumbstick only)");
                }
            }
            else
            {
                if (_currentMode != OrbitMode.None)
                {
                    _currentMode = OrbitMode.None;
                    if (verboseLogging) Debug.Log("[AimOrbit] Mode: None");
                }
            }

            // 2. Apply orbit rotation
            switch (_currentMode)
            {
                case OrbitMode.AimOrbit:
                    UpdateAimOrbit(stickValue.x);
                    break;
                case OrbitMode.TableOrbit:
                    UpdateTableOrbit(stickValue.x);
                    break;
                case OrbitMode.None:
                    // No rotation applied
                    break;
            }

            _wasOffHandGripHeld = offHandGripHeld;
        }
        #endregion

        #region Orbit Logic
        private void UpdateAimOrbit(float stickX)
        {
            if (Mathf.Abs(stickX) < 0.05f) return;

            // Find cue ball position
            Vector3 cueBallPos = FindCueBallPosition();
            if (cueBallPos == Vector3.zero) return;

            // Calculate rotation around cue ball
            float targetAngle = _currentAimAngle + stickX * inputMapping.aimOrbitSpeed * Time.deltaTime;
            _currentAimAngle = Mathf.SmoothDamp(_currentAimAngle, targetAngle, ref _aimVelocity, orbitSmoothTime);

            // Apply rotation to XR Origin around cue ball
            ApplyOrbitRotation(cueBallPos, _currentAimAngle);

            OnAimAngleChanged?.Invoke(_currentAimAngle);

            if (verboseLogging && Time.frameCount % 30 == 0)
                Debug.Log($"[AimOrbit] Aim angle: {_currentAimAngle:F1}°");
        }

        private void UpdateTableOrbit(float stickX)
        {
            if (Mathf.Abs(stickX) < 0.05f) return;

            // Find table center
            Vector3 tableCenter = FindTableCenter();
            if (tableCenter == Vector3.zero) return;

            // Calculate rotation around table
            float targetAngle = _currentTableAngle + stickX * inputMapping.tableOrbitSpeed * Time.deltaTime;
            _currentTableAngle = Mathf.SmoothDamp(_currentTableAngle, targetAngle, ref _tableVelocity, orbitSmoothTime);

            // Apply rotation to XR Origin around table center
            ApplyOrbitRotation(tableCenter, _currentTableAngle);

            OnTableOrbitAngleChanged?.Invoke(_currentTableAngle);

            if (verboseLogging && Time.frameCount % 30 == 0)
                Debug.Log($"[AimOrbit] Table angle: {_currentTableAngle:F1}°");
        }

        private void ApplyOrbitRotation(Vector3 pivot, float angleDegrees)
        {
            if (cameraTransform == null || xrOriginTransform == null) return;

            // Calculate current offset from pivot to camera
            Vector3 offset = cameraTransform.position - pivot;
            float distance = offset.magnitude;
            if (distance < 0.01f) return;

            // Get current horizontal direction
            Vector3 horizontalDir = new Vector3(offset.x, 0f, offset.z).normalized;

            // Rotate around Y axis
            Quaternion rotation = Quaternion.AngleAxis(angleDegrees, Vector3.up);
            Vector3 newOffset = rotation * (horizontalDir * distance);

            // Keep original height
            newOffset.y = offset.y;

            // Move XR Origin to maintain camera position at pivot + offset
            // Instead of moving XR Origin directly, we rotate it
            xrOriginTransform.RotateAround(pivot, Vector3.up, angleDegrees - GetCurrentAngleAround(pivot));
        }

        private float GetCurrentAngleAround(Vector3 pivot)
        {
            if (cameraTransform == null) return 0f;
            Vector3 offset = cameraTransform.position - pivot;
            return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }
        #endregion

        #region Helpers
        private Vector3 FindCueBallPosition()
        {
            var cueBall = GameObject.FindGameObjectWithTag("CueBall");
            if (cueBall != null)
                return cueBall.transform.position;

            var balls = FindObjectsByType<CueStrikeBall>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                if (ball.BallId == 0)
                    return ball.transform.position;
            }

            return Vector3.zero;
        }

        private Vector3 FindTableCenter()
        {
            // Try to find table object
            var table = GameObject.FindGameObjectWithTag("Table");
            if (table != null)
                return table.transform.position;

            // Fallback: find all balls, average their positions as table center estimate
            var balls = FindObjectsByType<CueStrikeBall>(FindObjectsSortMode.None);
            if (balls.Length > 0)
            {
                Vector3 sum = Vector3.zero;
                foreach (var b in balls)
                    sum += b.transform.position;
                return sum / balls.Length;
            }

            // Last resort: use XR Origin position
            if (xrOriginTransform != null)
                return xrOriginTransform.position + xrOriginTransform.forward * 2f;

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

        private static Vector2 ReadStickValue(InputActionReference actionRef)
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

        #region Public Setup
        /// <summary>
        /// Assign XR Origin and camera transform. Called by VRInputManager.
        /// </summary>
        public void AssignTransforms(Transform origin, Transform cam)
        {
            xrOriginTransform = origin;
            cameraTransform = cam;
        }
        #endregion
    }
}