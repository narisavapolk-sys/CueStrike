using System;
using UnityEngine;

namespace CueStrike.Gameplay.ChinesePool
{
    /// <summary>
    /// Ball type classification for Chinese 8-Ball (Red/Yellow/Black).
    /// </summary>
    public enum ChinesePoolBallType
    {
        CueBall = 0,
        Red = 1,      // 1-7
        Yellow = 2,   // 9-15
        BlackBall = 3 // 8
    }

    public enum ChinesePoolGameState
    {
        Waiting,
        BreakShot,
        OpenTable,
        GroupAssigned,
        CallShot,
        Settling,
        Foul,
        FrameOver
    }

    public static class ChinesePoolRules
    {
        public const int TotalBalls = 16; // 0-15 (0 = cue ball)
        public const int RedBallsStart = 1;
        public const int RedBallsEnd = 7;
        public const int BlackBall = 8;
        public const int YellowBallsStart = 9;
        public const int YellowBallsEnd = 15;
        public const int TotalPockets = 6;

        public static ChinesePoolBallType GetBallType(int ballId)
        {
            if (ballId == 0) return ChinesePoolBallType.CueBall;
            if (ballId >= RedBallsStart && ballId <= RedBallsEnd) return ChinesePoolBallType.Red;
            if (ballId == BlackBall) return ChinesePoolBallType.BlackBall;
            if (ballId >= YellowBallsStart && ballId <= YellowBallsEnd) return ChinesePoolBallType.Yellow;
            return ChinesePoolBallType.CueBall;
        }

        public static bool IsCueBall(int ballId) => ballId == 0;
        public static bool IsRedBall(int ballId) => ballId >= RedBallsStart && ballId <= RedBallsEnd;
        public static bool IsYellowBall(int ballId) => ballId >= YellowBallsStart && ballId <= YellowBallsEnd;
        public static bool IsBlackBall(int ballId) => ballId == BlackBall;
        public static bool IsObjectBall(int ballId) => ballId > 0 && ballId <= 15;

        public static bool IsBallInGroup(int ballId, ChinesePoolBallType group)
        {
            return GetBallType(ballId) == group;
        }

        public static int[] GetBallsInGroup(ChinesePoolBallType group)
        {
            var balls = new System.Collections.Generic.List<int>();
            for (int i = 1; i <= 15; i++)
            {
                if (GetBallType(i) == group)
                    balls.Add(i);
            }
            return balls.ToArray();
        }

        public static bool IsValidCallShot(int calledBallId, int calledPocketId, int[] availableBalls, int[] availablePockets)
        {
            if (calledBallId < 1 || calledBallId > 15) return false;
            if (calledPocketId < 0 || calledPocketId >= TotalPockets) return false;

            bool ballAvailable = false;
            foreach (int id in availableBalls)
            {
                if (id == calledBallId) { ballAvailable = true; break; }
            }

            bool pocketAvailable = false;
            foreach (int id in availablePockets)
            {
                if (id == calledPocketId) { pocketAvailable = true; break; }
            }

            return ballAvailable && pocketAvailable;
        }

        public static bool IsLegalShot(int struckBallId, int calledBallId, int calledPocketId, int pottedBallId, int pottedPocketId, ChinesePoolBallType assignedGroup, bool isOpenTable, bool isBreakShot)
        {
            if (isBreakShot)
            {
                return true; // Break shot has special rules
            }

            if (isOpenTable)
            {
                // On open table, any ball can be struck first, but must call a ball and pocket
                return struckBallId > 0 && pottedBallId == calledBallId && pottedPocketId == calledPocketId;
            }

            // Group assigned - must hit own group first
            if (!IsBallInGroup(struckBallId, assignedGroup))
            {
                return false; // Foul - didn't hit own group first
            }

            // Must pot called ball in called pocket
            return pottedBallId == calledBallId && pottedPocketId == calledPocketId;
        }

