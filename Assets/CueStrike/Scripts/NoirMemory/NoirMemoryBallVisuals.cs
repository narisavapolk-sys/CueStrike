using UnityEngine;

namespace CueStrike.NoirMemory
{
    public class NoirMemoryBallVisuals : MonoBehaviour
    {
        [SerializeField] private int ballId;
        private NoirMemoryPuzzleManager manager;
        private bool hasReportedHitThisShot = false;

        public void Setup(int id, NoirMemoryPuzzleManager mgr)
        {
            ballId = id;
            manager = mgr;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (manager == null) return;
            if (!manager.IsMemoryModeActive() || !manager.IsNoirPhase()) return;
            if (hasReportedHitThisShot) return;

            // Check if hit by cue ball
            if (IsCueBall(collision.gameObject))
            {
                hasReportedHitThisShot = true;
                manager.OnBallHitByCue(ballId);
            }
        }

        public void ResetHitFlag()
        {
            hasReportedHitThisShot = false;
        }

        private bool IsCueBall(GameObject go)
        {
            return go.name.Contains("CueBall", System.StringComparison.OrdinalIgnoreCase) 
                || go.name.Contains("cue_ball", System.StringComparison.OrdinalIgnoreCase)
                || go.name.Contains("WhiteBall", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}