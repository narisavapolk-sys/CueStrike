using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Multiplayer.Normcore
{
    /// <summary>
    /// Serializable room state for Normcore multiplayer sessions.
    /// Holds all replicated game state: room info, players, scores, timer.
    /// Used by CueStrikeNormcoreManager for state synchronization.
    /// </summary>
    [Serializable]
    public class CueStrikeNormcoreRoomState
    {
        #region Fields
        [SerializeField] private string _roomName = "";
        [SerializeField] private List<CueStrikeNormcoreManager.PlayerData> _players = new();
        [SerializeField] private CueStrikeNormcoreManager.GameState _state = CueStrikeNormcoreManager.GameState.Lobby;
        [SerializeField] private string _gameMode = "8-Ball";
        [SerializeField] private int _currentTurn = 0;
        [SerializeField] private int[] _scores = new int[0];
        [SerializeField] private float _matchTimer = 0f;
        [SerializeField] private string _lastAction = "";
        #endregion

        #region Properties
        /// <summary>Name of the current room.</summary>
        public string RoomName
        {
            get => _roomName;
            set => _roomName = value;
        }

        /// <summary>List of players currently in the room.</summary>
        public List<CueStrikeNormcoreManager.PlayerData> Players
        {
            get => _players;
            set => _players = value ?? new List<CueStrikeNormcoreManager.PlayerData>();
        }

        /// <summary>Current game state (Lobby, Countdown, Playing, etc).</summary>
        public CueStrikeNormcoreManager.GameState State
        {
            get => _state;
            set => _state = value;
        }

        /// <summary>Game mode identifier (e.g. "8-Ball", "9-Ball", "Chinese Pool", "Noir Memory").</summary>
        public string GameMode
        {
            get => _gameMode;
            set => _gameMode = value;
        }

        /// <summary>Index of the player whose turn it is.</summary>
        public int CurrentTurn
        {
            get => _currentTurn;
            set => _currentTurn = Mathf.Max(0, value);
        }

        /// <summary>Array of scores, indexed by player position.</summary>
        public int[] Scores
        {
            get => _scores;
            set => _scores = value ?? new int[0];
        }

        /// <summary>Match timer value in seconds.</summary>
        public float MatchTimer
        {
            get => _matchTimer;
            set => _matchTimer = Mathf.Max(0, value);
        }

        /// <summary>Description of the last action taken (for UI/log).</summary>
        public string LastAction
        {
            get => _lastAction;
            set => _lastAction = value ?? "";
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a player to the room state. Updates scores array if needed.
        /// </summary>
        public void AddPlayer(CueStrikeNormcoreManager.PlayerData player)
        {
            if (player == null)
            {
                Debug.LogWarning("[NormcoreRoomState] Cannot add null player.");
                return;
            }

            if (_players.Exists(p => p.playerId == player.playerId))
            {
                Debug.LogWarning($"[NormcoreRoomState] Player {player.playerName} (ID {player.playerId}) already in room.");
                return;
            }

            _players.Add(player);
            ResizeScoresArray();
            UpdateScoresFromPlayers();
            _lastAction = $"{player.playerName} joined";
            Debug.Log($"[NormcoreRoomState] Player '{player.playerName}' added. Total: {_players.Count}");
        }

        /// <summary>
        /// Removes a player by ID. Updates scores array if needed.
        /// </summary>
        public void RemovePlayer(int playerId)
        {
            int index = _players.FindIndex(p => p.playerId == playerId);
            if (index < 0)
            {
                Debug.LogWarning($"[NormcoreRoomState] Player ID {playerId} not found.");
                return;
            }

            var removed = _players[index];
            _players.RemoveAt(index);
            ResizeScoresArray();
            UpdateScoresFromPlayers();
            _lastAction = $"{removed.playerName} left";
            Debug.Log($"[NormcoreRoomState] Player '{removed.playerName}' removed. Total: {_players.Count}");
        }

        /// <summary>
        /// Updates a player's score by playerId.
        /// </summary>
        public void UpdatePlayerScore(int playerId, int newScore)
        {
            var player = _players.Find(p => p.playerId == playerId);
            if (player != null)
            {
                int oldScore = player.score;
                player.score = Mathf.Max(0, newScore);

                int index = _players.FindIndex(p => p.playerId == playerId);
                if (index >= 0 && index < _scores.Length)
                {
                    _scores[index] = player.score;
                }

                _lastAction = $"{player.playerName} score: {oldScore} -> {player.score}";
                Debug.Log($"[NormcoreRoomState] {_lastAction}");
            }
            else
            {
                Debug.LogWarning($"[NormcoreRoomState] Cannot update score: player ID {playerId} not found.");
            }
        }

        /// <summary>
        /// Sets the game state and logs the transition.
        /// </summary>
        public void SetState(CueStrikeNormcoreManager.GameState newState)
        {
            var oldState = _state;
            _state = newState;
            _lastAction = $"State: {oldState} -> {newState}";
            Debug.Log($"[NormcoreRoomState] {_lastAction}");
        }

        /// <summary>
        /// Creates a deep copy of this room state.
        /// </summary>
        public CueStrikeNormcoreRoomState Clone()
        {
            var clone = new CueStrikeNormcoreRoomState
            {
                _roomName = _roomName,
                _state = _state,
                _gameMode = _gameMode,
                _currentTurn = _currentTurn,
                _matchTimer = _matchTimer,
                _lastAction = _lastAction,
                _players = new List<CueStrikeNormcoreManager.PlayerData>(),
                _scores = new int[_scores.Length]
            };

            foreach (var p in _players)
            {
                clone._players.Add(new CueStrikeNormcoreManager.PlayerData
                {
                    playerName = p.playerName,
                    playerId = p.playerId,
                    isReady = p.isReady,
                    score = p.score
                });
            }

            Array.Copy(_scores, clone._scores, _scores.Length);
            return clone;
        }

        #endregion

        #region Private

        private void ResizeScoresArray()
        {
            if (_scores.Length != _players.Count)
            {
                Array.Resize(ref _scores, _players.Count);
            }
        }

        private void UpdateScoresFromPlayers()
        {
            for (int i = 0; i < _players.Count && i < _scores.Length; i++)
            {
                _scores[i] = _players[i].score;
            }
        }

        #endregion
    }
}