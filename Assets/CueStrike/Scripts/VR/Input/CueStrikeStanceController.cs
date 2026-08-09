using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CueStrike.VR.Input
{
    /// <summary>
    /// Manages VR stance: Standing ↔ Crouching.
    /// - Thumbstick click (L3/R3) toggles between modes.
    /// - While crouching, thumbstick Y-axis adjusts distance from cue ball.
    /// - Camera height lerps smoothly based on stance.
    /// </summary>
    public class CueStrikeStanceController : MonoBehaviour
    {
        #region Enums
        public enum StanceType
        {
            Standing,
            Crouching
        }
        #endregion

        #region Events
        /// <summary>Fired when stance changes.</summary>
        public event Action<StanceType> OnStanceChanged;

        /// <summary>Fired when crouch distance changes.</summary>
        public event Action<float> OnStanceDistanceChanged;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CueStrikeVRInputMapping inputMapping;

        [Tooltip("XR Origin / Camera rig. Assigned by VRInputManager.")]
        [SerializeField] private Transform xrOriginTransform;

        [Header("Settings")]
        [SerializeField] private float standingHeight = 1.7f;
        [SerializeField] private float crouchHeight = 0.8f;
        [SerializeField] private float heightLerpSpeed = 8f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = false;
        #endregion

        #region Private State
        private StanceType _currentStance = StanceType.Standing;
        private float _crouchDistance;
        private float _targetCameraHeight;
        private float _currentCameraHeight;
        private bool _stanceTogglePressed;

        // PlayerPrefs key
        private const string StanceDistancePrefKey = "CueStrike_StanceDistance";
        #endregion

        #region Properties
        public StanceType CurrentStance => _currentStance;
        public bool IsCrouching => _currentStance == StanceType.Crouching;
        public float CrouchDistance => _crouchDistance;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (inputMapping == null)
                inputMapping = Resources.Load<CueStrikeVRInputMapping>("VRInputMapping");

            // Load saved crouch distance
            _crouchDistance = PlayerPrefs.GetFloat(StanceDistancePrefKey, inputMapping != null
                ? inputMapping.crouchDistanceDefault : 0.8f);

            // Round to valid range
            if (inputMapping != null)
                _crouchDistance = Mathf.Clamp(_crouchDistance, inputMapping.crouchDistanceMin, inputMapping.crouchDistanceMax);

            _currentCameraHeight = standingHeight;
            _targetCameraHeight = standingHeight;
        }

        private void Start()
        {
            ApplyStanceHeight();
        }

        private void Update()
        {
            if (inputMapping == null) return;

            // 1. Read stance toggle (thumbstick click)
            bool togglePressed = ReadStanceTogglePressed();
            if (togglePressed && !_stanceTogglePressed)
            {
                ToggleStance();
            }
            _stanceTogglePressed = togglePressed;

            // 2. If crouching, read stance distance stick
            if (_currentStance == StanceType.Crouching)
            {
                Vector2 stickValue = ReadStickValue(inputMapping.stanceDistanceStickAction);
                if (Mathf.Abs(stickValue.y) > 0.1f)
                {
                    float range = inputMapping.crouchDistanceMax - inputMapping.crouchDistanceMin;
                    _crouchDistance += stickValue.y * Time.deltaTime * range * 0.5f; // 0.5s to traverse full range
                    _crouchDistance = Mathf.Clamp(_crouchDistance, inputMapping.crouchDistanceMin, inputMapping.crouchDistanceMax);

                    PlayerPrefs.SetFloat(StanceDistancePrefKey, _crouchDistance);
                    PlayerPrefs.Save();

                    OnStanceDistanceChanged?.Invoke(_crouchDistance);

                    if (verboseLogging)
                        Debug.Log($"[Stance] Crouch distance adjusted: {_crouchDistance:F2}m");
                }
            }

            // 3. Smooth lerp camera height
            float targetHeight = (_currentStance == StanceType.Standing) ? standingHeight : crouchHeight;
            _currentCameraHeight = Mathf.Lerp(_currentCameraHeight, targetHeight, Time.deltaTime * heightLerpSpeed);

            if (Mathf.Abs(_currentCameraHeight - _targetCameraHeight) > 0.01f)
            {
                _targetCameraHeight = targetHeight;
                ApplyStanceHeight();
            }
        }
        #endregion

        #region Stance Logic
        /// <summary>
        /// Toggle between Standing and Crouching.
        /// </summary>
        public void ToggleStance()
        {
            _currentStance = (_currentStance == StanceType.Standing) ? StanceType.Crouching : StanceType.Standing;

            PlayerPrefs.SetInt("CueStrike_Stance", (int)_currentStance);
            PlayerPrefs.Save();

            OnStanceChanged?.Invoke(_currentStance);

            if (verboseLogging)
                Debug.Log($"[Stance] Toggled → {_currentStance}");
        }

        /// <summary>
        /// Set stance directly.
        /// </summary>
        public void SetStance(StanceType stance)
        {
            if (_currentStance == stance) return;
            _currentStance = stance;
            OnStanceChanged?.Invoke(_currentStance);
        }

        /// <summary>
        /// Reset crouch distance to default.
        /// </summary>
        public void ResetCrouchDistance()
        {
            _crouchDistance = inputMapping != null ? inputMapping.crouchDistanceDefault : 0.8f;
            PlayerPrefs.SetFloat(StanceDistancePrefKey, _crouchDistance);
            PlayerPrefs.Save();
            OnStanceDistanceChanged?.Invoke(_crouchDistance);

            if (verboseLogging)
                Debug.Log($"[Stance] Crouch distance reset to default: {_crouchDistance:F2}m");
        }

        private void ApplyStanceHeight()
        {
            if (xrOriginTransform == null) return;

            // Adjust camera height relative to XR Origin
            Vector3 pos = xrOriginTransform.position;
            pos.y = _currentCameraHeight;
            xrOriginTransform.position = pos;
        }

        private bool ReadStanceTogglePressed()
        {
            if (inputMapping == null || inputMapping.stanceToggleAction == null) return false;
            try
            {
                return inputMapping.stanceToggleAction.action.WasPressedThisFrame();
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
        /// Assign XR Origin transform. Called by VRInputManager.
        /// </summary>
        public void AssignXROrigin(Transform originTransform)
        {
            xrOriginTransform = originTransform;
        }

        public Transform GetXROrigin() => xrOriginTransform;
        #endregion
    }
}