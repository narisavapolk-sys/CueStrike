using System;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.Gameplay.Rules;

namespace CueStrike.Gameplay.Rules
{
    /// <summary>
    /// Main WPA Rules Manager - coordinates between 8-Ball, 9-Ball, and Chinese Pool rulesets.
    /// Provides unified interface for game modes and manages active ruleset.
    /// </summary>
    public class CueStrikeWPARulesManager : MonoBehaviour
    {
        public enum GameMode
        {
            EightBall = 0,
            NineBall = 1,
            ChinesePool = 2
        }

        public enum GamePhase
        {
            Waiting,
            Break,
            OpenTable,
            Assigned,
            Playing,
            FrameOver
        }

        // Singleton
        public static CueStrikeWPARulesManager Instance { get; private set; }

        // Events
        public event Action<GameMode> OnGameModeChanged;
        public event Action<GamePhase> OnGamePhaseChanged;
        public event Action<int, CueStrikeEightBallWPARuleset.BallGroup> OnEightBallGroupAssigned;
        public event Action<CueStrikeEightBallWPARuleset.FoulType, string> OnEightBallFoul;
        public event Action<CueStrikeNineBallWPARuleset.FoulType, string> OnNineBallFoul;
        public event Action<int> OnFrameWon;
        public event Action<int> OnFrameLost;
        public event Action<CueStrikeEightBallWPARuleset.ShotResult, CueStrikeEightBallWPARuleset.FoulType> OnEightBallShotResolved;
        public event Action<CueStrikeNineBallWPARuleset.ShotResult, CueStrikeNineBallWPARuleset.FoulType> OnNineBallShotResolved;
        public event Action<CueStrikeNineBallWPARuleset.PushOutState> OnPushOutStateChanged;
        public event Action<int> OnConsecutiveFoulsChanged;

        // Chinese Pool events
        public event Action<int, CueStrikeChinesePoolRuleset.BallGroup> OnChinesePoolGroupAssigned;
        public event Action<CueStrikeChinesePoolRuleset.FoulType, string> OnChinesePoolFoul;
        public event Action<CueStrikeChinesePoolRuleset.ShotResult, CueStrikeChinesePoolRuleset.FoulType> OnChinesePoolShotResolved;

        // State
        private GameMode _currentMode = GameMode.EightBall;
        private GamePhase _currentPhase = GamePhase.Waiting;
        private int _currentPlayer = 0;

        // Ruleset references
        private CueStrikeEightBallWPARuleset _eightBallRuleset;
        private CueStrikeNineBallWPARuleset _nineBallRuleset;
        private CueStrikeChinesePoolRuleset _chinesePoolRuleset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Find or create ruleset components
            _eightBallRuleset = GetComponent<CueStrikeEightBallWPARuleset>();
            _nineBallRuleset = GetComponent<CueStrikeNineBallWPARuleset>();
            _chinesePoolRuleset = GetComponent<CueStrikeChinesePoolRuleset>();

            if (_eightBallRuleset == null)
                _eightBallRuleset = gameObject.AddComponent<CueStrikeEightBallWPARuleset>();
            if (_nineBallRuleset == null)
                _nineBallRuleset = gameObject.AddComponent<CueStrikeNineBallWPARuleset>();
            if (_chinesePoolRuleset == null)
                _chinesePoolRuleset = gameObject.AddComponent<CueStrikeChinesePoolRuleset>();

            SubscribeToRulesetEvents();
        }

