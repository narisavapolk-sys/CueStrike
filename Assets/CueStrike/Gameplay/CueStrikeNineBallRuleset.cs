using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CueStrikeNineBallRuleset - WPA World Standardized Rules for 9-Ball Pool
/// Created by Nari for P'Mong | 2026-07-21
/// 
/// 9-Ball Rules Summary (WPA):
/// - 9 object balls (1-9) + cue ball
/// - Rotation game: must hit lowest numbered ball on table first
/// - Any ball pocketed legally = continue turn
/// - 9-ball pocketed legally at any time = WIN FRAME
/// - Call shot NOT required (slop counts)
/// - Break: 1-ball must be hit first, 4 balls to rail OR 1 ball pocketed
/// - Push-out option after legal break
/// - Fouls: cue ball scratch, wrong ball first, no rail after contact
/// - Foul penalty: ball in hand anywhere on table
/// - Three consecutive fouls = loss of frame
/// </summary>
public class CueStrikeNineBallRuleset : MonoBehaviour
{
    public static CueStrikeNineBallRuleset Instance { get; private set; }

    /// <summary>
    /// Ball type identification for 9-Ball
    /// </summary>
    public enum NineBallType
    {
        CueBall = 0,
        One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
        Six = 6, Seven = 7, Eight = 8, Nine = 9
    }

    [Header("Frame Setup (WPA Standard)")]
    public int totalObjectBalls = 9;

    [Header("Game State")]
    public bool breakShot = true;
    public bool pushOutAvailable = false;
    public bool pushOutTaken = false;
    public int consecutiveFoulsPlayer1 = 0;
    public int consecutiveFoulsPlayer2 = 0;
    public bool frameOver = false;
    public int winner = -1; // 0 = Player 1, 1 = Player 2, -1 = none
    public int lowestBallOnTable = 1;

