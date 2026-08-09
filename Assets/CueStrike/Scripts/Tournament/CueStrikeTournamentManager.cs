using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CueStrike.Gameplay;
using CueStrike.Gameplay.SaveSystem;
using CueStrike.AI;

namespace CueStrike.Tournament
{
    public class CueStrikeTournamentManager : MonoBehaviour
    {
        public static CueStrikeTournamentManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private string saveFileName = "TournamentSaveData.json";
        [SerializeField] private int defaultFramesPerMatch = 3;

        [Header("References")]
        [SerializeField] private CueStrikeRulesManager rulesManager;
        [SerializeField] private CueStrikeTurnManager turnManager;
        [SerializeField] private CueStrikeShotManager shotManager;
        [SerializeField] private CueStrikeAIController aiController;

        // Tournament state
        private TournamentData currentTournament;
        private TournamentMatch currentMatch;
        private int currentFrame = 0;
        private string saveFilePath;

        // Events
        public event Action<TournamentData> OnTournamentStarted;
        public event Action<TournamentMatch> OnMatchStarted;
        public event Action<TournamentMatch> OnMatchCompleted;
        public event Action<TournamentData> OnTournamentCompleted;
        public event Action<TournamentLeaderboardEntry> OnLeaderboardUpdated;
        public event Action<string> OnStatusMessage;

        // Properties
        public TournamentData CurrentTournament => currentTournament;
        public TournamentMatch CurrentMatch => currentMatch;

        public TournamentData GetCurrentTournament() => currentTournament;
        public bool IsTournamentActive() => currentTournament != null && !currentTournament.isCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);

