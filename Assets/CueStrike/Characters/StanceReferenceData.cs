using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Holds reference values for the standard player stance.
    /// Used by CharacterIKAssist to configure the IK targets.
    /// </summary>
    [CreateAssetMenu(fileName = "StanceReference", menuName = "CueStrike/Stance Reference", order = 1)]
    public class StanceReferenceData : ScriptableObject
    {
        [Header("Elbow & Wrist")]
        [Tooltip("Target elbow angle in degrees (default 90° – straight).")]
        public float elbowAngle = 90f;

        [Header("Spine")]
        [Tooltip("Target spine bend angle in degrees (default 30°).")]
        public float spineBendAngle = 30f;

        [Header("Bridge & Grip Offsets")]
        [Tooltip("Distance from cue tip to left hand bridge point (meters).")]
        public float bridgeDistance = 0.35f;

        [Tooltip("Distance from cue butt to right hand grip point (meters).")]
        public float gripOffsetFromButt = 0.15f;
    }
}