using System;
using UnityEngine;
using CueStrike.NoirMemory.RCA;

namespace CueStrike.NoirMemory
{
    /// <summary>
    /// Central game controller for Noir Memory puzzle mode.
    /// Receives shot data from the RCA bridge or other input systems,
    /// validates against the current puzzle, and updates score/state.
    /// </summary>
    public class NoirMemoryGameController : MonoBehaviour
    {
        #region Singleton
        public static NoirMemoryGameController Instance { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired when a shot is processed with the result.</summary>
        public event Action<ShotProcessResult> OnShotProcessed;
        /// <summary>Fired when the puzzle is completed.</summary>
        public event Action<bool> OnPuzzleCompleted;
        /// <summary>Fired when the internal state changes.</summary>
        public event Action<GameState> OnStateChanged;
        #endregion

        #region Enums
        public enum GameState
        {
            Waiting,
            Playing,
            ShotResolving,
            Completed,
            Failed
        }

        public enum ShotResult
        {
            Success,
            WrongBall,
            Miss,
            Foul
        }

        public struct ShotProcessResult
        {
            public ShotResult result;
            public int ballId;
            public int targetBallId;
            public float accuracy;
            public int scoreGained;
        }
        #endregion

        #region Inspector
        [Header("Settings")]
        [SerializeField] private bool enableDummyMode = true;
        [SerializeField] private int maxMistakes = 3;
        [SerializeField] private float timeLimit = 120f;

        [Header("References")]
        [SerializeField] private NoirMemoryPuzzleManager puzzleManager;
        [SerializeField] private CueStrikeRCANoirBridge rcaBridge;
        [SerializeField] private NoirMemoryResultsScreen resultsScreen;
        #endregion

        #region State
        private GameState _currentState = GameState.Waiting;
        private int _currentPuzzleId = -1;
        private int _correctPots = 0;
        private int _wrongPots = 0;
        private int _comboCount = 0;
        private float _startTime = 0f;
        private float _accuracySum = 0f;
        private int _totalAttempts = 0;
        private bool _isInitialized = false;

        public GameState CurrentState => _currentState;
        public bool IsDummyMode => enableDummyMode;
        public int CorrectPots => _correctPots;
        public int WrongPots => _wrongPots;
        public int ComboCount => _comboCount;
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
            AutoWire();
        }

        private void AutoWire()
        {
            if (puzzleManager == null)
                puzzleManager = FindFirstObjectByType<NoirMemoryPuzzleManager>();

            if (rcaBridge == null)
                rcaBridge = FindFirstObjectByType<CueStrikeRCANoirBridge>();

            if (resultsScreen == null)
                resultsScreen = FindFirstObjectByType<NoirMemoryResultsScreen>();

            // Subscribe to RCA bridge if available
            if (rcaBridge != null)
            {
                rcaBridge.OnShotExecuted += OnRcaShotExecuted;
                Debug.Log("[NoirMemoryGame] Wired to CueStrikeRCANoirBridge.");
            }
            else if (enableDummyMode)
            {
                Debug.Log("[NoirMemoryGame] No RCA bridge found. Dummy mode enabled (no shot input).");
            }

            _isInitialized = true;
        }
        #endregion

        #region Public API

        /// <summary>
        /// Starts a new puzzle with the given ID.
        /// </summary>
        public void StartPuzzle(int puzzleId)
        {
            _currentPuzzleId = puzzleId;
            _correctPots = 0;
            _wrongPots = 0;
            _comboCount = 0;
            _accuracySum = 0f;
            _totalAttempts = 0;
            _startTime = Time.time;
            _currentState = GameState.Playing;
            OnStateChanged?.Invoke(_currentState);
            Debug.Log($"[NoirMemoryGame] Started puzzle {puzzleId}");
        }

