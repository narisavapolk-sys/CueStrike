using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Core
{
    /// <summary>
    /// Records shot snapshots for Undo functionality.
    /// Stores ball positions before/after each shot.
    /// </summary>
    public class CueStrikeShotHistory : MonoBehaviour
    {
        #region Structs
        /// <summary>
        /// Snapshot of all ball states at a given moment.
        /// </summary>
        [Serializable]
        public struct ShotSnapshot
        {
            public int shotIndex;
            public float timestamp;
            public List<BallState> ballStatesBefore;
            public List<BallState> ballStatesAfter;
            public Vector3 shotDirection;
            public float shotPower;
            public int playerTurn;
        }

        [Serializable]
        public struct BallState
        {
            public int ballId;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public bool isPocketed;
        }
        #endregion

        #region Events
        /// <summary>Fired when a shot is recorded.</summary>
        public event Action<int> OnShotRecorded; // shotIndex

        /// <summary>Fired when undo is performed.</summary>
        public event Action<int> OnShotUndone; // shotIndex that was undone
        #endregion

        #region Serialized Fields
        [Header("Settings")]
        [SerializeField] private int maxHistoryLength = 10;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = false;
        #endregion

        #region Private State
        private List<ShotSnapshot> _history = new List<ShotSnapshot>();
        private int _shotCounter = 0;
        private bool _isMultiplayer;
        #endregion

        #region Properties
        public int HistoryCount => _history.Count;
        public bool CanUndo => _history.Count > 0 && !_isMultiplayer;
        public bool IsMultiplayer
        {
            get => _isMultiplayer;
            set => _isMultiplayer = value;
        }
        public IReadOnlyList<ShotSnapshot> History => _history.AsReadOnly();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            // Detect multiplayer mode from scene or game manager
            DetectMultiplayerMode();
        }
        #endregion

        #region Public API
        /// <summary>
        /// Record ball states BEFORE a shot is executed.
        /// Call this from the shot manager before applying physics.
        /// </summary>
        public void RecordBeforeShot(Vector3 direction, float power, int playerTurn)
        {
            var snapshot = new ShotSnapshot
            {
                shotIndex = _shotCounter,
                timestamp = Time.time,
                ballStatesBefore = CaptureCurrentBallStates(),
                ballStatesAfter = new List<BallState>(),
                shotDirection = direction,
                shotPower = power,
                playerTurn = playerTurn
            };

            // Add to history (will fill after-shot states later)
            _history.Add(snapshot);

            // Enforce max history
            while (_history.Count > maxHistoryLength)
            {
                _history.RemoveAt(0);
            }

            if (verboseLogging)
                Debug.Log($"[ShotHistory] Recorded BEFORE shot #{_shotCounter}. History count: {_history.Count}");
        }

        /// <summary>
        /// Record ball states AFTER a shot resolves.
        /// Call this from the shot manager when balls stop moving.
        /// </summary>
        public void RecordAfterShot()
        {
            if (_history.Count == 0) return;

            var snapshot = _history[_history.Count - 1];
            snapshot.ballStatesAfter = CaptureCurrentBallStates();
            _history[_history.Count - 1] = snapshot;

            int shotIndex = snapshot.shotIndex;
            _shotCounter++;

            OnShotRecorded?.Invoke(shotIndex);

            if (verboseLogging)
                Debug.Log($"[ShotHistory] Recorded AFTER shot #{shotIndex}. Complete.");
        }

        /// <summary>
        /// Undo the last shot — restore ball positions to before state.
        /// Returns the snapshot that was undone, or null if nothing to undo.
        /// </summary>
        public ShotSnapshot? UndoLastShot()
        {
            if (!CanUndo)
            {
                if (_isMultiplayer)
                    Debug.LogWarning("[ShotHistory] Undo not available in multiplayer mode.");
                else
                    Debug.LogWarning("[ShotHistory] No shots to undo.");
                return null;
            }

            ShotSnapshot snapshot = _history[_history.Count - 1];

            // Restore ball positions
            RestoreBallStates(snapshot.ballStatesBefore);

            // Remove from history
            _history.RemoveAt(_history.Count - 1);

            OnShotUndone?.Invoke(snapshot.shotIndex);

            if (verboseLogging)
                Debug.Log($"[ShotHistory] Undo shot #{snapshot.shotIndex}. Remaining history: {_history.Count}");

            return snapshot;
        }

        /// <summary>
        /// Clear all history.
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
            _shotCounter = 0;

            if (verboseLogging)
                Debug.Log("[ShotHistory] History cleared.");
        }
        #endregion

        #region Helpers
        private List<BallState> CaptureCurrentBallStates()
        {
            var states = new List<BallState>();
            var balls = FindObjectsByType<CueStrikeBall>(FindObjectsSortMode.None);

            foreach (var ball in balls)
            {
                var rb = ball.GetComponent<Rigidbody>();
                states.Add(new BallState
                {
                    ballId = ball.BallId,
                    position = ball.transform.position,
                    rotation = ball.transform.rotation,
                    velocity = rb != null ? rb.linearVelocity : Vector3.zero,
                    isPocketed = ball.IsPocketed
                });
            }

            return states;
        }

        private void RestoreBallStates(List<BallState> states)
        {
            var balls = FindObjectsByType<CueStrikeBall>(FindObjectsSortMode.None);
            var ballMap = new Dictionary<int, CueStrikeBall>();
            foreach (var ball in balls)
            {
                ballMap[ball.BallId] = ball;
            }

            foreach (var state in states)
            {
                if (ballMap.TryGetValue(state.ballId, out var ball))
                {
                    ball.transform.position = state.position;
                    ball.transform.rotation = state.rotation;
                    ball.SetPocketed(state.isPocketed);

                    var rb = ball.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = state.velocity;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            if (verboseLogging)
                Debug.Log($"[ShotHistory] Restored {states.Count} ball states.");
        }

        private void DetectMultiplayerMode()
        {
            // Check multiplayer flag from Normcore or network manager
            var normcoreManager = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in normcoreManager)
            {
                string typeName = mb.GetType().Name.ToLowerInvariant();
                if (typeName.Contains("normcore") || typeName.Contains("realtime"))
                {
                    // Check if there's a connected session
                    var connectedField = mb.GetType().GetProperty("connected") ?? mb.GetType().GetProperty("isConnected");
                    if (connectedField != null)
                    {
                        try
                        {
                            object val = connectedField.GetValue(mb);
                            if (val is bool b && b)
                            {
                                _isMultiplayer = true;
                                return;
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        #endregion
    }
}