            // Auto-find references
            if (rulesManager == null) rulesManager = CueStrikeRulesManager.Instance;
            if (turnManager == null) turnManager = CueStrikeTurnManager.Instance;
            if (shotManager == null) shotManager = FindFirstObjectByType<CueStrikeShotManager>();
            if (aiController == null) aiController = FindFirstObjectByType<CueStrikeAIController>();
        }

        void Start()
        {
            // Subscribe to RulesManager events to track frames
            if (rulesManager != null)
            {
                rulesManager.OnFrameWon += OnFrameWon;
                rulesManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        void OnDestroy()
        {
            if (rulesManager != null)
            {
                rulesManager.OnFrameWon -= OnFrameWon;
                rulesManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        #region Public API - Tournament Creation

        /// <summary>
        /// Quick start a single elimination tournament
        /// </summary>
        public void QuickStartSingleElimination(int participantCount, bool fillWithAI = true)
        {
            // Validate participant count (must be power of 2 for single elimination)
            int validCount = Mathf.NextPowerOfTwo(Mathf.Max(4, participantCount));
            if (validCount > 16) validCount = 16;

            var tournament = new TournamentData
            {
                tournamentName = $"Tournament {DateTime.Now:MMM dd, HH:mm}",
                format = TournamentFormat.SingleElimination,
                totalParticipants = validCount,
                framesPerMatch = defaultFramesPerMatch
            };

            // Get player profiles from save system
            var profiles = CueStrikeSaveSystemIntegration.GetAllProfiles();
            string activeProfileId = CueStrikeSaveSystemIntegration.GetActiveProfile()?.profileId;

            // Add human player(s)
            int humanCount = Mathf.Min(profiles.Count, validCount);
            for (int i = 0; i < humanCount; i++)
            {
                var profile = profiles[i];
                tournament.participants.Add(new TournamentParticipant
                {
                    playerId = profile.profileId,
                    displayName = profile.profileName,
                    isAI = false,
                    seed = i + 1
                });
            }

            // Fill remaining slots with AI
            int aiCount = validCount - humanCount;
            if (fillWithAI && aiCount > 0)
            {
                string[] aiNames = { "AI Rookie", "AI Pro", "AI Master", "AI Champion", "AI Legend", "AI Grandmaster" };
                int[] aiSkills = { 0, 0, 1, 1, 2, 2 }; // Easy, Easy, Medium, Medium, Hard, Hard

                for (int i = 0; i < aiCount; i++)
                {
                    int skillIndex = Mathf.Min(i, aiSkills.Length - 1);
                    tournament.participants.Add(new TournamentParticipant
                    {
                        playerId = $"AI_{Guid.NewGuid().ToString().Substring(0, 8)}",
                        displayName = aiNames[Mathf.Min(i, aiNames.Length - 1)],
                        isAI = true,
                        aiSkillLevel = aiSkills[skillIndex],
                        seed = humanCount + i + 1
                    });
                }
            }

            // Shuffle for random seeding (except human player stays seed 1)
            ShuffleParticipants(tournament);

            // Generate bracket
            GenerateSingleEliminationBracket(tournament);

            StartTournament(tournament);
        }

        /// <summary>
        /// Create custom tournament with specific participants
        /// </summary>
        public void CreateTournament(TournamentData tournament)
        {
            if (tournament.participants.Count < 4)
            {
                LogError("Tournament needs at least 4 participants");
                return;
            }

            // Ensure power of 2 for single elimination
            if (tournament.format == TournamentFormat.SingleElimination)
            {
                int count = tournament.participants.Count;
                int powerOfTwo = Mathf.NextPowerOfTwo(count);
                if (count != powerOfTwo)
                {
                    LogError($"Single elimination requires power of 2 participants (got {count})");
                    return;
                }
            }

            GenerateSingleEliminationBracket(tournament);
            StartTournament(tournament);
        }

        #endregion

        #region Tournament Flow

        private void StartTournament(TournamentData tournament)
        {
            currentTournament = tournament;
            currentTournament.startedTimestamp = DateTime.UtcNow.ToString("o");
            currentTournament.isCompleted = false;
            currentTournament.currentRound = 0;

            // Assign seeds
            for (int i = 0; i < currentTournament.participants.Count; i++)
            {
                currentTournament.participants[i].seed = i + 1;
            }

            SaveTournamentProgress();
            OnTournamentStarted?.Invoke(currentTournament);
            PublishStatus($"Tournament '{currentTournament.tournamentName}' started with {currentTournament.participants.Count} participants!");

            // Start first match
            StartNextMatch();
        }

        public void StartNextMatch()
        {
            if (currentTournament == null || currentTournament.isCompleted)
            {
                LogError("No active tournament");
                return;
            }

            currentMatch = currentTournament.GetCurrentMatch();
            if (currentMatch == null)
            {
                // Check if tournament is complete
                if (currentTournament.bracket.matches.All(m => m.IsComplete))
                {
                    CompleteTournament();
                }
                else
                {
                    // Advance bracket - winners should have been placed
                    AdvanceBracket();
                    currentMatch = currentTournament.GetCurrentMatch();
                }
            }

            if (currentMatch != null)
            {
                currentMatch.state = MatchState.InProgress;
                currentMatch.scheduledTime = DateTime.UtcNow.ToString("o");
                currentFrame = 0;

                // Setup players in RulesManager
                SetupMatchPlayers(currentMatch);

                OnMatchStarted?.Invoke(currentMatch);
                SaveTournamentProgress();
                PublishStatus($"Match started: {GetParticipantName(currentMatch.player1Id)} vs {GetParticipantName(currentMatch.player2Id)}");
            }
        }

        private void SetupMatchPlayers(TournamentMatch match)
        {
            if (rulesManager == null) return;

            var p1 = currentTournament.GetParticipant(match.player1Id);
            var p2 = currentTournament.GetParticipant(match.player2Id);

            if (p1 != null && p2 != null)
            {
                rulesManager.playerNames[0] = p1.displayName;
                rulesManager.playerNames[1] = p2.displayName;
                rulesManager.currentPlayer = 0;
                rulesManager.scores[0] = 0;
                rulesManager.scores[1] = 0;
                rulesManager.framesWon[0] = 0;
                rulesManager.framesWon[1] = 0;
                rulesManager.currentBreak = 0;
            }
        }

        private void OnFrameWon(int winnerPlayerIndex)
        {
            if (currentMatch == null || currentTournament == null) return;

            currentFrame++;

            // Map RulesManager player index to tournament player IDs
            // RulesManager currentPlayer tracks whose turn it is, not who won
            // We need to check who actually won the frame
            string winnerId = winnerPlayerIndex == 0 ? currentMatch.player1Id : currentMatch.player2Id;
            string loserId = winnerPlayerIndex == 0 ? currentMatch.player2Id : currentMatch.player1Id;

            // Update frame scores
            if (winnerPlayerIndex == 0)
            {
                currentMatch.player1Score = rulesManager.framesWon[0];
                currentMatch.player2Score = rulesManager.framesWon[1];
            }
            else
            {
                currentMatch.player1Score = rulesManager.framesWon[0];
                currentMatch.player2Score = rulesManager.framesWon[1];
            }

            // Update participant stats
            var winner = currentTournament.GetParticipant(winnerId);
            var loser = currentTournament.GetParticipant(loserId);
            if (winner != null) winner.framesWon++;
            if (loser != null) loser.framesLost++;

            PublishStatus($"Frame won by {GetParticipantName(winnerId)}! Score: {currentMatch.player1Score} - {currentMatch.player2Score}");

            // Check if match is complete (best of N)
            int framesToWin = (currentMatch.framesToWin + 1) / 2; // e.g., Best of 3 = 2, Best of 5 = 3
            if (currentMatch.player1Score >= framesToWin || currentMatch.player2Score >= framesToWin)
            {
                CompleteMatch();
            }
        }

        private void CompleteMatch()
        {
            if (currentMatch == null || currentTournament == null) return;

            currentMatch.state = MatchState.Completed;
            currentMatch.completedTime = DateTime.UtcNow.ToString("o");
            currentMatch.winnerId = currentMatch.player1Score > currentMatch.player2Score ? currentMatch.player1Id : currentMatch.player2Id;
            currentMatch.framesToWin = currentTournament.framesPerMatch;

            // Update participant match stats
            var winner = currentTournament.GetParticipant(currentMatch.winnerId);
            var loser = currentTournament.GetParticipant(currentMatch.player1Id == currentMatch.winnerId ? currentMatch.player2Id : currentMatch.player1Id);

            if (winner != null) winner.matchesWon++;
            if (loser != null) loser.matchesLost++;

            // Advance bracket - place winner in next round
            currentTournament.bracket.GetNextMatchForWinner(currentMatch.winnerId, currentMatch.round);

            OnMatchCompleted?.Invoke(currentMatch);
            SaveTournamentProgress();
            PublishStatus($"Match complete! {GetParticipantName(currentMatch.winnerId)} wins {currentMatch.player1Score}-{currentMatch.player2Score}");

            // Check if tournament is complete
            if (currentTournament.bracket.matches.All(m => m.IsComplete))
            {
                CompleteTournament();
            }
            else
            {
                // Auto-start next match after delay
                Invoke(nameof(StartNextMatch), 2f);
            }
        }

        private void AdvanceBracket()
        {
            // For single elimination, winners are already placed by GetNextMatchForWinner
            // Just need to update current round
            currentTournament.currentRound++;
        }

        private void CompleteTournament()
        {
            if (currentTournament == null) return;

            currentTournament.isCompleted = true;
            currentTournament.completedTimestamp = DateTime.UtcNow.ToString("o");

            // Find champion (winner of final match)
            var finalMatch = currentTournament.bracket.matches
                .Where(m => m.round == currentTournament.bracket.GetMaxRound())
                .FirstOrDefault(m => m.IsComplete);

            if (finalMatch != null)
            {
                currentTournament.championId = finalMatch.winnerId;
                var champion = currentTournament.GetParticipant(finalMatch.winnerId);
                if (champion != null) champion.isChampion = true;
            }

            SaveTournamentProgress();
            OnTournamentCompleted?.Invoke(currentTournament);
            PublishStatus($"🏆 Tournament Complete! Champion: {GetParticipantName(currentTournament.championId)}");
        }

        #endregion

        #region Bracket Generation

        private void GenerateSingleEliminationBracket(TournamentData tournament)
        {
            tournament.bracket.matches.Clear();

            int participantCount = tournament.participants.Count;
            int totalRounds = (int)Mathf.Log(participantCount, 2);
            int matchIndex = 0;

            // First round - pair participants by seed (1 vs N, 2 vs N-1, etc.)
            for (int i = 0; i < participantCount / 2; i++)
            {
                int seed1 = i;
                int seed2 = participantCount - 1 - i;

                var match = new TournamentMatch
                {
                    round = 0,
                    matchIndex = matchIndex++,
                    player1Id = tournament.participants[seed1].playerId,
                    player2Id = tournament.participants[seed2].playerId,
                    framesToWin = tournament.framesPerMatch,
                    state = MatchState.Scheduled
                };
                tournament.bracket.matches.Add(match);
            }

            // Subsequent rounds - empty matches waiting for winners
            int currentRoundMatches = participantCount / 2;
            for (int round = 1; round < totalRounds; round++)
            {
                currentRoundMatches /= 2;
                for (int i = 0; i < currentRoundMatches; i++)
                {
                    var match = new TournamentMatch
                    {
                        round = round,
                        matchIndex = matchIndex++,
                        player1Id = "",
                        player2Id = "",
                        framesToWin = tournament.framesPerMatch,
                        state = MatchState.Scheduled
                    };
                    tournament.bracket.matches.Add(match);
                }
            }
        }

        // ==================== DOUBLE ELIMINATION BRACKET ====================
        
        private void GenerateDoubleEliminationBracket(TournamentData tournament)
        {
            tournament.bracket.matches.Clear();
            int participantCount = tournament.participants.Count;
            int totalRounds = (int)Mathf.Log(participantCount, 2);
            int matchIndex = 0;

            // ===== WINNERS BRACKET =====
            // Round 0: First round pairings (1 vs N, 2 vs N-1, etc.)
            for (int i = 0; i < participantCount / 2; i++)
            {
                int seed1 = i;
                int seed2 = participantCount - 1 - i;

                var match = new TournamentMatch
                {
                    matchId = $"W_R0_M{i}",
                    round = 0,
                    matchIndex = matchIndex++,
                    player1Id = tournament.participants[seed1].playerId,
                    player2Id = tournament.participants[seed2].playerId,
                    framesToWin = tournament.framesPerMatch,
                    state = MatchState.Scheduled
                };
                tournament.bracket.matches.Add(match);
            }

            // Subsequent winners bracket rounds - empty matches waiting for winners
            int currentRoundMatches = participantCount / 2;
            for (int round = 1; round < totalRounds; round++)
            {
                currentRoundMatches /= 2;
                for (int i = 0; i < currentRoundMatches; i++)
                {
                    var match = new TournamentMatch
                    {
                        matchId = $"W_R{round}_M{i}",
                        round = round,
                        matchIndex = matchIndex++,
                        player1Id = "",
                        player2Id = "",
                        framesToWin = tournament.framesPerMatch,
                        state = MatchState.Scheduled
                    };
                    tournament.bracket.matches.Add(match);
                }
            }

            // ===== LOSERS BRACKET =====
            // Losers bracket starts after first winners round
            // Round 0 of losers bracket = losers from W_R0
            int losersRound0Matches = participantCount / 2;
            for (int i = 0; i < losersRound0Matches; i++)
            {
                var match = new TournamentMatch
                {
                    matchId = $"L_R0_M{i}",
                    round = -1, // Negative round indicates losers bracket
                    matchIndex = matchIndex++,
                    player1Id = "", // Will be filled by losers from W_R0
                    player2Id = "",
                    framesToWin = tournament.framesPerMatch,
                    state = MatchState.Scheduled
                };
                tournament.bracket.matches.Add(match);
            }

            // Subsequent losers bracket rounds
            // Each losers round combines: losers from current winners round + winners from previous losers round
            int currentLosersRoundMatches = losersRound0Matches;
            for (int losersRound = 1; losersRound < totalRounds; losersRound++)
            {
                // Number of matches = (winners round losers) + (previous losers round winners) / 2
                int winnersRoundLosers = (participantCount / (int)Mathf.Pow(2, losersRound + 1));
                int prevLosersWinners = currentLosersRoundMatches / 2;
                int thisRoundMatches = winnersRoundLosers + prevLosersWinners;

                if (thisRoundMatches < 1) thisRoundMatches = 1;

                for (int i = 0; i < thisRoundMatches; i++)
                {
                    var match = new TournamentMatch
                    {
                        matchId = $"L_R{losersRound}_M{i}",
                        round = -(losersRound + 1), // Negative = losers bracket
                        matchIndex = matchIndex++,
                        player1Id = "",
                        player2Id = "",
                        framesToWin = tournament.framesPerMatch,
                        state = MatchState.Scheduled
                    };
                    tournament.bracket.matches.Add(match);
                }
                currentLosersRoundMatches = thisRoundMatches;
            }

            // ===== GRAND FINALS =====
            // Grand Finals: Winners bracket champion vs Losers bracket champion
            var grandFinals = new TournamentMatch
            {
                matchId = "GF_M0",
                round = totalRounds, // Final round
                matchIndex = matchIndex++,
                player1Id = "", // Winner of winners bracket
                player2Id = "", // Winner of losers bracket
                framesToWin = tournament.framesPerMatch,
                state = MatchState.Scheduled
            };
            tournament.bracket.matches.Add(grandFinals);

            // Bracket Reset (if losers bracket winner wins first GF)
            var grandFinalsReset = new TournamentMatch
            {
                matchId = "GF_M1",
                round = totalRounds + 1,
                matchIndex = matchIndex++,
                player1Id = "", // Same players
                player2Id = "",
                framesToWin = tournament.framesPerMatch, // Could be best-of-1 or best-of-3 per config
                state = MatchState.Scheduled
            };
            tournament.bracket.matches.Add(grandFinalsReset);
        }

        // ==================== ROUND ROBIN BRACKET ====================
        
        private void GenerateRoundRobinBracket(TournamentData tournament)
        {
            tournament.bracket.matches.Clear();
            int participantCount = tournament.participants.Count;
            int matchIndex = 0;

            // All-play-all: each pair plays exactly once
            for (int i = 0; i < participantCount; i++)
            {
                for (int j = i + 1; j < participantCount; j++)
                {
                    var match = new TournamentMatch
                    {
                        matchId = $"RR_M{matchIndex}",
                        round = 0, // Round robin has no rounds
                        matchIndex = matchIndex++,
                        player1Id = tournament.participants[i].playerId,
                        player2Id = tournament.participants[j].playerId,
                        framesToWin = tournament.framesPerMatch,
                        state = MatchState.Scheduled
                    };
                    tournament.bracket.matches.Add(match);
                }
            }

            // Note: Round robin uses points system, not bracket advancement
            // Scoring: Win = 2 pts, Draw = 1 pt, Loss = 0 pts
            // Final ranking: Match Points desc -> Frame Difference desc -> Head-to-head
        }

        private void ShuffleParticipants(TournamentData tournament)
        {
            // Keep human players at top seeds, shuffle AI
            var humans = tournament.participants.Where(p => !p.isAI).ToList();
            var ais = tournament.participants.Where(p => p.isAI).ToList();

            // Shuffle AI
            for (int i = 0; i < ais.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, ais.Count);
                (ais[i], ais[r]) = (ais[r], ais[i]);
            }

            tournament.participants = humans.Concat(ais).ToList();
        }

        #endregion

        #region Save/Load

        public void SaveTournamentProgress()
        {
            if (currentTournament == null) return;

            try
            {
                string json = JsonUtility.ToJson(currentTournament, true);
                File.WriteAllText(saveFilePath, json);
                Debug.Log($"[TournamentManager] Saved tournament to {saveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TournamentManager] Save failed: {e.Message}");
            }
        }

        public bool LoadTournamentProgress()
        {
            if (!File.Exists(saveFilePath)) return false;

            try
            {
                string json = File.ReadAllText(saveFilePath);
                currentTournament = JsonUtility.FromJson<TournamentData>(json);

                if (currentTournament != null && !currentTournament.isCompleted)
                {
                    PublishStatus($"Loaded tournament: {currentTournament.tournamentName}");
                    StartNextMatch(); // Resume from current state
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TournamentManager] Load failed: {e.Message}");
            }
            return false;
        }

        public bool HasSavedTournament()
        {
            return File.Exists(saveFilePath);
        }

        public void DeleteSavedTournament()
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                currentTournament = null;
                currentMatch = null;
                PublishStatus("Saved tournament deleted");
            }
        }

        #endregion

        #region Leaderboard

        public List<TournamentLeaderboardEntry> GetLeaderboard()
        {
            if (currentTournament == null) return new List<TournamentLeaderboardEntry>();

            var entries = new List<TournamentLeaderboardEntry>();

            foreach (var p in currentTournament.participants)
            {
                entries.Add(new TournamentLeaderboardEntry
                {
                    playerId = p.playerId,
                    displayName = p.displayName,
                    matchesWon = p.matchesWon,
                    matchesLost = p.matchesLost,
                    framesWon = p.framesWon,
                    framesLost = p.framesLost,
                    isChampion = p.playerId == currentTournament.championId
                });
            }

            // Sort: matches won DESC, frame difference DESC, frames won DESC
            return entries.OrderByDescending(e => e.matchesWon)
                .ThenByDescending(e => e.frameDifference)
                .ThenByDescending(e => e.framesWon)
                .ToList();
        }

        #endregion

        #region Helpers

        private string GetParticipantName(string playerId)
        {
            var p = currentTournament?.GetParticipant(playerId);
            return p?.displayName ?? "Unknown";
        }

        private void OnGameStateChanged(CueStrikeGameState state)
        {
            // Could track game state changes if needed
        }

        private void PublishStatus(string message)
        {
            Debug.Log($"[TournamentManager] {message}");
            OnStatusMessage?.Invoke(message);
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TournamentManager] {message}");
            OnStatusMessage?.Invoke($"Error: {message}");
        }

        #endregion

        #region Editor/Debug

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Create Tournament Manager")]
        private static void CreateTournamentManager()
        {
            var go = new GameObject("TournamentManager");
            go.AddComponent<CueStrikeTournamentManager>();
            UnityEditor.EditorUtility.SetDirty(go);
        }
#endif

        #endregion
    }
}