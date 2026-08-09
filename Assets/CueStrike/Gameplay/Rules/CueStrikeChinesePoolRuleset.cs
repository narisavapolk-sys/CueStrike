using System;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.Gameplay.Rules
{
    /// <summary>
    /// Chinese 8-Ball ruleset adapter that follows the WPA Rules Manager pattern
    /// but delegates to ChinesePoolGameManager for actual rule logic.
    /// This enables Chinese Pool to be selected as a game mode via
    /// CueStrikeWPARulesManager alongside 8-Ball and 9-Ball.
    /// </summary>
    public class CueStrikeChinesePoolRuleset : MonoBehaviour
    {
        #region Nested Types (mirroring EightBall/NineBall pattern)
        public enum BallGroup
        {
            Unassigned = 0,
            Red = 1,     // Balls 1-7
            Yellow = 2   // Balls 9-15
        }

        public enum ShotResult
        {
            Legal,
            Foul,
            Win,
            Loss
        }

        public enum FoulType
        {
            None,
            CueBallPotted,
            NoBallContacted,
            WrongBallFirst,
            WrongBallPotted,
            NoCushionAfterContact,
            EightBallEarly,
            EightBallWrongPocket,
            BallOffTable,
            DoubleHit,
            PushShot,
            Miscue
        }
        #endregion

        #region Singleton
        public static CueStrikeChinesePoolRuleset Instance { get; private set; }
        #endregion

        #region Events
        public event Action<int, BallGroup> OnGroupAssigned;
        public event Action<FoulType, string> OnFoulCommitted;
        public event Action<int> OnFrameWon;
        public event Action<int> OnFrameLost;
        public event Action<ShotResult, FoulType> OnShotResolved;
        #endregion

        #region State
        private int _currentPlayer = 0;
        private bool _isBreakShot = true;

        // Cache ref to ChinesePoolGameManager
        private ChinesePoolGameManager _chinesePoolMgr;
        private bool _wired = false;
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
            EnsureWired();
        }

        private void OnDestroy()
        {
            UnwireEvents();
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Finds or creates ChinesePoolGameManager and wires event hooks.
        /// </summary>
        private void EnsureWired()
        {
            if (_wired) return;

            // Try to find existing ChinesePoolGameManager
            _chinesePoolMgr = FindFirstObjectByType<ChinesePoolGameManager>();

            if (_chinesePoolMgr == null)
            {
                // Auto-create if not present (singleton pattern will keep one)
                var go = new GameObject("ChinesePoolGameManager");
                _chinesePoolMgr = go.AddComponent<ChinesePoolGameManager>();
                Debug.Log("[CueStrike] Auto-created ChinesePoolGameManager for ruleset integration.");
            }

            WireEvents();
        }

        private void WireEvents()
        {
            if (_chinesePoolMgr == null || _wired) return;

            _chinesePoolMgr.OnPhaseChanged += HandlePhaseChanged;
            _chinesePoolMgr.OnTurnChanged += HandleTurnChanged;
            _chinesePoolMgr.OnScoreChanged += HandleScoreChanged;
            _chinesePoolMgr.OnFrameWon += HandleFrameWon;
            _chinesePoolMgr.OnFrameLost += HandleFrameLost;
            _chinesePoolMgr.OnFoulCommitted += HandleFoulCommitted;
            _chinesePoolMgr.OnBallGroupAssigned += HandleBallGroupAssigned;

            _wired = true;
        }

        private void UnwireEvents()
        {
            if (_chinesePoolMgr == null || !_wired) return;

            _chinesePoolMgr.OnPhaseChanged -= HandlePhaseChanged;
            _chinesePoolMgr.OnTurnChanged -= HandleTurnChanged;
            _chinesePoolMgr.OnScoreChanged -= HandleScoreChanged;
            _chinesePoolMgr.OnFrameWon -= HandleFrameWon;
            _chinesePoolMgr.OnFrameLost -= HandleFrameLost;
            _chinesePoolMgr.OnFoulCommitted -= HandleFoulCommitted;
            _chinesePoolMgr.OnBallGroupAssigned -= HandleBallGroupAssigned;

            _wired = false;
        }
        #endregion

        #region Event Handlers (translate ChinesePool events → ruleset events)
        private void HandlePhaseChanged(ChinesePoolMatchState phase)
        {
            // ChinesePoolMatchState -> GamePhase handled by WPARulesManager
        }

        private void HandleTurnChanged(int playerIndex)
        {
            _currentPlayer = playerIndex;
        }

        private void HandleScoreChanged(int score1, int score2)
        {
            // Score tracking can be extended if needed
        }

        private void HandleFrameWon(int playerIndex)
        {
            OnFrameWon?.Invoke(playerIndex);
            OnShotResolved?.Invoke(ShotResult.Win, FoulType.None);
        }

        private void HandleFrameLost(int playerIndex)
        {
            OnFrameLost?.Invoke(playerIndex);
            OnShotResolved?.Invoke(ShotResult.Loss, FoulType.EightBallEarly);
        }

        private void HandleFoulCommitted(int playerIndex, string foulType)
        {
            FoulType mappedFoul = MapFoulType(foulType);
            OnFoulCommitted?.Invoke(mappedFoul, foulType);
            OnShotResolved?.Invoke(ShotResult.Foul, mappedFoul);
        }

        private void HandleBallGroupAssigned(int playerIndex, int groupCode)
        {
            BallGroup group = groupCode switch
            {
                1 => BallGroup.Red,
                2 => BallGroup.Yellow,
                _ => BallGroup.Unassigned
            };
            OnGroupAssigned?.Invoke(playerIndex, group);
        }

        /// <summary>
        /// Maps string foul type from ChinesePool to our FoulType enum.
        /// </summary>
        private FoulType MapFoulType(string foulType)
        {
            if (string.IsNullOrEmpty(foulType)) return FoulType.None;

            if (foulType.Contains("CueBall") || foulType.Contains("cueball") || foulType.Contains("White"))
                return FoulType.CueBallPotted;
            if (foulType.Contains("WrongBall") || foulType.Contains("Wrong ball"))
                return FoulType.WrongBallPotted;
            if (foulType.Contains("EightBall") || foulType.Contains("8Ball"))
                return FoulType.EightBallEarly;
            if (foulType.Contains("NoBall") || foulType.Contains("No cushion"))
                return FoulType.NoBallContacted;
            if (foulType.Contains("Double") || foulType.Contains("double"))
                return FoulType.DoubleHit;
            if (foulType.Contains("Push") || foulType.Contains("push"))
                return FoulType.PushShot;
            if (foulType.Contains("Miscue") || foulType.Contains("miscue"))
                return FoulType.Miscue;
            if (foulType.Contains("OffTable") || foulType.Contains("off table"))
                return FoulType.BallOffTable;

            return FoulType.NoBallContacted;
        }
        #endregion

        #region Public API (mirrors EightBall/NineBall ruleset interface)

        /// <summary>
        /// Resets for a new frame.
        /// </summary>
        public void ResetFrame()
        {
            EnsureWired();
            if (_chinesePoolMgr != null)
            {
                _chinesePoolMgr.StartNewFrame();
                _isBreakShot = true;
            }
        }

        /// <summary>
        /// Sets the current player index.
        /// </summary>
        public void SetCurrentPlayer(int playerIndex)
        {
            _currentPlayer = playerIndex;
        }

        /// <summary>
        /// Gets the current player index.
        /// </summary>
        public int GetCurrentPlayer() => _currentPlayer;

        /// <summary>
        /// Called when a ball is potted. Routes to ChinesePoolGameManager.
        /// </summary>
        public ShotResult OnBallPotted(
            int ballId,
            int pocketIndex,
            int playerIndex,
            bool isBreakShot,
            List<int> ballsContacted,
            bool cueBallPotted,
            bool ballHitCushion)
        {
            EnsureWired();
            if (_chinesePoolMgr == null)
            {
                Debug.LogError("[CueStrike] ChinesePoolGameManager not available!");
                return ShotResult.Foul;
            }

            _isBreakShot = isBreakShot;
            _chinesePoolMgr.SetCallShot(ballId, pocketIndex);

            // Build ShotResult and route to ChinesePoolGameManager
            // This is called from WPARulesManager.OnBallPotted pipeline
            return ShotResult.Legal;
        }

        /// <summary>
        /// Processes a completed shot result through ChinesePoolGameManager.
        /// Called after all ball potted events are collected for the shot.
        /// </summary>
        public void ProcessShot(ChinesePool.ShotResult shotResult)
        {
            EnsureWired();
            _chinesePoolMgr?.ProcessShotResult(shotResult);
        }

        /// <summary>
        /// Determines if the turn should change after the shot.
        /// </summary>
        public bool ShouldTurnChange(ShotResult result, bool ballPotted)
        {
            if (result == ShotResult.Win || result == ShotResult.Loss) return false;

            // In Chinese Pool: foul = turn change, no pot = turn change
            if (result == ShotResult.Foul) return true;
            if (!ballPotted) return true;

            // Potting your group ball = continue
            return false;
        }

        /// <summary>
        /// Returns true if frame is over.
        /// </summary>
        public bool IsFrameOver()
        {
            return _chinesePoolMgr != null && _chinesePoolMgr.IsFrameOver();
        }

        /// <summary>
        /// Returns the frame winner index (0 or 1), or -1 if not over.
        /// </summary>
        public int GetFrameWinner()
        {
            return _chinesePoolMgr != null ? _chinesePoolMgr.GetFrameWinner() : -1;
        }

        /// <summary>
        /// Gets the ball group assigned to a player.
        /// </summary>
        public BallGroup GetPlayerGroup(int playerIndex)
        {
            if (_chinesePoolMgr == null) return BallGroup.Unassigned;

            var poolGroup = (playerIndex == 0)
                ? _chinesePoolMgr.player1Group
                : _chinesePoolMgr.player2Group;

            return poolGroup switch
            {
                ChinesePoolGameManager.BallGroup.Red => BallGroup.Red,
                ChinesePoolGameManager.BallGroup.Yellow => BallGroup.Yellow,
                _ => BallGroup.Unassigned
            };
        }

        /// <summary>
        /// Returns true if table is still open (no group assigned).
        /// </summary>
        public bool IsOpenTable()
        {
            if (_chinesePoolMgr == null) return true;
            return _chinesePoolMgr.currentPhase == ChinesePoolMatchState.OpenTable
                || _chinesePoolMgr.currentPhase == ChinesePoolMatchState.Break;
        }
        #endregion
    }
}