using UnityEngine;
using System.Collections.Generic;

namespace CueStrike.RCA
{
    /// <summary>
    /// Calibration system for Controller-less RCA (Real Cue Alignment).
    /// Handles room-scale calibration, cue offset calibration, and hand-to-cue alignment.
    /// </summary>
    public class CueStrikeRCACalibrator : MonoBehaviour
    {
        [Header("Calibration Settings")]
        [SerializeField] private bool autoCalibrateOnStart = true;
        [SerializeField] private float calibrationDistance = 2.0f;
        [SerializeField] private float handAlignmentTolerance = 0.02f;
        [SerializeField] private int calibrationSamplesRequired = 30;
        [SerializeField] private float cueLength = 1.45f; // Standard cue length in meters
        
        [Header("References")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;
        [SerializeField] private Transform cueVisualTransform;
        
        [Header("Calibration Points")]
        [SerializeField] private Transform calibrationTarget;
        [SerializeField] private Transform cueTipTarget;
        [SerializeField] private Transform bridgeHandTarget;
        
        [Header("Calibration Data")]
        [SerializeField] private Vector3 cueOffsetFromRightHand = new Vector3(0.02f, -0.05f, 0.1f);
        [SerializeField] private Quaternion cueRotationOffsetFromRightHand = Quaternion.Euler(0, 90, 0);
        [SerializeField] private Vector3 bridgeHandOffsetFromLeftHand = new Vector3(-0.05f, -0.02f, 0.05f);
        [SerializeField] private float calibratedCueLength = 1.45f;
        
        [Header("Calibration State")]
        [SerializeField] private CalibrationState currentState = CalibrationState.NotStarted;
        [SerializeField] private int currentSampleCount = 0;
        [SerializeField] private List<Vector3> rightHandSamples = new List<Vector3>();
        [SerializeField] private List<Quaternion> rightHandRotationSamples = new List<Quaternion>();
        [SerializeField] private List<Vector3> leftHandSamples = new List<Vector3>();
        
        public enum CalibrationState
        {
            NotStarted,
            WaitingForHeadPosition,
            AligningRightHand,
            AligningLeftHand,
            MeasuringCueLength,
            Finalizing,
            Completed,
            Failed
        }
        
        public CalibrationState CurrentState => currentState;
        public bool IsCalibrated => currentState == CalibrationState.Completed;
        public Vector3 CueOffsetFromRightHand => cueOffsetFromRightHand;
        public Quaternion CueRotationOffsetFromRightHand => cueRotationOffsetFromRightHand;
        public Vector3 BridgeHandOffsetFromLeftHand => bridgeHandOffsetFromLeftHand;
        public float CalibratedCueLength => calibratedCueLength;
        
        public System.Action<CalibrationState> OnCalibrationStateChanged;
        public System.Action OnCalibrationCompleted;
        public System.Action OnCalibrationFailed;
        
        private void Start()
        {
            if (autoCalibrateOnStart)
            {
                StartCalibration();
            }
        }
        
        /// <summary>
        /// Starts the full calibration sequence.
        /// </summary>
        public void StartCalibration()
        {
            ResetCalibration();
            SetState(CalibrationState.WaitingForHeadPosition);
        }
        
        /// <summary>
        /// Resets all calibration data.
        /// </summary>
        public void ResetCalibration()
        {
            currentSampleCount = 0;
            rightHandSamples.Clear();
            rightHandRotationSamples.Clear();
            leftHandSamples.Clear();
            cueOffsetFromRightHand = new Vector3(0.02f, -0.05f, 0.1f);
            cueRotationOffsetFromRightHand = Quaternion.Euler(0, 90, 0);
            bridgeHandOffsetFromLeftHand = new Vector3(-0.05f, -0.02f, 0.05f);
            calibratedCueLength = cueLength;
            SetState(CalibrationState.NotStarted);
        }
        
        private void Update()
        {
            switch (currentState)
            {
                case CalibrationState.WaitingForHeadPosition:
                    UpdateWaitingForHeadPosition();
                    break;
                case CalibrationState.AligningRightHand:
                    UpdateAligningRightHand();
                    break;
                case CalibrationState.AligningLeftHand:
                    UpdateAligningLeftHand();
                    break;
                case CalibrationState.MeasuringCueLength:
                    UpdateMeasuringCueLength();
                    break;
                case CalibrationState.Finalizing:
                    UpdateFinalizing();
                    break;
            }
        }
        
        private void UpdateWaitingForHeadPosition()
        {
            if (headTransform == null)
            {
                Debug.LogWarning("[RCACalibrator] Head transform not assigned!");
                SetState(CalibrationState.Failed);
                return;
            }
            
            // Wait for user to look at calibration target
            if (calibrationTarget != null)
            {
                Vector3 directionToTarget = (calibrationTarget.position - headTransform.position).normalized;
                float angle = Vector3.Angle(headTransform.forward, directionToTarget);
                
                if (angle < 15f)
                {
                    SetState(CalibrationState.AligningRightHand);
                }
            }
            else
            {
                // No target, auto-proceed after a moment
                SetState(CalibrationState.AligningRightHand);
            }
        }
        
        private void UpdateAligningRightHand()
        {
            if (rightHandTransform == null || cueTipTarget == null)
            {
                Debug.LogWarning("[RCACalibrator] Right hand or cue tip target not assigned!");
                SetState(CalibrationState.Failed);
                return;
            }
            
            // User aligns right hand (cue hand) to cue tip target
            float distance = Vector3.Distance(rightHandTransform.position, cueTipTarget.position);
            
            if (distance < handAlignmentTolerance)
            {
                // Collect samples
                rightHandSamples.Add(rightHandTransform.position);
                rightHandRotationSamples.Add(rightHandTransform.rotation);
                currentSampleCount++;
                
                if (currentSampleCount >= calibrationSamplesRequired)
                {
                    // Calculate average offset
                    CalculateRightHandOffset();
                    currentSampleCount = 0;
                    rightHandSamples.Clear();
                    rightHandRotationSamples.Clear();
                    SetState(CalibrationState.AligningLeftHand);
                }
            }
        }
        
        private void UpdateAligningLeftHand()
        {
            if (leftHandTransform == null || bridgeHandTarget == null)
            {
                Debug.LogWarning("[RCACalibrator] Left hand or bridge hand target not assigned!");
                SetState(CalibrationState.Failed);
                return;
            }
            
            // User aligns left hand (bridge hand) to bridge target
            float distance = Vector3.Distance(leftHandTransform.position, bridgeHandTarget.position);
            
            if (distance < handAlignmentTolerance)
            {
                leftHandSamples.Add(leftHandTransform.position);
                currentSampleCount++;
                
                if (currentSampleCount >= calibrationSamplesRequired)
                {
                    CalculateLeftHandOffset();
                    currentSampleCount = 0;
                    leftHandSamples.Clear();
                    SetState(CalibrationState.MeasuringCueLength);
                }
            }
        }
        
        private void UpdateMeasuringCueLength()
        {
            if (rightHandTransform == null || leftHandTransform == null)
            {
                SetState(CalibrationState.Failed);
                return;
            }
            
            // Measure distance between hands when holding cue
            float measuredLength = Vector3.Distance(rightHandTransform.position, leftHandTransform.position);
            
            // Collect multiple samples for accuracy
            leftHandSamples.Add(leftHandTransform.position);
            rightHandSamples.Add(rightHandTransform.position);
            currentSampleCount++;
            
            if (currentSampleCount >= calibrationSamplesRequired)
            {
                // Average the measurements
                float sum = 0f;
                for (int i = 0; i < calibrationSamplesRequired; i++)
                {
                    sum += Vector3.Distance(rightHandSamples[i], leftHandSamples[i]);
                }
                calibratedCueLength = sum / calibrationSamplesRequired;
                
                // Clamp to reasonable range
                calibratedCueLength = Mathf.Clamp(calibratedCueLength, 1.0f, 1.6f);
                
                currentSampleCount = 0;
                leftHandSamples.Clear();
                rightHandSamples.Clear();
                SetState(CalibrationState.Finalizing);
            }
        }
        
        private void UpdateFinalizing()
        {
            // Apply calibration to visual cue
            ApplyCalibrationToVisual();
            
            // Save calibration data
            SaveCalibrationData();
            
            SetState(CalibrationState.Completed);
        }
        
        private void CalculateRightHandOffset()
        {
            if (rightHandSamples.Count == 0 || cueTipTarget == null) return;
            
            Vector3 avgPosition = Vector3.zero;
            Quaternion avgRotation = Quaternion.identity;
            
            foreach (var pos in rightHandSamples)
                avgPosition += pos;
            avgPosition /= rightHandSamples.Count;
            
            // Average rotation
            Vector3 avgEuler = Vector3.zero;
            foreach (var rot in rightHandRotationSamples)
                avgEuler += rot.eulerAngles;
            avgEuler /= rightHandRotationSamples.Count;
            avgRotation = Quaternion.Euler(avgEuler);
            
            // Calculate offset from hand to cue tip
            cueOffsetFromRightHand = cueTipTarget.position - avgPosition;
            cueRotationOffsetFromRightHand = Quaternion.Inverse(avgRotation) * cueTipTarget.rotation;
        }
        
        private void CalculateLeftHandOffset()
        {
            if (leftHandSamples.Count == 0 || bridgeHandTarget == null) return;
            
            Vector3 avgPosition = Vector3.zero;
            foreach (var pos in leftHandSamples)
                avgPosition += pos;
            avgPosition /= leftHandSamples.Count;
            
            bridgeHandOffsetFromLeftHand = bridgeHandTarget.position - avgPosition;
        }
        
        private void ApplyCalibrationToVisual()
        {
            if (cueVisualTransform == null || rightHandTransform == null) return;
            
            // Position and rotate cue visual based on calibrated right hand
            Vector3 cuePosition = rightHandTransform.position + rightHandTransform.TransformDirection(cueOffsetFromRightHand);
            Quaternion cueRotation = rightHandTransform.rotation * cueRotationOffsetFromRightHand;
            
            cueVisualTransform.position = cuePosition;
            cueVisualTransform.rotation = cueRotation;
        }
        
        private void SaveCalibrationData()
        {
            // Save to PlayerPrefs for persistence
            PlayerPrefs.SetFloat("RCA_CueLength", calibratedCueLength);
            PlayerPrefs.SetString("RCA_RightHandOffset", JsonUtility.ToJson(cueOffsetFromRightHand));
            PlayerPrefs.SetString("RCA_RightHandRotOffset", JsonUtility.ToJson(cueRotationOffsetFromRightHand));
            PlayerPrefs.SetString("RCA_LeftHandOffset", JsonUtility.ToJson(bridgeHandOffsetFromLeftHand));
            PlayerPrefs.SetInt("RCA_Calibrated", 1);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Loads saved calibration data from PlayerPrefs.
        /// </summary>
        public void LoadCalibrationData()
        {
            if (PlayerPrefs.GetInt("RCA_Calibrated", 0) == 1)
            {
                calibratedCueLength = PlayerPrefs.GetFloat("RCA_CueLength", cueLength);
                cueOffsetFromRightHand = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("RCA_RightHandOffset", JsonUtility.ToJson(cueOffsetFromRightHand)));
                cueRotationOffsetFromRightHand = JsonUtility.FromJson<Quaternion>(PlayerPrefs.GetString("RCA_RightHandRotOffset", JsonUtility.ToJson(cueRotationOffsetFromRightHand)));
                bridgeHandOffsetFromLeftHand = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("RCA_LeftHandOffset", JsonUtility.ToJson(bridgeHandOffsetFromLeftHand)));
                
                ApplyCalibrationToVisual();
                SetState(CalibrationState.Completed);
            }
        }
        
