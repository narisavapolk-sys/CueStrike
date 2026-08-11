using UnityEngine;
using System;
using System.Collections.Generic;
using CueStrike.UI.ChinesePool;

namespace CueStrike.Gameplay.ChinesePool
{
    /// <summary>
    /// Central game manager for Chinese 8-Ball (Red/Yellow/Black) mode.
    /// Coordinates rules, ball setup, call-shot UI, and AI.
    /// Singleton pattern — auto-created if missing from scene.
    /// </summary>
    public class ChinesePoolGameManager : MonoBehaviour
    {
        #region Singleton
        public static ChinesePoolGameManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CueStrike] ChinesePoolGameManager duplicate detected. Destroying self.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        #endregion

        #region Enums
        public enum BallGroup
        {
            None,
            Red,
            Yellow
        }
        #endregion

        #region Inspector References
        [Header("Subsystems")]
        [Tooltip("Assign ChinesePoolBallSetup component. Auto-found if left empty.")]
        public ChinesePoolBallSetup ballSetup;

        [Tooltip("Assign ChinesePoolCallShotUI component. Auto-found if left empty.")]
        public ChinesePoolCallShotUI callShotUI;

        [Tooltip("Assign ChinesePoolAIModifier component. Auto-found if left empty.")]
        public ChinesePoolAIModifier aiModifier;
        #endregion

        #region State
        [Header("Game State")]
        public ChinesePoolMatchState currentPhase = ChinesePoolMatchState.Waiting;
        public int currentPlayerIndex = 0; // 0 = Player 1, 1 = Player 2
        public BallGroup player1Group = BallGroup.None;
        public BallGroup player2Group = BallGroup.None;
        public int scorePlayer1 = 0;
        public int scorePlayer2 = 0;
        public int framesWonPlayer1 = 0;
        public int framesWonPlayer2 = 0;
        public int maxFrames = 5; // Best of 5 (0 = practice: no match end)
        public bool isPracticeMode = false; // R25 — practice = frames keep going, no match over
        public bool callShotRequired = true;
        public int calledBallId = -1;
        public int calledPocketId = -1;
        public bool isAiTurn = false;
        #endregion

        #region Events
        public event Action<ChinesePoolMatchState> OnPhaseChanged;
        public event Action<int> OnTurnChanged;
        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnFrameWon;
        public event Action<int> OnFrameLost;
        public event Action<int, string> OnFoulCommitted; // playerIndex, foulName
        public event Action OnMatchOver;
        public event Action<int, int> OnBallGroupAssigned; // playerIndex, group (0=None,1=Red,2=Yellow)
        #endregion

        #region Initialization
        void Start()
        {
            AutoWireReferences();
            Debug.Log("[CueStrike] ChinesePoolGameManager initialized. Phase: " + currentPhase);
        }

