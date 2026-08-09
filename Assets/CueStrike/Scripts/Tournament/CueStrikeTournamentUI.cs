using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CueStrike.Tournament
{
    /// <summary>
    /// Main Tournament UI Controller
    /// </summary>
    public class CueStrikeTournamentUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject bracketPanel;
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private GameObject matchResultPanel;

        [Header("Main Panel")]
        [SerializeField] private Button quickStartButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newTournamentButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private TMP_InputField participantCountInput;
        [SerializeField] private Toggle fillWithAIToggle;
        [SerializeField] private TextMeshProUGUI tournamentStatusText;

        [Header("Bracket Panel")]
        [SerializeField] private Transform bracketContainer;
        [SerializeField] private GameObject matchPrefab;
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Button backFromBracketButton;

        [Header("Leaderboard Panel")]
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject leaderboardEntryPrefab;
        [SerializeField] private Button backFromLeaderboardButton;

        [Header("Match Result Panel")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button nextMatchButton;
        [SerializeField] private Button returnToBracketButton;

        private CueStrikeTournamentManager manager;

        private void Start()
        {
            manager = CueStrikeTournamentManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[TournamentUI] CueStrikeTournamentManager not found in scene");
                return;
            }

            manager.OnTournamentStarted += OnTournamentStarted;
            manager.OnMatchCompleted += OnMatchCompleted;
            manager.OnTournamentCompleted += OnTournamentCompleted;
            manager.OnStatusMessage += OnStatusMessage;

            SetupButtons();
            RefreshMainPanel();
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnTournamentStarted -= OnTournamentStarted;
                manager.OnMatchCompleted -= OnMatchCompleted;
                manager.OnTournamentCompleted -= OnTournamentCompleted;
                manager.OnStatusMessage -= OnStatusMessage;
            }
        }

        private void SetupButtons()
        {
            if (quickStartButton != null)
                quickStartButton.onClick.AddListener(() => {
                    int count = 8;
                    if (participantCountInput != null && int.TryParse(participantCountInput.text, out int parsed))
                        count = parsed;
                    bool fillAI = fillWithAIToggle != null && fillWithAIToggle.isOn;
                    manager.QuickStartSingleElimination(count, fillAI);
                });

            if (continueButton != null)
                continueButton.onClick.AddListener(() => {
                    if (manager.LoadTournamentProgress())
                        ShowBracketPanel();
                });

            if (newTournamentButton != null)
                newTournamentButton.onClick.AddListener(() => {
                    manager.DeleteSavedTournament();
                    RefreshMainPanel();
                });

            if (leaderboardButton != null)
                leaderboardButton.onClick.AddListener(ShowLeaderboardPanel);

            if (startMatchButton != null)
                startMatchButton.onClick.AddListener(() => {
                    manager.StartNextMatch();
                });

            if (backFromBracketButton != null)
                backFromBracketButton.onClick.AddListener(ShowMainPanel);

            if (backFromLeaderboardButton != null)
                backFromLeaderboardButton.onClick.AddListener(ShowMainPanel);

            if (nextMatchButton != null)
                nextMatchButton.onClick.AddListener(() => {
                    matchResultPanel.SetActive(false);
                    manager.StartNextMatch();
                });

            if (returnToBracketButton != null)
                returnToBracketButton.onClick.AddListener(() => {
                    matchResultPanel.SetActive(false);
                    ShowBracketPanel();
                });
        }

        private void RefreshMainPanel()
        {
            if (tournamentStatusText != null)
            {
                var tournament = manager.GetCurrentTournament();
                if (tournament != null)
                {
                    tournamentStatusText.text = $"Current: {tournament.tournamentName}\n" +
                        $"Participants: {tournament.participants.Count} | " +
                        $"Matches: {tournament.bracket.matches.Count} | " +
                        (tournament.isCompleted ? "COMPLETED" : "IN PROGRESS");
                }
                else
                {
                    tournamentStatusText.text = "No active tournament";
                }
            }

            if (continueButton != null)
                continueButton.interactable = manager.HasSavedTournament();
        }

        private void ShowMainPanel()
        {
            SetActivePanel(mainPanel);
            RefreshMainPanel();
        }

        private void ShowBracketPanel()
        {
            SetActivePanel(bracketPanel);
            RefreshBracket();
        }

        private void ShowLeaderboardPanel()
        {
            SetActivePanel(leaderboardPanel);
            RefreshLeaderboard();
        }

        private void SetActivePanel(GameObject activePanel)
        {
            if (mainPanel != null) mainPanel.SetActive(mainPanel == activePanel);
            if (bracketPanel != null) bracketPanel.SetActive(bracketPanel == activePanel);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(leaderboardPanel == activePanel);
            if (matchResultPanel != null) matchResultPanel.SetActive(matchResultPanel == activePanel);
        }

        private void RefreshBracket()
        {
            if (bracketContainer == null) return;

            // Clear existing
            foreach (Transform child in bracketContainer)
                Destroy(child.gameObject);

            var tournament = manager.GetCurrentTournament();
            if (tournament == null) return;

            // Group matches by round
            var matchesByRound = new Dictionary<int, List<TournamentMatch>>();
            foreach (var match in tournament.bracket.matches)
            {
                if (!matchesByRound.ContainsKey(match.round))
                    matchesByRound[match.round] = new List<TournamentMatch>();
                matchesByRound[match.round].Add(match);
            }

            // Create UI for each round
            foreach (var kvp in matchesByRound)
            {
                int round = kvp.Key;
                var matches = kvp.Value;

                // Create round header
                if (matchPrefab != null)
                {
                    var headerGo = new GameObject($"Round {round + 1} Header");
                    headerGo.transform.SetParent(bracketContainer, false);
                    var text = headerGo.AddComponent<TextMeshProUGUI>();
                    text.text = $"=== ROUND {round + 1} ===";
                    text.fontSize = 18;
                    text.fontStyle = FontStyles.Bold;
                    text.color = Color.yellow;
                    text.alignment = TextAlignmentOptions.Center;

                    // Add match UIs for this round
                    foreach (var match in matches)
                    {
                        var go = Instantiate(matchPrefab, bracketContainer);
                        var ui = go.GetComponent<TournamentMatchUI>();
                        if (ui != null) ui.Setup(match, tournament.participants);
                    }
                }
            }

            if (startMatchButton != null)
                startMatchButton.interactable = manager.IsTournamentActive();
        }

        private void RefreshLeaderboard()
        {
            if (leaderboardContainer == null) return;

            foreach (Transform child in leaderboardContainer)
                Destroy(child.gameObject);

            var board = manager.GetLeaderboard();
            foreach (var entry in board)
            {
                if (leaderboardEntryPrefab != null)
                {
                    var go = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                    var ui = go.GetComponent<TournamentLeaderboardEntryUI>();
                    if (ui != null) ui.Setup(entry);
                }
            }
        }

        private void OnTournamentStarted(TournamentData data)
        {
            ShowBracketPanel();
        }

        private void OnMatchCompleted(TournamentMatch match)
        {
            var tournament = manager.GetCurrentTournament();
            var winner = tournament?.GetParticipant(match.winnerId);

            if (resultText != null)
                resultText.text = $"Match Complete!\n" +
                    $"Winner: {winner?.displayName ?? "Unknown"}\n" +
                    $"Score: {match.player1Score} - {match.player2Score}";

            ShowMatchResultPanel();
        }

        private void OnTournamentCompleted(TournamentData data)
        {
            var champion = data?.GetParticipant(data.championId);
            if (resultText != null)
                resultText.text = $"🏆 TOURNAMENT CHAMPION 🏆\n\n" +
                    $"{champion?.displayName ?? "Unknown"}\n\n" +
                    $"Congratulations!";

            ShowMatchResultPanel();
        }

        private void OnStatusMessage(string message)
        {
            if (tournamentStatusText != null)
                tournamentStatusText.text = message;
        }

        private void ShowMatchResultPanel()
        {
            if (matchResultPanel != null) matchResultPanel.SetActive(true);
        }
    }

    /// <summary>
    /// UI for individual match in bracket
    /// </summary>
    public class TournamentMatchUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI player1Text;
        [SerializeField] private TextMeshProUGUI player2Text;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private Image player1Color;
        [SerializeField] private Image player2Color;

        public void Setup(TournamentMatch match, List<TournamentParticipant> participants)
        {
            var p1 = participants.Find(p => p.playerId == match.player1Id);
            var p2 = participants.Find(p => p.playerId == match.player2Id);

            if (player1Text != null)
                player1Text.text = p1?.displayName ?? "TBD";
            if (player2Text != null)
                player2Text.text = p2?.displayName ?? "TBD";

            if (scoreText != null)
                scoreText.text = $"{match.player1Score} - {match.player2Score}";

            if (stateText != null)
            {
                stateText.text = match.state.ToString();
                switch (match.state)
                {
                    case MatchState.Scheduled:
                        stateText.color = Color.gray;
                        break;
                    case MatchState.InProgress:
                        stateText.color = Color.yellow;
                        break;
                    case MatchState.Completed:
                        stateText.color = Color.green;
                        break;
                    case MatchState.Cancelled:
                        stateText.color = Color.red;
                        break;
                }
            }

            if (player1Color != null && p1 != null)
                player1Color.color = p1.isAI ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
            if (player2Color != null && p2 != null)
                player2Color.color = p2.isAI ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
        }
    }

    /// <summary>
    /// UI for leaderboard entry
    /// </summary>
    public class TournamentLeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI matchesText;
        [SerializeField] private TextMeshProUGUI framesText;
        [SerializeField] private TextMeshProUGUI winRateText;
        [SerializeField] private Image championIcon;
        [SerializeField] private Color championColor = Color.yellow;

        public void Setup(TournamentLeaderboardEntry entry)
        {
            // Rank will be set by parent layout group index
            if (nameText != null)
                nameText.text = entry.displayName + (entry.isChampion ? " 👑" : "");

            if (matchesText != null)
                matchesText.text = $"{entry.matchesWon} - {entry.matchesLost}";

            if (framesText != null)
                framesText.text = $"{entry.framesWon} - {entry.framesLost}";

            if (winRateText != null)
                winRateText.text = $"{(entry.WinRate * 100f):F0}%";

            if (championIcon != null)
            {
                championIcon.enabled = entry.isChampion;
                championIcon.color = championColor;
            }
        }

        public void SetRank(int rank)
        {
            if (rankText != null)
                rankText.text = $"#{rank}";
        }
    }
}