        /// <summary>
        /// Gets the calibrated cue tip position based on right hand.
        /// </summary>
        public Vector3 GetCalibratedCueTipPosition()
        {
            if (rightHandTransform == null) return Vector3.zero;
            return rightHandTransform.position + rightHandTransform.TransformDirection(cueOffsetFromRightHand);
        }
        
        /// <summary>
        /// Gets the calibrated cue rotation based on right hand.
        /// </summary>
        public Quaternion GetCalibratedCueRotation()
        {
            if (rightHandTransform == null) return Quaternion.identity;
            return rightHandTransform.rotation * cueRotationOffsetFromRightHand;
        }
        
        /// <summary>
        /// Gets the calibrated bridge hand position based on left hand.
        /// </summary>
        public Vector3 GetCalibratedBridgeHandPosition()
        {
            if (leftHandTransform == null) return Vector3.zero;
            return leftHandTransform.position + leftHandTransform.TransformDirection(bridgeHandOffsetFromLeftHand);
        }
        
        /// <summary>
        /// Gets the cue direction vector (from bridge to tip).
        /// </summary>
        public Vector3 GetCueDirection()
        {
            Vector3 tip = GetCalibratedCueTipPosition();
            Vector3 bridge = GetCalibratedBridgeHandPosition();
            return (tip - bridge).normalized;
        }
        
        private void SetState(CalibrationState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            OnCalibrationStateChanged?.Invoke(currentState);
            
            if (currentState == CalibrationState.Completed)
            {
                OnCalibrationCompleted?.Invoke();
            }
            else if (currentState == CalibrationState.Failed)
            {
                OnCalibrationFailed?.Invoke();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (currentState == CalibrationState.AligningRightHand && cueTipTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(cueTipTarget.position, handAlignmentTolerance);
            }
            
            if (currentState == CalibrationState.AligningLeftHand && bridgeHandTarget != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(bridgeHandTarget.position, handAlignmentTolerance);
            }
            
            if (IsCalibrated && rightHandTransform != null && leftHandTransform != null)
            {
                Vector3 tip = GetCalibratedCueTipPosition();
                Vector3 bridge = GetCalibratedBridgeHandPosition();
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(bridge, tip);
                Gizmos.DrawWireSphere(tip, 0.02f);
                Gizmos.DrawWireSphere(bridge, 0.03f);
            }
        }
    }
}