    // Events
    public event Action<int> OnBallPotted;
    public event Action<string> OnFoulCommitted;
    public event Action<int> OnFrameWon; // winner index
    public event Action<int> OnFrameLost; // loser index
    public event Action<string> OnStatusMessage;
    public event Action<bool> OnPushOutAvailable; // true = available
    public event Action<int> OnLowestBallChanged; // new lowest ball
    public event Action<int, int> OnConsecutiveFouls; // player index, count

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
        breakShot = true;
        pushOutAvailable = false;
        pushOutTaken = false;
        consecutiveFoulsPlayer1 = 0;
        consecutiveFoulsPlayer2 = 0;
        frameOver = false;
        winner = -1;
        lowestBallOnTable = 1;
        OnPushOutAvailable?.Invoke(false);
        OnLowestBallChanged?.Invoke(1);
        PublishStatus("New frame. Break shot - hit 1-ball first.");
    }

    /// <summary>
    /// Gets the lowest numbered ball currently on table
    /// </summary>
    public int GetLowestBallOnTable(HashSet<int> pottedBalls)
    {
        for (int i = 1; i <= 9; i++)
        {
            if (!pottedBalls.Contains(i))
                return i;
        }
        return 9; // All balls potted (shouldn't happen in normal play)
    }

    /// <summary>
    /// Registers a potted ball during a shot
    /// </summary>
    /// <returns>Points scored (1 per ball, special for 9-ball win)</returns>
    public int RegisterPot(int ballId, int currentPlayerIndex, bool isBreakShot = false, HashSet<int> pottedBalls = null)
    {
        if (frameOver) return 0;

        // Cue ball potted = foul (scratch)
        if (ballId == 0)
        {
            CommitFoul(currentPlayerIndex, "Cue ball potted (scratch)");
            return 0;
        }

        // 9-ball potted
        if (ballId == 9)
        {
            return HandleNineBallPot(currentPlayerIndex, isBreakShot, pottedBalls);
        }

        // Other object ball potted (1-8)
        if (ballId >= 1 && ballId <= 8)
        {
            return HandleObjectBallPot(ballId, currentPlayerIndex, isBreakShot, pottedBalls);
        }

        return 0;
    }

    /// <summary>
    /// Handles potting object balls 1-8
    /// </summary>
    private int HandleObjectBallPot(int ballId, int currentPlayerIndex, bool isBreakShot, HashSet<int> pottedBalls)
    {
        int opponentIndex = 1 - currentPlayerIndex;

        // Break shot
        if (isBreakShot)
        {
            breakShot = false;
            
            // On break: ball potted legally continues turn
            // Push-out becomes available after legal break
            pushOutAvailable = true;
            pushOutTaken = false;
            OnPushOutAvailable?.Invoke(true);
            
            PublishStatus($"Break: Ball {ballId} potted. Push-out available.");
            OnBallPotted?.Invoke(ballId);
            
            // Update lowest ball
            UpdateLowestBall(pottedBalls);
            return 1;
        }

        // Normal play: any ball pocketed legally continues turn
        // No call shot required in 9-ball (slop counts)
        PublishStatus($"{GetPlayerName(currentPlayerIndex)} potted ball {ballId}.");
        OnBallPotted?.Invoke(ballId);
        
        // Update lowest ball on table
        UpdateLowestBall(pottedBalls);
        
        // Check consecutive fouls reset on legal pot
        ResetConsecutiveFouls(currentPlayerIndex);
        
        return 1;
    }

    /// <summary>
    /// Handles 9-ball pot - instant win if legal
    /// </summary>
    private int HandleNineBallPot(int currentPlayerIndex, bool isBreakShot, HashSet<int> pottedBalls)
    {
        int opponentIndex = 1 - currentPlayerIndex;

        // 9-ball on break = WIN (golden break)
        if (isBreakShot)
        {
            FrameWon(currentPlayerIndex);
            PublishStatus($"GOLDEN BREAK! {GetPlayerName(currentPlayerIndex)} wins on the break!");
            return 100; // Special score for frame win
        }

        // 9-ball during normal play = WIN if legal shot
        // Legality is checked in ValidateShot before this is called
        FrameWon(currentPlayerIndex);
        return 100;
    }

    /// <summary>
    /// Validates a shot for fouls (called after shot physics settle)
    /// </summary>
    public bool ValidateShot(int currentPlayerIndex, int firstHitBallId, bool cueBallHitRail, bool anyBallHitRail, bool cueBallPotted, List<int> pottedBallIds, HashSet<int> allPottedBalls)
    {
        if (frameOver) return false;

        int opponentIndex = 1 - currentPlayerIndex;
        bool isFoul = false;
        string foulReason = "";

        // Cue ball scratch
        if (cueBallPotted)
        {
            isFoul = true;
            foulReason = "Cue ball potted (scratch)";
        }
        // Break shot validation
        else if (breakShot)
        {
            isFoul = ValidateBreakShot(currentPlayerIndex, firstHitBallId, cueBallHitRail, anyBallHitRail, pottedBallIds, ref foulReason);
            breakShot = false;
            
            if (!isFoul)
            {
                // Legal break - push-out available
                pushOutAvailable = true;
                pushOutTaken = false;
                OnPushOutAvailable?.Invoke(true);
            }
        }
        // Push-out shot validation
        else if (pushOutTaken)
        {
            // Push-out: no requirement to hit lowest ball or rail
            // But cue ball scratch still fouls
            pushOutTaken = false;
            pushOutAvailable = false;
            OnPushOutAvailable?.Invoke(false);
            
            if (cueBallPotted)
            {
                isFoul = true;
                foulReason = "Cue ball potted on push-out";
            }
            // No other fouls on push-out
        }
        // Normal shot validation
        else
        {
            // Must hit lowest numbered ball first
            if (firstHitBallId != lowestBallOnTable)
            {
                if (firstHitBallId == 0)
                {
                    isFoul = true;
                    foulReason = "No ball contacted";
                }
                else
                {
                    isFoul = true;
                    foulReason = $"Wrong ball first: hit {firstHitBallId} instead of lowest ball ({lowestBallOnTable})";
                }
            }
            // Must hit rail after contact (unless ball potted)
            else if (!cueBallHitRail && !anyBallHitRail && pottedBallIds.Count == 0)
            {
                isFoul = true;
                foulReason = "No rail contacted after legal hit";
            }
        }

        if (isFoul)
        {
            CommitFoul(currentPlayerIndex, foulReason);
            IncrementConsecutiveFouls(currentPlayerIndex);
            
            // Check three consecutive fouls
            int foulCount = currentPlayerIndex == 0 ? consecutiveFoulsPlayer1 : consecutiveFoulsPlayer2;
            if (foulCount >= 3)
            {
                FrameLost(currentPlayerIndex);
                PublishStatus($"THREE CONSECUTIVE FOULS! {GetPlayerName(opponentIndex)} wins the frame!");
            }
            
            // Push-out no longer available after foul
            pushOutAvailable = false;
            OnPushOutAvailable?.Invoke(false);
            return false;
        }
        else
        {
            // Legal shot - reset consecutive fouls for this player
            ResetConsecutiveFouls(currentPlayerIndex);
            
            // Update lowest ball
            UpdateLowestBall(allPottedBalls);
            
            // Push-out no longer available after normal shot
            pushOutAvailable = false;
            OnPushOutAvailable?.Invoke(false);
            return true;
        }
    }

    /// <summary>
    /// Validates break shot per WPA rules
    /// </summary>
    private bool ValidateBreakShot(int playerIndex, int firstHitBallId, bool cueBallHitRail, bool anyBallHitRail, List<int> pottedBallIds, ref string foulReason)
    {
        // Must hit 1-ball first on break
        if (firstHitBallId != 1)
        {
            foulReason = "Break: 1-ball not struck first";
            return true;
        }

        // Legal break: 4 balls hit cushion OR 1 ball pocketed
        bool legalBreak = pottedBallIds.Count > 0 || (cueBallHitRail && anyBallHitRail);
        
        if (!legalBreak)
        {
            foulReason = "Illegal break: fewer than 4 balls hit cushion and no ball potted";
            return true;
        }

        return false; // No foul
    }

    /// <summary>
    /// Player takes push-out option
    /// </summary>
    public void TakePushOut(int playerIndex)
    {
        if (!pushOutAvailable || pushOutTaken || breakShot || frameOver)
        {
            PublishStatus("Push-out not available.");
            return;
        }

        pushOutTaken = true;
        pushOutAvailable = false;
        OnPushOutAvailable?.Invoke(false);
        PublishStatus($"{GetPlayerName(playerIndex)} takes push-out. Opponent chooses who shoots next.");
    }

    /// <summary>
    /// Player declines push-out (shoots normally)
    /// </summary>
    public void DeclinePushOut(int playerIndex)
    {
        if (!pushOutAvailable || breakShot || frameOver) return;

        pushOutAvailable = false;
        pushOutTaken = false;
        OnPushOutAvailable?.Invoke(false);
        PublishStatus($"{GetPlayerName(playerIndex)} declines push-out. Normal shot.");
    }

    /// <summary>
    /// Commits a foul - ball in hand to opponent
    /// </summary>
    public void CommitFoul(int playerIndex, string reason)
    {
        if (frameOver) return;
        
        int opponentIndex = 1 - playerIndex;
        OnFoulCommitted?.Invoke(reason);
        PublishStatus($"FOUL - {GetPlayerName(playerIndex)}: {reason}. {GetPlayerName(opponentIndex)} gets ball in hand anywhere.");
    }

    /// <summary>
    /// Gets current lowest ball on table
    /// </summary>
    public int GetLowestBall() => lowestBallOnTable;

    /// <summary>
    /// Checks if push-out is available
    /// </summary>
    public bool IsPushOutAvailable() => pushOutAvailable && !breakShot && !frameOver;

    /// <summary>
    /// Checks if frame is over
    /// </summary>
    public bool IsFrameOver() => frameOver;

    /// <summary>
    /// Gets frame winner
    /// </summary>
    public int GetWinner() => winner;

    /// <summary>
    /// Gets consecutive fouls for player
    /// </summary>
    public int GetConsecutiveFouls(int playerIndex)
    {
        return playerIndex == 0 ? consecutiveFoulsPlayer1 : consecutiveFoulsPlayer2;
    }

    // Helper methods
    private void UpdateLowestBall(HashSet<int> pottedBalls)
    {
        if (pottedBalls == null) return;
        
        int newLowest = GetLowestBallOnTable(pottedBalls);
        if (newLowest != lowestBallOnTable)
        {
            lowestBallOnTable = newLowest;
            OnLowestBallChanged?.Invoke(newLowest);
            PublishStatus($"Lowest ball on table: {newLowest}");
        }
    }

    private void IncrementConsecutiveFouls(int playerIndex)
    {
        if (playerIndex == 0)
        {
            consecutiveFoulsPlayer1++;
            OnConsecutiveFouls?.Invoke(0, consecutiveFoulsPlayer1);
            if (consecutiveFoulsPlayer1 >= 2)
                PublishStatus($"WARNING: {GetPlayerName(0)} has {consecutiveFoulsPlayer1} consecutive fouls!");
        }
        else
        {
            consecutiveFoulsPlayer2++;
            OnConsecutiveFouls?.Invoke(1, consecutiveFoulsPlayer2);
            if (consecutiveFoulsPlayer2 >= 2)
                PublishStatus($"WARNING: {GetPlayerName(1)} has {consecutiveFoulsPlayer2} consecutive fouls!");
        }
    }

    private void ResetConsecutiveFouls(int playerIndex)
    {
        if (playerIndex == 0)
        {
            if (consecutiveFoulsPlayer1 > 0)
            {
                consecutiveFoulsPlayer1 = 0;
                OnConsecutiveFouls?.Invoke(0, 0);
            }
        }
        else
        {
            if (consecutiveFoulsPlayer2 > 0)
            {
                consecutiveFoulsPlayer2 = 0;
                OnConsecutiveFouls?.Invoke(1, 0);
            }
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
        Debug.Log($"[9-Ball] {message}");
        OnStatusMessage?.Invoke(message);
    }
}