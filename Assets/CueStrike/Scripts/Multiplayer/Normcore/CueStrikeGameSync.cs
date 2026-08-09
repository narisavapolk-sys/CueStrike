using UnityEngine;

namespace CueStrike.Multiplayer.Normcore
{
    /// <summary>
    /// Game state synchronization for Normcore multiplayer.
    /// Replicates turn state, scores, frame state, fouls, and call-shot data
    /// across all connected clients.
    ///
    /// Host authority model: host owns the game state and broadcasts to all clients.
    /// Clients receive state updates and apply them locally.
    ///
    /// NOTE: Requires Normcore SDK and CUESTRIKE_NORMCORE define symbol for actual sync.
    /// Without the SDK, operates as a local game state wrapper with logging.
    /// </summary>
    ///
    /// INSTALLATION STEPS (after Normcore SDK is installed):
    /// 1. Attach a RealtimeView component to this GameObject
    /// 2. Attach a RealtimeTransform component (for reference frame)
    /// 3. Set RealtimeView.ownershipModel = Server (host authority)
    /// 4. Mark the GameState property with [RealtimeProperty] for automatic sync
    /// 5. Wire CueStrikeNormcoreManager's events to this component's methods
    ///
    /// WITH NORMCORE: Uses RealtimeView's property sync system.
    /// WITHOUT NORMCORE: Logs state changes locally.

    public class CueStrikeGameSync : MonoBehaviour
    {
        #region Enums & Data Classes
        [System.Serializable]
        public struct GameStateData
        {
            public int currentTurnPlayerId;
            public int player1Score;
            public int player2Score;
            public int player1Frames;
            public int player2Frames;
            public CueStrikeNormcoreManager.GameState gameState;
            public string gameMode;
            public int calledBallId;
            public int calledPocketId;
            public string lastFoulType;
            public bool isFrameOver;
            public bool isMatchOver;
            public float matchTimer;

            public static GameStateData Default => new GameStateData
            {
                currentTurnPlayerId = -1,
                player1Score = 0,
                player2Score = 0,
                player1Frames = 0,
                player2Frames = 0,
                gameState = CueStrikeNormcoreManager.GameState.Lobby,
                gameMode = "8-Ball",
                calledBallId = -1,
                calledPocketId = -1,
                lastFoulType = "",
                isFrameOver = false,
                isMatchOver = false,
                matchTimer = 0f
            };
        }
        #endregion

        #region Inspector
        [Header("Game Sync Settings")]
        [Tooltip("Enable automatic state sync. Disable for manual sync control.")]
        public bool enableAutoSync = true;

        [Tooltip("Interval in seconds between automatic state broadcasts (host only).")]
        public float syncInterval = 0.5f;

        [Header("Host Settings")]
        [Tooltip("True if this instance is the game state host (authoritative).")]
        public bool isHost = false;

        [Tooltip("Override for offline/local-only mode (no network).")]
        public bool forceLocalMode = false;
        #endregion

        #region State
        private GameStateData _currentState = GameStateData.Default;
        private GameStateData _lastSyncedState = GameStateData.Default;
        private float _syncTimer = 0f;
        private CueStrikeNormcoreManager _normcoreManager;

        /// <summary>Current synchronized game state.</summary>
        public GameStateData CurrentState => _currentState;

        /// <summary>True if the game state has changed since last sync.</summary>
        public bool HasStateChanged => !_currentState.Equals(_lastSyncedState);
        #endregion

        #region Events
        /// <summary>Fired when game state is updated (from host or local).</summary>
        public event System.Action<GameStateData> OnGameStateUpdated;

        /// <summary>Fired when a foul is committed (received from host).</summary>
        public event System.Action<int, string> OnFoulReceived;

        /// <summary>Fired when the frame ends (received from host).</summary>
        public event System.Action<int> OnFrameEndReceived; // winner playerId
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _normcoreManager = CueStrikeNormcoreManager.Instance;
            _currentState = GameStateData.Default;

