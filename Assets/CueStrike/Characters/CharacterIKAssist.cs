using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace CueStrike.Characters
{
    /// <summary>
    /// Reads the StanceReferenceData ScriptableObject and drives the IK
    /// targets (hand, bridge, spine) to match the desired elbow and spine angles.
    /// This works with the Unity Animation Rigging setup created by
    /// CharacterAAASetup.cs.
    /// </summary>
    [ExecuteAlways]
    public class CharacterIKAssist : MonoBehaviour
    {
        [Header("References")]
        public StanceReferenceData stanceData;

        // Targets that will be driven by the rigging constraints
        public Transform leftHandIKTarget;
        public Transform rightHandIKTarget;
        public Transform headIKTarget;   // used for spine aim constraint

        private void Reset()
        {
            // Try to auto‑assign if the GameObject already has the expected children
            leftHandIKTarget = FindChildRecursive(transform, "LeftHand_IKTarget");
            rightHandIKTarget = FindChildRecursive(transform, "RightHand_IKTarget");
            headIKTarget = FindChildRecursive(transform, "Head_IKTarget");
        }

        private void Update()
        {
            if (stanceData == null) return;

            // ----- Elbow angle (affects hand targets) -----
            // The TwoBoneIK constraints will try to reach the target positions.
            // By rotating the targets we can influence the elbow angle.
            // A simple approach: set the target forward direction based on elbowAngle.

            if (leftHandIKTarget != null)
            {
                // Position target a bit in front of the character, then rotate around the elbow axis
                Quaternion rot = Quaternion.Euler(0f, 0f, stanceData.elbowAngle);
                leftHandIKTarget.localRotation = rot;
            }

            if (rightHandIKTarget != null)
            {
                Quaternion rot = Quaternion.Euler(0f, 0f, -stanceData.elbowAngle);
                rightHandIKTarget.localRotation = rot;
            }

            // ----- Spine bend (aim constraint) -----
            if (headIKTarget != null)
            {
                // Rotate the head target to make the spine bend towards the cue direction.
                // A positive spineBendAngle will tilt the head forward.
                headIKTarget.localRotation = Quaternion.Euler(stanceData.spineBendAngle, 0f, 0f);
            }
        }

        private Transform FindChildRecursive(Transform parent, string namePart)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains(namePart.ToLower()))
                    return child;
            }
            return null;
        }
    }
}