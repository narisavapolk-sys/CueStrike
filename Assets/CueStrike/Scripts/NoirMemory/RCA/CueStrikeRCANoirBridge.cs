using System;
using UnityEngine;

namespace CueStrike.NoirMemory.RCA
{
    /// <summary>
    /// Bridge between the CueStrike framework and the RCA (Real Cue Adapter) hardware.
    /// Provides a singleton-managed interface for aiming, charging, and executing shots
    /// using the optional NoirMemory RCA peripheral. Includes a calibration subsystem and
    /// a dummy/mock mode that can be used for testing without physical hardware present.
    /// </summary>
    public class CueStrikeRCANoirBridge : MonoBehaviour
    {
        #region Singleton
        private static CueStrikeRCANoirBridge _instance;
        public static CueStrikeRCANoirBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CueStrikeRCANoirBridge>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CueStrikeRCANoirBridge");
                        _instance = go.AddComponent<CueStrikeRCANoirBridge>();
                        Debug.Log("[RCANoir] Auto-created CueStrikeRCANoirBridge.");
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Enums
        /// <summary>
        /// State machine for the RCA bridge.
        /// </summary>
        public enum RCAState
        {
            Idle,
            Aiming,
            Charging,
            Shooting,
            Resolving
        }
        #endregion

        #region Events
        /// <summary>Fired when the player begins aiming with the cue.</summary>
        public event Action OnAimingStarted;
        /// <summary>Fired when shot power is being charged.</summary>
        public event Action<float> OnCharging;
        /// <summary>Fired when a shot is executed with full data.</summary>
        public event Action<NoirMemoryShotData> OnShotExecuted;
        /// <summary>Fired when calibration state changes.</summary>
        public event Action<bool> OnCalibrationChanged;
        #endregion

        #region Inspector
        [Header("RCA Settings")]
        [SerializeField] private bool enableDummyMode = true;
        [SerializeField] private float dummyPower = 0.5f;
        [SerializeField] private float chargeSpeed = 1.0f;
        [SerializeField] private string rcaDeviceName = "CueStrike RCA";
        #endregion

        #region State
        private RCAState _currentState = RCAState.Idle;
        private RCANoirCalibrationData _calibrationData;
        private bool _hardwareConnected = false;
        private float _currentCharge = 0f;
        private float _tipOffsetX = 0f;
        private float _tipOffsetY = 0f;
        private float _cueAngle = 0f;
        private Vector3 _aimDirection = Vector3.forward;

        /// <summary>
        /// Current state of the RCA bridge state machine.
        /// </summary>
        public RCAState CurrentState => _currentState;
        /// <summary>
        /// Whether the bridge is in offline/dummy mode (no real hardware).
        /// </summary>
        public bool IsDummyMode => enableDummyMode || !_hardwareConnected;
        /// <summary>
        /// Whether hardware is connected and detected.
        /// </summary>
        public bool IsHardwareConnected => _hardwareConnected;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _calibrationData = RCANoirCalibrationData.Load();
            if (_calibrationData == null)
            {
                _calibrationData = new RCANoirCalibrationData();
            }
        }

        private void Start()
        {
            DetectHardware();
        }

        private void Update()
        {
            // Dummy mode auto-charge when in Charging state
            if (_currentState == RCAState.Charging && enableDummyMode)
            {
                _currentCharge += Time.deltaTime * chargeSpeed;
                _currentCharge = Mathf.Clamp01(_currentCharge);
                OnCharging?.Invoke(_currentCharge);
            }
        }
        #endregion

        #region Public API

        /// <summary>
        /// Begins the aiming phase. Transitions from Idle to Aiming state.
        /// </summary>
        public void StartAim()
        {
            if (_currentState != RCAState.Idle)
            {
                Debug.LogWarning($"[RCANoir] Cannot start aiming from state {_currentState}.");
                return;
            }
            _currentState = RCAState.Aiming;
            _currentCharge = 0f;
            OnAimingStarted?.Invoke();
            Debug.Log("[RCANoir] Aiming started.");
        }