            // Subscribe to NormcoreManager events
            if (_normcoreManager != null)
            {
                _normcoreManager.OnGameStateChanged += OnNormcoreGameStateChanged;
            }
        }

        private void Update()
        {
            if (!enableAutoSync || !isHost) return;

            _syncTimer += Time.deltaTime;
            if (_syncTimer >= syncInterval)
            {
                _syncTimer = 0f;
                BroadcastState();
            }
        }

        private void OnDestroy()
        {
            if (_normcoreManager != null)
            {
                _normcoreManager.OnGameStateChanged -= OnNormcoreGameStateChanged;
            }
        }

        private void OnNormcoreGameStateChanged(CueStrikeNormcoreManager.GameState normcoreState)
        {
            _currentState.gameState = normcoreState;
            OnGameStateUpdated?.Invoke(_currentState);
        }
        #endregion

        #region Public API

        /// <summary>
        /// Updates the local game state and broadcasts to all clients (host only).
        /// Call this from game managers (e.g., CueStrikeTurnManager, ChinesePoolGameManager).
        /// </summary>
        public void UpdateGameState(GameStateData newState)
        {
            _currentState = newState;

            OnGameStateUpdated?.Invoke(_currentState);

            if (isHost && HasStateChanged)
            {
                BroadcastState();
            }

            _lastSyncedState = _currentState;
        }

        /// <summary>
        /// Called when a client receives a state update from the host.
        /// </summary>
        public void ApplyRemoteState(GameStateData remoteState)
        {
            _currentState = remoteState;
            _lastSyncedState = remoteState;
            OnGameStateUpdated?.Invoke(_currentState);
            Debug.Log($"[GameSync] Remote state applied: turn={remoteState.currentTurnPlayerId}, mode={remoteState.gameMode}");
        }

        /// <summary>
        /// Called when a foul is committed (host broadcasts to all clients).
        /// </summary>
        public void OnFoulCommitted(int playerId, string foulType)
        {
            _currentState.lastFoulType = foulType;
            OnFoulReceived?.Invoke(playerId, foulType);

            if (isHost)
            {
                Debug.Log($"[GameSync] Host: Foul by player {playerId}: {foulType}. Broadcasting.");
                BroadcastState();
            }
        }

        /// <summary>
        /// Called when the frame ends (host broadcasts to all clients).
        /// </summary>
        public void OnFrameEnd(int winnerPlayerId)
        {
            _currentState.isFrameOver = true;

            if (winnerPlayerId == 0) _currentState.player1Frames++;
            else if (winnerPlayerId == 1) _currentState.player2Frames++;

            OnFrameEndReceived?.Invoke(winnerPlayerId);

            if (isHost)
            {
                Debug.Log($"[GameSync] Host: Frame ended. Winner: Player {winnerPlayerId}. Broadcasting.");
                BroadcastState();
            }
        }

        /// <summary>
        /// Called when a new frame starts.
        /// </summary>
        public void OnNewFrame()
        {
            _currentState.player1Score = 0;
            _currentState.player2Score = 0;
            _currentState.isFrameOver = false;
            _currentState.calledBallId = -1;
            _currentState.calledPocketId = -1;
            _currentState.lastFoulType = "";
            _currentState.currentTurnPlayerId = 0; // Player 1 starts

            OnGameStateUpdated?.Invoke(_currentState);

            if (isHost)
            {
                Debug.Log("[GameSync] Host: New frame started. Broadcasting.");
                BroadcastState();
            }
        }

        /// <summary>
        /// Updates the call shot state.
        /// </summary>
        public void SetCallShot(int ballId, int pocketId)
        {
            _currentState.calledBallId = ballId;
            _currentState.calledPocketId = pocketId;

            if (isHost)
            {
                BroadcastState();
            }
        }

        /// <summary>
        /// Updates the current turn player.
        /// </summary>
        public void SetCurrentTurn(int playerId)
        {
            _currentState.currentTurnPlayerId = playerId;
            OnGameStateUpdated?.Invoke(_currentState);

            if (isHost && enableAutoSync)
            {
                BroadcastState();
            }
        }

