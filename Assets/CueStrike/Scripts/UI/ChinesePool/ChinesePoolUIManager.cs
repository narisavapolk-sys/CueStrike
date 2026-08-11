using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI.ChinesePool
{
    /// <summary>
    /// Central manager for all Chinese Pool-specific UI.
    /// Coordinates CallShot, GroupDisplay, and Scoreboard.
    /// </summary>
    public class ChinesePoolUIManager : MonoBehaviour
    {
        [Header("Sub-Managers")]
        [SerializeField] private ChinesePoolCallShotUI _callShotUI;
        [SerializeField] private ChinesePoolGroupDisplay _groupDisplay;
        [SerializeField] private ChinesePoolScoreboard _scoreboard;

        [Header("Game State")]
        [SerializeField] private Text _gameStateText;
        [SerializeField] private GameObject _foulNotification;
        [SerializeField] private float _foulDisplayDuration = 3f;

        [Header("Turn Info")]
        [SerializeField] private Text _currentPlayerText;
        [SerializeField] private Image _turnIndicator;

        public static ChinesePoolUIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void InitializeGame()
        {
            _groupDisplay?.Initialize();
            _scoreboard?.ResetScoreboard();
            UpdateGameState("OPEN TABLE — Break Shot");
        }

        public void ShowCallShot(bool isOpenTable, int playerGroup)
        {
            _callShotUI?.ShowCallShot(isOpenTable, playerGroup);
        }

        public void OnGroupAssigned(int playerGroup)
        {
            _groupDisplay?.SetPlayerGroup(1, playerGroup);
            string groupName = playerGroup == 1 ? "RED" : "YELLOW";
            UpdateGameState($"GROUP ASSIGNED: You are {groupName}");
        }

        public void OnBallPotted(int ballNumber, int playerNumber)
        {
            _groupDisplay?.OnBallPotted(ballNumber);
            _scoreboard?.RegisterPottedBall(playerNumber, ballNumber);

            if (_groupDisplay != null && _groupDisplay.IsEightBallTime())
            {
                UpdateGameState("8-BALL TIME — Pot the black to win!");
            }
        }

        public void OnFoul(string foulType, string playerName)
        {
            _scoreboard?.RegisterFoul(foulType);
            ShowFoulNotification($"FOUL by {playerName}: {foulType}");
            UpdateGameState($"FOUL — {foulType}");
        }

        public void OnTurnChanged(int playerNumber, string playerName)
        {
            if (_currentPlayerText != null)
                _currentPlayerText.text = $"Current: {playerName}";

            if (_turnIndicator != null)
            {
                _turnIndicator.color = (playerNumber == 1)
                    ? new Color(0.2f, 0.6f, 1f, 1f)
                    : new Color(1f, 0.4f, 0.2f, 1f);
            }

            UpdateGameState($"{playerName}'s Turn");
        }

        public void OnGameOver(int winnerPlayer, string winnerName)
        {
            UpdateGameState($"GAME OVER — {winnerName} WINS!");
            if (_scoreboard != null)
            {
                _scoreboard.StopMatch();
            }
        }

        /// <summary>R25 — updates the match score (frames won) on the scoreboard.</summary>
        public void SetFrameScore(int player1Frames, int player2Frames)
        {
            if (_scoreboard != null)
            {
                _scoreboard.SetFrameScore(player1Frames, player2Frames);
            }
        }

        /// <summary>R25 — called when a frame ends (before next frame starts).</summary>
        public void OnFrameEnded(int player1Frames, int player2Frames)
        {
            SetFrameScore(player1Frames, player2Frames);
        }

        /// <summary>R25 — called when the match is over (WINNER screen handles visuals).</summary>
        public void ShowMatchOver(string winnerText)
        {
            UpdateGameState(winnerText);
            if (_scoreboard != null)
            {
                _scoreboard.StopMatch();
            }
        }

        private void UpdateGameState(string state)
        {
            if (_gameStateText != null)
                _gameStateText.text = state;
            Debug.Log($"[ChinesePoolUI] {state}");
        }

        private void ShowFoulNotification(string message)
        {
            if (_foulNotification == null) return;

            Text txt = _foulNotification.GetComponentInChildren<Text>();
            if (txt != null) txt.text = message;

            _foulNotification.SetActive(true);
            CancelInvoke(nameof(HideFoulNotification));
            Invoke(nameof(HideFoulNotification), _foulDisplayDuration);
        }

        private void HideFoulNotification()
        {
            if (_foulNotification != null)
                _foulNotification.SetActive(false);
        }

        #region Self-Test
        public bool RunSelfTest()
        {
            bool pass = true;
            if (_callShotUI == null)
            {
                Debug.LogWarning("[Self-Test] ChinesePoolUIManager: CallShotUI not assigned.");
            }
            if (_groupDisplay == null)
            {
                Debug.LogWarning("[Self-Test] ChinesePoolUIManager: GroupDisplay not assigned.");
            }
            if (_scoreboard == null)
            {
                Debug.LogError("[Self-Test] ChinesePoolUIManager: Scoreboard not assigned.");
                pass = false;
            }
            Debug.Log($"[Self-Test] ChinesePoolUIManager: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}