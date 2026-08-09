using UnityEngine;
using System;
using System.Collections.Generic;
using CueStrike.Physics;

namespace CueStrike.RCA
{
    /// <summary>
    /// Central manager for the Controller-less RCA (Real Cue Alignment) system.
    /// Coordinates calibration, hand tracking, prediction, compensation, and physics.
    /// </summary>
    public class CueStrikeRCAManager : MonoBehaviour
    {
        [Header("RCA Components")]
        [SerializeField] private CueStrikeRCACalibrator calibrator;
        [SerializeField] private CueStrikeDualHandTracker handTracker;
        [SerializeField] private CueStrikeKalmanPredictor cueTipPredictor;
        [SerializeField] private CueStrikeKalmanPredictor bridgeHandPredictor;
        [SerializeField] private CueStrikeVisualVelocityCompensation visualCompensation;
        [SerializeField] private CueStrikeCuePhysicsProfile physicsProfile;
        
        [Header("Cue Visual References")]
        [SerializeField] private Transform cueVisualRoot;
        [SerializeField] private Transform cueTipVisual;
        [SerializeField] private Transform bridgeHandVisual;
        [SerializeField] private Transform predictedCueTipVisual;
        [SerializeField] private Transform predictedBridgeVisual;
        
        [Header("System Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool enableVisualCompensation = true;
        [SerializeField] private bool enableKalmanPrediction = true;
        [SerializeField] private bool enablePhysicsSimulation = true;
        [SerializeField] private float systemUpdateRate = 90f; // Hz
        
        [Header("Integration References")]
        [SerializeField] private CueStrikeStrikeRealism strikeRealism;
        [SerializeField] private Rigidbody cueBallRigidbody;
        [SerializeField] private Transform cueBallTransform;
        
        [Header("Runtime State")]
        [SerializeField] private RCAState currentState = RCAState.Uninitialized;
        [SerializeField] private bool isTracking = false;
        [SerializeField] private float lastSystemUpdate = 0f;
        [SerializeField] private Vector3 lastCueTipPosition = Vector3.zero;
        [SerializeField] private Vector3 lastBridgePosition = Vector3.zero;
        [SerializeField] private float cueSpeed = 0f;
        [SerializeField] private bool isStriking = false;
        [SerializeField] private float strikeStartTime = 0f;
        
        // Events
        public System.Action<RCAState> OnStateChanged;
        public System.Action<Vector3, Vector3, float> OnStrikeDetected; // cueTipPos, direction, force
        public System.Action<Vector3, Vector3> OnCuePositionUpdated; // tipPos, bridgePos
        public System.Action<bool> OnTrackingStateChanged;
        public System.Action<string> OnCalibrationMessage;
        
        public enum RCAState
        {
            Uninitialized,
            Calibrating,
            Calibrated,
            Tracking,
            Striking,
            Error
        }
        
        public RCAState CurrentState => currentState;
        public bool IsTracking => isTracking;
        public bool IsCalibrated => calibrator != null && calibrator.IsCalibrated;
        public float CueSpeed => cueSpeed;
        public Vector3 CueTipPosition => visualCompensation != null ? visualCompensation.transform.position : lastCueTipPosition;
        public Vector3 BridgePosition => visualCompensation != null ? visualCompensation.transform.position : lastBridgePosition;
        public Vector3 CueDirection => GetCueDirection();
        public CueStrikeRCACalibrator Calibrator => calibrator;
        public CueStrikeDualHandTracker HandTracker => handTracker;
        public CueStrikeVisualVelocityCompensation VisualCompensation => visualCompensation;
        public CueStrikeCuePhysicsProfile PhysicsProfile => physicsProfile;
        
        private void Awake()
        {
            // Auto-find components if not assigned
            FindComponents();
        }
        
        private void Start()
        {
            if (autoInitialize)
            {
                InitializeSystem();
            }
        }
        
        private void FindComponents()
        {
            if (calibrator == null) calibrator = GetComponentInChildren<CueStrikeRCACalibrator>();
            if (handTracker == null) handTracker = GetComponentInChildren<CueStrikeDualHandTracker>();
            if (cueTipPredictor == null)
            {
                var predictors = GetComponentsInChildren<CueStrikeKalmanPredictor>();
                if (predictors.Length > 0) cueTipPredictor = predictors[0];
                if (predictors.Length > 1) bridgeHandPredictor = predictors[1];
            }
            if (visualCompensation == null) visualCompensation = GetComponentInChildren<CueStrikeVisualVelocityCompensation>();
            if (physicsProfile == null) physicsProfile = Resources.Load<CueStrikeCuePhysicsProfile>("CuePhysicsProfile");
            if (strikeRealism == null) strikeRealism = GetComponentInChildren<CueStrikeStrikeRealism>();
            
            // Auto-find cue ball
            if (cueBallRigidbody == null)
            {
                var ball = GameObject.FindGameObjectWithTag("CueBall");
                if (ball != null) cueBallRigidbody = ball.GetComponent<Rigidbody>();
                if (ball != null) cueBallTransform = ball.transform;
            }
        }
        
        /// <summary>
        /// Initializes the entire RCA system.
        /// </summary>
        public void InitializeSystem()
        {
            SetState(RCAState.Uninitialized);
            
            // Validate components
            if (!ValidateComponents())
            {
                SetState(RCAState.Error);
                return;
            }
            
            // Setup component references
            SetupComponentReferences();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Load existing calibration
            if (calibrator != null)
            {
                calibrator.LoadCalibrationData();
                
                if (calibrator.IsCalibrated)
                {
                    SetState(RCAState.Calibrated);
                    OnCalibrationMessage?.Invoke("Calibration loaded successfully.");
                }
                else
                {
                    SetState(RCAState.Calibrating);
                    StartCalibration();
                }
            }
            
            // Initialize predictors
            if (enableKalmanPrediction)
            {
                InitializePredictors();
            }
            
            // Initialize visual compensation
            if (enableVisualCompensation && visualCompensation != null)
            {
                visualCompensation.SetUseKalmanPrediction(enableKalmanPrediction);
            }
            
            SetState(RCAState.Tracking);
            isTracking = true;
            OnTrackingStateChanged?.Invoke(true);
            
            Debug.Log("[RCAManager] System initialized successfully.");
        }
        
        private bool ValidateComponents()
        {
            bool valid = true;
            
            if (calibrator == null)
            {
                Debug.LogError("[RCAManager] Calibrator not found!");
                valid = false;
            }
            
            if (handTracker == null)
            {
                Debug.LogWarning("[RCAManager] Hand tracker not found. Using fallback transforms.");
            }
            
            if (visualCompensation == null)
            {
                Debug.LogWarning("[RCAManager] Visual compensation not found.");
            }
            
            if (physicsProfile == null)
            {
                Debug.LogWarning("[RCAManager] Physics profile not assigned. Using defaults.");
            }
            
            return valid;
        }
        
        private void SetupComponentReferences()
        {
            // Link calibrator to visual compensation
            if (visualCompensation != null && calibrator != null)
            {
                // Visual compensation will use calibrator data via events
            }
            
            // Link hand tracker to predictors
            if (handTracker != null)
            {
                handTracker.OnRightHandUpdated += OnCueHandUpdated;
                handTracker.OnLeftHandUpdated += OnBridgeHandUpdated;
            }
        }
        
        private void SubscribeToEvents()
        {
            if (calibrator != null)
            {
                calibrator.OnCalibrationStateChanged += OnCalibrationStateChanged;
                calibrator.OnCalibrationCompleted += OnCalibrationCompleted;
                calibrator.OnCalibrationFailed += OnCalibrationFailed;
            }
            
            if (visualCompensation != null)
            {
                visualCompensation.OnCompensationUpdated += OnCompensationUpdated;
                visualCompensation.OnPredictionUpdated += OnPredictionUpdated;
            }
        }
        
        private void InitializePredictors()
        {
            if (cueTipPredictor != null)
            {
                cueTipPredictor.ResetFilter();
            }
            
            if (bridgeHandPredictor != null)
            {
                bridgeHandPredictor.ResetFilter();
            }
        }
        
        /// <summary>
        /// Starts the calibration process.
        /// </summary>
        public void StartCalibration()
        {
            if (calibrator != null)
            {
                SetState(RCAState.Calibrating);
                calibrator.StartCalibration();
                OnCalibrationMessage?.Invoke("Calibration started. Follow the on-screen instructions.");
            }
        }
        
        /// <summary>
        /// Resets calibration and restarts.
        /// </summary>
        public void ResetCalibration()
        {
            if (calibrator != null)
            {
                calibrator.ResetCalibration();
                InitializePredictors();
                SetState(RCAState.Calibrating);
                StartCalibration();
            }
        }
        
        private void Update()
        {
            float dt = Time.time - lastSystemUpdate;
            if (dt < 1f / systemUpdateRate) return;
            lastSystemUpdate = Time.time;
            
            if (currentState == RCAState.Tracking || currentState == RCAState.Striking)
            {
                UpdateTracking();
                CheckForStrike();
            }
        }
        
        private void UpdateTracking()
        {
            // Get cue direction and speed
            Vector3 cueTipPos = CueTipPosition;
            Vector3 bridgePos = BridgePosition;
            Vector3 cueDir = GetCueDirection();
            
            // Calculate cue speed
            if (lastCueTipPosition != Vector3.zero)
            {
                cueSpeed = Vector3.Distance(cueTipPos, lastCueTipPosition) / (Time.time - lastSystemUpdate);
            }
            
            lastCueTipPosition = cueTipPos;
            lastBridgePosition = bridgePos;
            
            // Fire update event
            OnCuePositionUpdated?.Invoke(cueTipPos, bridgePos);
        }
        
        private void CheckForStrike()
        {
            if (cueBallRigidbody == null || cueBallTransform == null) return;
            
            Vector3 cueTipPos = CueTipPosition;
            Vector3 cueDir = GetCueDirection();
            float distanceToBall = Vector3.Distance(cueTipPos, cueBallTransform.position);
            
            // Check if cue is approaching ball
            Vector3 toBall = (cueBallTransform.position - cueTipPos).normalized;
            float alignment = Vector3.Dot(cueDir, toBall);
            
            bool wasStriking = isStriking;
            isStriking = distanceToBall < 0.05f && alignment > 0.7f && cueSpeed > 0.5f;
            
            // Detect strike start
            if (isStriking && !wasStriking)
            {
                strikeStartTime = Time.time;
                SetState(RCAState.Striking);
            }
            // Detect strike end (impact)
            else if (!isStriking && wasStriking)
            {
                float strikeDuration = Time.time - strikeStartTime;
                if (strikeDuration < 0.5f && cueSpeed > 1f)
                {
                    // Impact detected!
                    float force = physicsProfile != null 
                        ? physicsProfile.CalculateStrikeForce(cueSpeed)
                        : cueSpeed * 10f;
                    
                    OnStrikeDetected?.Invoke(cueTipPos, cueDir, force);
                    
                    // Apply physics through StrikeRealism if available
                    if (enablePhysicsSimulation && strikeRealism != null)
                    {
                        // Calculate strike point from cue alignment
                        Vector3 toCueBall = (cueBallTransform.position - cueTipPos).normalized;
                        float sideOffset = Vector3.Dot(Vector3.Cross(Vector3.up, cueDir).normalized, toCueBall);
                        float verticalOffset = Vector3.Dot(Vector3.up, toCueBall);
                        Vector2 strikePoint = new Vector2(sideOffset, -verticalOffset); // x=side, y=top/back
                        
                        // Use cue speed normalized to max strike speed
                        float triggerPull = Mathf.Clamp01(cueSpeed / strikeRealism.maxStrikeSpeed);
                        
                        strikeRealism.Strike(cueDir, strikePoint, triggerPull);
                    }
                    else if (enablePhysicsSimulation && physicsProfile != null)
                    {
                        ApplyPhysicsDirectly(cueTipPos, cueDir, force);
                    }
                }
                
                SetState(RCAState.Tracking);
            }
        }
        
        private void ApplyPhysicsDirectly(Vector3 cueTipPos, Vector3 cueDir, float force)
        {
            if (cueBallRigidbody == null) return;
            
            // Calculate ball velocity using physics profile
            Vector3 ballVelocity = physicsProfile.CalculateBallVelocity(
                cueDir * cueSpeed, 
                cueDir, 
                (cueBallTransform.position - cueTipPos).normalized,
                0f // spin factor - would need tip offset calculation
            );
            
            // Apply impulse
            cueBallRigidbody.linearVelocity = ballVelocity;
            
            // Apply spin
            Vector3 spin = physicsProfile.CalculateSpin(
                cueDir * cueSpeed,
                cueDir,
                cueTipPos,
                cueBallTransform.position
            );
            cueBallRigidbody.angularVelocity = spin;
        }
        
        private Vector3 GetCueDirection()
        {
            if (calibrator != null && calibrator.IsCalibrated)
            {
                return calibrator.GetCueDirection();
            }
            
            if (visualCompensation != null)
            {
                Vector3 tip = visualCompensation.transform.position;
                Vector3 bridge = visualCompensation.transform.position; // Would need bridge position
                return (tip - bridge).normalized;
            }
            
            if (handTracker != null)
            {
                var cueHand = handTracker.GetCueHand();
                var bridgeHand = handTracker.GetBridgeHand();
                
                if (cueHand.isTracked && bridgeHand.isTracked)
                {
                    return (cueHand.indexTipPosition - bridgeHand.palmPosition).normalized;
                }
            }
            
            return Vector3.forward;
        }
        
        // Event Handlers
        private void OnCalibrationStateChanged(CueStrikeRCACalibrator.CalibrationState state)
        {
            switch (state)
            {
                case CueStrikeRCACalibrator.CalibrationState.WaitingForHeadPosition:
                    OnCalibrationMessage?.Invoke("Look at the calibration target.");
                    break;
                case CueStrikeRCACalibrator.CalibrationState.AligningRightHand:
                    OnCalibrationMessage?.Invoke("Align your right hand (cue hand) to the target.");
                    break;
                case CueStrikeRCACalibrator.CalibrationState.AligningLeftHand:
                    OnCalibrationMessage?.Invoke("Align your left hand (bridge hand) to the target.");
                    break;
                case CueStrikeRCACalibrator.CalibrationState.MeasuringCueLength:
                    OnCalibrationMessage?.Invoke("Hold the cue naturally to measure length.");
                    break;
            }
        }
        
        private void OnCalibrationCompleted()
        {
            SetState(RCAState.Calibrated);
            OnCalibrationMessage?.Invoke("Calibration completed successfully!");
            
            // Initialize predictors with calibrated positions
            if (enableKalmanPrediction)
            {
                Vector3 cueTipPos = calibrator.GetCalibratedCueTipPosition();
                Vector3 bridgePos = calibrator.GetCalibratedBridgeHandPosition();
                
                if (cueTipPredictor != null)
                    cueTipPredictor.Initialize(cueTipPos);
                
                if (bridgeHandPredictor != null)
                    bridgeHandPredictor.Initialize(bridgePos);
            }
        }
        
        private void OnCalibrationFailed()
        {
            SetState(RCAState.Error);
            OnCalibrationMessage?.Invoke("Calibration failed. Please try again.");
        }
        
        private void OnCueHandUpdated(HandData handData)
        {
            if (handData.isTracked && cueTipPredictor != null && enableKalmanPrediction)
            {
                cueTipPredictor.UpdateMeasurement(handData.indexTipPosition);
            }
        }
        
        private void OnBridgeHandUpdated(HandData handData)
        {
            if (handData.isTracked && bridgeHandPredictor != null && enableKalmanPrediction)
            {
                bridgeHandPredictor.UpdateMeasurement(handData.palmPosition);
            }
        }
        
        private void OnCompensationUpdated(Vector3 cueTipPos, Quaternion cueRot, Vector3 bridgePos)
        {
            // Update visuals
            if (cueTipVisual != null)
            {
                cueTipVisual.position = cueTipPos;
                cueTipVisual.rotation = cueRot;
            }
            
            if (bridgeHandVisual != null)
            {
                bridgeHandVisual.position = bridgePos;
            }
        }
        
        private void OnPredictionUpdated(Vector3 predictedCueTip, Vector3 predictedBridge)
        {
            if (predictedCueTipVisual != null)
            {
                predictedCueTipVisual.position = predictedCueTip;
            }
            
            if (predictedBridgeVisual != null)
            {
                predictedBridgeVisual.position = predictedBridge;
            }
        }
        
        private void SetState(RCAState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }
        
    /// <summary>
    /// Gets system diagnostics info.
    /// </summary>
    public RCADiagnostics GetDiagnostics()
    {
        return new RCADiagnostics
        {
            state = currentState,
            isTracking = isTracking,
            isCalibrated = IsCalibrated,
            cueSpeed = cueSpeed,
            cueTipPosition = lastCueTipPosition,
            bridgePosition = lastBridgePosition,
            cueDirection = GetCueDirection(),
            kalmanConfidence = cueTipPredictor != null ? cueTipPredictor.GetConfidence() : 0f,
            compensationMagnitude = visualCompensation != null ? visualCompensation.GetCompensationMagnitude() : 0f,
            predictionMagnitude = visualCompensation != null ? visualCompensation.GetPredictionMagnitude() : 0f
        };
    }
        
        /// <summary>
        /// Toggles visual compensation.
        /// </summary>
        public void SetVisualCompensationEnabled(bool enabled)
        {
            enableVisualCompensation = enabled;
            if (visualCompensation != null)
            {
                visualCompensation.enabled = enabled;
            }
        }
        
        /// <summary>
        /// Toggles Kalman prediction.
        /// </summary>
        public void SetKalmanPredictionEnabled(bool enabled)
        {
            enableKalmanPrediction = enabled;
            if (visualCompensation != null)
            {
                visualCompensation.SetUseKalmanPrediction(enabled);
            }
        }
        
        /// <summary>
        /// Sets the visual latency for compensation.
        /// </summary>
        public void SetVisualLatency(float latency)
        {
            if (visualCompensation != null)
            {
                visualCompensation.SetVisualLatency(latency);
            }
        }
        
        /// <summary>
        /// Shuts down the RCA system.
        /// </summary>
        public void Shutdown()
        {
            isTracking = false;
            OnTrackingStateChanged?.Invoke(false);
            
            // Unsubscribe events
            if (calibrator != null)
            {
                calibrator.OnCalibrationStateChanged -= OnCalibrationStateChanged;
                calibrator.OnCalibrationCompleted -= OnCalibrationCompleted;
                calibrator.OnCalibrationFailed -= OnCalibrationFailed;
            }
            
            if (handTracker != null)
            {
                handTracker.OnRightHandUpdated -= OnCueHandUpdated;
                handTracker.OnLeftHandUpdated -= OnBridgeHandUpdated;
            }
            
            if (visualCompensation != null)
            {
                visualCompensation.OnCompensationUpdated -= OnCompensationUpdated;
                visualCompensation.OnPredictionUpdated -= OnPredictionUpdated;
            }
            
            SetState(RCAState.Uninitialized);
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (currentState == RCAState.Tracking || currentState == RCAState.Striking)
            {
                // Draw cue line
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(lastBridgePosition, lastCueTipPosition);
                
                // Draw cue direction
                Gizmos.color = Color.green;
                Gizmos.DrawRay(lastCueTipPosition, GetCueDirection() * 0.5f);
                
                // Draw cue speed
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lastCueTipPosition, 0.02f + cueSpeed * 0.01f);
            }
        }
    }
    
    /// <summary>
    /// Diagnostics data structure for RCA system.
    /// </summary>
    [System.Serializable]
    public struct RCADiagnostics
    {
        public CueStrikeRCAManager.RCAState state;
        public bool isTracking;
        public bool isCalibrated;
        public float cueSpeed;
        public Vector3 cueTipPosition;
        public Vector3 bridgePosition;
        public Vector3 cueDirection;
        public float kalmanConfidence;
        public float compensationMagnitude;
        public float predictionMagnitude;
    }
}
