using UnityEngine;

namespace CueStrike.Gameplay.ChinesePool
{
    /// <summary>
    /// Stub component for Chinese 8-Ball AI decision making.
    /// Attach to the same GameObject as ChinesePoolGameManager or a dedicated AI controller.
    /// This is a stub implementation — replace with full AI logic (minimax, MCTS, or rule-based).
    /// </summary>
    public class ChinesePoolAIModifier : MonoBehaviour
    {
        #region Inspector Settings
        [Header("AI Behavior")]
        [Tooltip("Enable AI for Player 2 (index 1).")]
        public bool enableAI = true;

        [Tooltip("AI difficulty level.")]
        public AIDifficulty difficulty = AIDifficulty.Medium;

        [Tooltip("Delay before AI makes a decision (seconds). Simulates thinking time.")]
        public float decisionDelay = 1.0f;

        [Header("Shot Selection (Stub Weights)")]
        [Range(0f, 1f)] public float pottingPriority = 0.7f;
        [Range(0f, 1f)] public float safetyPriority = 0.2f;
        [Range(0f, 1f)] public float positionalPriority = 0.1f;

        [Header("Randomness")]
        [Tooltip("Random seed for reproducible AI behavior. 0 = random each session.")]
        public int randomSeed = 0;
        #endregion

        #region Enums
        public enum AIDifficulty
        {
            Easy = 0,
            Medium = 1,
            Hard = 2,
            Expert = 3
        }
        #endregion

        #region Internal State
        private System.Random rng;
        private ChinesePoolGameManager gameManager;
        private ChinesePoolBallSetup ballSetup;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            InitializeRNG();
            gameManager = FindFirstObjectByType<ChinesePoolGameManager>();
            ballSetup = FindFirstObjectByType<ChinesePoolBallSetup>();
        }

        void OnValidate()
        {
            InitializeRNG();
        }
        #endregion

        #region Public API

        /// <summary>
        /// Requests the AI to decide on a call shot (which ball and pocket).
        /// Call this from ChinesePoolGameManager when it's AI's turn.
        /// </summary>
        /// <returns>Tuple of (ballId, pocketId). Returns (-1, -1) if no valid shot found.</returns>
        public (int ballId, int pocketId) DecideCallShot()
        {
            if (gameManager == null || ballSetup == null)
            {
                Debug.LogWarning("[CueStrike] ChinesePoolAIModifier: Missing references. Cannot decide call shot.");
                return (-1, -1);
            }

            // Get current game state
            int playerIndex = gameManager.currentPlayerIndex;
            var currentPhase = gameManager.currentPhase;
            var playerGroup = gameManager.GetCurrentPlayerGroup();
            var ballsOnTable = ballSetup.GetBallsOnTable();

            // Filter available balls based on game rules
            var availableBalls = GetAvailableBalls(ballsOnTable, playerGroup, currentPhase);
            var availablePockets = GetAvailablePockets(); // All 6 pockets for now

            if (availableBalls.Length == 0 || availablePockets.Length == 0)
            {
                Debug.Log("[CueStrike] AI: No valid balls or pockets available.");
                return (-1, -1);
            }

            // STUB: Simple heuristic - pick first available ball and pocket
            // TODO: Replace with proper AI evaluation (angle, distance, safety, position play)
            int chosenBall = availableBalls[0];
            int chosenPocket = availablePockets[0];

            Debug.Log($"[CueStrike] AI (Player {playerIndex + 1}) calls: Ball {chosenBall} -> Pocket {chosenPocket} (STUB)");

            return (chosenBall, chosenPocket);
        }

        /// <summary>
        /// Requests the AI to decide shot parameters (aim point, power, spin).
        /// Call this after AI has decided on call shot.
        /// </summary>
        /// <returns>Tuple of (aimPoint, power [0-1], spin [Vector3]).</returns>
        public (Vector3 aimPoint, float power, Vector3 spin) DecideShotParameters(int calledBallId, int calledPocketId)
        {
            if (ballSetup == null)
                return (Vector3.zero, 0.5f, Vector3.zero);

            var cueBall = ballSetup.GetBallById(0);
            var targetBall = ballSetup.GetBallById(calledBallId);
            var pocketPos = GetPocketWorldPosition(calledPocketId);

            if (cueBall == null || targetBall == null || pocketPos == Vector3.zero)
            {
                Debug.LogWarning("[CueStrike] AI: Missing ball/pocket references for shot parameters.");
                return (Vector3.zero, 0.5f, Vector3.zero);
            }

            // STUB: Simple straight-shot calculation
            // TODO: Implement proper aim calculation with cut angles, spin, power control
            Vector3 ballToPocket = (pocketPos - targetBall.transform.position).normalized;
            float ballRadius = 0.028575f; // Standard ball radius
            Vector3 contactPoint = targetBall.transform.position - ballToPocket * (ballRadius * 2f);

            Vector3 aimPoint = contactPoint;
            float power = 0.5f; // Medium power
            Vector3 spin = Vector3.zero;

            Debug.Log($"[CueStrike] AI shot params: aim={aimPoint}, power={power}, spin={spin} (STUB)");

            return (aimPoint, power, spin);
        }

        /// <summary>
        /// Sets the AI difficulty and adjusts internal parameters.
        /// </summary>
        public void SetDifficulty(AIDifficulty newDifficulty)
        {
            difficulty = newDifficulty;
            ApplyDifficultySettings();
        }

        /// <summary>
        /// Returns the current AI difficulty.
        /// </summary>
        public AIDifficulty GetDifficulty() => difficulty;

