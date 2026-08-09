using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Gameplay.Practice
{
    /// <summary>
    /// Practice routine types.
    /// </summary>
    public enum PracticeRoutine
    {
        FreePlacement,
        LineUp,
        DZoneClearance,
        CushionKiss,
        AroundTheBlack,
        SpiralCurve,
        StraightIn,
        CutShots,
        FollowDraw,
        SideSpin,
        PositionPlay,
        BreakPractice,
        SafetyPlay,
        PatternPlay,
        PressureDrills,
        CustomBuilder
    }

    /// <summary>
    /// BallPositionData is now defined in CueStrike.Gameplay.SaveSystem.CueStrikeSaveData
    /// Use CueStrike.Gameplay.SaveSystem.BallPositionData instead.
    /// </summary>
    [System.Obsolete("Use CueStrike.Gameplay.SaveSystem.BallPositionData instead")]
    public class BallPositionData
    {
        // This class is kept for backward compatibility but is deprecated.
        // Use CueStrike.Gameplay.SaveSystem.BallPositionData instead.
    }

    /// <summary>
    /// Vector3Serializable is now defined in CueStrike.Gameplay.SaveSystem.CueStrikeSaveData
    /// Use CueStrike.Gameplay.SaveSystem.Vector3Serializable instead.
    /// </summary>
    [System.Obsolete("Use CueStrike.Gameplay.SaveSystem.Vector3Serializable instead")]
    public struct Vector3Serializable
    {
        // This struct is kept for backward compatibility but is deprecated.
        // Use CueStrike.Gameplay.SaveSystem.Vector3Serializable instead.
        public float x, y, z;
        
        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }
        
        public Vector3 ToVector3() => new Vector3(x, y, z);
        public static implicit operator Vector3(Vector3Serializable v) => v.ToVector3();
        public static implicit operator Vector3Serializable(Vector3 v) => new Vector3Serializable(v.x, v.y, v.z);
    }
}
