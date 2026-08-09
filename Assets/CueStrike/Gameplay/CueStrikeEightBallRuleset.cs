using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CueStrikeEightBallRuleset - WPA World Standardized Rules for 8-Ball Pool
/// Created by Nari for P'Mong | 2026-07-21
/// 
/// 8-Ball Rules Summary (WPA):
/// - 15 object balls (1-7 solids, 9-15 stripes) + cue ball + 8-ball
/// - Call shot game: must call ball and pocket
/// - Break: 4 balls hit cushion OR 1 ball pocketed = legal break
/// - Open table after break: player chooses group by legally pocketing called ball
/// - Once group assigned: must hit own group first
/// - 8-ball: must be called and pocketed in designated pocket AFTER all group balls cleared
/// - Fouls: cue ball scratch, wrong ball first, no rail after contact, 8-ball early = loss of frame
/// - Foul penalty: ball in hand anywhere on table (except break)
/// </summary>
public class CueStrikeEightBallRuleset : MonoBehaviour
{
    public static CueStrikeEightBallRuleset Instance { get; private set; }

    /// <summary>
    /// Ball groups per WPA 8-Ball standard
    /// </summary>
    public enum BallGroup
    {
        None = 0,
        Solids = 1,   // Balls 1-7
        Stripes = 2   // Balls 9-15
    }

    /// <summary>
    /// Ball type identification for 8-Ball
    /// </summary>
    public enum EightBallType
    {
        CueBall = 0,
        Solid1 = 1, Solid2 = 2, Solid3 = 3, Solid4 = 4, Solid5 = 5, Solid6 = 6, Solid7 = 7,
        EightBall = 8,
        Stripe9 = 9, Stripe10 = 10, Stripe11 = 11, Stripe12 = 12, Stripe13 = 13, Stripe14 = 14, Stripe15 = 15
    }

    [Header("Frame Setup (WPA Standard)")]
    public int totalObjectBalls = 15;
    public int solidsCount = 7;
    public int stripesCount = 7;

    [Header("Game State")]
    public BallGroup player1Group = BallGroup.None;
    public BallGroup player2Group = BallGroup.None;
    public BallGroup currentPlayerGroup = BallGroup.None;
    public bool tableOpen = true; // Open table after break until group assigned
    public bool breakShot = true;
    public int ballsRemainingPlayer1 = 7;
    public int ballsRemainingPlayer2 = 7;
    public bool eightBallCalled = false;
    public int eightBallCalledPocket = -1;
    public bool frameOver = false;
    public int winner = -1; // 0 = Player 1, 1 = Player 2, -1 = none

