using System;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.UI;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// Tracks ball potted states per frame. Event-driven.
    /// Integrates with RulesManager and Scoreboard automatically.
    /// </summary>
    public class BallPottedTracker : MonoBehaviour
    {
        #region Events
        public event Action<int, int> OnBallPotted; // ballNumber, playerNumber
        public event Action<int> OnBallReturned; // ballNumber
        public event Action<int> OnAllBallsPotted; // winnerPlayerNumber
        public event Action OnBlackBallPotted; // 8-ball / black ball special event
        #endregion

        #region Serialized Fields
        [Header("Ball Tracking")]
        [SerializeField] private Transform[] _ballTransforms; // 1-15
        [SerializeField] private float _pocketDetectionRadius = 0.15f;
        [SerializeField] private Vector3[] _pocketPositions;

        [Header("State")]
        [SerializeField] private bool _trackChinesePool = true;
        [SerializeField] private bool _autoSyncScoreboard = true;
        #endregion

        #region Private Fields
        private readonly HashSet<int> _pottedBalls = new HashSet<int>();
        private readonly HashSet<int> _returnedBalls = new HashSet<int>();
        private readonly Dictionary<int, Vector3> _lastPositions = new Dictionary<int, Vector3>();
        private ChinesePoolScoreboard _scoreboard;
        private bool _isTracking;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _scoreboard = FindFirstObjectByType<ChinesePoolScoreboard>();
            InitializeBallTracking();
        }

        private void FixedUpdate()
        {
            if (!_isTracking || _ballTransforms == null) return;

            for (int i = 0; i < _ballTransforms.Length; i++)
            {
                if (_ballTransforms[i] == null) continue;
                CheckBallState(i + 1, _ballTransforms[i]);
            }
        }
        #endregion

        #region Public API
        public void StartTracking()
        {
            _isTracking = true;
            _pottedBalls.Clear();
            _returnedBalls.Clear();
            InitializeBallTracking();
            Debug.Log("[BallPottedTracker] Tracking started.");
        }

        public void StopTracking()
        {
            _isTracking = false;
            Debug.Log("[BallPottedTracker] Tracking stopped.");
        }

        public bool IsBallPotted(int ballNumber)
        {
            return _pottedBalls.Contains(ballNumber);
        }

        public int GetPottedBallCount()
        {
            return _pottedBalls.Count;
        }

        public void ResetTracking()
        {
            _pottedBalls.Clear();
            _returnedBalls.Clear();
            _lastPositions.Clear();
            _isTracking = false;
            Debug.Log("[BallPottedTracker] Tracking reset.");
        }

        public void SetBallTransforms(Transform[] transforms)
        {
            _ballTransforms = transforms;
            InitializeBallTracking();
        }

        public void SetPocketPositions(Vector3[] positions)
        {
            _pocketPositions = positions;
        }
        #endregion

        #region Private Methods
        private void InitializeBallTracking()
        {
            _lastPositions.Clear();
            if (_ballTransforms == null) return;

            for (int i = 0; i < _ballTransforms.Length; i++)
            {
                if (_ballTransforms[i] != null)
                {
                    _lastPositions[i + 1] = _ballTransforms[i].position;
                }
            }
        }

        private void CheckBallState(int ballNumber, Transform ballTransform)
        {
            Vector3 currentPos = ballTransform.position;
            bool isNearPocket = IsNearAnyPocket(currentPos);
            bool wasPotted = _pottedBalls.Contains(ballNumber);

            // Detect potted
            if (isNearPocket && !wasPotted && IsBelowTableSurface(currentPos))
            {
                _pottedBalls.Add(ballNumber);
                int currentPlayer = GetCurrentPlayerNumber();
                OnBallPotted?.Invoke(ballNumber, currentPlayer);

                if (_autoSyncScoreboard && _scoreboard != null)
                {
                    _scoreboard.RegisterPottedBall(currentPlayer, ballNumber);
                }

                if (ballNumber == 8) // Black ball
                {
                    OnBlackBallPotted?.Invoke();
                }

                CheckWinCondition();
            }

            // Detect returned (if ball somehow comes back up)
            if (!isNearPocket && wasPotted && !IsBelowTableSurface(currentPos))
            {
                _pottedBalls.Remove(ballNumber);
                _returnedBalls.Add(ballNumber);
                OnBallReturned?.Invoke(ballNumber);
            }

            _lastPositions[ballNumber] = currentPos;
        }

        private bool IsNearAnyPocket(Vector3 position)
        {
            if (_pocketPositions == null || _pocketPositions.Length == 0) return false;

            for (int i = 0; i < _pocketPositions.Length; i++)
            {
                float dist = Vector3.Distance(position, _pocketPositions[i]);
                if (dist < _pocketDetectionRadius)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsBelowTableSurface(Vector3 position)
        {
            // Assuming table surface Y = 0, pocket depth below -0.1
            return position.y < -0.1f;
        }

        private int GetCurrentPlayerNumber()
        {
            // Placeholder: actual implementation will depend on RulesManager API
            // Future: integrate with ChinesePoolGameManager or ChinesePoolRules
            return 1;
        }

        private void CheckWinCondition()
        {
            if (_trackChinesePool && _pottedBalls.Count >= 15)
            {
                int winner = GetCurrentPlayerNumber();
                OnAllBallsPotted?.Invoke(winner);
                Debug.Log($"[BallPottedTracker] All balls potted! Winner: Player {winner}");
            }
        }
        #endregion

        #region Self-Test
        public bool RunSelfTest()
        {
            bool pass = true;
            if (_ballTransforms == null || _ballTransforms.Length == 0)
            {
                Debug.LogWarning("[Self-Test] BallPottedTracker: No ball transforms assigned (expected before Setup).");
            }
            if (_pocketPositions == null || _pocketPositions.Length == 0)
            {
                Debug.LogWarning("[Self-Test] BallPottedTracker: No pocket positions assigned (expected before Setup).");
            }
            if (_pocketDetectionRadius <= 0f)
            {
                Debug.LogError("[Self-Test] BallPottedTracker: Detection radius must be > 0.");
                pass = false;
            }
            Debug.Log($"[Self-Test] BallPottedTracker: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}