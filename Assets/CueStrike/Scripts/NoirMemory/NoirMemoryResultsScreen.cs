using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CueStrike.NoirMemory
{
    /// <summary>
    /// Noir Memory Results Screen — shows after completing a Noir Memory puzzle.
    /// Displays performance stats, score breakdown, and leaderboard.
    /// </summary>
    public class NoirMemoryResultsScreen : MonoBehaviour
    {
        #region Singleton
        public static NoirMemoryResultsScreen Instance { get; private set; }
        #endregion

        #region Events
        public event Action<NoirMemoryScoreData> OnResultsDisplayed;
        public event Action OnResultsClosed;
        #endregion

        #region UI References
        [Header("Panels")]
        [SerializeField] private GameObject resultsPanel;

        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI totalScoreText;
        [SerializeField] private TextMeshProUGUI correctPotsText;
        [SerializeField] private TextMeshProUGUI wrongPotsText;
        [SerializeField] private TextMeshProUGUI memoryBonusText;
        [SerializeField] private TextMeshProUGUI speedBonusText;
        [SerializeField] private TextMeshProUGUI comboBonusText;
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI rankText;

        [Header("Animations")]
        [SerializeField] private Animator resultsAnimator;
        [SerializeField] private float scoreAnimationDuration = 1.5f;
        [SerializeField] private AnimationCurve scoreCurve = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));

        [Header("Leaderboard")]
        [SerializeField] private RectTransform leaderboardContent;
        [SerializeField] private GameObject leaderboardEntryPrefab;
        [SerializeField] private int maxLeaderboardEntries = 10;

        [Header("Buttons")]
        [SerializeField] private Button replayButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button nextPuzzleButton;

        [Header("Effects")]
        [SerializeField] private ParticleSystem confettiEffect;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip failSound;

        [Header("Colors")]
        [SerializeField] private Color gradeSPerfect = new Color(1, 0.84f, 0, 1);
        [SerializeField] private Color gradeAGreat = Color.green;
        [SerializeField] private Color gradeBGood = Color.blue;
        [SerializeField] private Color gradeCOkay = new Color(1, 0.5f, 0, 1);
        [SerializeField] private Color gradeDNeedsWork = Color.red;

        #endregion

        #region State
        private NoirMemoryScoreData _currentScore;
        private List<LeaderboardEntry> _leaderboardData = new List<LeaderboardEntry>();
        private string _saveKey = "NoirMemoryLeaderboard";
        #endregion

        #region Score Data
        [Serializable]
        public class NoirMemoryScoreData
        {
            public int correctPots;
            public int wrongPots;
            public int totalAttempts;
            public float memoryAccuracy;
            public float completionTime;
            public int memoryBonus;
            public int speedBonus;
            public int comboBonus;
            public int totalScore;
            public string grade;
            public string rank;
            public string puzzleName;
            public DateTime timestamp;
        }

        [Serializable]
        public class LeaderboardEntry : IComparable<LeaderboardEntry>
        {
            public string playerName;
            public int score;
            public string grade;
            public float accuracy;
            public float time;
            public string puzzleName;
            public string date;

            public int CompareTo(LeaderboardEntry other)
            {
                return other.score.CompareTo(this.score); // Descending
            }
        }

        [Serializable]
        public class LeaderboardData
        {
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        }
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
            InitializeUI();
            HideResults();

            // Load saved leaderboard
            LoadLeaderboard();
        }

        private void InitializeUI()
        {
            if (replayButton != null)
                replayButton.onClick.AddListener(Replay);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (nextPuzzleButton != null)
                nextPuzzleButton.onClick.AddListener(NextPuzzle);
        }
        #endregion

        #region Public API

        /// <summary>
        /// Shows results screen with the given score data.
        /// </summary>
        public void ShowResults(NoirMemoryScoreData scoreData)
        {
            _currentScore = scoreData;
            if (resultsPanel != null) resultsPanel.SetActive(true);

            DisplayScoreData(scoreData);
            UpdateLeaderboard(scoreData);
            SaveLeaderboard();

            OnResultsDisplayed?.Invoke(scoreData);

            // Play effects
            if (scoreData.totalScore >= 500 && confettiEffect != null)
                confettiEffect.Play();

            // Animate score
            if (resultsAnimator != null)
                resultsAnimator.SetTrigger("Show");
        }

        /// <summary>
        /// Hides the results screen.
        /// </summary>
        public void HideResults()
        {
            if (resultsPanel != null) resultsPanel.SetActive(false);
        }

        /// <summary>
        /// Creates a score data from the puzzle manager state.
        /// </summary>
        public NoirMemoryScoreData CalculateScore(
            int correctPots, int wrongPots, int totalAttempts,
            float memoryAccuracy, float completionTime,
            int comboCount, string puzzleName
        )
        {
            var data = new NoirMemoryScoreData();
            data.correctPots = correctPots;
            data.wrongPots = wrongPots;
            data.totalAttempts = totalAttempts;
            data.memoryAccuracy = memoryAccuracy;
            data.completionTime = completionTime;
            data.puzzleName = puzzleName;
            data.timestamp = DateTime.Now;

            // Calculate bonuses
            // Memory bonus: 100 points per 10% accuracy above 50%
            data.memoryBonus = Mathf.Max(0, Mathf.RoundToInt((memoryAccuracy - 0.5f) * 200f));

            // Speed bonus: 50 points per 10 seconds under 60s
            float timeRemaining = Mathf.Max(0, 60f - completionTime);
            data.speedBonus = Mathf.RoundToInt(timeRemaining * 5f);

            // Combo bonus: 50 points per combo level
            data.comboBonus = comboCount * 50;

            // Total
            int baseScore = correctPots * 100 - wrongPots * 50;
            data.totalScore = Mathf.Max(0, baseScore + data.memoryBonus + data.speedBonus + data.comboBonus);

            // Grade
            data.grade = CalculateGrade(data.totalScore, memoryAccuracy);
            data.rank = CalculateRank(data.totalScore);

            return data;
        }

        /// <summary>
        /// Returns the leaderboard data.
        /// </summary>
        public List<LeaderboardEntry> GetLeaderboard() => _leaderboardData;

        /// <summary>
        /// Clears all leaderboard data.
        /// </summary>
        public void ClearLeaderboard()
        {
            _leaderboardData.Clear();
            SaveLeaderboard();
            RefreshLeaderboardUI();
            Debug.Log("[NoirMemory] Leaderboard cleared.");
        }

        #endregion

        #region Internal

        private void DisplayScoreData(NoirMemoryScoreData data)
        {
            if (totalScoreText != null)
                totalScoreText.text = data.totalScore.ToString();

            if (correctPotsText != null)
                correctPotsText.text = data.correctPots.ToString();

            if (wrongPotsText != null)
                wrongPotsText.text = data.wrongPots.ToString();

            if (memoryBonusText != null)
                memoryBonusText.text = $"+{data.memoryBonus}";

            if (speedBonusText != null)
                speedBonusText.text = $"+{data.speedBonus}";

            if (comboBonusText != null)
                comboBonusText.text = $"+{data.comboBonus}";

            if (gradeText != null)
            {
                gradeText.text = data.grade;
                gradeText.color = GetGradeColor(data.grade);
            }

            if (rankText != null)
            {
                string rankLabel = data.rank;
                if (_leaderboardData.Count > 0)
                {
                    int pos = _leaderboardData.FindIndex(e => e.playerName == "You" && Mathf.Approximately(e.score, data.totalScore));
                    if (pos < 0) pos = _leaderboardData.Count;
                    rankLabel = $"#{pos + 1} — {data.rank}";
                }
                rankText.text = rankLabel;
            }
        }

        private string CalculateGrade(int score, float accuracy)
        {
            if (score >= 800 && accuracy >= 0.9f) return "S";
            if (score >= 600 && accuracy >= 0.75f) return "A";
            if (score >= 400 && accuracy >= 0.6f) return "B";
            if (score >= 200) return "C";
            return "D";
        }

        private string CalculateRank(int score)
        {
            if (score >= 800) return "Grandmaster";
            if (score >= 600) return "Master";
            if (score >= 400) return "Expert";
            if (score >= 200) return "Intermediate";
            return "Novice";
        }

        private Color GetGradeColor(string grade)
        {
            return grade switch
            {
                "S" => gradeSPerfect,
                "A" => gradeAGreat,
                "B" => gradeBGood,
                "C" => gradeCOkay,
                "D" => gradeDNeedsWork,
                _ => Color.white
            };
        }

        private void UpdateLeaderboard(NoirMemoryScoreData score)
        {
            var entry = new LeaderboardEntry
            {
                playerName = "You",
                score = score.totalScore,
                grade = score.grade,
                accuracy = score.memoryAccuracy,
                time = score.completionTime,
                puzzleName = score.puzzleName,
                date = score.timestamp.ToString("yyyy-MM-dd HH:mm")
            };

            _leaderboardData.Add(entry);
            _leaderboardData.Sort();

            // Trim to max
            if (_leaderboardData.Count > maxLeaderboardEntries)
                _leaderboardData = _leaderboardData.Take(maxLeaderboardEntries).ToList();

            RefreshLeaderboardUI();
        }

        private void RefreshLeaderboardUI()
        {
            if (leaderboardContent == null || leaderboardEntryPrefab == null) return;

            // Clear existing
            foreach (Transform child in leaderboardContent)
                Destroy(child.gameObject);

            // Populate
            for (int i = 0; i < _leaderboardData.Count; i++)
            {
                var entry = _leaderboardData[i];
                var item = Instantiate(leaderboardEntryPrefab, leaderboardContent);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 4)
                {
                    texts[0].text = $"#{i + 1}";
                    texts[1].text = entry.playerName;
                    texts[2].text = entry.score.ToString();
                    texts[3].text = entry.grade;
                }

                // Highlight "You"
                if (entry.playerName == "You")
                {
                    var bg = item.GetComponent<Image>();
                    if (bg != null) bg.color = new Color(1, 0.84f, 0, 0.2f);
                }
            }
        }

        private void LoadLeaderboard()
        {
            string json = PlayerPrefs.GetString(_saveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<LeaderboardData>(json);
                    _leaderboardData = data?.entries ?? new List<LeaderboardEntry>();
                }
                catch
                {
                    _leaderboardData = new List<LeaderboardEntry>();
                }
            }
            else
            {
                _leaderboardData = new List<LeaderboardEntry>();
            }

            _leaderboardData.Sort();
            RefreshLeaderboardUI();
        }

        private void SaveLeaderboard()
        {
            var data = new LeaderboardData { entries = _leaderboardData };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(_saveKey, json);
            PlayerPrefs.Save();
        }

        private void Replay()
        {
            HideResults();
            OnResultsClosed?.Invoke();
            Debug.Log("[NoirMemory] Replaying puzzle...");
        }

        private void Close()
        {
            HideResults();
            OnResultsClosed?.Invoke();
        }

        private void NextPuzzle()
        {
            HideResults();
            OnResultsClosed?.Invoke();
            Debug.Log("[NoirMemory] Loading next puzzle...");
        }

        #endregion
    }
}