    // Events
    public event Action<int, BallGroup> OnGroupAssigned;
    public event Action<int> OnBallPotted;
    public event Action<string> OnFoulCommitted;
    public event Action<int> OnFrameWon; // winner index
    public event Action<int> OnFrameLost; // loser index
    public event Action<string> OnStatusMessage;
    public event Action<bool> OnTableStateChanged; // true = open, false = assigned

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        ResetFrame();
    }

    /// <summary>
    /// Resets frame to initial state
    /// </summary>
    public void ResetFrame()
    {
        player1Group = BallGroup.None;
        player2Group = BallGroup.None;
        currentPlayerGroup = BallGroup.None;
        tableOpen = true;
        breakShot = true;
        ballsRemainingPlayer1 = solidsCount;
        ballsRemainingPlayer2 = stripesCount;
        eightBallCalled = false;
        eightBallCalledPocket = -1;
        frameOver = false;
        winner = -1;
        OnTableStateChanged?.Invoke(true);
        PublishStatus("New frame. Break shot - table is open.");
    }

    /// <summary>
    /// Gets the ball group for a given ball ID (1-15)
    /// </summary>
    public static BallGroup GetBallGroup(int ballId)
    {
        if (ballId >= 1 && ballId <= 7) return BallGroup.Solids;
        if (ballId >= 9 && ballId <= 15) return BallGroup.Stripes;
        return BallGroup.None; // 0 = cue ball, 8 = eight ball
    }

    /// <summary>
    /// Gets the ball type enum from ball ID
    /// </summary>
    public static EightBallType GetBallType(int ballId)
    {
        if (ballId >= 0 && ballId <= 15)
            return (EightBallType)ballId;
        return EightBallType.CueBall;
    }

    /// <summary>
    /// Checks if ball ID is the 8-ball
    /// </summary>
    public static bool IsEightBall(int ballId) => ballId == 8;

    /// <summary>
    /// Checks if ball ID is a valid object ball (1-15)
    /// </summary>
    public static bool IsObjectBall(int ballId) => ballId >= 1 && ballId <= 15 && ballId != 8;

    /// <summary>
    /// Registers a potted ball during a shot
    /// </summary>
    /// <returns>Points scored (1 for normal ball, 0 for foul, special for 8-ball)</returns>
    public int RegisterPot(int ballId, int pocketIndex, int currentPlayerIndex, bool isBreakShot = false)
    {
        if (frameOver) return 0;

        // Cue ball potted = foul (scratch)
        if (ballId == 0)
        {
            CommitFoul(currentPlayerIndex, "Cue ball potted (scratch)");
            return 0;
        }

        // 8-Ball potted
        if (IsEightBall(ballId))
        {
            return HandleEightBallPot(ballId, pocketIndex, currentPlayerIndex, isBreakShot);
        }

        // Object ball potted
        if (IsObjectBall(ballId))
        {
            return HandleObjectBallPot(ballId, pocketIndex, currentPlayerIndex, isBreakShot);
        }

        return 0;
    }

    /// <summary>
    /// Handles potting an object ball (1-7, 9-15)
    /// </summary>
    private int HandleObjectBallPot(int ballId, int pocketIndex, int currentPlayerIndex, bool isBreakShot)
    {
        BallGroup ballGroup = GetBallGroup(ballId);
        int opponentIndex = 1 - currentPlayerIndex;

        // Break shot special handling
        if (isBreakShot)
        {
            breakShot = false;
            
            // On break: pocketing balls doesn't assign group yet (table remains open)
            // but we track balls potted
            PublishStatus($"Break: Ball {ballId} potted. Table remains open.");
            OnBallPotted?.Invoke(ballId);
            
            // Check if both groups potted on break - table stays open
            // Group assignment happens on FIRST legal pot AFTER break
            return 1;
        }

        // After break: table is open until group legally assigned
        if (tableOpen)
        {
            // Legal pot on open table assigns group to current player
            AssignGroup(currentPlayerIndex, ballGroup);
            currentPlayerGroup = ballGroup;
            
            // Decrement remaining balls for assigned group
            DecrementBallsRemaining(currentPlayerIndex);
            
            PublishStatus($"{GetPlayerName(currentPlayerIndex)} assigned {ballGroup}s. Ball {ballId} potted.");
            OnBallPotted?.Invoke(ballId);
            
            // Check for frame win (all group balls + 8-ball)
            CheckFrameWin(currentPlayerIndex);
            return 1;
        }

        // Table assigned: must hit own group first
        if (ballGroup != currentPlayerGroup)
        {
            // Potted opponent's ball = foul (wrong ball first)
            CommitFoul(currentPlayerIndex, $"Potted opponent's {ballGroup} ball ({ballId})");
            return 0;
        }

        // Legal pot: own group ball
        DecrementBallsRemaining(currentPlayerIndex);
        PublishStatus($"{GetPlayerName(currentPlayerIndex)} potted {ballGroup} ball {ballId}. {GetBallsRemaining(currentPlayerIndex)} remaining.");
        OnBallPotted?.Invoke(ballId);

        // Check for frame win (all group balls cleared, now need 8-ball)
        CheckFrameWin(currentPlayerIndex);
        return 1;
    }

    /// <summary>
    /// Handles 8-ball pot - can win or lose frame
    /// </summary>
    private int HandleEightBallPot(int ballId, int pocketIndex, int currentPlayerIndex, bool isBreakShot)
    {
        int opponentIndex = 1 - currentPlayerIndex;

        // 8-ball on break
        if (isBreakShot)
        {
            // WPA: 8-ball on break = re-rack OR spot 8-ball and continue (player choice)
            // We implement: spot 8-ball, no frame win, turn continues
            PublishStatus("8-ball potted on break! 8-ball spotted. Turn continues.");
            eightBallCalled = false;
            OnBallPotted?.Invoke(ballId);
            return 0; // No frame win, ball spotted
        }

        // 8-ball during normal play
        if (!eightBallCalled)
        {
            // 8-ball potted without call = LOSS OF FRAME (WPA rule)
            CommitFoul(currentPlayerIndex, "8-ball potted without call - LOSS OF FRAME");
            FrameLost(currentPlayerIndex);
            return 0;
        }

        // 8-ball called - check if correct pocket
        if (pocketIndex == eightBallCalledPocket)
        {
            // Correct pocket - check if all group balls cleared
            int ballsRemaining = GetBallsRemaining(currentPlayerIndex);
            if (ballsRemaining <= 0)
            {
                // FRAME WON!
                FrameWon(currentPlayerIndex);
                return 100; // Special score for frame win
            }
            else
            {
                // 8-ball potted early but in called pocket = LOSS OF FRAME
                CommitFoul(currentPlayerIndex, $"8-ball potted early ({ballsRemaining} balls remain) - LOSS OF FRAME");
                FrameLost(currentPlayerIndex);
                return 0;
            }
        }
        else
        {
            // Wrong pocket = LOSS OF FRAME
            CommitFoul(currentPlayerIndex, $"8-ball potted in wrong pocket (called {eightBallCalledPocket}, went {pocketIndex}) - LOSS OF FRAME");
            FrameLost(currentPlayerIndex);
            return 0;
        }
    }

    /// <summary>
    /// Assigns ball group to player
    /// </summary>
    public void AssignGroup(int playerIndex, BallGroup group)
    {
        if (playerIndex == 0)
        {
            player1Group = group;
            player2Group = (group == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids;
        }
        else
        {
            player2Group = group;
            player1Group = (group == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids;
        }

        tableOpen = false;
        currentPlayerGroup = group;
        OnGroupAssigned?.Invoke(playerIndex, group);
        OnTableStateChanged?.Invoke(false);
        PublishStatus($"Table assigned: {GetPlayerName(playerIndex)} = {group}s");
    }

    /// <summary>
    /// Validates a shot for fouls (called after shot physics settle)
    /// </summary>
    public void ValidateShot(int currentPlayerIndex, int firstHitBallId, bool cueBallHitRail, bool anyBallHitRail, bool cueBallPotted, List<int> pottedBallIds)
    {
        if (frameOver) return;

        int opponentIndex = 1 - currentPlayerIndex;

        // Cue ball scratch
        if (cueBallPotted)
        {
            CommitFoul(currentPlayerIndex, "Cue ball potted (scratch)");
            return;
        }

        // Break shot validation
        if (breakShot)
        {
            ValidateBreakShot(currentPlayerIndex, firstHitBallId, cueBallHitRail, anyBallHitRail, pottedBallIds);
            breakShot = false;
            return;
        }

        // Open table: any object ball first is legal (except cue ball)
        if (tableOpen)
        {
            if (firstHitBallId == 0)
            {
                CommitFoul(currentPlayerIndex, "Cue ball hit first (no object ball contacted)");
            }
            else if (firstHitBallId == 8)
            {
                // 8-ball hit first on open table = foul (unless only 8-ball remains)
                int ballsRemaining = GetBallsRemaining(currentPlayerIndex);
                if (ballsRemaining > 0)
                {
                    CommitFoul(currentPlayerIndex, "8-ball struck first on open table");
                }
            }
            // No rail requirement on open table after contact
            return;
        }

        // Assigned table: must hit own group first
        BallGroup firstHitGroup = GetBallGroup(firstHitBallId);
        
        if (firstHitBallId == 0)
        {
            CommitFoul(currentPlayerIndex, "No ball contacted");
        }
        else if (firstHitBallId == 8)
        {
            // 8-ball hit first only legal if all group balls cleared
            int ballsRemaining = GetBallsRemaining(currentPlayerIndex);
            if (ballsRemaining > 0)
            {
                CommitFoul(currentPlayerIndex, "8-ball struck first before group cleared");
            }
        }
        else if (firstHitGroup != currentPlayerGroup)
        {
            CommitFoul(currentPlayerIndex, $"Wrong ball first: hit {firstHitGroup} ({firstHitBallId}) instead of {currentPlayerGroup}");
        }
        else if (!cueBallHitRail && !anyBallHitRail)
        {
            // No rail after contact = foul (unless ball potted)
            if (pottedBallIds.Count == 0)
            {
                CommitFoul(currentPlayerIndex, "No rail contacted after legal hit");
            }
        }
    }

    /// <summary>
    /// Validates break shot per WPA rules
    /// </summary>
    private void ValidateBreakShot(int playerIndex, int firstHitBallId, bool cueBallHitRail, bool anyBallHitRail, List<int> pottedBallIds)
    {
        // Legal break: 4 balls hit cushion OR 1 ball pocketed
        bool legalBreak = pottedBallIds.Count > 0 || (cueBallHitRail && anyBallHitRail);
        
        if (!legalBreak)
        {
            CommitFoul(playerIndex, "Illegal break: fewer than 4 balls hit cushion and no ball potted");
            // Opponent gets ball in hand behind head string
            PublishStatus("Illegal break. Opponent gets ball in hand behind head string.");
        }
        else if (firstHitBallId != 1)
        {
            // WPA: Must hit head ball (1-ball) first on break
            // But many house rules don't enforce this strictly
            // We'll warn but not foul for now
            PublishStatus("Break: Head ball (1) not struck first (warning only).");
        }
    }

    /// <summary>
    /// Commits a foul - ball in hand to opponent
    /// </summary>
    public void CommitFoul(int playerIndex, string reason)
    {
        if (frameOver) return;
        
        int opponentIndex = 1 - playerIndex;
        OnFoulCommitted?.Invoke(reason);
        PublishStatus($"FOUL - {GetPlayerName(playerIndex)}: {reason}. {GetPlayerName(opponentIndex)} gets ball in hand.");
        
        // Ball in hand for opponent (except on break)
        // Game logic handles ball-in-hand positioning
    }

    /// <summary>
    /// Player calls 8-ball pocket
    /// </summary>
    public void CallEightBall(int playerIndex, int pocketIndex)
    {
        if (frameOver) return;
        if (GetBallsRemaining(playerIndex) > 0)
        {
            PublishStatus($"{GetPlayerName(playerIndex)} cannot call 8-ball: {GetBallsRemaining(playerIndex)} group balls remain.");
            return;
        }
        
        eightBallCalled = true;
        eightBallCalledPocket = pocketIndex;
        PublishStatus($"{GetPlayerName(playerIndex)} calls 8-ball in pocket {pocketIndex}.");
    }

    /// <summary>
    /// Clears 8-ball call (e.g., turn ends)
    /// </summary>
    public void ClearEightBallCall()
    {
        eightBallCalled = false;
        eightBallCalledPocket = -1;
    }

    /// <summary>
    /// Gets balls remaining for current player's group
    /// </summary>
    public int GetBallsRemaining(int playerIndex)
    {
        return playerIndex == 0 ? ballsRemainingPlayer1 : ballsRemainingPlayer2;
    }

    /// <summary>
    /// Gets current player's assigned group
    /// </summary>
    public BallGroup GetPlayerGroup(int playerIndex)
    {
        return playerIndex == 0 ? player1Group : player2Group;
    }

    /// <summary>
    /// Checks if table is open
    /// </summary>
    public bool IsTableOpen() => tableOpen;

    /// <summary>
    /// Checks if frame is over
    /// </summary>
    public bool IsFrameOver() => frameOver;

    /// <summary>
    /// Gets frame winner
    /// </summary>
    public int GetWinner() => winner;

    // Helper methods
    private void DecrementBallsRemaining(int playerIndex)
    {
        if (playerIndex == 0) ballsRemainingPlayer1 = Mathf.Max(0, ballsRemainingPlayer1 - 1);
        else ballsRemainingPlayer2 = Mathf.Max(0, ballsRemainingPlayer2 - 1);
    }

    private void CheckFrameWin(int playerIndex)
    {
        if (GetBallsRemaining(playerIndex) <= 0)
        {
            PublishStatus($"{GetPlayerName(playerIndex)} cleared all {GetPlayerGroup(playerIndex)}s! Call 8-ball to win.");
        }
    }

    private void FrameWon(int playerIndex)
    {
        frameOver = true;
        winner = playerIndex;
        OnFrameWon?.Invoke(playerIndex);
        PublishStatus($"FRAME WON! {GetPlayerName(playerIndex)} wins the frame!");
    }

    private void FrameLost(int playerIndex)
    {
        frameOver = true;
        winner = 1 - playerIndex;
        OnFrameLost?.Invoke(playerIndex);
        OnFrameWon?.Invoke(winner);
        PublishStatus($"FRAME LOST! {GetPlayerName(1 - playerIndex)} wins the frame!");
    }

    private string GetPlayerName(int index)
    {
        var rules = CueStrikeRulesManager.Instance;
        if (rules != null && rules.playerNames.Length > index)
            return rules.playerNames[index];
        return $"Player {index + 1}";
    }

    private void PublishStatus(string message)
    {
        Debug.Log($"[8-Ball] {message}");
        OnStatusMessage?.Invoke(message);
    }
}