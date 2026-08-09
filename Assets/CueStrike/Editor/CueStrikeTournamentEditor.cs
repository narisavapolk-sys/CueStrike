using System.Linq;
using UnityEngine;
using UnityEditor;
using CueStrike.Tournament;

namespace CueStrike.Editor
{
    public class CueStrikeTournamentEditor : EditorWindow
    {
        private int testParticipantCount = 8;
        private bool fillWithAI = true;
        private TournamentFormat testFormat = TournamentFormat.SingleElimination;
        private int testFramesPerMatch = 3;

        [MenuItem("Tools/CueStrike/Debug/Test Tournament System")]
        public static void ShowWindow()
        {
            GetWindow<CueStrikeTournamentEditor>("Tournament Debug");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tournament System Debug", EditorStyles.largeLabel);
            EditorGUILayout.Space(10);

            var manager = FindFirstObjectByType<CueStrikeTournamentManager>();
            if (manager == null)
            {
                EditorGUILayout.HelpBox("CueStrikeTournamentManager not found in scene", MessageType.Warning);
                if (GUILayout.Button("Create Tournament Manager", GUILayout.Height(30)))
                {
                    var go = new GameObject("TournamentManager");
                    go.AddComponent<CueStrikeTournamentManager>();
                    EditorUtility.SetDirty(go);
                }
                return;
            }

            GUILayout.Label("Quick Test Settings", EditorStyles.boldLabel);
            testParticipantCount = EditorGUILayout.IntSlider("Participants", testParticipantCount, 4, 16);
            fillWithAI = EditorGUILayout.Toggle("Fill with AI", fillWithAI);
            testFormat = (TournamentFormat)EditorGUILayout.EnumPopup("Format", testFormat);
            testFramesPerMatch = EditorGUILayout.IntSlider("Frames per Match (Best of N)", testFramesPerMatch, 1, 9);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Test Tournament (Single Elimination)", GUILayout.Height(35)))
            {
                manager.QuickStartSingleElimination(testParticipantCount, fillWithAI);
                Debug.Log("[TournamentEditor] Test tournament created");
            }

            EditorGUILayout.Space(10);

            // Show current tournament status
            var tournament = manager.GetCurrentTournament();
            if (tournament != null)
            {
                GUILayout.Label("Current Tournament Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Name", tournament.tournamentName);
                EditorGUILayout.LabelField("Format", tournament.format.ToString());
                EditorGUILayout.LabelField("Participants", tournament.participants.Count.ToString());
                EditorGUILayout.LabelField("Total Matches", tournament.bracket.matches.Count.ToString());
                EditorGUILayout.LabelField("Completed", tournament.isCompleted ? "Yes" : "No");

                if (!tournament.isCompleted)
                {
                    var currentMatch = tournament.GetCurrentMatch();
                    if (currentMatch != null)
                    {
                        EditorGUILayout.LabelField("Current Match", $"{currentMatch.player1Id} vs {currentMatch.player2Id}");
                        EditorGUILayout.LabelField("Score", $"{currentMatch.player1Score} - {currentMatch.player2Score}");
                    }
                }

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Simulate Complete Tournament (Auto-win)", GUILayout.Height(30)))
                {
                    SimulateTournament(manager);
                }

                if (GUILayout.Button("Advance One Match (Next Frame Winner)", GUILayout.Height(30)))
                {
                    SimulateNextMatch(manager);
                }

                if (GUILayout.Button("Save Tournament Progress", GUILayout.Height(25)))
                {
                    manager.SaveTournamentProgress();
                    Debug.Log("[TournamentEditor] Tournament saved");
                }

                if (GUILayout.Button("Clear Tournament", GUILayout.Height(25)))
                {
                    manager.DeleteSavedTournament();
                    Debug.Log("[TournamentEditor] Tournament cleared");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No active tournament", MessageType.Info);

                if (manager.HasSavedTournament())
                {
                    if (GUILayout.Button("Load Saved Tournament", GUILayout.Height(25)))
                    {
                        if (manager.LoadTournamentProgress())
                        {
                            Debug.Log("[TournamentEditor] Loaded saved tournament");
                        }
                    }
                }
            }

            EditorGUILayout.Space(20);
            GUILayout.Label("Helper Functions", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Persistent Data Folder", GUILayout.Height(25)))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            }

            if (GUILayout.Button("List All Save Files", GUILayout.Height(25)))
            {
                ListSaveFiles();
            }
        }

        private void SimulateTournament(CueStrikeTournamentManager manager)
        {
            var tournament = manager.GetCurrentTournament();
            if (tournament == null) return;

            // Simulate all matches completing
            bool flip = false;
            foreach (var match in tournament.bracket.matches)
            {
                if (match.IsReadyToPlay)
                {
                    match.state = MatchState.Completed;
                    match.completedTime = System.DateTime.UtcNow.ToString("o");
                    int framesToWin = (match.framesToWin + 1) / 2;

                    if (flip)
                    {
                        match.player1Score = framesToWin;
                        match.player2Score = framesToWin - 1;
                        match.winnerId = match.player1Id;
                    }
                    else
                    {
                        match.player1Score = framesToWin - 1;
                        match.player2Score = framesToWin;
                        match.winnerId = match.player2Id;
                    }
                    flip = !flip;

                    // Update participant stats
                    var winner = tournament.GetParticipant(match.winnerId);
                    var loser = tournament.GetParticipant(match.winnerId == match.player1Id ? match.player2Id : match.player1Id);
                    if (winner != null) winner.matchesWon++;
                    if (loser != null) loser.matchesLost++;
                    if (winner != null) winner.framesWon += match.player1Score;
                    if (loser != null) loser.framesLost += match.player2Score;
                }
            }

            // Advance bracket for single elimination
            AdvanceBracketForTournament(tournament);

            manager.SaveTournamentProgress();
            Debug.Log("[TournamentEditor] Tournament simulated completely");
        }

        private void SimulateNextMatch(CueStrikeTournamentManager manager)
        {
            var tournament = manager.GetCurrentTournament();
            if (tournament == null) return;

            var currentMatch = tournament.GetCurrentMatch();
            if (currentMatch == null) return;

            currentMatch.state = MatchState.Completed;
            currentMatch.completedTime = System.DateTime.UtcNow.ToString("o");
            int framesToWin = (currentMatch.framesToWin + 1) / 2;

            // Alternate winners for variety
            bool player1Wins = UnityEngine.Random.value > 0.5f;
            currentMatch.player1Score = player1Wins ? framesToWin : framesToWin - 1;
            currentMatch.player2Score = player1Wins ? framesToWin - 1 : framesToWin;
            currentMatch.winnerId = player1Wins ? currentMatch.player1Id : currentMatch.player2Id;

            // Update participant stats
            var winner = tournament.GetParticipant(currentMatch.winnerId);
            var loser = tournament.GetParticipant(currentMatch.winnerId == currentMatch.player1Id ? currentMatch.player2Id : currentMatch.player1Id);
            if (winner != null) winner.matchesWon++;
            if (loser != null) loser.matchesLost++;

            // Advance bracket
            AdvanceBracketForTournament(tournament);

            manager.SaveTournamentProgress();
            Debug.Log($"[TournamentEditor] Simulated match: {currentMatch.winnerId} wins {currentMatch.player1Score}-{currentMatch.player2Score}");
        }

        private void AdvanceBracketForTournament(TournamentData tournament)
        {
            if (tournament.format != TournamentFormat.SingleElimination) return;

            // Find completed matches in current round
            var completedInRound = tournament.bracket.matches
                .Where(m => m.state == MatchState.Completed && m.round == tournament.currentRound)
                .ToList();

            foreach (var match in completedInRound)
            {
                var nextMatch = tournament.bracket.GetNextMatchForWinner(match.winnerId, match.round);
                if (nextMatch != null)
                {
                    Debug.Log($"[TournamentEditor] Advanced {match.winnerId} to round {nextMatch.round}, match {nextMatch.matchIndex}");
                }
            }

            // Check if all matches in current round are done
            var roundMatches = tournament.bracket.GetMatchesByRound(tournament.currentRound);
            if (roundMatches.All(m => m.state == MatchState.Completed))
            {
                tournament.currentRound++;
            }
        }

        private void ListSaveFiles()
        {
            string path = Application.persistentDataPath;
            string[] files = System.IO.Directory.GetFiles(path, "*.json");
            Debug.Log($"[TournamentEditor] Save files in {path}:");
            foreach (var f in files)
            {
                Debug.Log($"  - {System.IO.Path.GetFileName(f)}");
            }
        }
    }
}