using UnityEngine;
using System;

namespace CueStrike.RCA
{
    /// <summary>
    /// Kalman filter predictor for smoothing and predicting hand/cue positions.
    /// Reduces latency and jitter in hand tracking for precise cue alignment.
    /// </summary>
    public class CueStrikeKalmanPredictor : MonoBehaviour
    {
        [Header("Kalman Filter Settings")]
        [SerializeField] private float processNoise = 0.01f;      // Q - Process noise covariance
        [SerializeField] private float measurementNoise = 0.1f;   // R - Measurement noise covariance
        [SerializeField] private float estimationError = 1.0f;    // P - Initial estimation error covariance
        [SerializeField] private float predictionTimeStep = 0.016f; // dt - Prediction time step (seconds)
        
        [Header("Prediction Settings")]
        [SerializeField] private int maxPredictionSteps = 3;      // Max frames to predict ahead
        [SerializeField] private float maxVelocity = 5f;          // Maximum expected velocity (m/s)
        [SerializeField] private float maxAcceleration = 20f;     // Maximum expected acceleration (m/s^2)
        
        [Header("State")]
        [SerializeField] private Vector3 estimatedPosition = Vector3.zero;
        [SerializeField] private Vector3 estimatedVelocity = Vector3.zero;
        [SerializeField] private Vector3 estimatedAcceleration = Vector3.zero;
        [SerializeField] private float currentEstimationError = 1.0f;
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private Vector3 lastMeasurement = Vector3.zero;
        [SerializeField] private float lastUpdateTime = 0f;
        
        [Header("Prediction Output")]
        [SerializeField] private Vector3 predictedPosition = Vector3.zero;
        [SerializeField] private Vector3 predictedVelocity = Vector3.zero;
        [SerializeField] private int predictionSteps = 0;
        
        // Kalman Filter State (Constant Velocity Model)
        // State vector: [x, y, z, vx, vy, vz]
        // State transition matrix F (constant velocity model)
        // Measurement matrix H (position only)
        
        public Vector3 EstimatedPosition => estimatedPosition;
        public Vector3 EstimatedVelocity => estimatedVelocity;
        public Vector3 EstimatedAcceleration => estimatedAcceleration;
        public Vector3 PredictedPosition => predictedPosition;
        public Vector3 PredictedVelocity => predictedVelocity;
        public bool IsInitialized => isInitialized;
        public float CurrentEstimationError => currentEstimationError;
        public int PredictionSteps => predictionSteps;
        
        public System.Action<Vector3, Vector3> OnPredictionUpdated; // predictedPos, predictedVel
        public System.Action<Vector3, Vector3> OnEstimateUpdated;   // estimatedPos, estimatedVel
        
        private void Awake()
        {
            ResetFilter();
        }
        
        /// <summary>
        /// Resets the Kalman filter to initial state.
        /// </summary>
        public void ResetFilter()
        {
            estimatedPosition = Vector3.zero;
            estimatedVelocity = Vector3.zero;
            estimatedAcceleration = Vector3.zero;
            currentEstimationError = estimationError;
            isInitialized = false;
            lastMeasurement = Vector3.zero;
            lastUpdateTime = Time.time;
            predictedPosition = Vector3.zero;
            predictedVelocity = Vector3.zero;
            predictionSteps = 0;
        }
        
        /// <summary>
        /// Initializes the filter with a starting position.
        /// </summary>
        public void Initialize(Vector3 initialPosition)
        {
            estimatedPosition = initialPosition;
            estimatedVelocity = Vector3.zero;
            estimatedAcceleration = Vector3.zero;
            currentEstimationError = estimationError;
            isInitialized = true;
            lastMeasurement = initialPosition;
            lastUpdateTime = Time.time;
            predictedPosition = initialPosition;
            predictedVelocity = Vector3.zero;
        }
        
        /// <summary>
        /// Updates the filter with a new position measurement.
        /// </summary>
        public void UpdateMeasurement(Vector3 measurement)
        {
            if (!isInitialized)
            {
                Initialize(measurement);
                return;
            }
            
            float dt = Time.time - lastUpdateTime;
            if (dt <= 0f) dt = predictionTimeStep;
            lastUpdateTime = Time.time;
            
            // --- PREDICT STEP ---
            // State prediction: x_k|k-1 = F * x_k-1|k-1
            // For constant velocity model:
            // pos_new = pos_old + vel_old * dt
            // vel_new = vel_old
            Vector3 predictedPos = estimatedPosition + estimatedVelocity * dt;
            Vector3 predictedVel = estimatedVelocity;
            
            // Error covariance prediction: P_k|k-1 = F * P_k-1|k-1 * F^T + Q
            // Simplified: P increases by process noise
            float predictedError = currentEstimationError + processNoise * dt;
            
            // --- UPDATE STEP ---
            // Innovation (measurement residual): y = z - H * x_k|k-1
            Vector3 innovation = measurement - predictedPos;
            
            // Innovation covariance: S = H * P_k|k-1 * H^T + R
            float innovationCovariance = predictedError + measurementNoise;
            
            // Kalman Gain: K = P_k|k-1 * H^T * S^-1
            float kalmanGain = predictedError / innovationCovariance;
            
            // State update: x_k|k = x_k|k-1 + K * y
            estimatedPosition = predictedPos + innovation * kalmanGain;
            estimatedVelocity = predictedVel + (innovation / dt) * kalmanGain * 0.5f; // Velocity correction
            
            // Clamp velocity
            estimatedVelocity = Vector3.ClampMagnitude(estimatedVelocity, maxVelocity);
            
            // Error covariance update: P_k|k = (I - K * H) * P_k|k-1
            currentEstimationError = (1f - kalmanGain) * predictedError;
            
            lastMeasurement = measurement;
            
            // --- PREDICT FUTURE STEPS ---
            PredictFutureSteps(predictionTimeStep * maxPredictionSteps);
            
            // Fire events
            OnEstimateUpdated?.Invoke(estimatedPosition, estimatedVelocity);
            OnPredictionUpdated?.Invoke(predictedPosition, predictedVelocity);
        }
        
