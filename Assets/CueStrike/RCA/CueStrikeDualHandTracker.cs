using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

namespace CueStrike.RCA
{
    /// <summary>
    /// Dual hand tracking system for controller-less RCA.
    /// Tracks both hands using XR Hands subsystem for cue and bridge hand tracking.
    /// </summary>
    public class CueStrikeDualHandTracker : MonoBehaviour
    {
        [Header("XR Hands References")]
        [SerializeField] private XRHandSubsystem handSubsystem;
        [SerializeField] private bool useXRHands = true;
        
        [Header("Hand Transforms (Fallback)")]
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;
        
        [Header("Hand Tracking Settings")]
        [SerializeField] private float handTrackingUpdateRate = 60f;
        [SerializeField] private float handConfidenceThreshold = 0.5f;
        [SerializeField] private bool smoothHandPositions = true;
        [SerializeField] private float positionSmoothing = 0.1f;
        [SerializeField] private float rotationSmoothing = 0.1f;
        
        [Header("Hand Joint References")]
        [SerializeField] private Transform[] leftHandJoints = new Transform[26];
        [SerializeField] private Transform[] rightHandJoints = new Transform[26];
        
        [Header("Key Joint Indices (XR Hands)")]
        private const int WristJoint = 0;
        private const int PalmJoint = 1;
        private const int ThumbTipJoint = 4;
        private const int IndexTipJoint = 8;
        private const int MiddleTipJoint = 12;
        private const int RingTipJoint = 16;
        private const int LittleTipJoint = 20;
        
        [Header("Tracked Data")]
        [SerializeField] private HandData leftHandData = new HandData();
        [SerializeField] private HandData rightHandData = new HandData();
        
        [Header("Events")]
        public System.Action<HandData> OnLeftHandUpdated;
        public System.Action<HandData> OnRightHandUpdated;
        public System.Action<bool> OnLeftHandTracked;
        public System.Action<bool> OnRightHandTracked;
        
        private float lastUpdateTime = 0f;
        private Vector3 leftHandSmoothedPos;
        private Quaternion leftHandSmoothedRot;
        private Vector3 rightHandSmoothedPos;
        private Quaternion rightHandSmoothedRot;
        
        public HandData LeftHandData => leftHandData;
        public HandData RightHandData => rightHandData;
        public bool IsLeftHandTracked => leftHandData.isTracked;
        public bool IsRightHandTracked => rightHandData.isTracked;
        public Transform LeftHandTransform => leftHandTransform;
        public Transform RightHandTransform => rightHandTransform;
        
        private void Awake()
        {
            if (useXRHands)
            {
                InitializeXRHands();
            }
        }
        
        private void Start()
        {
            leftHandSmoothedPos = leftHandTransform != null ? leftHandTransform.position : Vector3.zero;
            leftHandSmoothedRot = leftHandTransform != null ? leftHandTransform.rotation : Quaternion.identity;
            rightHandSmoothedPos = rightHandTransform != null ? rightHandTransform.position : Vector3.zero;
            rightHandSmoothedRot = rightHandTransform != null ? rightHandTransform.rotation : Quaternion.identity;
        }
        
        private void InitializeXRHands()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            
            foreach (var subsystem in subsystems)
            {
                if (subsystem.running)
                {
                    handSubsystem = subsystem;
                    break;
                }
            }
            
            if (handSubsystem == null)
            {
                Debug.LogWarning("[DualHandTracker] No XR Hands subsystem found. Falling back to transform tracking.");
                useXRHands = false;
            }
        }
        
        private void Update()
        {
            if (Time.time - lastUpdateTime < 1f / handTrackingUpdateRate)
                return;
            
            lastUpdateTime = Time.time;
            
            if (useXRHands && handSubsystem != null)
            {
                UpdateFromXRHands();
            }
            else
            {
                UpdateFromTransforms();
            }
        }
        
        private void UpdateFromXRHands()
        {
            // Update left hand
            var leftHand = handSubsystem.leftHand;
            if (leftHand.isTracked)
            {
                UpdateHandFromXR(leftHand, ref leftHandData, true);
            }
            else
            {
                leftHandData.isTracked = false;
                OnLeftHandTracked?.Invoke(false);
            }
            
            // Update right hand
            var rightHand = handSubsystem.rightHand;
            if (rightHand.isTracked)
            {
                UpdateHandFromXR(rightHand, ref rightHandData, false);
            }
            else
            {
                rightHandData.isTracked = false;
                OnRightHandTracked?.Invoke(false);
            }
        }
        
