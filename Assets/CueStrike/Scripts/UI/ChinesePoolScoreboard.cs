using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI
{
    /// <summary>
    /// AAA Scoreboard for Chinese Pool (Chinese 8-Ball).
    /// Real-time score tracking, ball potted visualization, turn indicator.
    /// VR-optimized world-space canvas support.
    /// </summary>
    public class ChinesePoolScoreboard : MonoBehaviour
    {
        #region Events
        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnTurnChanged;
        public event Action<string> OnFoulCommitted;
        #endregion

        #region Serialized Fields
        [Header("Player 1")]
        [SerializeField] private Text _player1NameText;
        [SerializeField] private Text _player1ScoreText;
        [SerializeField] private Text _player1FramesText; // R25 — frames won (match score)
        [SerializeField] private Image _player1TurnIndicator;
        [SerializeField] private Transform _player1BallsContainer;

        [Header("Player 2")]
        [SerializeField] private Text _player2NameText;
        [SerializeField] private Text _player2ScoreText;
        [SerializeField] private Text _player2FramesText; // R25 — frames won (match score)
        [SerializeField] private Image _player2TurnIndicator;
        [SerializeField] private Transform _player2BallsContainer;

        [Header("Match Info")]
        [SerializeField] private Text _matchTimerText;
        [SerializeField] private Text _foulCounterText;
        [SerializeField] private Text _currentInningText;
        [SerializeField] private GameObject _foulBadge;

        [Header("Ball Icons")]
        [SerializeField] private GameObject _ballIconPrefab;
        [SerializeField] private Sprite[] _ballSprites; // 1-15

        [Header("Visual Settings")]
        [SerializeField] private Color _activeTurnColor = new Color(1f, 0.84f, 0f, 1f); // Gold
        [SerializeField] private Color _inactiveTurnColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color _foulColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float _turnPulseSpeed = 2f;
        #endregion

        #region Private Fields
        private int _player1Score;
        private int _player2Score;
        private int _currentPlayerTurn = 1; // 1 or 2
        private int _foulCount;
        private int _inningCount = 1;
        private float _matchTimer;
        private bool _timerRunning;
        private readonly List<int> _player1PottedBalls = new List<int>();
        private readonly List<int> _player2PottedBalls = new List<int>();
        private readonly Dictionary<int, GameObject> _ballIconMap = new Dictionary<int, GameObject>();
        private CueStrikeUIAnimations _animator;
        private StringBuilder _stringBuilder = new StringBuilder(32);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _animator = FindFirstObjectByType<CueStrikeUIAnimations>();
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<CueStrikeUIAnimations>();
            }
        }

        private void Update()
        {
            if (_timerRunning)
            {
                _matchTimer += Time.deltaTime;
                UpdateTimerDisplay();
            }

            PulseTurnIndicator();
        }
        #endregion

        #region Public API
        public void Initialize(string player1Name, string player2Name)
        {
            if (_player1NameText != null) _player1NameText.text = player1Name ?? "Player 1";
            if (_player2NameText != null) _player2NameText.text = player2Name ?? "Player 2";
            ResetScoreboard();
        }

        public void StartMatch()
        {
            _matchTimer = 0f;
            _timerRunning = true;
            _inningCount = 1;
            UpdateInningDisplay();
            UpdateTurnIndicator();
        }

        public void StopMatch()
        {
            _timerRunning = false;
        }

        /// <summary>
        /// R25 — updates the frames-won display (match score, e.g. Best-of).
        /// Called by ChinesePoolUIManager on frame end.
        /// </summary>
        public void SetFrameScore(int player1Frames, int player2Frames)
        {
            if (_player1FramesText != null) _player1FramesText.text = player1Frames.ToString("D2");
            if (_player2FramesText != null) _player2FramesText.text = player2Frames.ToString("D2");
        }

        public void AddScore(int playerNumber, int points)
        {
            if (playerNumber == 1)
            {
                _player1Score += points;
                UpdateScoreText(_player1ScoreText, _player1Score);
            }
            else if (playerNumber == 2)
            {
                _player2Score += points;
                UpdateScoreText(_player2ScoreText, _player2Score);
            }
            OnScoreChanged?.Invoke(_player1Score, _player2Score);
        }

        public void SetScore(int playerNumber, int score)
        {
            if (playerNumber == 1)
            {
                _player1Score = score;
                UpdateScoreText(_player1ScoreText, _player1Score);
            }
            else if (playerNumber == 2)
            {
                _player2Score = score;
                UpdateScoreText(_player2ScoreText, _player2Score);
            }
            OnScoreChanged?.Invoke(_player1Score, _player2Score);
        }

        public void SwitchTurn()
        {
            _currentPlayerTurn = _currentPlayerTurn == 1 ? 2 : 1;
            if (_currentPlayerTurn == 1)
            {
                _inningCount++;
                UpdateInningDisplay();
            }
            UpdateTurnIndicator();
            OnTurnChanged?.Invoke(_currentPlayerTurn);
        }

        public void RegisterPottedBall(int playerNumber, int ballNumber)
        {
            if (ballNumber < 1 || ballNumber > 15) return;

            if (playerNumber == 1)
            {
                if (!_player1PottedBalls.Contains(ballNumber))
                {
                    _player1PottedBalls.Add(ballNumber);
                    CreateBallIcon(_player1BallsContainer, ballNumber);
                }
            }
            else if (playerNumber == 2)
            {
                if (!_player2PottedBalls.Contains(ballNumber))
                {
                    _player2PottedBalls.Add(ballNumber);
                    CreateBallIcon(_player2BallsContainer, ballNumber);
                }
            }
        }

        public void RegisterFoul(string foulType)
        {
            _foulCount++;
            UpdateFoulDisplay();
            OnFoulCommitted?.Invoke(foulType);

            if (_foulBadge != null)
            {
                _foulBadge.SetActive(true);
                if (_animator != null)
                {
                    _animator.Bounce(_foulBadge.transform, 0.4f);
                }
            }

            CueStrikeUIManager.Instance?.ShowNotification($"FOUL: {foulType}", 3f);
        }

        public void ResetScoreboard()
        {
            _player1Score = 0;
            _player2Score = 0;
            _currentPlayerTurn = 1;
            _foulCount = 0;
            _matchTimer = 0f;
            _timerRunning = false;
            _inningCount = 1;
            _player1PottedBalls.Clear();
            _player2PottedBalls.Clear();

            UpdateScoreText(_player1ScoreText, 0);
            UpdateScoreText(_player2ScoreText, 0);
            SetFrameScore(0, 0);
            UpdateTimerDisplay();
            UpdateFoulDisplay();
            UpdateInningDisplay();
            UpdateTurnIndicator();

            ClearBallIcons();

            if (_foulBadge != null) _foulBadge.SetActive(false);
        }
        #endregion

        #region Visual Updates
        private void UpdateScoreText(Text target, int score)
        {
            if (target == null) return;
            _stringBuilder.Clear();
            _stringBuilder.Append(score.ToString("D2"));
            target.text = _stringBuilder.ToString();

            if (_animator != null)
            {
                _animator.ScaleBounce(target.transform, 0.25f);
            }
        }

        private void UpdateTurnIndicator()
        {
            if (_player1TurnIndicator != null)
            {
                _player1TurnIndicator.color = _currentPlayerTurn == 1 ? _activeTurnColor : _inactiveTurnColor;
            }
            if (_player2TurnIndicator != null)
            {
                _player2TurnIndicator.color = _currentPlayerTurn == 2 ? _activeTurnColor : _inactiveTurnColor;
            }
        }

        private void PulseTurnIndicator()
        {
            Image activeIndicator = _currentPlayerTurn == 1 ? _player1TurnIndicator : _player2TurnIndicator;
            if (activeIndicator == null) return;

            float pulse = Mathf.PingPong(Time.time * _turnPulseSpeed, 1f);
            Color baseColor = _activeTurnColor;
            activeIndicator.color = Color.Lerp(baseColor * 0.7f, baseColor, pulse);
        }

        private void UpdateTimerDisplay()
        {
            if (_matchTimerText == null) return;
            int minutes = Mathf.FloorToInt(_matchTimer / 60f);
            int seconds = Mathf.FloorToInt(_matchTimer % 60f);
            _stringBuilder.Clear();
            _stringBuilder.AppendFormat("{0:D2}:{1:D2}", minutes, seconds);
            _matchTimerText.text = _stringBuilder.ToString();
        }

        private void UpdateFoulDisplay()
        {
            if (_foulCounterText == null) return;
            _stringBuilder.Clear();
            _stringBuilder.Append("Fouls: ").Append(_foulCount);
            _foulCounterText.text = _stringBuilder.ToString();

            if (_foulCount > 0 && _foulCounterText != null)
            {
                _foulCounterText.color = _foulColor;
            }
        }

        private void UpdateInningDisplay()
        {
            if (_currentInningText == null) return;
            _stringBuilder.Clear();
            _stringBuilder.Append("Inning ").Append(_inningCount);
            _currentInningText.text = _stringBuilder.ToString();
        }
        #endregion

        #region Ball Icons
        private void CreateBallIcon(Transform container, int ballNumber)
        {
            if (container == null || _ballIconPrefab == null) return;

            GameObject icon = Instantiate(_ballIconPrefab, container);
            icon.name = $"Ball_{ballNumber}";

            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null && ballNumber >= 1 && ballNumber <= 15 && _ballSprites != null && _ballSprites.Length >= ballNumber)
            {
                iconImage.sprite = _ballSprites[ballNumber - 1];
            }

            _ballIconMap[ballNumber] = icon;

            if (_animator != null)
            {
                _animator.ScaleIn(icon.transform, 0.3f);
            }
        }

        private void ClearBallIcons()
        {
            foreach (var kvp in _ballIconMap)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            _ballIconMap.Clear();
        }
        #endregion

        #region Self-Test
        public bool RunSelfTest()
        {
            bool pass = true;
            if (_player1ScoreText == null || _player2ScoreText == null)
            {
                Debug.LogError("[Self-Test] Scoreboard: Score texts not assigned.");
                pass = false;
            }
            if (_player1TurnIndicator == null || _player2TurnIndicator == null)
            {
                Debug.LogError("[Self-Test] Scoreboard: Turn indicators not assigned.");
                pass = false;
            }
            Debug.Log($"[Self-Test] ChinesePoolScoreboard: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}