        /// <summary>
        /// Predicts future position and velocity for a given time horizon.
        /// </summary>
        public void PredictFutureSteps(float timeHorizon)
        {
            predictionSteps = Mathf.RoundToInt(timeHorizon / predictionTimeStep);
            predictionSteps = Mathf.Clamp(predictionSteps, 1, maxPredictionSteps);
            
            predictedPosition = estimatedPosition;
            predictedVelocity = estimatedVelocity;
            
            for (int i = 0; i < predictionSteps; i++)
            {
                predictedPosition += predictedVelocity * predictionTimeStep;
                // Velocity remains constant in constant velocity model
            }
        }
        
        /// <summary>
        /// Gets predicted position at a specific time in the future.
        /// </summary>
        public Vector3 GetPredictedPosition(float timeAhead)
        {
            int steps = Mathf.RoundToInt(timeAhead / predictionTimeStep);
            steps = Mathf.Clamp(steps, 0, maxPredictionSteps);
            
            Vector3 pos = estimatedPosition;
            Vector3 vel = estimatedVelocity;
            
            for (int i = 0; i < steps; i++)
            {
                pos += vel * predictionTimeStep;
            }
            
            return pos;
        }
        
        /// <summary>
        /// Gets predicted velocity at a specific time in the future.
        /// </summary>
        public Vector3 GetPredictedVelocity(float timeAhead)
        {
            // In constant velocity model, velocity doesn't change
            return estimatedVelocity;
        }
        
        /// <summary>
        /// Updates filter with velocity measurement (if available from hand tracking).
        /// </summary>
        public void UpdateWithVelocity(Vector3 position, Vector3 velocity)
        {
            if (!isInitialized)
            {
                Initialize(position);
                estimatedVelocity = velocity;
                return;
            }
            
            float dt = Time.time - lastUpdateTime;
            if (dt <= 0f) dt = predictionTimeStep;
            lastUpdateTime = Time.time;
            
            // Predict
            Vector3 predictedPos = estimatedPosition + estimatedVelocity * dt;
            Vector3 predictedVel = estimatedVelocity;
            float predictedError = currentEstimationError + processNoise * dt;
            
            // Update position
            Vector3 posInnovation = position - predictedPos;
            float posKalmanGain = predictedError / (predictedError + measurementNoise);
            
            estimatedPosition = predictedPos + posInnovation * posKalmanGain;
            
            // Update velocity (using velocity measurement)
            Vector3 velInnovation = velocity - predictedVel;
            float velMeasurementNoise = measurementNoise * 2f; // Velocity typically noisier
            float velKalmanGain = predictedError / (predictedError + velMeasurementNoise);
            
            estimatedVelocity = predictedVel + velInnovation * velKalmanGain;
            estimatedVelocity = Vector3.ClampMagnitude(estimatedVelocity, maxVelocity);
            
            // Update error
            currentEstimationError = (1f - posKalmanGain) * predictedError;
            
            lastMeasurement = position;
            
            PredictFutureSteps(predictionTimeStep * maxPredictionSteps);
            
            OnEstimateUpdated?.Invoke(estimatedPosition, estimatedVelocity);
            OnPredictionUpdated?.Invoke(predictedPosition, predictedVelocity);
        }
        
        /// <summary>
        /// Sets the process noise (Q) - higher = more responsive to changes.
        /// </summary>
        public void SetProcessNoise(float noise)
        {
            processNoise = Mathf.Max(0.001f, noise);
        }
        
        /// <summary>
        /// Sets the measurement noise (R) - higher = trust measurements less.
        /// </summary>
        public void SetMeasurementNoise(float noise)
        {
            measurementNoise = Mathf.Max(0.001f, noise);
        }
        
        /// <summary>
        /// Sets the maximum prediction steps.
        /// </summary>
        public void SetMaxPredictionSteps(int steps)
        {
            maxPredictionSteps = Mathf.Max(1, steps);
        }
        
        /// <summary>
        /// Gets the current filter confidence (inverse of estimation error).
        /// </summary>
        public float GetConfidence()
        {
            return Mathf.Clamp01(1f / (1f + currentEstimationError));
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!isInitialized) return;
            
            // Draw estimated position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(estimatedPosition, 0.02f);
            Gizmos.DrawRay(estimatedPosition, estimatedVelocity * 0.1f);
            
            // Draw predicted position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(predictedPosition, 0.025f);
            Gizmos.DrawRay(predictedPosition, predictedVelocity * 0.1f);
            
            // Draw last measurement
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastMeasurement, 0.015f);
            
            // Draw prediction path
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Vector3 prevPos = estimatedPosition;
            Vector3 predPos = estimatedPosition;
            Vector3 predVel = estimatedVelocity;
            
            for (int i = 0; i < predictionSteps; i++)
            {
                predPos += predVel * predictionTimeStep;
                Gizmos.DrawLine(prevPos, predPos);
                prevPos = predPos;
            }
        }
    }
}