        /// <summary>
        /// Updates aim parameters during the Aiming state.
        /// </summary>
        /// <param name="cueAngle">Angle of the cue in degrees from horizontal.</param>
        /// <param name="tipOffset">Normalized tip offset from center (-1 to 1).</param>
        public void UpdateAim(float cueAngle, Vector2 tipOffset)
        {
            if (_currentState != RCAState.Aiming)
            {
                Debug.LogWarning("[RCANoir] UpdateAim called outside Aiming state.");
                return;
            }
            _cueAngle = Mathf.Clamp(cueAngle, 0f, 90f);
            _tipOffsetX = Mathf.Clamp(tipOffset.x, -1f, 1f);
            _tipOffsetY = Mathf.Clamp(tipOffset.y, -1f, 1f);

            // Calculate aim direction from angle
            float rad = _cueAngle * Mathf.Deg2Rad;
            _aimDirection = new Vector3(Mathf.Sin(rad), -Mathf.Cos(rad), 0f).normalized;
        }

        /// <summary>
        /// Begins charging a shot. Transitions from Aiming to Charging state.
        /// </summary>
        /// <param name="power">Initial power value (0-1).</param>
        public void ChargeShot(float power)
        {
            if (_currentState != RCAState.Aiming)
            {
                Debug.LogWarning($"[RCANoir] Cannot charge shot from state {_currentState}.");
                return;
            }
            _currentState = RCAState.Charging;
            _currentCharge = Mathf.Clamp01(power);
            OnCharging?.Invoke(_currentCharge);
            Debug.Log($"[RCANoir] Charging shot: power={_currentCharge:F2}");
        }

        /// <summary>
        /// Executes the shot. Transitions from Charging to Shooting to Resolving to Idle.
        /// Fires OnShotExecuted with the final shot data.
        /// </summary>
        public void ExecuteShot()
        {
            if (_currentState != RCAState.Charging)
            {
                Debug.LogWarning($"[RCANoir] Cannot execute shot from state {_currentState}.");
                return;
            }

            _currentState = RCAState.Shooting;

            // Build shot data
            float finalPower = _currentCharge;
            if (_calibrationData != null && _calibrationData.IsValid)
            {
                finalPower *= _calibrationData.PowerScale;
            }
            finalPower = Mathf.Clamp01(finalPower);

            float confidence = CalculateShotConfidence();

            var shotData = new NoirMemoryShotData
            {
                aimDirection = _aimDirection,
                power = finalPower,
                cueAngle = _cueAngle,
                tipOffsetX = _tipOffsetX,
                tipOffsetY = _tipOffsetY,
                confidence = confidence,
                timestamp = DateTime.UtcNow
            };

            OnShotExecuted?.Invoke(shotData);
            Debug.Log($"[RCANoir] Shot executed: power={finalPower:F2}, confidence={confidence:F2}");

            _currentState = RCAState.Resolving;
            Invoke(nameof(Reset), 0.1f);
        }

        /// <summary>
        /// Cancels the current operation and returns to Idle.
        /// </summary>
        public void CancelShot()
        {
            if (_currentState == RCAState.Idle) return;
            _currentState = RCAState.Idle;
            _currentCharge = 0f;
            Debug.Log("[RCANoir] Shot cancelled.");
        }

        /// <summary>
        /// Resets the bridge to Idle state.
        /// </summary>
        public void Reset()
        {
            _currentState = RCAState.Idle;
            _currentCharge = 0f;
            _cueAngle = 0f;
            _tipOffsetX = 0f;
            _tipOffsetY = 0f;
            _aimDirection = Vector3.forward;
        }