        private void SubscribeToRulesetEvents()
        {
            // 8-Ball events
            _eightBallRuleset.OnGroupAssigned += (player, group) =>
            {
                OnEightBallGroupAssigned?.Invoke(player, group);
                SetPhase(GamePhase.Assigned);
            };
            _eightBallRuleset.OnFoulCommitted += (foul, reason) => OnEightBallFoul?.Invoke(foul, reason);
            _eightBallRuleset.OnFrameWon += (player) =>
            {
                OnFrameWon?.Invoke(player);
                SetPhase(GamePhase.FrameOver);
            };
            _eightBallRuleset.OnFrameLost += (player) => OnFrameLost?.Invoke(player);
            _eightBallRuleset.OnShotResolved += (result, foul) => OnEightBallShotResolved?.Invoke(result, foul);

            // 9-Ball events
            _nineBallRuleset.OnFoulCommitted += (foul, reason) => OnNineBallFoul?.Invoke(foul, reason);
            _nineBallRuleset.OnFrameWon += (player) =>
            {
                OnFrameWon?.Invoke(player);
                SetPhase(GamePhase.FrameOver);
            };
            _nineBallRuleset.OnFrameLost += (player) => OnFrameLost?.Invoke(player);
            _nineBallRuleset.OnShotResolved += (result, foul) => OnNineBallShotResolved?.Invoke(result, foul);
            _nineBallRuleset.OnPushOutStateChanged += (state) => OnPushOutStateChanged?.Invoke(state);
            _nineBallRuleset.OnConsecutiveFoulsChanged += (count) => OnConsecutiveFoulsChanged?.Invoke(count);

            // Chinese Pool events
            _chinesePoolRuleset.OnGroupAssigned += (player, group) =>
            {
                OnChinesePoolGroupAssigned?.Invoke(player, group);
                SetPhase(GamePhase.Assigned);
            };
            _chinesePoolRuleset.OnFoulCommitted += (foul, reason) => OnChinesePoolFoul?.Invoke(foul, reason);
            _chinesePoolRuleset.OnFrameWon += (player) =>
            {
                OnFrameWon?.Invoke(player);
                SetPhase(GamePhase.FrameOver);
            };
            _chinesePoolRuleset.OnFrameLost += (player) => OnFrameLost?.Invoke(player);
            _chinesePoolRuleset.OnShotResolved += (result, foul) => OnChinesePoolShotResolved?.Invoke(result, foul);
        }

        private void Start()
        {
            SetMode(_currentMode);
        }

        /// <summary>
        /// Sets the active game mode (8-Ball, 9-Ball, or Chinese Pool).
        /// </summary>
        public void SetMode(GameMode mode)
        {
            if (_currentMode == mode) return;

            _currentMode = mode;
            OnGameModeChanged?.Invoke(mode);

            // Reset all rulesets
            _eightBallRuleset.ResetFrame();
            _nineBallRuleset.ResetFrame();
            _chinesePoolRuleset.ResetFrame();

            SetPhase(GamePhase.Waiting);
        }

        /// <summary>
        /// Gets the current game mode.
        /// </summary>
        public GameMode GetCurrentMode() => _currentMode;

        /// <summary>
        /// Gets the current game phase.
        /// </summary>
        public GamePhase GetCurrentPhase() => _currentPhase;

        /// <summary>
        /// Sets the current game phase.
        /// </summary>
        private void SetPhase(GamePhase phase)
        {
            if (_currentPhase == phase) return;
            _currentPhase = phase;
            OnGamePhaseChanged?.Invoke(phase);
        }

        /// <summary>
        /// Sets the current player.
        /// </summary>
        public void SetCurrentPlayer(int playerIndex)
        {
            _currentPlayer = playerIndex;
            _eightBallRuleset.SetCurrentPlayer(playerIndex);
            _nineBallRuleset.SetCurrentPlayer(playerIndex);
            _chinesePoolRuleset.SetCurrentPlayer(playerIndex);
        }

        /// <summary>
        /// Gets the current player index.
        /// </summary>
        public int GetCurrentPlayer() => _currentPlayer;

        /// <summary>
        /// Gets the opponent player index.
        /// </summary>
        public int GetOpponent() => 1 - _currentPlayer;

        /// <summary>
        /// Called when a ball is potted. Routes to active ruleset.
        /// </summary>
        public object OnBallPotted(int ballId, int pocketIndex, int playerIndex, bool isBreakShot, List<int> ballsContacted, bool cueBallPotted, bool ballHitCushion)
        {
            SetCurrentPlayer(playerIndex);

