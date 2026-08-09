using UnityEngine;

namespace CueStrike.RCA
{
    /// <summary>
    /// Visual velocity compensation for controller-less RCA.
    /// Compensates for visual latency by extrapolating cue position based on hand velocity.
    /// </summary>
    public class CueStrikeVisualVelocityCompensation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CueStrikeDualHandTracker handTracker;
        [SerializeField] private CueStrikeRCACalibrator calibrator;
        [SerializeField] private Transform cueVisualTransform;
        [SerializeField] private Transform cueTipVisual;
        [SerializeField] private Transform bridgeHandVisual;
        
        [Header("Compensation Settings")]
        [SerializeField] private bool enableCompensation = true;
        [SerializeField] private float visualLatency = 0.02f;       // Estimated visual latency (seconds)
        [SerializeField] private float maxCompensationDistance = 0.1f; // Max compensation distance (meters)
        [SerializeField] private float velocitySmoothing = 0.15f;   // Velocity smoothing factor
        [SerializeField] private float angularVelocitySmoothing = 0.15f; // Angular velocity smoothing
        
        [Header("Prediction Settings")]
        [SerializeField] private bool useKalmanPrediction = true;
        [SerializeField] private CueStrikeKalmanPredictor cueTipPredictor;
        [SerializeField] private CueStrikeKalmanPredictor bridgeHandPredictor;
        [SerializeField] private float predictionHorizon = 0.03f;   // How far ahead to predict
        
        [Header("Visual Settings")]
        [SerializeField] private bool showPredictedPosition = true;
        [SerializeField] private Transform predictedCueTipVisual;
        [SerializeField] private Transform predictedBridgeVisual;
        [SerializeField] private Color compensationColor = Color.yellow;
        [SerializeField] private Color predictionColor = Color.cyan;
        
        [Header("Runtime Data")]
        [SerializeField] private Vector3 cueTipVelocity = Vector3.zero;
        [SerializeField] private Vector3 bridgeHandVelocity = Vector3.zero;
        [SerializeField] private Vector3 cueTipAngularVelocity = Vector3.zero;
        [SerializeField] private Vector3 compensatedCueTipPosition = Vector3.zero;
        [SerializeField] private Quaternion compensatedCueRotation = Quaternion.identity;
        [SerializeField] private Vector3 compensatedBridgePosition = Vector3.zero;
        [SerializeField] private Vector3 predictedCueTipPosition = Vector3.zero;
        [SerializeField] private Vector3 predictedBridgePosition = Vector3.zero;
        
        [Header("Previous Frame Data")]
        [SerializeField] private Vector3 lastCueTipPos = Vector3.zero;
        [SerializeField] private Quaternion lastCueTipRot = Quaternion.identity;
        [SerializeField] private Vector3 lastBridgePos = Vector3.zero;
        [SerializeField] private float lastUpdateTime = 0f;
        [SerializeField] private bool isFirstFrame = true;
        
        public bool EnableCompensation
        {
            get => enableCompensation;
            set => enableCompensation = value;
        }
        
        public Vector3 CompensatedCueTipPosition => compensatedCueTipPosition;
        public Quaternion CompensatedCueRotation => compensatedCueRotation;
        public Vector3 CompensatedBridgePosition => compensatedBridgePosition;
        public Vector3 PredictedCueTipPosition => predictedCueTipPosition;
        public Vector3 PredictedBridgePosition => predictedBridgePosition;
        public Vector3 CueTipVelocity => cueTipVelocity;
        public Vector3 BridgeHandVelocity => bridgeHandVelocity;
        
        public System.Action<Vector3, Quaternion, Vector3> OnCompensationUpdated; // cueTipPos, cueRot, bridgePos
        public System.Action<Vector3, Vector3> OnPredictionUpdated; // predictedCueTip, predictedBridge
        
        private void Awake()
        {
            // Auto-find components if not assigned
            if (handTracker == null)
                handTracker = FindFirstObjectByType<CueStrikeDualHandTracker>();
            
            if (calibrator == null)
                calibrator = FindFirstObjectByType<CueStrikeRCACalibrator>();
            
            // Create Kalman predictors if not assigned and prediction is enabled
            if (useKalmanPrediction)
            {
                if (cueTipPredictor == null)
                {
                    var go = new GameObject("CueTipPredictor");
                    go.transform.SetParent(transform);
                    cueTipPredictor = go.AddComponent<CueStrikeKalmanPredictor>();
                    cueTipPredictor.SetProcessNoise(0.005f);
                    cueTipPredictor.SetMeasurementNoise(0.05f);
                    cueTipPredictor.SetMaxPredictionSteps(2);
                }
                
                if (bridgeHandPredictor == null)
                {
                    var go = new GameObject("BridgeHandPredictor");
                    go.transform.SetParent(transform);
                    bridgeHandPredictor = go.AddComponent<CueStrikeKalmanPredictor>();
                    bridgeHandPredictor.SetProcessNoise(0.005f);
                    bridgeHandPredictor.SetMeasurementNoise(0.05f);
                    bridgeHandPredictor.SetMaxPredictionSteps(2);
                }
            }
        }
        