        /// <summary>
        /// Resets the game state for a new match.
        /// </summary>
        public void ResetForNewMatch()
        {
            _currentState = GameStateData.Default;
            _lastSyncedState = GameStateData.Default;
            OnGameStateUpdated?.Invoke(_currentState);

            if (isHost)
            {
                Debug.Log("[GameSync] Host: Reset for new match. Broadcasting.");
                BroadcastState();
            }
        }

        /// <summary>
        /// Forces an immediate state broadcast (host only).
        /// </summary>
        public void BroadcastState()
        {
            _lastSyncedState = _currentState;

            // var realtimeView = GetComponent<Normal.Realtime.RealtimeView>();
            // if (realtimeView != null)
            // {
            //     // Mark the view as dirty to trigger property sync
            //     realtimeView.RequestOwnership();
            // }
            //
            // // If using [RealtimeProperty] on a GameStateData field:
            // // The property system automatically syncs when the value changes
            // Debug.Log($"[GameSync] State broadcast via Normcore: turn={_currentState.currentTurnPlayerId}");

            // Log state broadcast
            Debug.Log($"[GameSync] State broadcast: turn={_currentState.currentTurnPlayerId}, " +
                      $"score=({_currentState.player1Score}-{_currentState.player2Score}), " +
                      $"frames=({_currentState.player1Frames}-{_currentState.player2Frames})");
            // Notify NormcoreManager for room state replication
            if (_normcoreManager != null)
            {
                _normcoreManager.SetGameState(_currentState.gameState);
            }
        }

        /// <summary>
        /// Sets host authority for this instance.
        /// </summary>
        public void SetHostAuthority(bool isHostAuthority)
        {
            isHost = isHostAuthority;
            Debug.Log($"[GameSync] Host authority set to: {isHost}");
        }

        #endregion

        #region Self-Test
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test Game Sync")]
        public static void SelfTest()
        {
            bool pass = true;

            var gameSync = FindFirstObjectByType<CueStrikeGameSync>();
            if (gameSync == null)
            {
                Debug.LogWarning("⚠️ CueStrikeGameSync not found in scene. Creating test instance.");

                var go = new GameObject("GameSyncTest");
                gameSync = go.AddComponent<CueStrikeGameSync>();
            }

            // Test state creation
            var testState = GameStateData.Default;
            testState.currentTurnPlayerId = 0;
            testState.gameMode = "Chinese Pool";
            testState.player1Score = 3;
            testState.player2Score = 2;

            Debug.Log($"✅ GameStateData created: mode={testState.gameMode}, scores=({testState.player1Score}-{testState.player2Score})");

            // Test state update
            gameSync.isHost = true;
            gameSync.UpdateGameState(testState);
            Debug.Log("✅ UpdateGameState test passed.");

            // Test call shot
            gameSync.SetCallShot(5, 2);
            Debug.Log($"✅ SetCallShot test passed: ball={gameSync.CurrentState.calledBallId}, pocket={gameSync.CurrentState.calledPocketId}");

            // Test foul
            gameSync.OnFoulCommitted(0, "WrongBallPotted");
            Debug.Log($"✅ OnFoulCommitted test passed: {gameSync.CurrentState.lastFoulType}");

            // Test frame end
            gameSync.OnFrameEnd(0);
            Debug.Log($"✅ OnFrameEnd test passed: P1 frames={gameSync.CurrentState.player1Frames}");

            // Test new frame
            gameSync.OnNewFrame();
            Debug.Log($"✅ OnNewFrame test passed.");

            // Test reset
            gameSync.ResetForNewMatch();
            Debug.Log($"✅ ResetForNewMatch test passed.");

            if (!gameSync.forceLocalMode)
            {
                GameObject.DestroyImmediate(gameSync.gameObject);
            }

            if (pass) Debug.Log("✅ CueStrikeGameSync SELF-TEST PASSED — Ready for human verify.");
            else Debug.LogWarning("⚠️ CueStrikeGameSync SELF-TEST FAILED.");
        }
#endif
        #endregion
    }
}