        public static bool IsFoul(int cueBallPotted, int struckBallId, int calledBallId, int calledPocketId, int pottedBallId, int pottedPocketId, ChinesePoolBallType assignedGroup, bool isOpenTable, bool isBreakShot)
        {
            if (cueBallPotted != 0) return true; // Cue ball potted = foul

            if (isBreakShot)
            {
                // Break shot: must hit red ball first, or pot a ball
                if (struckBallId > 0 && !IsRedBall(struckBallId) && pottedBallId <= 0)
                    return true; // Foul on break
                return false;
            }

            if (isOpenTable)
            {
                // Open table: must hit object ball first, and pot called ball in called pocket
                if (struckBallId <= 0) return true; // Didn't hit any ball
                if (pottedBallId != calledBallId || pottedPocketId != calledPocketId) return true;
                return false;
            }

            // Group assigned
            if (!IsBallInGroup(struckBallId, assignedGroup)) return true; // Didn't hit own group first

            if (pottedBallId > 0)
            {
                if (pottedBallId != calledBallId || pottedPocketId != calledPocketId) return true; // Wrong ball/pocket
                if (!IsBallInGroup(pottedBallId, assignedGroup) && pottedBallId != BlackBall) return true; // Potted opponent's ball (not black)
            }

            return false;
        }

        public static ChinesePoolGameState GetNextState(ChinesePoolGameState currentState, bool ballPotted, bool foul, bool isBreakShot, bool isOpenTable, ChinesePoolBallType assignedGroup, int pottedBallId = -1)
        {
            if (foul) return ChinesePoolGameState.Foul;

            if (isBreakShot)
            {
                if (ballPotted) return ChinesePoolGameState.OpenTable;
                return ChinesePoolGameState.OpenTable; // Turn passes, table still open
            }

            if (isOpenTable)
            {
                if (ballPotted)
                {
                    // Group assigned based on what was potted
                    return ChinesePoolGameState.GroupAssigned;
                }
                return ChinesePoolGameState.OpenTable; // Turn passes, table still open
            }

            // Group assigned
            if (ballPotted)
            {
                // Check if black ball potted legally (frame win)
                if (pottedBallId == BlackBall && AreGroupBallsCleared(assignedGroup, new int[0]))
                {
                    return ChinesePoolGameState.FrameOver;
                }
                return ChinesePoolGameState.CallShot; // Continue turn, call next shot
            }

            return ChinesePoolGameState.CallShot; // Turn passes, opponent calls shot
        }

        public static bool AreGroupBallsCleared(ChinesePoolBallType group, int[] remainingBalls)
        {
            foreach (int ballId in remainingBalls)
            {
                if (GetBallType(ballId) == group) return false;
            }
            return true;
        }

        public static int CalculateFoulPenalty(bool cueBallPotted, bool wrongBallFirst, bool wrongBallPotted, bool blackBallPottedEarly)
        {
            int penalty = 4; // Standard foul = 4 points
            if (blackBallPottedEarly) penalty = 7; // Black ball foul = 7 points
            return penalty;
        }

        // Events for integration with CueStrikeRulesManager
        public static event Action<int, int, int> OnCallShotRequested; // playerIndex, ballId, pocketId
        public static event Action<int, bool> OnShotResolved; // playerIndex, success
        public static event Action<int, int> OnFoulCommitted; // playerIndex, penaltyPoints
        public static event Action<int> OnFrameWon; // winnerPlayerIndex
        public static event Action<int, ChinesePoolBallType> OnGroupAssigned; // playerIndex, assignedGroup

        public static void RequestCallShot(int playerIndex, int ballId, int pocketId)
        {
            OnCallShotRequested?.Invoke(playerIndex, ballId, pocketId);
        }

        public static void ResolveShot(int playerIndex, bool success)
        {
            OnShotResolved?.Invoke(playerIndex, success);
        }

        public static void CommitFoul(int playerIndex, int penaltyPoints)
        {
            OnFoulCommitted?.Invoke(playerIndex, penaltyPoints);
        }

        public static void WinFrame(int winnerIndex)
        {
            OnFrameWon?.Invoke(winnerIndex);
        }

        public static void AssignGroup(int playerIndex, ChinesePoolBallType group)
        {
            OnGroupAssigned?.Invoke(playerIndex, group);
        }
    }
}