using System;
using UnityEngine;

public enum CueStrikeGameState
{
    Waiting,
    Aiming,
    Shooting,
    Settling,
    TurnEnd,
    Foul,
    GameOver
}

public class CueStrikeRulesManager : MonoBehaviour
{
    public static CueStrikeRulesManager Instance { get; private set; }

    public string[] playerNames = new[] { "Player 1", "Player 2" };
    public int currentPlayer = 0;
    public int[] scores = new int[2];
    public int[] framesWon = new int[2];
    public int currentBreak = 0;
    public bool lastShotPotted = false;
    public bool lastShotFoul = false;
    public int foulPoints = 4;
    public CueStrikeGameState gameState = CueStrikeGameState.Waiting;

    // Existing events
    public event Action<int> OnPlayerScore;
    public event Action OnTurnChanged;
    public event Action<bool, bool> OnShotResolved;
    public event Action<string> OnStatusMessage;
    public event Action<CueStrikeGameState> OnGameStateChanged;

    // New events for mascot/crowd systems
    public event Action<int> OnFrameWon; // winner player index
    public event Action<int, int> OnScoreChanged; // playerIndex, newScore
    public event Action<int> OnBreakUpdated; // current break score

    // Event for NoirMemory integration: ballId, isCorrectBallForCurrentPlayer
    public event Action<int, bool> OnBallPottedEvent;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }

    void Start()
    {
        SetGameState(CueStrikeGameState.Waiting);
        PublishStatus($"Ready. {playerNames[currentPlayer]}'s turn.");
    }

    public void BeginShot()
    {
        lastShotPotted = false;
        lastShotFoul = false;
        SetGameState(CueStrikeGameState.Aiming);
        PublishStatus($"{playerNames[currentPlayer]} is aiming...");
    }

    public void BallPotted(int ballId, string pocket)
    {
        if (ballId == 0)
        {
            RecordFoul("Cue ball potted");
            return;
        }

        int points = 1;
        lastShotPotted = true;
        currentBreak += points;
        scores[currentPlayer] += points;
        OnPlayerScore?.Invoke(currentPlayer);
        OnScoreChanged?.Invoke(currentPlayer, scores[currentPlayer]);
        OnBreakUpdated?.Invoke(currentBreak);
        PublishStatus($"{playerNames[currentPlayer]} potted ball {ballId} in {pocket}.");
    }

    public void RecordFoul(string reason)
    {
        lastShotFoul = true;
        lastShotPotted = false;
        scores[currentPlayer] = Mathf.Max(0, scores[currentPlayer] - foulPoints);
        currentBreak = 0;
        OnPlayerScore?.Invoke(currentPlayer);
        OnScoreChanged?.Invoke(currentPlayer, scores[currentPlayer]);
        OnBreakUpdated?.Invoke(currentBreak);
        PublishStatus($"Foul: {reason}. {playerNames[currentPlayer]} loses {foulPoints} points.");
    }

    public void WinFrame()
    {
        framesWon[currentPlayer]++;
        currentBreak = 0;
        OnBreakUpdated?.Invoke(currentBreak);
        PublishStatus($"{playerNames[currentPlayer]} wins the frame! Frame score updated.");
        OnPlayerScore?.Invoke(currentPlayer);
        
        // Fire frame won event for mascots/crowd
        OnFrameWon?.Invoke(currentPlayer);
    }

    public void ResolveShot()
    {
        SetGameState(CueStrikeGameState.Settling);

        if (lastShotFoul)
        {
            PublishStatus("Foul committed. Turn will change.");
        }
        else if (lastShotPotted)
        {
            PublishStatus($"Good shot. {playerNames[currentPlayer]} continues.");
        }
        else
        {
            PublishStatus("No ball potted. Turn will change.");
        }

        OnShotResolved?.Invoke(lastShotPotted, lastShotFoul);
    }

    public void NextPlayer()
    {
        currentPlayer = (currentPlayer + 1) % playerNames.Length;
        currentBreak = 0;
        OnBreakUpdated?.Invoke(currentBreak);
        OnTurnChanged?.Invoke();
        SetGameState(CueStrikeGameState.Aiming); // changed from Waiting to Aiming to allow immediate shoot preparation
        PublishStatus($"Now {playerNames[currentPlayer]}'s turn.");
    }

    public void SetCurrentPlayer(int playerIndex)
    {
        if (currentPlayer != playerIndex)
        {
            currentPlayer = playerIndex;
            OnTurnChanged?.Invoke();
            PublishStatus($"Network synced current player: {playerNames[currentPlayer]}");
        }
    }

    public void SetPlayerScore(int playerIndex, int score)
    {
        if (playerIndex >= 0 && playerIndex < scores.Length && scores[playerIndex] != score)
        {
            scores[playerIndex] = score;
            OnPlayerScore?.Invoke(playerIndex);
            OnScoreChanged?.Invoke(playerIndex, score);
            PublishStatus($"Network synced {playerNames[playerIndex]} score to {score}");
        }
    }

    public void SetGameState(CueStrikeGameState state)
    {
        if (gameState != state)
        {
            gameState = state;
            OnGameStateChanged?.Invoke(state);
            PublishStatus($"Network synced GameState: {state}");
        }
    }

    void PublishStatus(string message)
    {
        Debug.Log($"RulesManager: {message}");
        OnStatusMessage?.Invoke(message);
    }
}
