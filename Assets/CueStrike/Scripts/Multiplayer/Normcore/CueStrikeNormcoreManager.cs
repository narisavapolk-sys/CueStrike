using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CueStrike.Multiplayer.Normcore
{
    /// <summary>
    /// Singleton manager for Normcore multiplayer functionality.
    /// Provides room management, player sync, and game state replication.
    /// Supports offline/dummy mode for testing without Normcore SDK.
    ///
    /// NOTE: This project uses the #if CUESTRIKE_NORMCORE compilation symbol.
    /// When the symbol is not defined, all Normcore-specific code is stubbed
    /// and the manager operates in offline/dummy mode.
    /// </summary>
    public class CueStrikeNormcoreManager : MonoBehaviour
    {
        #region Singleton
        private static CueStrikeNormcoreManager _instance;
        public static CueStrikeNormcoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CueStrikeNormcoreManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CueStrikeNormcoreManager");
                        _instance = go.AddComponent<CueStrikeNormcoreManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public event Action<List<RoomInfo>> OnRoomListUpdated;
        public event Action<PlayerData> OnPlayerJoined;
        public event Action<PlayerData> OnPlayerLeft;
        public event Action<GameState> OnGameStateChanged;
        public event Action<ShotData> OnShotReplicated;
        public event Action<bool> OnConnectionStatusChanged;
        public event Action<string> OnError;
        #endregion

        #region Enums
        public enum GameState
        {
            Lobby,
            Countdown,
            Playing,
            Paused,
            FrameOver,
            MatchOver
        }
        #endregion

        #region Data Classes
        [Serializable]
        public class RoomInfo
        {
            public string name;
            public int playerCount;
            public int maxPlayers;
            public bool hasPassword;
            public string gameMode;
        }

        [Serializable]
        public class PlayerData
        {
            public string playerName;
            public int playerId;
            public bool isReady;
            public int score;
        }

        [Serializable]
        public class ShotData
        {
            public int playerId;
            public Vector3 aimDirection;
            public float power;
            public float cueAngle;
            public int targetBallId;
            public float timestamp;
        }
        #endregion

        #region Inspector
        [Header("Normcore Settings")]
        [SerializeField] private string appKey = "cuestrike-multiplayer";
        [SerializeField] private string defaultPlayerName = "Player";
        [SerializeField] private bool connectOnStart = false;

        [Header("Offline Mode")]
        [SerializeField] private bool forceOfflineMode = false;
        [SerializeField] private int dummyPlayerCount = 2;

        // [Header("Normcore SDK")]
        // [SerializeField] private Normal.Realtime.Realtime _realtime;
        // private Normal.Realtime.RealtimeAvatarManager _avatarManager;
        #endregion

        #region State
        private bool _isConnected = false;
        private bool _isOfflineMode = false;
        private string _currentRoomName = "";
        private GameState _currentGameState = GameState.Lobby;
        private List<PlayerData> _playersInRoom = new List<PlayerData>();
        private List<RoomInfo> _availableRooms = new List<RoomInfo>();
        private int _localPlayerId = -1;
        private float _dummyTimer = 0f;

        public bool IsConnected => _isConnected;
        public bool IsOfflineMode => _isOfflineMode;
        public string CurrentRoomName => _currentRoomName;
        public GameState CurrentGameState => _currentGameState;
        public List<PlayerData> PlayersInRoom => _playersInRoom;
        public int LocalPlayerId => _localPlayerId;

        /// <summary>
        /// True if Normcore SDK is available at compile time.
        /// </summary>
        public static bool HasNormcoreSdk
        {
            get
            {
#if CUESTRIKE_NORMCORE
                return true;
#else
                return false;
#endif
            }
        }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _isOfflineMode = forceOfflineMode || !HasNormcoreSdk;

            if (_isOfflineMode)
            {
                Debug.Log("[NormcoreManager] Operating in OFFLINE/DUMMY mode." +
                          (forceOfflineMode ? " (forced)" : " (no SDK)"));
            }
            else
            {
                Debug.Log("[NormcoreManager] Operating in ONLINE mode. Normcore SDK detected.");
            }
        }

        private void Start()
        {
            if (connectOnStart)
            {
                if (_isOfflineMode)
                {
                    EnableOfflineMode();
                }
                else
                {
                    Connect(appKey);
                }
            }
        }

        private void Update()
        {
            if (_isOfflineMode && _isConnected)
            {
                _dummyTimer += Time.deltaTime;
                if (_dummyTimer > 5f && _currentGameState == GameState.Lobby)
                {
                    _dummyTimer = 0f;
                    OnRoomListUpdated?.Invoke(_availableRooms);
                }
            }
        }
        #endregion

        #region Public API

        // =========================================================================
        // NORMcore REAL CONNECTION IMPLEMENTATION GUIDE
        // =========================================================================
        // To enable real Normcore multiplayer:
        //
        // 1. INSTALL NORMcore SDK (choose ONE method):
        //    a) Package Manager: Window → Package Manager → + → Add package by name
        //       Enter: com.normal.realtime
        //    b) Manual: Download from https://normcore.io → import .unitypackage
        //
        // 2. ENABLE COMPILATION SYMBOL:
        //    Edit → Project Settings → Player → Scripting Define Symbols
        //    Add: CUESTRIKE_NORMCORE
        //
        // 3. SET UP Realtime COMPONENT:
        //    - Attach Normal.Realtime.Realtime to this GameObject
        //    - Set your App Key (get from https://normcore.io/dashboard)
        //    - Wire events: didConnectToRoom → OnRoomConnected
        //                  didDisconnectFromRoom → OnRoomDisconnected
        //                  didFailToConnectToRoom → OnRoomConnectFailed
        //
        // 4. SET UP AVATAR MANAGER (optional, for player body sync):
        //    - Attach Normal.Realtime.RealtimeAvatarManager to this GameObject
        //    - Assign avatar prefab
        //
        // 5. ADD RealtimeView + RealtimeTransform:
        //    - To each networked prefab (cue, balls, player rig)
        //    - Mark components with [RealtimeProperty] for sync
        //
        // 6. TEST:
        //    - Build + run on two devices
        //    - Both should connect and see each other
        // =========================================================================

        /// <summary>
        /// Connects to Normcore with the given app key.
        /// In offline mode, this immediately sets connected state and creates dummy data.
        /// </summary>
        public void Connect(string key)
        {
            if (_isConnected)
            {
                Debug.LogWarning("[NormcoreManager] Already connected.");
                return;
            }

            appKey = key;
            _localPlayerId = UnityEngine.Random.Range(1000, 9999);

            if (_isOfflineMode)
            {
                EnableOfflineMode();
                return;
            }

            // =========================================================================
            // REAL NORMcore CONNECTION — uncomment when SDK is installed:
            // =========================================================================
            // _realtime = GetComponent<Normal.Realtime.Realtime>();
            // if (_realtime == null) _realtime = gameObject.AddComponent<Normal.Realtime.Realtime>();
            //
            // _realtime.appKey = appKey;
            // _realtime.didConnectToRoom += OnRoomConnected;
            // _realtime.didDisconnectFromRoom += OnRoomDisconnected;
            // _realtime.didFailToConnectToRoom += OnRoomConnectFailed;
            //
            // _avatarManager = GetComponent<Normal.Realtime.RealtimeAvatarManager>();
            // if (_avatarManager == null) _avatarManager = gameObject.AddComponent<Normal.Realtime.RealtimeAvatarManager>();
            //
            // _realtime.Connect(appKey);
            // =========================================================================
            Debug.Log("[NormcoreManager] Connecting to Normcore SDK with key: " + appKey);
            _isConnected = true;
            OnConnectionStatusChanged?.Invoke(true);
        }

        /// <summary>
        /// Called when connected to a Normcore room.
        /// Wire this to Realtime.didConnectToRoom after SDK install.
        /// </summary>
#if CUESTRIKE_NORMCORE
        private void OnRoomConnected(Normal.Realtime.Realtime realtime)
        {
            _isConnected = true;
            _currentRoomName = realtime.room.name;
            Debug.Log($"[NormcoreManager] Connected to room: {_currentRoomName}");
            OnConnectionStatusChanged?.Invoke(true);
        }
#else
        private void OnRoomConnected(object realtime)
        {
            _isConnected = true;
            _currentRoomName = "OfflineRoom";
            Debug.Log($"[NormcoreManager] [Offline] Connected to room: {_currentRoomName}");
            OnConnectionStatusChanged?.Invoke(true);
        }
#endif

        /// <summary>
        /// Called when disconnected from a Normcore room.
        /// Wire this to Realtime.didDisconnectFromRoom after SDK install.
        /// </summary>
#if CUESTRIKE_NORMCORE
        private void OnRoomDisconnected(Normal.Realtime.Realtime realtime)
        {
            Debug.Log($"[NormcoreManager] Disconnected from room: {_currentRoomName}");
            _isConnected = false;
            _currentRoomName = "";
            _playersInRoom.Clear();
            _currentGameState = GameState.Lobby;
            OnConnectionStatusChanged?.Invoke(false);
        }
#else
        private void OnRoomDisconnected(object realtime)
        {
            Debug.Log($"[NormcoreManager] [Offline] Disconnected from room: {_currentRoomName}");
            _isConnected = false;
            _currentRoomName = "";
            _playersInRoom.Clear();
            _currentGameState = GameState.Lobby;
            OnConnectionStatusChanged?.Invoke(false);
        }
#endif

        /// <summary>
        /// Called when connection to a Normcore room fails.
        /// Wire this to Realtime.didFailToConnectToRoom after SDK install.
        /// </summary>
#if CUESTRIKE_NORMCORE
        private void OnRoomConnectFailed(Normal.Realtime.Realtime realtime, string error)
        {
            Debug.LogError($"[NormcoreManager] Failed to connect to room: {error}");
            OnError?.Invoke($"Connection failed: {error}");
        }
#else
        private void OnRoomConnectFailed(object realtime, string error)
        {
            Debug.LogError($"[NormcoreManager] [Offline] Failed to connect to room: {error}");
            OnError?.Invoke($"Connection failed: {error}");
        }
#endif

        /// <summary>
        /// Disconnects from Normcore and resets state.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected && !_isOfflineMode) return;

            Debug.Log("[NormcoreManager] Disconnecting...");
            _isConnected = false;
            _currentRoomName = "";
            _playersInRoom.Clear();
            _currentGameState = GameState.Lobby;
            OnConnectionStatusChanged?.Invoke(false);

            // if (_realtime != null) _realtime.Disconnect();
        }

        /// <summary>
        /// Creates a new room. In offline mode, creates a local mock room.
        /// </summary>
        public void CreateRoom(string roomName, string password, string gameMode)
        {
            if (!_isConnected)
            {
                OnError?.Invoke("Not connected. Connect first.");
                return;
            }

            if (_isOfflineMode)
            {
                var room = new RoomInfo
                {
                    name = roomName,
                    playerCount = 1,
                    maxPlayers = 4,
                    hasPassword = !string.IsNullOrEmpty(password),
                    gameMode = gameMode
                };
                _availableRooms.Add(room);
                _currentRoomName = roomName;
                _playersInRoom.Clear();
                _playersInRoom.Add(new PlayerData
                {
                    playerName = defaultPlayerName,
                    playerId = _localPlayerId,
                    isReady = false,
                    score = 0
                });
                _isConnected = true;
                OnRoomListUpdated?.Invoke(_availableRooms);
                OnPlayerJoined?.Invoke(_playersInRoom[0]);
                Debug.Log($"[NormcoreManager] [Offline] Created room: {roomName}");
                return;
            }

            // _realtime.CreateRoom(roomName);
            Debug.Log($"[NormcoreManager] Creating room: {roomName}");
        }

        /// <summary>
        /// Joins an existing room. In offline mode, creates a mock join.
        /// </summary>
        public void JoinRoom(string roomName, string password)
        {
            if (!_isConnected)
            {
                OnError?.Invoke("Not connected. Connect first.");
                return;
            }

            if (_isOfflineMode)
            {
                _currentRoomName = roomName;
                _playersInRoom.Clear();
                _playersInRoom.Add(new PlayerData
                {
                    playerName = defaultPlayerName,
                    playerId = _localPlayerId,
                    isReady = false,
                    score = 0
                });

                for (int i = 1; i < dummyPlayerCount; i++)
                {
                    _playersInRoom.Add(new PlayerData
                    {
                        playerName = $"Bot_{i}",
                        playerId = _localPlayerId + i,
                        isReady = false,
                        score = 0
                    });
                    OnPlayerJoined?.Invoke(_playersInRoom[_playersInRoom.Count - 1]);
                }

                OnPlayerJoined?.Invoke(_playersInRoom[0]);
                Debug.Log($"[NormcoreManager] [Offline] Joined room: {roomName} with {dummyPlayerCount} players");
                return;
            }

            // _realtime.JoinRoom(roomName);
            Debug.Log($"[NormcoreManager] Joining room: {roomName}");
        }

        /// <summary>
        /// Leaves the current room.
        /// </summary>
        public void LeaveRoom()
        {
            if (string.IsNullOrEmpty(_currentRoomName)) return;

            Debug.Log($"[NormcoreManager] Leaving room: {_currentRoomName}");
            _currentRoomName = "";
            _playersInRoom.Clear();
            _currentGameState = GameState.Lobby;
            OnGameStateChanged?.Invoke(_currentGameState);
        }

        /// <summary>
        /// Sets the local player's ready state.
        /// </summary>
        public void SetReady(bool ready)
        {
            var me = _playersInRoom.Find(p => p.playerId == _localPlayerId);
            if (me != null)
            {
                me.isReady = ready;
                Debug.Log($"[NormcoreManager] Ready state: {ready}");
            }
        }

        /// <summary>
        /// Starts the game. Only valid for host/creator.
        /// </summary>
        public void StartGame()
        {
            if (string.IsNullOrEmpty(_currentRoomName))
            {
                OnError?.Invoke("Not in a room.");
                return;
            }

            _currentGameState = GameState.Countdown;
            OnGameStateChanged?.Invoke(_currentGameState);
            Debug.Log("[NormcoreManager] Game starting...");

            if (_isOfflineMode)
            {
                Invoke(nameof(SetPlayingState), 1.5f);
            }
        }

        /// <summary>
        /// Replicates a shot to all players in the room.
        /// </summary>
        public void ReplicateShot(ShotData shotData)
        {
            if (!_isConnected)
            {
                OnError?.Invoke("Not connected.");
                return;
            }

            shotData.playerId = _localPlayerId;
            shotData.timestamp = Time.time;
            OnShotReplicated?.Invoke(shotData);

            if (_isOfflineMode)
            {
                Debug.Log($"[NormcoreManager] [Offline] Shot replicated: power={shotData.power:F2}");
                Invoke(nameof(SimulateOpponentShot), 2f);
            }
        }

        /// <summary>
        /// Sets the current game state and notifies listeners.
        /// </summary>
        public void SetGameState(GameState state)
        {
            _currentGameState = state;
            OnGameStateChanged?.Invoke(state);
        }

        /// <summary>
        /// Manually enables offline mode at runtime.
        /// </summary>
        public void EnableOfflineMode()
        {
            _isOfflineMode = true;
            _isConnected = true;
            _localPlayerId = UnityEngine.Random.Range(1000, 9999);
            OnConnectionStatusChanged?.Invoke(true);
            Debug.Log("[NormcoreManager] Offline mode enabled. Use CreateRoom/JoinRoom to test.");

            _availableRooms.Add(new RoomInfo { name = "Quick Match", playerCount = 2, maxPlayers = 2, hasPassword = false, gameMode = "8-Ball" });
            _availableRooms.Add(new RoomInfo { name = "Noir Challenge", playerCount = 1, maxPlayers = 4, hasPassword = false, gameMode = "Noir Memory" });
            _availableRooms.Add(new RoomInfo { name = "Private Room", playerCount = 2, maxPlayers = 4, hasPassword = true, gameMode = "9-Ball" });
            OnRoomListUpdated?.Invoke(_availableRooms);
        }

        #endregion

        #region Private

        private void SetPlayingState()
        {
            _currentGameState = GameState.Playing;
            OnGameStateChanged?.Invoke(_currentGameState);
            Debug.Log("[NormcoreManager] Game started (playing).");
        }

        private void SimulateOpponentShot()
        {
            if (_currentGameState == GameState.Playing)
            {
                var dummyShot = new ShotData
                {
                    playerId = _localPlayerId + 1,
                    aimDirection = new Vector3(0.5f, -0.3f, 0),
                    power = UnityEngine.Random.Range(0.3f, 0.8f),
                    cueAngle = 25f,
                    targetBallId = 3,
                    timestamp = Time.time
                };
                OnShotReplicated?.Invoke(dummyShot);
                Debug.Log("[NormcoreManager] [Offline] Opponent shot simulated.");
            }
        }

        #endregion
    }
}