        /// <summary>
        /// Toggles AI enabled state.
        /// </summary>
        public void SetAIEnabled(bool enabled)
        {
            enableAI = enabled;
        }

        /// <summary>
        /// Returns true if AI is enabled and it's AI's turn.
        /// </summary>
        public bool IsAITurn()
        {
            if (!enableAI || gameManager == null) return false;
            return gameManager.currentPlayerIndex == 1; // Player 2 is AI
        }
        #endregion

        #region Private Helpers

        void InitializeRNG()
        {
            rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();
        }

        void ApplyDifficultySettings()
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    pottingPriority = 0.5f;
                    safetyPriority = 0.3f;
                    positionalPriority = 0.2f;
                    decisionDelay = 0.5f;
                    break;
                case AIDifficulty.Medium:
                    pottingPriority = 0.7f;
                    safetyPriority = 0.2f;
                    positionalPriority = 0.1f;
                    decisionDelay = 1.0f;
                    break;
                case AIDifficulty.Hard:
                    pottingPriority = 0.8f;
                    safetyPriority = 0.15f;
                    positionalPriority = 0.05f;
                    decisionDelay = 1.5f;
                    break;
                case AIDifficulty.Expert:
                    pottingPriority = 0.9f;
                    safetyPriority = 0.05f;
                    positionalPriority = 0.05f;
                    decisionDelay = 2.0f;
                    break;
            }
        }

        int[] GetAvailableBalls(int[] ballsOnTable, ChinesePoolGameManager.BallGroup playerGroup, ChinesePoolMatchState phase)
        {
            var available = new System.Collections.Generic.List<int>();

            foreach (int ballId in ballsOnTable)
            {
                if (ballId == 0) continue; // Skip cue ball

                var ballGroup = gameManager.GetBallGroup(ballId);

                // On break or open table, all object balls are available
                if (phase == ChinesePoolMatchState.Break ||
                    phase == ChinesePoolMatchState.OpenTable)
                {
                    available.Add(ballId);
                }
                // After group assignment, only own group balls (and black if group cleared)
                else if (ballGroup == playerGroup)
                {
                    available.Add(ballId);
                }
                else if (ballGroup == ChinesePoolGameManager.BallGroup.None && ballId == 8)
                {
                    // Black ball (8-ball) available if own group cleared
                    // STUB: simplified - always available after group assigned
                    available.Add(ballId);
                }
            }

            return available.ToArray();
        }

        int[] GetAvailablePockets()
        {
            // All 6 pockets available for now
            // TODO: Filter based on table layout, ball positions, etc.
            return new int[] { 0, 1, 2, 3, 4, 5 };
        }

        Vector3 GetPocketWorldPosition(int pocketId)
        {
            // Standard 12ft snooker table pocket positions (approx)
            // Table: 3.6576m x 1.8288m
            float halfLength = 1.8288f;
            float halfWidth = 0.9144f;
            float pocketInset = 0.05f;

            switch (pocketId)
            {
                case 0: return new Vector3(-halfLength + pocketInset, 0, halfWidth - pocketInset);   // Top Left
                case 1: return new Vector3(halfLength - pocketInset, 0, halfWidth - pocketInset);    // Top Right
                case 2: return new Vector3(0, 0, halfWidth - pocketInset);                           // Middle Left
                case 3: return new Vector3(0, 0, -halfWidth + pocketInset);                          // Middle Right
                case 4: return new Vector3(-halfLength + pocketInset, 0, -halfWidth + pocketInset);  // Bottom Left
                case 5: return new Vector3(halfLength - pocketInset, 0, -halfWidth + pocketInset);   // Bottom Right
                default: return Vector3.zero;
            }
        }
        #endregion

        #region Self-Test
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test ChinesePool AIModifier")]
        public static void SelfTest()
        {
            bool pass = true;

            var ai = FindFirstObjectByType<ChinesePoolAIModifier>();
            if (ai == null)
            {
                Debug.LogError("❌ FAIL: ChinesePoolAIModifier not found in scene!");
                pass = false;
            }
            else
            {
                var mgr = FindFirstObjectByType<ChinesePoolGameManager>();
                var setup = FindFirstObjectByType<ChinesePoolBallSetup>();

                if (mgr == null)
                    Debug.LogWarning("⚠️ ChinesePoolGameManager not found in scene.");
                if (setup == null)
                    Debug.LogWarning("⚠️ ChinesePoolBallSetup not found in scene.");

                // Test call shot decision
                var callShot = ai.DecideCallShot();
                if (callShot.ballId == -1 || callShot.pocketId == -1)
                {
                    Debug.LogWarning("⚠️ AI returned invalid call shot (expected in empty scene).");
                }

                // Test shot parameters
                if (callShot.ballId != -1)
                {
                    var shotParams = ai.DecideShotParameters(callShot.ballId, callShot.pocketId);
                    if (shotParams.aimPoint == Vector3.zero)
                    {
                        Debug.LogWarning("⚠️ AI returned zero aim point (expected in empty scene).");
                    }
                }

                Debug.Log($"[CueStrike] AI Self-Test: difficulty={ai.difficulty}, enabled={ai.enableAI}");
            }

            if (pass) Debug.Log("✅ ChinesePoolAIModifier SELF-TEST PASSED — Ready for human verify.");
            else Debug.LogWarning("⚠️ ChinesePoolAIModifier SELF-TEST FAILED — Fix missing references before proceeding.");
        }
#endif
        #endregion
    }
}