        /// <summary>
        /// Simulates a complete shot cycle with mock data. Useful for testing without hardware.
        /// </summary>
        public void SimulateShot()
        {
            if (!enableDummyMode)
            {
                Debug.LogWarning("[RCANoir] Dummy mode disabled. Cannot simulate.");
                return;
            }

            Debug.Log("[RCANoir] Simulating shot...");
            StartAim();
            UpdateAim(25f, new Vector2(0.1f, -0.05f));
            ChargeShot(dummyPower);
            ExecuteShot();
        }

        #endregion

        #region Calibration

        /// <summary>
        /// Starts the calibration flow.
        /// </summary>
        public void StartCalibration()
        {
            Debug.Log("[RCANoir] Calibration started. Place cue at rest position and confirm.");
        }

        /// <summary>
        /// Completes calibration with the given cue rest position and rotation.
        /// </summary>
        public void CompleteCalibration(Vector3 restPosition, Quaternion restRotation)
        {
            _calibrationData.CueRestPosition = restPosition;
            _calibrationData.CueRestRotation = restRotation;
            _calibrationData.CalibratedAt = DateTime.UtcNow;
            RCANoirCalibrationData.Save(_calibrationData);
            OnCalibrationChanged?.Invoke(true);
            Debug.Log($"[RCANoir] Calibration complete at {restPosition}");
        }

        /// <summary>
        /// Cancels calibration without saving.
        /// </summary>
        public void CancelCalibration()
        {
            _calibrationData = RCANoirCalibrationData.Load() ?? new RCANoirCalibrationData();
            OnCalibrationChanged?.Invoke(false);
            Debug.Log("[RCANoir] Calibration cancelled.");
        }

        /// <summary>
        /// Returns true if the bridge has valid calibration data.
        /// </summary>
        public bool IsCalibrated()
        {
            return _calibrationData != null && _calibrationData.IsValid;
        }

        /// <summary>
        /// Gets the current calibration data.
        /// </summary>
        public RCANoirCalibrationData GetCalibrationData() => _calibrationData;

        #endregion

        #region Private

        private void DetectHardware()
        {
            // Attempt to detect RCA hardware
            // In a real implementation, this would scan USB/serial ports
            try
            {
                // Placeholder: check if mock device name matches
                _hardwareConnected = !string.IsNullOrEmpty(rcaDeviceName) && !enableDummyMode;
                if (_hardwareConnected)
                {
                    Debug.Log($"[RCANoir] Hardware detected: {rcaDeviceName}");
                }
                else
                {
                    Debug.Log($"[RCANoir] No hardware. Using {(enableDummyMode ? "dummy" : "fallback")} mode.");
                }
            }
            catch (Exception ex)
            {
                _hardwareConnected = false;
                Debug.LogWarning($"[RCANoir] Hardware detection failed: {ex.Message}");
            }
        }

        private float CalculateShotConfidence()
        {
            // Confidence based on calibration validity, tip offset, and angle stability
            float confidence = 1.0f;
            if (!IsCalibrated()) confidence -= 0.3f;
            confidence -= Mathf.Abs(_tipOffsetX) * 0.2f;
            confidence -= Mathf.Abs(_tipOffsetY) * 0.2f;
            return Mathf.Clamp01(confidence);
        }

        #endregion
    }

    #region Shot Data

    /// <summary>
    /// Data structure representing a completed shot from the RCA bridge.
    /// </summary>
    public struct NoirMemoryShotData
    {
        /// <summary>Normalized aim direction vector.</summary>
        public Vector3 aimDirection;
        /// <summary>Shot power normalized 0-1.</summary>
        public float power;
        /// <summary>Cue angle in degrees from horizontal.</summary>
        public float cueAngle;
        /// <summary>Horizontal tip offset from center (-1 to 1).</summary>
        public float tipOffsetX;
        /// <summary>Vertical tip offset from center (-1 to 1).</summary>
        public float tipOffsetY;
        /// <summary>Confidence score 0-1 based on calibration and stability.</summary>
        public float confidence;
        /// <summary>UTC timestamp of shot execution.</summary>
        public DateTime timestamp;
    }

    #endregion
}