using UnityEngine;
using CueStrike;

namespace CueStrike
{
    /// <summary>
    /// Identity component for billiard balls.
    /// </summary>
    public class BallIdentity : MonoBehaviour
    {
        [Header("Ball Identity")]
        public int ballId = 0; // 0 = cue ball, 1-15 = object balls, 16-21 = snooker colors
        public string ballName = "Ball";
        public BallType type = BallType.ObjectBall;

        public enum BallType
        {
            CueBall = 0,
            ObjectBall = 1,
            RedBall = 2,
            ColorBall = 3,
            EightBall = 4
        }
    }
}