            switch (_currentMode)
            {
                case GameMode.EightBall:
                    var eightResult = _eightBallRuleset.OnBallPotted(ballId, pocketIndex, playerIndex, isBreakShot, ballsContacted, cueBallPotted, ballHitCushion);
                    UpdateEightBallPhase(eightResult, isBreakShot);
                    return eightResult;

                case GameMode.ChinesePool:
                    var chineseResult = _chinesePoolRuleset.OnBallPotted(ballId, pocketIndex, playerIndex, isBreakShot, ballsContacted, cueBallPotted, ballHitCushion);
                    UpdateChinesePoolPhase(chineseResult, isBreakShot);
                    return chineseResult;

                default:
                    var nineResult = _nineBallRuleset.OnBallPotted(ballId, pocketIndex, playerIndex, isBreakShot, ballsContacted, cueBallPotted, ballHitCushion);
                    UpdateNineBallPhase(nineResult, isBreakShot);
                    return nineResult;
            }
        }

        private void UpdateEightBallPhase(CueStrikeEightBallWPARuleset.ShotResult result, bool isBreakShot)
        {
            if (isBreakShot) SetPhase(GamePhase.Break);
            else if (_eightBallRuleset.IsOpenTable()) SetPhase(GamePhase.OpenTable);
            else if (result == CueStrikeEightBallWPARuleset.ShotResult.Win || result == CueStrikeEightBallWPARuleset.ShotResult.Loss) SetPhase(GamePhase.FrameOver);
            else SetPhase(GamePhase.Playing);
        }

        private void UpdateNineBallPhase(CueStrikeNineBallWPARuleset.ShotResult result, bool isBreakShot)
        {
            if (isBreakShot) SetPhase(GamePhase.Break);
            else if (result == CueStrikeNineBallWPARuleset.ShotResult.Win || result == CueStrikeNineBallWPARuleset.ShotResult.Loss) SetPhase(GamePhase.FrameOver);
            else SetPhase(GamePhase.Playing);
        }

        private void UpdateChinesePoolPhase(CueStrikeChinesePoolRuleset.ShotResult result, bool isBreakShot)
        {
            if (isBreakShot) SetPhase(GamePhase.Break);
            else if (_chinesePoolRuleset.IsOpenTable()) SetPhase(GamePhase.OpenTable);
            else if (result == CueStrikeChinesePoolRuleset.ShotResult.Win || result == CueStrikeChinesePoolRuleset.ShotResult.Loss) SetPhase(GamePhase.FrameOver);
            else SetPhase(GamePhase.Playing);
        }

        /// <summary>
        /// Determines if turn should change after shot.
        /// </summary>
        public bool ShouldTurnChange(object shotResult, bool ballPotted)
        {
            switch (_currentMode)
            {
                case GameMode.EightBall:
                    return _eightBallRuleset.ShouldTurnChange((CueStrikeEightBallWPARuleset.ShotResult)shotResult, ballPotted);
                case GameMode.ChinesePool:
                    return _chinesePoolRuleset.ShouldTurnChange((CueStrikeChinesePoolRuleset.ShotResult)shotResult, ballPotted);
                default:
                    return _nineBallRuleset.ShouldTurnChange((CueStrikeNineBallWPARuleset.ShotResult)shotResult, ballPotted);
            }
        }

        /// <summary>
        /// Advances to next player.
        /// </summary>
        public void NextPlayer()
        {
            _currentPlayer = GetOpponent();
            _eightBallRuleset.SetCurrentPlayer(_currentPlayer);
            _nineBallRuleset.SetCurrentPlayer(_currentPlayer);
            _chinesePoolRuleset.SetCurrentPlayer(_currentPlayer);
            _nineBallRuleset.ResetConsecutiveFouls();
        }

        /// <summary>
        /// Starts a new frame.
        /// </summary>
        public void StartNewFrame()
        {
            _eightBallRuleset.ResetFrame();
            _nineBallRuleset.ResetFrame();
            _chinesePoolRuleset.ResetFrame();
            _currentPlayer = 0;
            SetPhase(GamePhase.Waiting);
        }

