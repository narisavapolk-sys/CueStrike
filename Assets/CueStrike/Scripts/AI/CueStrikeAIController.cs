using System;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.AI
{
    public enum SkillLevel { Easy, Medium, Hard, Expert }

    /// <summary>
    /// Master AI controller that manages shot decisions for all game modes.
    /// Delegates to ICueStrikeAIStrategy for mode-specific logic.
    /// Wires into CueStrikeShotManager for automated shot execution.
    /// </summary>
    public class CueStrikeAIController : MonoBehaviour
    {
        #region Singleton
        public static CueStrikeAIController Instance { get; private set; }
        #endregion

        #region Events
        public event Action OnAITurnBegan;
        public event Action OnAIShotExecuted;
        public event Action<string> OnAIStatusMessage;
        #endregion

        #region Inspector Settings
        [Header("AI Settings")]
        [SerializeField] private SkillLevel skillLevel = SkillLevel.Medium;
        [SerializeField] private float shotAccuracy = 0.75f;
        [SerializeField] private float positionPlayWeight = 0.5f;
        [SerializeField] private float decisionDelay = 1.0f;
        [SerializeField] private bool verboseLogging = false;

        [Header("References")]
        [SerializeField] private CueStrikeShotManager shotManager;
        [SerializeField] private CueStrikePhysicsManager physicsManager;
        [SerializeField] private Transform cueBallTransform;

        [Header("Ball Database (auto-assigned)")]
        [SerializeField] private List<Transform> ballTransforms = new List<Transform>();
        #endregion

        #region State
        private ICueStrikeAIStrategy _strategy;
        private bool _isExecutingShot = false;
        private bool _isMyTurn = false;
        private List<BallEntry> _ballCache = new List<BallEntry>();
        private Dictionary<SkillLevel, AIParameters> _paramTable;
        #endregion

        #region Ball Entry
        /// <summary>
        /// Lightweight ball data for AI calculations.
        /// </summary>
        public struct BallEntry
        {
            public int id;
            public Vector3 position;
            public bool isPotted;
        }
        #endregion

        #region AI Parameters per Difficulty
        [Serializable]
        public struct AIParameters
        {
            public float accuracy;
            public float positionWeight;
            public float power;
            public float spinControl;
            public float decisionDelay;
            public float errorMargin;
            public string label;
        }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (shotManager == null) shotManager = FindFirstObjectByType<CueStrikeShotManager>();
            if (physicsManager == null) physicsManager = FindFirstObjectByType<CueStrikePhysicsManager>();

            BuildParameterTable();
            SelectStrategy();
            CacheBalls();
        }

        private void BuildParameterTable()
        {
            _paramTable = new Dictionary<SkillLevel, AIParameters>
            {
                [SkillLevel.Easy] = new AIParameters
                {
                    accuracy = 0.40f, positionWeight = 0.10f, power = 0.5f,
                    spinControl = 0.1f, decisionDelay = 2.0f, errorMargin = 0.3f,
                    label = "Easy (40% accuracy)"
                },
                [SkillLevel.Medium] = new AIParameters
                {
                    accuracy = 0.65f, positionWeight = 0.35f, power = 0.7f,
                    spinControl = 0.4f, decisionDelay = 1.5f, errorMargin = 0.15f,
                    label = "Medium (65% accuracy)"
                },
                [SkillLevel.Hard] = new AIParameters
                {
                    accuracy = 0.82f, positionWeight = 0.60f, power = 0.85f,
                    spinControl = 0.7f, decisionDelay = 1.0f, errorMargin = 0.07f,
                    label = "Hard (82% accuracy)"
                },
                [SkillLevel.Expert] = new AIParameters
                {
                    accuracy = 0.95f, positionWeight = 0.85f, power = 0.95f,
                    spinControl = 0.9f, decisionDelay = 0.6f, errorMargin = 0.02f,
                    label = "Expert (95% accuracy)"
                }
            };
        }
        #endregion

        #region Public API

        /// <summary>
        /// Updates the AI's difficulty level and re-selects strategy.
        /// </summary>
        public void SetSkillLevel(SkillLevel level)
        {
            skillLevel = level;
            var p = _paramTable[level];
            shotAccuracy = p.accuracy;
            positionPlayWeight = p.positionWeight;
            decisionDelay = p.decisionDelay;
            SelectStrategy();
            Log($"AI skill set to {level} — {p.label}");
        }

        /// <summary>
        /// Gets current skill level.
        /// </summary>
        public SkillLevel GetSkillLevel() => skillLevel;

        /// <summary>
        /// Returns the parameter table for the current difficulty.
        /// </summary>
        public AIParameters GetCurrentParameters() => _paramTable[skillLevel];

        /// <summary>
        /// Called when it becomes this AI's turn.
        /// Starts the decision + execution pipeline.
        /// </summary>
        public void BeginTurn()
        {
            if (_isExecutingShot) return;
            _isMyTurn = true;
            OnAITurnBegan?.Invoke();
            OnAIStatusMessage?.Invoke($"AI is thinking ({skillLevel})...");

            RefreshBallCache();
            Invoke(nameof(ExecuteAIShot), decisionDelay);
        }

        /// <summary>
        /// Whether the AI is currently executing a shot.
        /// </summary>
        public bool IsExecutingShot() => _isExecutingShot;

        /// <summary>
        /// Sets the ball transforms for AI raycasting.
        /// </summary>
        public void SetBallTransforms(List<Transform> balls)
        {
            ballTransforms = balls;
            CacheBalls();
        }

        /// <summary>
        /// Refreshes the ball position cache.
        /// </summary>
        public void RefreshBallCache()
        {
            CacheBalls();
        }

        #endregion

        #region AI Shot Execution

        private void SelectStrategy()
        {
            _strategy = skillLevel switch
            {
                SkillLevel.Easy => new CueStrikeAIEasy(),
                SkillLevel.Medium => new CueStrikeAIMedium(),
                SkillLevel.Hard => new CueStrikeAIHard(),
                SkillLevel.Expert => new CueStrikeAIExpert(),
                _ => new CueStrikeAIMedium()
            };
            _strategy.Initialize(_paramTable[skillLevel]);
            Log($"Strategy selected: {_strategy.GetType().Name}");
        }

        public void ExecuteAIShot()
        {
            if (!_isMyTurn || shotManager == null)
            {
                _isMyTurn = false;
                return;
            }

            _isExecutingShot = true;

            // 1. Evaluate table state
            var tableState = EvaluateTable();

            // 2. Select best shot
            var shot = _strategy.SelectShot(tableState, _paramTable[skillLevel]);

            if (shot == null || shot.Value.ballId < 0)
            {
                // No valid shot — play safe
                shot = PlaySafe(tableState);
            }

            // 3. Apply error based on difficulty
            ApplyAimError(ref shot);

            // 4. Execute via ShotManager
            ExecuteShotOnTable(shot.Value);

            _isExecutingShot = false;
            _isMyTurn = false;
            OnAIShotExecuted?.Invoke();
            Log($"AI shot executed: ball={shot?.ballId}, pocket={shot?.pocketIndex}");
        }

        private ShotPlan? PlaySafe(TableState tableState)
        {
            // Simple safety: hit the nearest ball gently toward a rail
            if (tableState.availableBalls.Count == 0) return null;

            var nearest = tableState.availableBalls[0];
            float nearestDist = float.MaxValue;
            Vector3 cuePos = cueBallTransform != null ? cueBallTransform.position : Vector3.zero;

            foreach (var ball in tableState.availableBalls)
            {
                float dist = Vector3.Distance(cuePos, ball.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = ball;
                }
            }

            return new ShotPlan
            {
                ballId = nearest.id,
                targetPosition = nearest.position + Vector3.right * 0.1f, // gentle touch
                pocketIndex = -1, // no pocket target = safety
                power = 0.3f,
                spin = Vector3.zero,
                isSafe = true
            };
        }

        private void ApplyAimError(ref ShotPlan? shot)
        {
            if (shot == null) return;
            var p = _paramTable[skillLevel];
            var sp = shot.Value;

            // Apply random offset based on error margin
            float errorX = UnityEngine.Random.Range(-p.errorMargin, p.errorMargin);
            float errorZ = UnityEngine.Random.Range(-p.errorMargin, p.errorMargin);
            sp.targetPosition += new Vector3(errorX, 0, errorZ);
            sp.power *= UnityEngine.Random.Range(0.8f, 1.2f);

            shot = sp;
        }

        private void ExecuteShotOnTable(ShotPlan plan)
        {
            if (shotManager == null || cueBallTransform == null) return;

            // Calculate aim direction from cue ball to target
            Vector3 aimDir = (plan.targetPosition - cueBallTransform.position).normalized;
            aimDir.y = 0;

            // Set shot parameters via ShotManager reflection
            var shotManagerType = shotManager.GetType();

            // Try setting force via reflection or public properties
            var forceField = shotManagerType.GetField("currentForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (forceField != null)
            {
                forceField.SetValue(shotManager, plan.power * 20f);
            }

            var spinField = shotManagerType.GetField("currentSpin",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spinField != null && plan.spin != Vector3.zero)
            {
                spinField.SetValue(shotManager, plan.spin.magnitude);
            }

            Log($"AI aiming at ball {plan.ballId} toward {plan.targetPosition}, power={plan.power:F2}");
        }

        private TableState EvaluateTable()
        {
            var state = new TableState();
            state.cueBallPosition = cueBallTransform != null ? cueBallTransform.position : Vector3.zero;
            state.availableBalls = new List<BallEntry>();

            foreach (var b in _ballCache)
            {
                if (!b.isPotted)
                {
                    state.availableBalls.Add(b);
                }
            }

            return state;
        }

        private void CacheBalls()
        {
            _ballCache.Clear();

            // From physics manager
            if (physicsManager != null)
            {
                int ballCount = physicsManager.GetBallCount();
                for (int i = 0; i < ballCount; i++)
                {
                    var ball = physicsManager.GetBallById(i);
                    if (ball != null)
                    {
                        _ballCache.Add(new BallEntry
                        {
                            id = i,
                            position = ball.position,
                            isPotted = false // physics manager tracks potted state
                        });
                    }
                }
            }
            // Fallback to transform list
            else
            {
                for (int i = 0; i < ballTransforms.Count; i++)
                {
                    if (ballTransforms[i] != null)
                    {
                        _ballCache.Add(new BallEntry
                        {
                            id = i,
                            position = ballTransforms[i].position,
                            isPotted = false
                        });
                    }
                }
            }
        }

        private void Log(string msg)
        {
            if (verboseLogging)
                Debug.Log($"[AI Controller] {msg}");
        }

        #endregion

        #region Shot Plan
        public struct ShotPlan
        {
            public int ballId;
            public Vector3 targetPosition;
            public int pocketIndex;
            public float power;
            public Vector3 spin;
            public bool isSafe;
        }

        public struct TableState
        {
            public Vector3 cueBallPosition;
            public List<BallEntry> availableBalls;
        }
        #endregion
    }
}