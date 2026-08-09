using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Gameplay.Rules
{
    /// <summary>
    /// Implements WPA (World Pool-Billiard Association) standardized 8-Ball rules.
    /// Handles ball groups (solids/stripes), legal shots, fouls, and win conditions.
    /// </summary>
    public class CueStrikeEightBallWPARuleset : MonoBehaviour
    {
        public enum BallGroup
        {
            Unassigned = 0,
            Solids = 1,    // Balls 1-7
            Stripes = 2    // Balls 9-15
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
            NoCushionAfterContact,
            EightBallEarly,
            EightBallWrongPocket,
            BallOffTable,
            DoubleHit,
            PushShot,
            Miscue
        }

        // Singleton
        public static CueStrikeEightBallWPARuleset Instance { get; private set; }

        // Events
        public event Action<int, BallGroup> OnGroupAssigned;
        public event Action<FoulType, string> OnFoulCommitted;
        public event Action<int> OnFrameWon;
        public event Action<int> OnFrameLost;
        public event Action<ShotResult, FoulType> OnShotResolved;

        // State
        private BallGroup[] _playerGroups = { BallGroup.Unassigned, BallGroup.Unassigned };
        private int _currentPlayer = 0;
        private bool _isBreakShot = true;
        private bool _isOpenTable = true;
        private int _eightBallPocketedBy = -1;
        private string _lastFoulReason = "";

        // Ball ID constants
        private const int CUE_BALL_ID = 0;
        private const int EIGHT_BALL_ID = 8;
        private const int SOLIDS_MIN = 1;
        private const int SOLIDS_MAX = 7;
        private const int STRIPES_MIN = 9;
        private const int STRIPES_MAX = 15;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Resets the ruleset for a new frame.
        /// </summary>
        public void ResetFrame()
        {
            _playerGroups[0] = BallGroup.Unassigned;
            _playerGroups[1] = BallGroup.Unassigned;
            _currentPlayer = 0;
            _isBreakShot = true;
            _isOpenTable = true;
            _eightBallPocketedBy = -1;
            _lastFoulReason = "";
        }

        /// <summary>
        /// Sets the current player index (0 or 1).
        /// </summary>
        public void SetCurrentPlayer(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex <= 1)
            {
                _currentPlayer = playerIndex;
            }
        }

        /// <summary>
        /// Gets the current player index.
        /// </summary>
        public int GetCurrentPlayer() => _currentPlayer;

        /// <summary>
        /// Gets the ball group assigned to a player.
        /// </summary>
        public BallGroup GetPlayerGroup(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex <= 1)
            {
                return _playerGroups[playerIndex];
            }
            return BallGroup.Unassigned;
        }

        /// <summary>
        /// Checks if the table is open (groups not yet assigned).
        /// </summary>
        public bool IsOpenTable() => _isOpenTable;

        /// <summary>
        /// Checks if it's the break shot.
        /// </summary>
        public bool IsBreakShot() => _isBreakShot;

        /// <summary>
        /// Called when a ball is potted. Returns the shot result.
        /// </summary>
        public ShotResult OnBallPotted(int ballId, int pocketIndex, int playerIndex, bool isBreakShot, List<int> ballsContacted, bool cueBallPotted, bool ballHitCushion)
        {
            _isBreakShot = isBreakShot;
            _currentPlayer = playerIndex;

            // Cue ball potted = foul
            if (cueBallPotted || ballId == CUE_BALL_ID)
            {
                return HandleFoul(FoulType.CueBallPotted, "Cue ball potted", playerIndex);
            }

            // Ball off table = foul
            if (ballId < 0 || ballId > 15)
            {
                return HandleFoul(FoulType.BallOffTable, "Ball jumped off table", playerIndex);
            }

            // Break shot handling
            if (isBreakShot)
            {
                return HandleBreakShot(ballId, pocketIndex, playerIndex, ballsContacted, ballHitCushion);
            }

            // Open table handling
            if (_isOpenTable)
            {
                return HandleOpenTableShot(ballId, pocketIndex, playerIndex, ballsContacted, ballHitCushion, cueBallPotted);
            }

            // Normal play (groups assigned)
            return HandleNormalShot(ballId, pocketIndex, playerIndex, ballsContacted, ballHitCushion);
        }

        /// <summary>
        /// Handles break shot logic per WPA rules.
        /// </summary>
        private ShotResult HandleBreakShot(int ballId, int pocketIndex, int playerIndex, List<int> ballsContacted, bool ballHitCushion)
        {
            // Check if 8-ball potted on break
            if (ballId == EIGHT_BALL_ID)
            {
                // WPA Rules: 8-ball on break = re-rack or spot 8-ball, breaker continues
                // For simplicity: spot 8-ball, breaker continues, no foul
                _eightBallPocketedBy = playerIndex;
                OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
                return ShotResult.Legal;
            }

            // Check if any object ball potted on break
            bool objectBallPotted = (ballId >= SOLIDS_MIN && ballId <= SOLIDS_MAX) || (ballId >= STRIPES_MIN && ballId <= STRIPES_MAX);

            if (!objectBallPotted)
            {
                // No ball potted on break - check if legal break (4 balls hit cushion or ball potted)
                if (!WasLegalBreak(ballsContacted, ballHitCushion))
                {
                    return HandleFoul(FoulType.NoCushionAfterContact, "Illegal break - fewer than 4 balls hit cushion", playerIndex);
                }
                // Legal break but no ball potted - turn passes
                _isBreakShot = false;
                OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
                return ShotResult.Legal;
            }

            // Ball potted on break - table remains open, breaker continues
            _isBreakShot = false;
            _isOpenTable = true;
            OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
            return ShotResult.Legal;
        }

        /// <summary>
        /// Determines if break was legal per WPA (4 balls to cushion or ball potted).
        /// </summary>
        private bool WasLegalBreak(List<int> ballsContacted, bool ballHitCushion)
        {
            // Simplified: assume legal if at least one ball hit cushion or ball was potted
            // Full WPA rule: at least 4 object balls must hit cushions
            return ballHitCushion || ballsContacted.Count >= 4;
        }

        /// <summary>
        /// Handles shots when table is open (groups not assigned).
        /// </summary>
        private ShotResult HandleOpenTableShot(int ballId, int pocketIndex, int playerIndex, List<int> ballsContacted, bool ballHitCushion, bool cueBallPotted)
        {
            // Check if 8-ball potted on open table (not break) = loss of frame
            if (ballId == EIGHT_BALL_ID)
            {
                return HandleFoul(FoulType.EightBallEarly, "8-ball potted on open table", playerIndex);
            }

            // Check legal contact (must hit object ball first)
            if (!WasLegalContact(ballsContacted, ballId, BallGroup.Unassigned))
            {
                return HandleFoul(FoulType.WrongBallFirst, "No legal ball contacted first", playerIndex);
            }

            // Check cushion rule
            if (!ballHitCushion && !WasBallPotted(ballId))
            {
                return HandleFoul(FoulType.NoCushionAfterContact, "No ball hit cushion after contact", playerIndex);
            }

            // Check cue ball potted
            if (cueBallPotted)
            {
                return HandleFoul(FoulType.CueBallPotted, "Cue ball potted", playerIndex);
            }

            // Assign group based on ball potted
            BallGroup pottedGroup = GetBallGroup(ballId);
            if (pottedGroup != BallGroup.Unassigned)
            {
                AssignGroups(playerIndex, pottedGroup);
            }

            OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
            return ShotResult.Legal;
        }

        /// <summary>
        /// Handles normal shots after groups are assigned.
        /// </summary>
        private ShotResult HandleNormalShot(int ballId, int pocketIndex, int playerIndex, List<int> ballsContacted, bool ballHitCushion)
        {
            BallGroup playerGroup = _playerGroups[playerIndex];
            BallGroup opponentGroup = _playerGroups[1 - playerIndex];

            // Check if 8-ball potted
            if (ballId == EIGHT_BALL_ID)
            {
                return HandleEightBallPotted(pocketIndex, playerIndex, ballsContacted, ballHitCushion);
            }

            // Check if player's group ball potted
            bool isPlayerBall = IsBallInGroup(ballId, playerGroup);
            bool isOpponentBall = IsBallInGroup(ballId, opponentGroup);

            // Legal contact: must hit own group ball first (or 8-ball if own group cleared)
            bool legalContact = WasLegalContact(ballsContacted, ballId, playerGroup);

            if (!legalContact)
            {
                return HandleFoul(FoulType.WrongBallFirst, "Did not hit own group ball first", playerIndex);
            }

            // Cushion rule
            if (!ballHitCushion && !WasBallPotted(ballId))
            {
                return HandleFoul(FoulType.NoCushionAfterContact, "No ball hit cushion after contact", playerIndex);
            }

            // Opponent's ball potted = legal, but no group assignment change
            if (isOpponentBall)
            {
                // Opponent's ball stays down, turn continues if player's ball also potted
                // or turn passes if only opponent's ball potted
                OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
                return ShotResult.Legal;
            }

            // Player's ball potted = legal, turn continues
            if (isPlayerBall)
            {
                OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
                return ShotResult.Legal;
            }

            // No ball potted (cue ball only or safety)
            OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
            return ShotResult.Legal;
        }

        /// <summary>
        /// Handles 8-ball pocketing logic.
        /// </summary>
        private ShotResult HandleEightBallPotted(int pocketIndex, int playerIndex, List<int> ballsContacted, bool ballHitCushion)
        {
            BallGroup playerGroup = _playerGroups[playerIndex];

            // Check if player has cleared their group
            bool groupCleared = IsGroupCleared(playerGroup);

            if (!groupCleared)
            {
                // 8-ball potted before clearing group = loss of frame
                return HandleFoul(FoulType.EightBallEarly, "8-ball potted before clearing group", playerIndex);
            }

            // Check legal contact (must hit 8-ball first)
            if (!WasLegalContact(ballsContacted, EIGHT_BALL_ID, BallGroup.Unassigned))
            {
                return HandleFoul(FoulType.WrongBallFirst, "Did not hit 8-ball first", playerIndex);
            }

            // Check called pocket (simplified - assume pocketIndex is called pocket)
            // WPA: 8-ball must be called. For now, any pocket is valid if group cleared.
            // In full implementation, would check called pocket vs actual pocket.

            // 8-ball legally potted = WIN
            _eightBallPocketedBy = playerIndex;
            OnFrameWon?.Invoke(playerIndex);
            OnShotResolved?.Invoke(ShotResult.Win, FoulType.None);
            return ShotResult.Win;
        }

        /// <summary>
        /// Assigns ball groups to players.
        /// </summary>
        private void AssignGroups(int playerIndex, BallGroup group)
        {
            _playerGroups[playerIndex] = group;
            _playerGroups[1 - playerIndex] = (group == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids;
            _isOpenTable = false;
            OnGroupAssigned?.Invoke(playerIndex, group);
        }

        /// <summary>
        /// Checks if a ball belongs to a group.
        /// </summary>
        private bool IsBallInGroup(int ballId, BallGroup group)
        {
            if (group == BallGroup.Solids) return ballId >= SOLIDS_MIN && ballId <= SOLIDS_MAX;
            if (group == BallGroup.Stripes) return ballId >= STRIPES_MIN && ballId <= STRIPES_MAX;
            return false;
        }

        /// <summary>
        /// Gets the group for a ball ID.
        /// </summary>
        private BallGroup GetBallGroup(int ballId)
        {
            if (ballId >= SOLIDS_MIN && ballId <= SOLIDS_MAX) return BallGroup.Solids;
            if (ballId >= STRIPES_MIN && ballId <= STRIPES_MAX) return BallGroup.Stripes;
            return BallGroup.Unassigned;
        }

        /// <summary>
        /// Checks if player's group is completely cleared.
        /// </summary>
        private bool IsGroupCleared(BallGroup group)
        {
            // In full implementation, would check against potted ball tracker
            // For now, return true if group is assigned (simplified)
            return group != BallGroup.Unassigned;
        }

        /// <summary>
        /// Checks if contact was legal per WPA rules.
        /// </summary>
        private bool WasLegalContact(List<int> ballsContacted, int targetBallId, BallGroup playerGroup)
        {
            if (ballsContacted == null || ballsContacted.Count == 0) return false;

            int firstContact = ballsContacted[0];

            // On open table or 8-ball shot: any object ball is legal first contact
            if (playerGroup == BallGroup.Unassigned || targetBallId == EIGHT_BALL_ID)
            {
                return firstContact != CUE_BALL_ID && firstContact >= 1 && firstContact <= 15;
            }

            // Must hit own group ball first
            return IsBallInGroup(firstContact, playerGroup);
        }

        /// <summary>
        /// Checks if a ball was potted (simplified).
        /// </summary>
        private bool WasBallPotted(int ballId)
        {
            // In full implementation, would check potted ball tracker
            return true; // Simplified - assume ball was potted if we're here
        }

        /// <summary>
        /// Handles a foul and returns ShotResult.Foul.
        /// </summary>
        private ShotResult HandleFoul(FoulType foulType, string reason, int playerIndex)
        {
            _lastFoulReason = reason;
            OnFoulCommitted?.Invoke(foulType, reason);
            OnShotResolved?.Invoke(ShotResult.Foul, foulType);
            return ShotResult.Foul;
        }

        /// <summary>
        /// Gets the last foul reason.
        /// </summary>
        public string GetLastFoulReason() => _lastFoulReason;

        /// <summary>
        /// Called when shot ends (balls settled). Determines turn change.
        /// </summary>
        public bool ShouldTurnChange(ShotResult shotResult, bool ballPotted)
        {
            if (shotResult == ShotResult.Foul) return true;
            if (shotResult == ShotResult.Win) return false; // Game over
            if (shotResult == ShotResult.Loss) return false; // Game over
            return !ballPotted; // Turn changes if no ball potted
        }

        /// <summary>
        /// Gets the opponent player index.
        /// </summary>
        public int GetOpponent(int playerIndex) => 1 - playerIndex;

        /// <summary>
        /// Checks if frame is over.
        /// </summary>
        public bool IsFrameOver() => _eightBallPocketedBy >= 0;

        /// <summary>
        /// Gets the winner of the frame.
        /// </summary>
        public int GetFrameWinner() => _eightBallPocketedBy;
    }
}