        private void Start()
        {
            lastUpdateTime = Time.time;
            
            // Initialize visual references
            if (predictedCueTipVisual == null && showPredictedPosition)
            {
                predictedCueTipVisual = new GameObject("PredictedCueTip").transform;
                predictedCueTipVisual.SetParent(transform);
                var sphere = predictedCueTipVisual.gameObject.AddComponent<MeshRenderer>();
                sphere.material = CreatePredictionMaterial(predictionColor);
                predictedCueTipVisual.localScale = Vector3.one * 0.02f;
            }
            
            if (predictedBridgeVisual == null && showPredictedPosition)
            {
                predictedBridgeVisual = new GameObject("PredictedBridge").transform;
                predictedBridgeVisual.SetParent(transform);
                var sphere = predictedBridgeVisual.gameObject.AddComponent<MeshRenderer>();
                sphere.material = CreatePredictionMaterial(predictionColor);
                predictedBridgeVisual.localScale = Vector3.one * 0.03f;
            }
        }
        
        private void Update()
        {
            if (!enableCompensation) return;
            
            float dt = Time.time - lastUpdateTime;
            if (dt <= 0f) dt = Time.deltaTime;
            lastUpdateTime = Time.time;
            
            UpdateVelocities(dt);
            ApplyCompensation();
            ApplyPrediction();
            UpdateVisuals();
            
            if (isFirstFrame)
            {
                isFirstFrame = false;
            }
        }
        
        private void UpdateVelocities(float dt)
        {
            Vector3 currentCueTipPos = Vector3.zero;
            Quaternion currentCueTipRot = Quaternion.identity;
            Vector3 currentBridgePos = Vector3.zero;
            
            // Get current positions from calibrator (if calibrated) or hand tracker
            if (calibrator != null && calibrator.IsCalibrated)
            {
                currentCueTipPos = calibrator.GetCalibratedCueTipPosition();
                currentCueTipRot = calibrator.GetCalibratedCueRotation();
                currentBridgePos = calibrator.GetCalibratedBridgeHandPosition();
            }
            else if (handTracker != null)
            {
                // Fallback to hand tracker
                var cueHand = handTracker.GetCueHand();
                var bridgeHand = handTracker.GetBridgeHand();
                
                if (cueHand.isTracked)
                {
                    currentCueTipPos = cueHand.indexTipPosition;
                    currentCueTipRot = cueHand.rootRotation;
                }
                
                if (bridgeHand.isTracked)
                {
                    currentBridgePos = bridgeHand.palmPosition;
                }
            }
            
            // Calculate linear velocities
            if (!isFirstFrame)
            {
                cueTipVelocity = Vector3.Lerp(
                    cueTipVelocity,
                    (currentCueTipPos - lastCueTipPos) / dt,
                    1f - Mathf.Exp(-velocitySmoothing * dt * 60f)
                );
                
                bridgeHandVelocity = Vector3.Lerp(
                    bridgeHandVelocity,
                    (currentBridgePos - lastBridgePos) / dt,
                    1f - Mathf.Exp(-velocitySmoothing * dt * 60f)
                );
                
                // Calculate angular velocity
                Quaternion deltaRotation = currentCueTipRot * Quaternion.Inverse(lastCueTipRot);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                Vector3 angularVel = axis * (angle * Mathf.Deg2Rad) / dt;
                
                cueTipAngularVelocity = Vector3.Lerp(
                    cueTipAngularVelocity,
                    angularVel,
                    1f - Mathf.Exp(-angularVelocitySmoothing * dt * 60f)
                );
            }
            
            // Update last positions
            lastCueTipPos = currentCueTipPos;
            lastCueTipRot = currentCueTipRot;
            lastBridgePos = currentBridgePos;
            
            // Update Kalman predictors
            if (useKalmanPrediction)
            {
                if (cueTipPredictor != null)
                {
                    cueTipPredictor.UpdateMeasurement(currentCueTipPos);
                }
                
                if (bridgeHandPredictor != null)
                {
                    bridgeHandPredictor.UpdateMeasurement(currentBridgePos);
                }
            }
        }
        
        private void ApplyCompensation()
        {
            // Compensate for visual latency by extrapolating position based on velocity
            Vector3 cueTipCompensation = cueTipVelocity * visualLatency;
            Vector3 bridgeCompensation = bridgeHandVelocity * visualLatency;
            
            // Clamp compensation to max distance
            cueTipCompensation = Vector3.ClampMagnitude(cueTipCompensation, maxCompensationDistance);
            bridgeCompensation = Vector3.ClampMagnitude(bridgeCompensation, maxCompensationDistance);
            
            // Apply compensation
            compensatedCueTipPosition = lastCueTipPos + cueTipCompensation;
            compensatedBridgePosition = lastBridgePos + bridgeCompensation;
            
            // Compensate rotation using angular velocity
            Quaternion rotationCompensation = Quaternion.Euler(cueTipAngularVelocity * visualLatency * Mathf.Rad2Deg);
            compensatedCueRotation = lastCueTipRot * rotationCompensation;
        }
        