        /// <summary>
        /// Processes a shot from any input source (RCA bridge, VR controller, etc).
        /// Validates against the current puzzle state.
        /// </summary>
        public void ProcessShot(NoirMemoryShotData shotData)
        {
            if (_currentState != GameState.Playing)
            {
                Debug.LogWarning($"[NoirMemoryGame] Cannot process shot in state {_currentState}.");
                return;
            }

            _currentState = GameState.ShotResolving;
            _totalAttempts++;

            // Simulate validation against puzzle
            ShotProcessResult result = EvaluateShot(shotData);

            // Update stats
            if (result.result == ShotResult.Success)
            {
                _correctPots++;
                _comboCount++;
                _accuracySum += result.accuracy;
            }
            else
            {
                _wrongPots++;
                _comboCount = 0;
                _accuracySum += result.accuracy;
            }

            OnShotProcessed?.Invoke(result);
            Debug.Log($"[NoirMemoryGame] Shot result: {result.result}, score+={result.scoreGained}");

            // Check puzzle completion or failure
            if (_wrongPots >= maxMistakes)
            {
                EndPuzzle(false);
            }
            else if (_correctPots >= GetTotalBallsRequired())
            {
                EndPuzzle(true);
            }
            else
            {
                _currentState = GameState.Playing;
                OnStateChanged?.Invoke(_currentState);
            }
        }

        /// <summary>
        /// Gets the current puzzle ID. Returns -1 if no puzzle active.
        /// </summary>
        public int GetCurrentPuzzle() => _currentPuzzleId;

        /// <summary>
        /// Resets the controller to idle state.
        /// </summary>
        public void ResetPuzzle()
        {
            _currentState = GameState.Waiting;
            _currentPuzzleId = -1;
            _correctPots = 0;
            _wrongPots = 0;
            _comboCount = 0;
            _accuracySum = 0f;
            _totalAttempts = 0;
            OnStateChanged?.Invoke(_currentState);
            Debug.Log("[NoirMemoryGame] Puzzle reset.");
        }

        /// <summary>
        /// Simulates a complete shot for testing purposes.
        /// </summary>
        public void SimulateDummyShot()
        {
            if (!enableDummyMode)
            {
                Debug.LogWarning("[NoirMemoryGame] Dummy mode disabled.");
                return;
            }

            if (_currentState != GameState.Playing)
            {
                Debug.Log("[NoirMemoryGame] Cannot simulate shot - not in Playing state. Starting dummy puzzle...");
                StartPuzzle(0);
            }

            var dummyShot = new NoirMemoryShotData
            {
                aimDirection = Vector3.forward,
                power = 0.6f,
                cueAngle = 30f,
                tipOffsetX = 0.05f,
                tipOffsetY = -0.02f,
                confidence = 0.85f,
                timestamp = DateTime.UtcNow
            };

            ProcessShot(dummyShot);
        }

        #endregion

        #region Private

        private void OnRcaShotExecuted(NoirMemoryShotData shotData)
        {
            ProcessShot(shotData);
        }

        private ShotProcessResult EvaluateShot(NoirMemoryShotData shotData)
        {
            // Simplified validation — in production, this checks against
            // actual ball positions and puzzle requirements.
            var result = new ShotProcessResult();

            // Accuracy based on confidence and tip offset
            float accuracy = shotData.confidence * (1f - Mathf.Abs(shotData.tipOffsetX) * 0.3f);
            result.accuracy = Mathf.Clamp01(accuracy);
            result.ballId = _totalAttempts; // placeholder ball ID
            result.targetBallId = _currentPuzzleId;

            if (accuracy >= 0.6f)
            {
                result.result = ShotResult.Success;
                result.scoreGained = Mathf.RoundToInt(accuracy * 100f) + (_comboCount * 10);
            }
            else if (accuracy >= 0.3f)
            {
                result.result = ShotResult.WrongBall;
                result.scoreGained = 0;
            }
            else
            {
                result.result = ShotResult.Miss;
                result.scoreGained = -10;
            }

            return result;
        }

        private int GetTotalBallsRequired()
        {
            // In production, query the puzzle data
            return 5;
        }

        private void EndPuzzle(bool success)
        {
            _currentState = success ? GameState.Completed : GameState.Failed;
            OnStateChanged?.Invoke(_currentState);
            OnPuzzleCompleted?.Invoke(success);

            // Show results via ResultsScreen if available
            if (resultsScreen != null)
            {
                float elapsed = Time.time - _startTime;
                float accuracy = _totalAttempts > 0 ? _accuracySum / _totalAttempts : 0f;

                var scoreData = resultsScreen.CalculateScore(
                    _correctPots, _wrongPots, _totalAttempts,
                    accuracy, elapsed, _comboCount,
                    $"Puzzle_{_currentPuzzleId}"
                );
                resultsScreen.ShowResults(scoreData);
            }

            Debug.Log($"[NoirMemoryGame] Puzzle ended: {(success ? "SUCCESS" : "FAILED")} " +
                      $"({_correctPots}/{_correctPots + _wrongPots} pots)");
        }

        #endregion
    }
}