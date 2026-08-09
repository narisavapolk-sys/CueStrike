using UnityEngine;

namespace CueStrike
{
    /// <summary>
    /// Ball identity and physics component.
    /// </summary>
    public class CueStrikeBall : MonoBehaviour
    {
        public enum BallType { CueBall, ObjectBall, RedBall, ColorBall, EightBall }

        [Header("Ball Identity")]
        public int BallId = 0;
        public string BallName = "Ball";
        public BallType Type = BallType.ObjectBall;

        [Header("State")]
        [SerializeField] private bool _isPocketed = false;

        /// <summary>
        /// Whether this ball has been pocketed.
        /// </summary>
        public bool IsPocketed => _isPocketed;

        /// <summary>
        /// Set the pocketed state of this ball.
        /// </summary>
        public void SetPocketed(bool pocketed)
        {
            _isPocketed = pocketed;

            // Hide/show the ball visually
            if (TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.enabled = !pocketed;
            }

            // Disable collider when pocketed
            if (TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = !pocketed;
            }

            // Stop rigidbody when pocketed
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = pocketed;
                if (pocketed)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}