        void OnDestroy()
        {
            if (callShotUI != null)
            {
                callShotUI.OnShotCalled -= SetCallShot;
                callShotUI.OnCallShotCancelled -= ClearCallShot;
            }

            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Auto-finds subsystem references if not assigned in Inspector.
        /// </summary>
        void AutoWireReferences()
        {
            // ChinesePoolRules is a static utility class - no runtime instance needed.
            // Log removed to avoid confusion.

            if (ballSetup == null)
            {
                ballSetup = FindFirstObjectByType<ChinesePoolBallSetup>();
                if (ballSetup == null)
                    Debug.LogWarning("[CueStrike] ChinesePoolBallSetup not found in scene. Assign manually in Inspector.");
            }

            if (callShotUI == null)
            {
                callShotUI = FindFirstObjectByType<ChinesePoolCallShotUI>();
                if (callShotUI == null)
                    Debug.LogWarning("[CueStrike] ChinesePoolCallShotUI not found in scene. Assign manually in Inspector.");
            }

            if (callShotUI != null)
            {
                callShotUI.OnShotCalled += SetCallShot;
                callShotUI.OnCallShotCancelled += ClearCallShot;
            }

            if (aiModifier == null)
            {
                aiModifier = FindFirstObjectByType<ChinesePoolAIModifier>();
                if (aiModifier == null)
                    Debug.Log("[CueStrike] ChinesePoolAIModifier not found. AI features will be disabled.");
            }
        }
        #endregion

        #region Public API — Game Flow

        /// <summary>
        /// Starts a new frame (rack). Resets balls, scores, phase to Break.
        /// </summary>
        public void StartNewFrame()
        {
            currentPhase = ChinesePoolMatchState.Break;
            currentPlayerIndex = 0;
            player1Group = BallGroup.None;
            player2Group = BallGroup.None;
            scorePlayer1 = 0;
            scorePlayer2 = 0;
            calledBallId = -1;
            calledPocketId = -1;
            isAiTurn = false;

            if (ballSetup != null)
            {
                ballSetup.SetupRack();
            }
            else
            {
                Debug.LogError("[CueStrike] Cannot start frame — ChinesePoolBallSetup is null! Assign in Inspector.");
            }

            OnPhaseChanged?.Invoke(currentPhase);
            OnTurnChanged?.Invoke(currentPlayerIndex);
            Debug.Log("[CueStrike] New frame started. Player 1 to break.");
        }

        /// <summary>
        /// Processes the result of a completed shot.
        /// </summary>
        public void ProcessShotResult(ShotResult result)
        {
            // FAIL-SAFE: Null check
            if (result == null)
            {
                Debug.LogError("[CueStrike] ProcessShotResult called with null result.");
                return;
            }

            // During break, determine groups
            if (currentPhase == ChinesePoolMatchState.Break || currentPhase == ChinesePoolMatchState.OpenTable)
            {
                HandleBreakOrOpenTable(result);
                return;
            }

            // Normal play
            if (result.isFoul)
            {
                HandleFoul(result.foulType);
                return;
            }

            // Check call shot compliance
            if (callShotRequired && currentPhase != ChinesePoolMatchState.Break)
            {
                if (!result.callShotMatched)
                {
                    HandleFoul("WrongBallOrPocket");
                    return;
                }
            }

            // Valid pot
            if (result.ballPottedId == 8)
            {
                HandleEightBallPotted(result);
                return;
            }

            // Normal ball potted — check if correct group
            BallGroup ballGroup = GetBallGroup(result.ballPottedId);
            BallGroup playerGroup = (currentPlayerIndex == 0) ? player1Group : player2Group;

            if (ballGroup == playerGroup)
            {
                // Correct pot — same player continues
                Debug.Log($"[CueStrike] Player {currentPlayerIndex + 1} potted correct ball {result.ballPottedId}.");
                UpdateScore(currentPlayerIndex, 1);
                // Stay on same player
            }
            else if (ballGroup != BallGroup.None)
            {
                // Wrong ball potted — foul
                HandleFoul("WrongBallPotted");
                return;
            }
            else
            {
                // No ball potted or cue ball only — switch turn
                NextPlayer();
            }

            OnPhaseChanged?.Invoke(currentPhase);
        }

        /// <summary>
        /// Advances to the next player's turn.
        /// </summary>
        public void NextPlayer()
        {
            currentPlayerIndex = (currentPlayerIndex == 0) ? 1 : 0;
            calledBallId = -1;
            calledPocketId = -1;
            isAiTurn = (currentPlayerIndex == 1 && aiModifier != null);

            OnTurnChanged?.Invoke(currentPlayerIndex);
            Debug.Log($"[CueStrike] Turn changed to Player {currentPlayerIndex + 1}.");

            MaybeShowCallShotUI();
        }

        /// <summary>
        /// Returns true if the current frame is over.
        /// </summary>
        public bool IsFrameOver()
        {
            return currentPhase == ChinesePoolMatchState.FrameOver || currentPhase == ChinesePoolMatchState.MatchOver;
        }

        /// <summary>
        /// Returns the winner of the current frame (0 or 1), or -1 if not over.
        /// </summary>
        public int GetFrameWinner()
        {
            if (!IsFrameOver()) return -1;
            if (framesWonPlayer1 > framesWonPlayer2) return 0;
            if (framesWonPlayer2 > framesWonPlayer1) return 1;
            return -1; // Draw or not decided
        }
        #endregion

        #region Public API — Call Shot

        /// <summary>
        /// Sets the called shot before striking. Call from ChinesePoolCallShotUI.
        /// </summary>
        public void SetCallShot(int ballId, int pocketId)
        {
            if (ballId < 1 || ballId > 15)
            {
                Debug.LogError($"[CueStrike] Invalid ballId {ballId} for call shot. Must be 1-15.");
                return;
            }
            if (pocketId < 0 || pocketId > 5)
            {
                Debug.LogError($"[CueStrike] Invalid pocketId {pocketId}. Must be 0-5.");
                return;
            }

            calledBallId = ballId;
            calledPocketId = pocketId;
            Debug.Log($"[CueStrike] Call shot set: Ball {ballId} -> Pocket {pocketId}");
        }

        /// <summary>
        /// Clears the current call shot.
        /// </summary>
        public void ClearCallShot()
        {
            calledBallId = -1;
            calledPocketId = -1;
        }
        #endregion

        #region Public API — Match Control

        /// <summary>
        /// Starts a new match with the specified frame limit.
        /// bestOfFrames = 0 → Practice mode (play indefinitely, no match end).
        /// bestOfFrames = 1 → Single frame (Best of 1).
        /// </summary>
        public void StartNewMatch(int bestOfFrames = 5)
        {
            maxFrames = Mathf.Max(0, bestOfFrames);
            isPracticeMode = maxFrames == 0;
            framesWonPlayer1 = 0;
            framesWonPlayer2 = 0;
            StartNewFrame();
            Debug.Log($"[CueStrike] New match started — {(isPracticeMode ? "Practice (no end)" : $"Best of {maxFrames}")}.");
        }

        /// <summary>
        /// Convenience: start a practice match (frames keep going, no match end).
        /// </summary>
        public void StartPracticeMatch()
        {
            StartNewMatch(0);
        }

        /// <summary>
        /// Ends the current frame and awards it to the specified player.
        /// </summary>
        public void EndFrame(int winnerIndex)
        {
            if (winnerIndex == 0)
            {
                framesWonPlayer1++;
                OnFrameWon?.Invoke(0);
                OnFrameLost?.Invoke(1);
            }
            else if (winnerIndex == 1)
            {
                framesWonPlayer2++;
                OnFrameWon?.Invoke(1);
                OnFrameLost?.Invoke(0);
            }

            // Check match end (skip in practice mode — frames keep going indefinitely)
            int framesNeeded = (maxFrames / 2) + 1;
            if (!isPracticeMode && (framesWonPlayer1 >= framesNeeded || framesWonPlayer2 >= framesNeeded))
            {
                currentPhase = ChinesePoolMatchState.MatchOver;
                OnMatchOver?.Invoke();
                Debug.Log($"[CueStrike] Match over! Winner: Player {winnerIndex + 1}");
            }
            else
            {
                currentPhase = ChinesePoolMatchState.FrameOver;
                Debug.Log($"[CueStrike] {(isPracticeMode ? "Practice" : "Frame")} over. Score: P1 {framesWonPlayer1} - P2 {framesWonPlayer2}");
            }

            OnPhaseChanged?.Invoke(currentPhase);
        }
        #endregion

        #region Public API — Queries

        /// <summary>
        /// Returns the ball group (Red/Yellow/None) for a given ball ID.
        /// </summary>
        public BallGroup GetBallGroup(int ballId)
        {
            if (ballId >= 1 && ballId <= 7) return BallGroup.Red;
            if (ballId >= 9 && ballId <= 15) return BallGroup.Yellow;
            return BallGroup.None; // 8-ball or cue ball
        }

        /// <summary>
        /// Returns the assigned group for the current player.
        /// </summary>
        public BallGroup GetCurrentPlayerGroup()
        {
            return (currentPlayerIndex == 0) ? player1Group : player2Group;
        }

        /// <summary>
        /// Returns true if the current player must call shot before striking.
        /// </summary>
        public bool IsCallShotRequired()
        {
            return callShotRequired && currentPhase != ChinesePoolMatchState.Break && currentPhase != ChinesePoolMatchState.OpenTable;
        }
        #endregion

        #region Private Helpers

        /// <summary>
        /// Shows the call-shot panel when the current player must call a shot
        /// (call-shot rules, past the break/open-table phase, human turn only).
        /// </summary>
        private void MaybeShowCallShotUI()
        {
            if (!IsCallShotRequired()) return;
            if (isAiTurn) return;

            ChinesePoolUIManager.Instance?.ShowCallShot(false, BallGroupToPlayerGroup(GetCurrentPlayerGroup()));
        }

        private static int BallGroupToPlayerGroup(BallGroup group)
        {
            if (group == BallGroup.Red) return 1;
            if (group == BallGroup.Yellow) return 2;
            return 0;
        }

    void HandleBreakOrOpenTable(ShotResult result)
    {
        bool redPotted = result.redBallsPotted > 0;
        bool yellowPotted = result.yellowBallsPotted > 0;

        if (redPotted && !yellowPotted)
        {
            AssignBallGroup(currentPlayerIndex, BallGroup.Red);
            AssignBallGroup(OtherPlayer(), BallGroup.Yellow);
            currentPhase = ChinesePoolMatchState.Playing;
            Debug.Log("[CueStrike] Red ball potted on break. Player assigned RED.");
        }
        else if (yellowPotted && !redPotted)
        {
            AssignBallGroup(currentPlayerIndex, BallGroup.Yellow);
            AssignBallGroup(OtherPlayer(), BallGroup.Red);
            currentPhase = ChinesePoolMatchState.Playing;
            Debug.Log("[CueStrike] Yellow ball potted on break. Player assigned YELLOW.");
        }
        else if (redPotted && yellowPotted)
        {
            // Both potted — player chooses (stay OpenTable until chosen)
            currentPhase = ChinesePoolMatchState.OpenTable;
            Debug.Log("[CueStrike] Both groups potted on break. Player chooses group.");
        }
        else
        {
            // No group ball potted — stay OpenTable
            currentPhase = ChinesePoolMatchState.OpenTable;
            NextPlayer();
        }

        OnPhaseChanged?.Invoke(currentPhase);

        // Show the call-shot panel when this turn requires a called shot (after group assignment).
        MaybeShowCallShotUI();
    }

        void HandleEightBallPotted(ShotResult result)
        {
            BallGroup currentGroup = GetCurrentPlayerGroup();
            bool hasClearedGroup = HasClearedAllGroupBalls(currentPlayerIndex);

            if (!hasClearedGroup)
            {
                // 8-ball potted early — loss of frame
                Debug.LogError($"[CueStrike] Player {currentPlayerIndex + 1} potted 8-ball early! Frame lost.");
                EndFrame(OtherPlayer());
                return;
            }

            if (callShotRequired && (calledBallId != 8 || !result.callShotMatched))
            {
                Debug.LogError($"[CueStrike] Player {currentPlayerIndex + 1} potted 8-ball without correct call shot! Frame lost.");
                EndFrame(OtherPlayer());
                return;
            }

            // Legal 8-ball pot — win frame
            Debug.Log($"[CueStrike] Player {currentPlayerIndex + 1} legally potted 8-ball! Frame won.");
            EndFrame(currentPlayerIndex);
        }

        void HandleFoul(string foulType)
        {
            Debug.LogWarning($"[CueStrike] FOUL by Player {currentPlayerIndex + 1}: {foulType}");
            OnFoulCommitted?.Invoke(currentPlayerIndex, foulType);

            // If 8-ball was involved in foul, check for loss
            if (foulType.Contains("EightBall") || foulType.Contains("8Ball"))
            {
                EndFrame(OtherPlayer());
                return;
            }

            NextPlayer();
        }

        void AssignBallGroup(int playerIndex, BallGroup group)
        {
            if (playerIndex == 0) player1Group = group;
            else player2Group = group;

            int groupCode = (group == BallGroup.Red) ? 1 : (group == BallGroup.Yellow) ? 2 : 0;
            OnBallGroupAssigned?.Invoke(playerIndex, groupCode);
        }

        int OtherPlayer()
        {
            return (currentPlayerIndex == 0) ? 1 : 0;
        }

        void UpdateScore(int playerIndex, int points)
        {
            if (playerIndex == 0) scorePlayer1 += points;
            else scorePlayer2 += points;
            OnScoreChanged?.Invoke(scorePlayer1, scorePlayer2);
        }

        bool HasClearedAllGroupBalls(int playerIndex)
        {
            BallGroup group = (playerIndex == 0) ? player1Group : player2Group;
            // This would normally query the ball setup to count remaining balls
            // Stub: return true if score >= 7 (all group balls potted)
            int score = (playerIndex == 0) ? scorePlayer1 : scorePlayer2;
            return score >= 7;
        }
        #endregion

        #region Self-Test
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test ChinesePool GameManager")]
        public static void SelfTest()
        {
            bool pass = true;

            var mgr = FindFirstObjectByType<ChinesePoolGameManager>();
            if (mgr == null)
            {
                Debug.LogError("FAIL: ChinesePoolGameManager not found in scene! Create an empty GameObject and attach this script.");
                pass = false;
            }
            else
            {
                // ChinesePoolRules is a static utility class - no runtime instance needed.
                Debug.Log("[SelfTest] ChinesePoolRules is a static utility class (no instance check needed).");
                if (mgr.ballSetup == null)
                {
                    Debug.LogWarning("ChinesePoolBallSetup not assigned. Assign in Inspector for full functionality.");
                }
                if (mgr.callShotUI == null)
                {
                    Debug.LogWarning("ChinesePoolCallShotUI not assigned. Assign in Inspector for full functionality.");
                }
            }

            if (pass) Debug.Log("ChinesePoolGameManager SELF-TEST PASSED — Ready for human verify.");
            else Debug.LogWarning("ChinesePoolGameManager SELF-TEST FAILED — Fix missing references before proceeding.");
        }
#endif
        #endregion
    }

    #region ShotResult Data Class
    /// <summary>
    /// Data container for shot outcome. Passed to ProcessShotResult().
    /// </summary>
    [Serializable]
    public class ShotResult
    {
        public bool isFoul;
        public string foulType;
        public int ballPottedId; // -1 if none
        public bool callShotMatched;
        public int redBallsPotted;
        public int yellowBallsPotted;
        public int cueBallPocketId; // -1 if not potted
    }
    #endregion
}