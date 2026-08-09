using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Gameplay.Rules
{
    /// <summary>
    /// Implements WPA (World Pool-Billiard Association) standardized 9-Ball rules.
    /// Handles lowest ball first, push-out, ball-in-hand, three fouls, and win conditions.
    /// </summary>
    public class CueStrikeNineBallWPARuleset : MonoBehaviour
    {
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
            BallOffTable,
            DoubleHit,
            PushShot,
            Miscue,
            ThreeConsecutiveFouls
        }

        public enum PushOutState
        {
            NotAvailable,
            Available,
            Executed,
            Declined
        }

        // Singleton
        public static CueStrikeNineBallWPARuleset Instance { get; private set; }

        // Events
        public event Action<FoulType, string> OnFoulCommitted;
        public event Action<int> OnFrameWon;
        public event Action<int> OnFrameLost;
        public event Action<ShotResult, FoulType> OnShotResolved;
        public event Action<PushOutState> OnPushOutStateChanged;
        public event Action<int> OnConsecutiveFoulsChanged;

        // State
        private int _currentPlayer = 0;
        private bool _isBreakShot = true;
        private int _consecutiveFouls = 0;
        private int _nineBallPocketedBy = -1;
        private string _lastFoulReason = "";
        private PushOutState _pushOutState = PushOutState.NotAvailable;
        private bool _pushOutDeclared = false;
        private int _lowestBallOnTable = 1;

        // Ball ID constants
        private const int CUE_BALL_ID = 0;
        private const int NINE_BALL_ID = 9;
        private const int MIN_BALL_ID = 1;
        private const int MAX_BALL_ID = 9;

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
            _currentPlayer = 0;
            _isBreakShot = true;
            _consecutiveFouls = 0;
            _nineBallPocketedBy = -1;
            _lastFoulReason = "";
            _pushOutState = PushOutState.NotAvailable;
            _pushOutDeclared = false;
            _lowestBallOnTable = 1;
            OnConsecutiveFoulsChanged?.Invoke(_consecutiveFouls);
            OnPushOutStateChanged?.Invoke(_pushOutState);
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
            if (ballId < MIN_BALL_ID || ballId > MAX_BALL_ID)
            {
                return HandleFoul(FoulType.BallOffTable, "Ball jumped off table", playerIndex);
            }

            // Break shot handling
            if (isBreakShot)
            {
                return HandleBreakShot(ballId, pocketIndex, playerIndex, ballsContacted, ballHitCushion);
            }

            // Push-out handling
            if (_pushOutState == PushOutState.Available)
            {
                return HandlePushOutShot(ballId, pocketIndex, playerIndex, ballsContacted, ballHitCushion, cueBallPotted);
            }

            // Normal play
            return HandleNormalShot(ballId, pocketIndex, playerIndex, ballsContacted, ballHitCushion);
        }

        /// <summary>
        /// Handles break shot logic per WPA 9-Ball rules.
        /// </summary>
        private ShotResult HandleBreakShot(int ballId, int pocketIndex, int playerIndex, List<int> ballsContacted, bool ballHitCushion)
        {
            // Check if 9-ball potted on break
            if (ballId == NINE_BALL_ID)
            {
                // WPA 9-Ball: 9-ball on break = WIN (if legal break)
                if (WasLegalBreak(ballsContacted, ballHitCushion))
                {
                    _nineBallPocketedBy = playerIndex;
                    _isBreakShot = false;
                    OnFrameWon?.Invoke(playerIndex);
                    OnShotResolved?.Invoke(ShotResult.Win, FoulType.None);
                    return ShotResult.Win;
                }
                else
                {
                    // Illegal break, 9-ball spotted, ball in hand
                    return HandleFoul(FoulType.NoCushionAfterContact, "Illegal break - 9-ball spotted, ball in hand", playerIndex);
                }
            }

            // Check if any ball potted on break
            bool ballPotted = (ballId >= MIN_BALL_ID && ballId <= NINE_BALL_ID);

            // Check legal break (4 balls to cushion or ball potted)
            bool legalBreak = WasLegalBreak(ballsContacted, ballHitCushion);

            if (!legalBreak)
            {
                // Illegal break = ball in hand for opponent
                _isBreakShot = false;
                _pushOutState = PushOutState.Available; // Push-out available after illegal break
                OnPushOutStateChanged?.Invoke(_pushOutState);
                return HandleFoul(FoulType.NoCushionAfterContact, "Illegal break - fewer than 4 balls hit cushion", playerIndex);
            }

            // Legal break
            _isBreakShot = false;

            if (ballPotted)
            {
                // Ball potted on legal break - breaker continues, no push-out
                _pushOutState = PushOutState.NotAvailable;
                UpdateLowestBall(ballId);
                OnPushOutStateChanged?.Invoke(_pushOutState);
                OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
                return ShotResult.Legal;
            }
            else
            {
                // No ball potted on legal break - push-out available for incoming player
                _pushOutState = PushOutState.Available;
                OnPushOutStateChanged?.Invoke(_pushOutState);
                OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
                return ShotResult.Legal;
            }
        }

        /// <summary>
        /// Determines if break was legal per WPA (4 balls to cushion or ball potted).
        /// </summary>
        private bool WasLegalBreak(List<int> ballsContacted, bool ballHitCushion)
        {
            // Simplified: legal if ball hit cushion or 4+ balls contacted
            // Full WPA: at least 4 object balls must hit cushions
            return ballHitCushion || (ballsContacted != null && ballsContacted.Count >= 4);
        }

        /// <summary>
        /// Handles push-out shot logic.
        /// </summary>
        private ShotResult HandlePushOutShot(int ballId, int pocketIndex, int playerIndex, List<int> ballsContacted, bool ballHitCushion, bool cueBallPotted)
        {
            // During push-out: no requirement to hit lowest ball, no foul for not hitting cushion
            // But cue ball potted or ball off table still fouls

            if (cueBallPotted || ballId == CUE_BALL_ID)
            {
                _pushOutState = PushOutState.Executed;
                OnPushOutStateChanged?.Invoke(_pushOutState);
                return HandleFoul(FoulType.CueBallPotted, "Cue ball potted on push-out", playerIndex);
            }

            // Any ball potted on push-out stays down (except 9-ball which is spotted)
            if (ballId == NINE_BALL_ID)
            {
                // 9-ball potted on push-out = spotted
                // In full implementation, would trigger ball spotting
            }

            // Push-out executed - turn passes to opponent, push-out no longer available
            _pushOutState = PushOutState.Executed;
            OnPushOutStateChanged?.Invoke(_pushOutState);

            // No foul for push-out itself
            OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
            return ShotResult.Legal;
        }

        /// <summary>
        /// Declines push-out option (incoming player chooses not to push out).
        /// </summary>
        public void DeclinePushOut()
        {
            if (_pushOutState == PushOutState.Available)
            {
                _pushOutState = PushOutState.Declined;
                OnPushOutStateChanged?.Invoke(_pushOutState);
            }
        }

        /// <summary>
        /// Handles normal shots after break/push-out.
        /// </summary>
        private ShotResult HandleNormalShot(int ballId, int pocketIndex, int playerIndex, List<int> ballsContacted, bool ballHitCushion)
        {
            // Push-out no longer available after first normal shot
            if (_pushOutState == PushOutState.Available)
            {
                _pushOutState = PushOutState.Declined;
                OnPushOutStateChanged?.Invoke(_pushOutState);
            }

            // Check if 9-ball potted
            if (ballId == NINE_BALL_ID)
            {
                return HandleNineBallPotted(ballsContacted, ballHitCushion, playerIndex);
            }

            // Check legal contact: must hit lowest numbered ball first
            if (!WasLegalContact(ballsContacted))
            {
                return HandleFoul(FoulType.WrongBallFirst, "Did not hit lowest numbered ball first", playerIndex);
            }

            // Check cushion rule (after legal contact, at least one ball must hit cushion or be potted)
            if (!ballHitCushion && !WasBallPotted(ballId))
            {
                return HandleFoul(FoulType.NoCushionAfterContact, "No ball hit cushion after contact", playerIndex);
            }

            // Legal shot - update lowest ball if it was potted
            UpdateLowestBall(ballId);

            // Reset consecutive fouls on legal shot
            _consecutiveFouls = 0;
            OnConsecutiveFoulsChanged?.Invoke(_consecutiveFouls);

            OnShotResolved?.Invoke(ShotResult.Legal, FoulType.None);
            return ShotResult.Legal;
        }

        /// <summary>
        /// Handles 9-ball pocketing logic.
        /// </summary>
        private ShotResult HandleNineBallPotted(List<int> ballsContacted, bool ballHitCushion, int playerIndex)
        {
            // Must hit lowest ball first (which should be 9-ball if it's the last ball)
            // Or if 9-ball combo: must hit lowest ball first, then 9-ball
            if (!WasLegalContact(ballsContacted))
            {
                // 9-ball potted on foul = spotted, ball in hand
                return HandleFoul(FoulType.WrongBallFirst, "9-ball potted on foul - spotted", playerIndex);
            }

            // Check cushion rule
            if (!ballHitCushion)
            {
                // 9-ball potted but no cushion = foul, 9-ball spotted
                return HandleFoul(FoulType.NoCushionAfterContact, "9-ball potted but no cushion - spotted", playerIndex);
            }

            // 9-ball legally potted = WIN
            _nineBallPocketedBy = playerIndex;
            OnFrameWon?.Invoke(playerIndex);
            OnShotResolved?.Invoke(ShotResult.Win, FoulType.None);
            return ShotResult.Win;
        }

        /// <summary>
        /// Checks if contact was legal per WPA 9-Ball rules.
        /// Must hit lowest numbered ball on table first.
        /// </summary>
        private bool WasLegalContact(List<int> ballsContacted)
        {
            if (ballsContacted == null || ballsContacted.Count == 0) return false;

            int firstContact = ballsContacted[0];

            // Must hit lowest ball first (cannot hit cue ball first)
            if (firstContact == CUE_BALL_ID) return false;

            // Must hit the lowest numbered ball on table
            return firstContact == _lowestBallOnTable;
        }

        /// <summary>
        /// Updates the lowest ball on table after a ball is potted.
        /// </summary>
        private void UpdateLowestBall(int pottedBallId)
        {
            if (pottedBallId == _lowestBallOnTable)
            {
                // Find next lowest ball still on table
                for (int i = _lowestBallOnTable + 1; i <= NINE_BALL_ID; i++)
                {
                    // In full implementation, would check potted ball tracker
                    // For now, just increment
                    _lowestBallOnTable = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if a ball was potted (simplified).
        /// </summary>
        private bool WasBallPotted(int ballId)
        {
            // In full implementation, would check potted ball tracker
            return true; // Simplified
        }

        /// <summary>
        /// Handles a foul and returns ShotResult.Foul.
        /// </summary>
        private ShotResult HandleFoul(FoulType foulType, string reason, int playerIndex)
        {
            _lastFoulReason = reason;
            _consecutiveFouls++;

            // Check for three consecutive fouls = loss of frame
            if (_consecutiveFouls >= 3)
            {
                OnConsecutiveFoulsChanged?.Invoke(_consecutiveFouls);
                OnFoulCommitted?.Invoke(FoulType.ThreeConsecutiveFouls, "Three consecutive fouls - loss of frame");
                OnFrameLost?.Invoke(playerIndex);
                OnShotResolved?.Invoke(ShotResult.Loss, FoulType.ThreeConsecutiveFouls);
                return ShotResult.Loss;
            }

            OnConsecutiveFoulsChanged?.Invoke(_consecutiveFouls);
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
        public bool IsFrameOver() => _nineBallPocketedBy >= 0;

        /// <summary>
        /// Gets the winner of the frame.
        /// </summary>
        public int GetFrameWinner() => _nineBallPocketedBy;

        /// <summary>
        /// Resets consecutive fouls counter (called when player changes).
        /// </summary>
        public void ResetConsecutiveFouls()
        {
            _consecutiveFouls = 0;
            OnConsecutiveFoulsChanged?.Invoke(_consecutiveFouls);
        }

        /// <summary>
        /// Gets the current push-out state.
        /// </summary>
        public PushOutState GetPushOutState() => _pushOutState;

        /// <summary>
        /// Gets the current consecutive fouls count.
        /// </summary>
        public int GetConsecutiveFouls() => _consecutiveFouls;

        /// <summary>
        /// Gets the lowest numbered ball currently on the table.
        /// </summary>
        public int GetLowestBallOnTable() => _lowestBallOnTable;

        /// <summary>
        /// Checks if the current shot is a break shot.
        /// </summary>
        public bool IsBreakShot() => _isBreakShot;
    }
}