        private void ApplyPrediction()
        {
            if (useKalmanPrediction)
            {
                // Use Kalman filter prediction
                if (cueTipPredictor != null && cueTipPredictor.IsInitialized)
                {
                    predictedCueTipPosition = cueTipPredictor.GetPredictedPosition(predictionHorizon);
                }
                else
                {
                    // Fallback: simple velocity extrapolation
                    predictedCueTipPosition = compensatedCueTipPosition + cueTipVelocity * predictionHorizon;
                }
                
                if (bridgeHandPredictor != null && bridgeHandPredictor.IsInitialized)
                {
                    predictedBridgePosition = bridgeHandPredictor.GetPredictedPosition(predictionHorizon);
                }
                else
                {
                    predictedBridgePosition = compensatedBridgePosition + bridgeHandVelocity * predictionHorizon;
                }
            }
            else
            {
                // Simple velocity extrapolation
                predictedCueTipPosition = compensatedCueTipPosition + cueTipVelocity * predictionHorizon;
                predictedBridgePosition = compensatedBridgePosition + bridgeHandVelocity * predictionHorizon;
            }
        }
        
        private void UpdateVisuals()
        {
            // Update actual cue visual with compensated position
            if (cueVisualTransform != null)
            {
                cueVisualTransform.position = compensatedCueTipPosition;
                cueVisualTransform.rotation = compensatedCueRotation;
            }
            
            if (cueTipVisual != null)
            {
                cueTipVisual.position = compensatedCueTipPosition;
                cueTipVisual.rotation = compensatedCueRotation;
            }
            
            if (bridgeHandVisual != null)
            {
                bridgeHandVisual.position = compensatedBridgePosition;
            }
            
            // Update predicted visuals
            if (showPredictedPosition)
            {
                if (predictedCueTipVisual != null)
                {
                    predictedCueTipVisual.position = predictedCueTipPosition;
                }
                
                if (predictedBridgeVisual != null)
                {
                    predictedBridgeVisual.position = predictedBridgePosition;
                }
            }
            
            // Fire events
            OnCompensationUpdated?.Invoke(compensatedCueTipPosition, compensatedCueRotation, compensatedBridgePosition);
            OnPredictionUpdated?.Invoke(predictedCueTipPosition, predictedBridgePosition);
        }
        
        /// <summary>
        /// Sets the visual latency compensation value.
        /// </summary>
        public void SetVisualLatency(float latency)
        {
            visualLatency = Mathf.Max(0f, latency);
        }
        
        /// <summary>
        /// Sets the prediction horizon.
        /// </summary>
        public void SetPredictionHorizon(float horizon)
        {
            predictionHorizon = Mathf.Max(0f, horizon);
        }
        
        /// <summary>
        /// Enables or disables Kalman prediction.
        /// </summary>
        public void SetUseKalmanPrediction(bool use)
        {
            useKalmanPrediction = use;
        }
        
        /// <summary>
        /// Gets the current compensation magnitude.
        /// </summary>
        public float GetCompensationMagnitude()
        {
            return Vector3.Distance(lastCueTipPos, compensatedCueTipPosition);
        }
        
        /// <summary>
        /// Gets the current prediction magnitude.
        /// </summary>
        public float GetPredictionMagnitude()
        {
            return Vector3.Distance(compensatedCueTipPosition, predictedCueTipPosition);
        }
        
        /// <summary>
        /// Creates a material using the URP Lit shader. Falls back to Standard if URP is unavailable.
        /// </summary>
        private Material CreatePredictionMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                // Fallback to Unlit if URP Lit is not found (Unlit renders correctly in URP; Standard would be pink).
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!enableCompensation) return;
            
            // Draw velocity vectors
            Gizmos.color = Color.red;
            Gizmos.DrawRay(lastCueTipPos, cueTipVelocity * 0.1f);
            Gizmos.DrawRay(lastBridgePos, bridgeHandVelocity * 0.1f);
            
            // Draw compensated positions
            Gizmos.color = compensationColor;
            Gizmos.DrawWireSphere(compensatedCueTipPosition, 0.025f);
            Gizmos.DrawWireSphere(compensatedBridgePosition, 0.03f);
            Gizmos.DrawLine(lastCueTipPos, compensatedCueTipPosition);
            Gizmos.DrawLine(lastBridgePos, compensatedBridgePosition);
            
            // Draw predicted positions
            Gizmos.color = predictionColor;
            Gizmos.DrawWireSphere(predictedCueTipPosition, 0.03f);
            Gizmos.DrawWireSphere(predictedBridgePosition, 0.035f);
            Gizmos.DrawLine(compensatedCueTipPosition, predictedCueTipPosition);
            Gizmos.DrawLine(compensatedBridgePosition, predictedBridgePosition);
            
            // Draw cue line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(compensatedBridgePosition, compensatedCueTipPosition);
        }
    }
}