        private void UpdateHandFromXR(XRHand hand, ref HandData handData, bool isLeft)
        {
            handData.isTracked = true;
            
            // Get wrist pose (root of hand)
            if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out var wristPose))
            {
                Vector3 position = wristPose.position;
                Quaternion rotation = wristPose.rotation;
                
                if (smoothHandPositions)
                {
                    if (isLeft)
                    {
                        leftHandSmoothedPos = Vector3.Lerp(leftHandSmoothedPos, position, 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime * 60f));
                        leftHandSmoothedRot = Quaternion.Slerp(leftHandSmoothedRot, rotation, 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime * 60f));
                        position = leftHandSmoothedPos;
                        rotation = leftHandSmoothedRot;
                    }
                    else
                    {
                        rightHandSmoothedPos = Vector3.Lerp(rightHandSmoothedPos, position, 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime * 60f));
                        rightHandSmoothedRot = Quaternion.Slerp(rightHandSmoothedRot, rotation, 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime * 60f));
                        position = rightHandSmoothedPos;
                        rotation = rightHandSmoothedRot;
                    }
                }
                
                handData.rootPosition = position;
                handData.rootRotation = rotation;
                
                // Update transform if assigned
                Transform handTransform = isLeft ? leftHandTransform : rightHandTransform;
                if (handTransform != null)
                {
                    handTransform.position = position;
                    handTransform.rotation = rotation;
                }
            }
            
            // Update joint positions
            UpdateJointPositions(hand, handData, isLeft);
            
            // Calculate derived data
            CalculateDerivedData(handData, isLeft);
            
            // Fire events
            if (isLeft)
            {
                OnLeftHandUpdated?.Invoke(handData);
                OnLeftHandTracked?.Invoke(true);
            }
            else
            {
                OnRightHandUpdated?.Invoke(handData);
                OnRightHandTracked?.Invoke(true);
            }
        }
        
        private void UpdateJointPositions(XRHand hand, HandData handData, bool isLeft)
        {
            Transform[] joints = isLeft ? leftHandJoints : rightHandJoints;
            
            // Update all tracked joints
            for (int i = 0; i < 26; i++)
            {
                XRHandJointID jointId = (XRHandJointID)i;
                if (hand.GetJoint(jointId).TryGetPose(out var jointPose))
                {
                    handData.jointPositions[i] = jointPose.position;
                    handData.jointRotations[i] = jointPose.rotation;
                    
                    if (joints[i] != null)
                    {
                        joints[i].position = jointPose.position;
                        joints[i].rotation = jointPose.rotation;
                    }
                }
            }
        }
        
        private void CalculateDerivedData(HandData handData, bool isLeft)
        {
            // Calculate palm position (average of key joints)
            Vector3 palmSum = Vector3.zero;
            int palmJointCount = 0;
            
            // Use wrist, palm, and finger bases for palm position
            int[] palmIndices = { 0, 1, 2, 5, 9, 13, 17 };
            foreach (int idx in palmIndices)
            {
                if (handData.jointPositions[idx] != Vector3.zero)
                {
                    palmSum += handData.jointPositions[idx];
                    palmJointCount++;
                }
            }
            
            if (palmJointCount > 0)
            {
                handData.palmPosition = palmSum / palmJointCount;
            }
            
            // Calculate finger tip positions
            handData.thumbTipPosition = handData.jointPositions[ThumbTipJoint];
            handData.indexTipPosition = handData.jointPositions[IndexTipJoint];
            handData.middleTipPosition = handData.jointPositions[MiddleTipJoint];
            handData.ringTipPosition = handData.jointPositions[RingTipJoint];
            handData.littleTipPosition = handData.jointPositions[LittleTipJoint];
            
            // Calculate pinch strength (thumb to index distance)
            float pinchDistance = Vector3.Distance(handData.thumbTipPosition, handData.indexTipPosition);
            handData.pinchStrength = Mathf.Clamp01(1f - pinchDistance / 0.05f);
            
            // Calculate grab strength (average of all fingertips to palm)
            float grabSum = 0f;
            int grabCount = 0;
            Vector3[] fingerTips = { handData.thumbTipPosition, handData.indexTipPosition, handData.middleTipPosition, handData.ringTipPosition, handData.littleTipPosition };
            
            foreach (var tip in fingerTips)
            {
                if (tip != Vector3.zero && handData.palmPosition != Vector3.zero)
                {
                    float dist = Vector3.Distance(tip, handData.palmPosition);
                    grabSum += Mathf.Clamp01(1f - dist / 0.1f);
                    grabCount++;
                }
            }
            
            if (grabCount > 0)
            {
                handData.grabStrength = grabSum / grabCount;
            }
            
            // Calculate pointing direction (index finger direction)
            if (handData.jointPositions[IndexTipJoint] != Vector3.zero && handData.jointPositions[7] != Vector3.zero)
            {
                handData.pointingDirection = (handData.jointPositions[IndexTipJoint] - handData.jointPositions[7]).normalized;
            }
            
            // Hand orientation (palm normal)
            if (handData.jointPositions[PalmJoint] != Vector3.zero && handData.jointPositions[WristJoint] != Vector3.zero)
            {
                Vector3 palmForward = (handData.jointPositions[PalmJoint] - handData.jointPositions[WristJoint]).normalized;
                Vector3 palmUp = Vector3.Cross(palmForward, isLeft ? Vector3.left : Vector3.right).normalized;
                handData.palmNormal = Vector3.Cross(palmUp, palmForward).normalized;
            }
        }
        
        private void UpdateFromTransforms()
        {
            // Fallback: use assigned transforms directly
            if (leftHandTransform != null)
            {
                leftHandData.isTracked = true;
                leftHandData.rootPosition = leftHandTransform.position;
                leftHandData.rootRotation = leftHandTransform.rotation;
                OnLeftHandUpdated?.Invoke(leftHandData);
                OnLeftHandTracked?.Invoke(true);
            }
            else
            {
                leftHandData.isTracked = false;
                OnLeftHandTracked?.Invoke(false);
            }
            
            if (rightHandTransform != null)
            {
                rightHandData.isTracked = true;
                rightHandData.rootPosition = rightHandTransform.position;
                rightHandData.rootRotation = rightHandTransform.rotation;
                OnRightHandUpdated?.Invoke(rightHandData);
                OnRightHandTracked?.Invoke(true);
            }
            else
            {
                rightHandData.isTracked = false;
                OnRightHandTracked?.Invoke(false);
            }
        }
        
        /// <summary>
        /// Gets the cue hand (right hand by default for right-handed players).
        /// </summary>
        public HandData GetCueHand()
        {
            return rightHandData;
        }
        
        /// <summary>
        /// Gets the bridge hand (left hand by default for right-handed players).
        /// </summary>
        public HandData GetBridgeHand()
        {
            return leftHandData;
        }
        
        /// <summary>
        /// Sets handedness (swaps left/right roles).
        /// </summary>
        public void SetHandedness(bool isLeftHanded)
        {
            // This would swap the roles of left/right hands for cue/bridge
            // Implementation depends on game design
        }
        
        private void OnDrawGizmosSelected()
        {
            if (leftHandData.isTracked)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(leftHandData.rootPosition, 0.03f);
                Gizmos.DrawRay(leftHandData.rootPosition, leftHandData.rootRotation * Vector3.forward * 0.1f);
                
                if (leftHandData.indexTipPosition != Vector3.zero)
                {
                    Gizmos.DrawWireSphere(leftHandData.indexTipPosition, 0.01f);
                }
            }
            
            if (rightHandData.isTracked)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(rightHandData.rootPosition, 0.03f);
                Gizmos.DrawRay(rightHandData.rootPosition, rightHandData.rootRotation * Vector3.forward * 0.1f);
                
                if (rightHandData.indexTipPosition != Vector3.zero)
                {
                    Gizmos.DrawWireSphere(rightHandData.indexTipPosition, 0.01f);
                }
            }
        }
    }
    
    /// <summary>
    /// Data structure for tracked hand information.
    /// </summary>
    [System.Serializable]
    public class HandData
    {
        public bool isTracked = false;
        public Vector3 rootPosition = Vector3.zero;
        public Quaternion rootRotation = Quaternion.identity;
        public Vector3 palmPosition = Vector3.zero;
        public Vector3 palmNormal = Vector3.up;
        public Vector3 pointingDirection = Vector3.forward;
        
        public Vector3 thumbTipPosition = Vector3.zero;
        public Vector3 indexTipPosition = Vector3.zero;
        public Vector3 middleTipPosition = Vector3.zero;
        public Vector3 ringTipPosition = Vector3.zero;
        public Vector3 littleTipPosition = Vector3.zero;
        
        public float pinchStrength = 0f;
        public float grabStrength = 0f;
        
        public Vector3[] jointPositions = new Vector3[26];
        public Quaternion[] jointRotations = new Quaternion[26];
        
        public float confidence = 1f;
        public float timestamp = 0f;
    }
}