        /// <summary>
        /// Gets the 8-Ball ruleset for direct access.
        /// </summary>
        public CueStrikeEightBallWPARuleset GetEightBallRuleset() => _eightBallRuleset;

        /// <summary>
        /// Gets the 9-Ball ruleset for direct access.
        /// </summary>
        public CueStrikeNineBallWPARuleset GetNineBallRuleset() => _nineBallRuleset;

        /// <summary>
        /// Gets the Chinese Pool ruleset for direct access.
        /// </summary>
        public CueStrikeChinesePoolRuleset GetChinesePoolRuleset() => _chinesePoolRuleset;

        /// <summary>
        /// Checks if frame is over.
        /// </summary>
        public bool IsFrameOver()
        {
            switch (_currentMode)
            {
                case GameMode.EightBall: return _eightBallRuleset.IsFrameOver();
                case GameMode.ChinesePool: return _chinesePoolRuleset.IsFrameOver();
                default: return _nineBallRuleset.IsFrameOver();
            }
        }

        /// <summary>
        /// Gets the frame winner.
        /// </summary>
        public int GetFrameWinner()
        {
            switch (_currentMode)
            {
                case GameMode.EightBall: return _eightBallRuleset.GetFrameWinner();
                case GameMode.ChinesePool: return _chinesePoolRuleset.GetFrameWinner();
                default: return _nineBallRuleset.GetFrameWinner();
            }
        }

        /// <summary>
        /// Declines push-out (9-Ball only).
        /// </summary>
        public void DeclinePushOut()
        {
            if (_currentMode == GameMode.NineBall)
                _nineBallRuleset.DeclinePushOut();
        }

        /// <summary>
        /// Gets push-out state (9-Ball only).
        /// </summary>
        public CueStrikeNineBallWPARuleset.PushOutState GetPushOutState()
        {
            if (_currentMode == GameMode.NineBall)
                return _nineBallRuleset.GetPushOutState();
            return CueStrikeNineBallWPARuleset.PushOutState.NotAvailable;
        }

        /// <summary>
        /// Gets consecutive fouls count (9-Ball only).
        /// </summary>
        public int GetConsecutiveFouls()
        {
            if (_currentMode == GameMode.NineBall)
                return _nineBallRuleset.GetConsecutiveFouls();
            return 0;
        }

        /// <summary>
        /// Gets lowest ball on table (9-Ball only).
        /// </summary>
        public int GetLowestBallOnTable()
        {
            if (_currentMode == GameMode.NineBall)
                return _nineBallRuleset.GetLowestBallOnTable();
            return 1;
        }

        /// <summary>
        /// Gets 8-Ball group for player.
        /// </summary>
        public CueStrikeEightBallWPARuleset.BallGroup GetEightBallGroup(int playerIndex)
        {
            if (_currentMode == GameMode.EightBall)
                return _eightBallRuleset.GetPlayerGroup(playerIndex);
            return CueStrikeEightBallWPARuleset.BallGroup.Unassigned;
        }

        /// <summary>
        /// Gets Chinese Pool group for player.
        /// </summary>
        public CueStrikeChinesePoolRuleset.BallGroup GetChinesePoolGroup(int playerIndex)
        {
            if (_currentMode == GameMode.ChinesePool)
                return _chinesePoolRuleset.GetPlayerGroup(playerIndex);
            return CueStrikeChinesePoolRuleset.BallGroup.Unassigned;
        }

        /// <summary>
        /// Checks if 8-Ball table is open.
        /// </summary>
        public bool IsEightBallOpenTable()
        {
            if (_currentMode == GameMode.EightBall)
                return _eightBallRuleset.IsOpenTable();
            return false;
        }

        /// <summary>
        /// Checks if Chinese Pool table is open.
        /// </summary>
        public bool IsChinesePoolOpenTable()
        {
            if (_currentMode == GameMode.ChinesePool)
                return _chinesePoolRuleset.IsOpenTable();
            return false;
        }
    }
}