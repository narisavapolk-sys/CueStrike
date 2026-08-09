using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.NoirMemory
{
    public class NoirMemoryAIMemory : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NoirMemoryPuzzleManager memoryManager;
        [SerializeField] private CueStrikePhysicsManager physicsManager;

        [Header("Memory Settings")]
        [SerializeField] [Range(0f, 1f)] private float memoryAccuracy = 0.85f;

        // What AI remembers: ballId -> remembers (true/false)
        private Dictionary<int, bool> rememberedBalls = new Dictionary<int, bool>();
        private Dictionary<int, Vector3> rememberedPositions = new Dictionary<int, Vector3>();

        private void Start()
        {
            if (memoryManager != null)
            {
                memoryManager.OnRevealPhaseStarted += OnRevealPhaseStarted;
                memoryManager.OnNoirPhaseStarted += OnNoirPhaseStarted;
            }
        }

        private void OnDestroy()
        {
            if (memoryManager != null)
            {
                memoryManager.OnRevealPhaseStarted -= OnRevealPhaseStarted;
                memoryManager.OnNoirPhaseStarted -= OnNoirPhaseStarted;
            }
        }

        private void OnRevealPhaseStarted()
        {
            // AI "memorizes" all ball positions during reveal
            MemorizeTable();
        }

        private void OnNoirPhaseStarted()
        {
            // AI ready to use memory
        }

        private void MemorizeTable()
        {
            rememberedBalls.Clear();
            rememberedPositions.Clear();

            int ballCount = physicsManager?.GetBallCount() ?? 16;
            for (int i = 0; i < ballCount; i++)
            {
                var ball = physicsManager?.GetBallById(i);
                if (ball == null) continue;

                // AI remembers based on accuracy (not always perfect)
                bool remembers = Random.value < memoryAccuracy;
                rememberedBalls[i] = remembers;
                
                if (remembers)
                {
                    rememberedPositions[i] = ball.transform.position;
                }
            }

            Debug.Log($"[NoirAI] Memorized {CountRemembered()} balls with {memoryAccuracy:P0} accuracy");
        }

        private int CountRemembered()
        {
            int count = 0;
            foreach (var kvp in rememberedBalls)
            {
                if (kvp.Value) count++;
            }
            return count;
        }

        /// <summary>
        /// Ask AI if it remembers a specific ball
        /// </summary>
        public bool DoesAIRememberBall(int ballId)
        {
            return rememberedBalls.ContainsKey(ballId) && rememberedBalls[ballId];
        }

        /// <summary>
        /// Ask AI which ball to target (from memory)
        /// Returns ballId AI thinks is correct, or -1 if doesn't know
        /// </summary>
        public int GetAITargetBallId(int intendedBallId)
        {
            // If AI remembers the intended ball -> target it
            if (DoesAIRememberBall(intendedBallId))
                return intendedBallId;

            // Otherwise pick from remembered balls
            var remembered = new List<int>();
            foreach (var kvp in rememberedBalls)
            {
                if (kvp.Value) remembered.Add(kvp.Key);
            }

            if (remembered.Count > 0)
                return remembered[Random.Range(0, remembered.Count)];

            return -1; // AI doesn't remember anything -> random guess
        }

        public void SetMemoryAccuracy(float accuracy)
        {
            memoryAccuracy = Mathf.Clamp01(accuracy);
        }
    }
}