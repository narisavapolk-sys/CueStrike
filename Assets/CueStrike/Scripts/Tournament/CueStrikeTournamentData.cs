using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Tournament
{
    /// <summary>
    /// Tournament format types
    /// </summary>
    public enum TournamentFormat
    {
        SingleElimination,
        DoubleElimination,
        RoundRobin,
        GroupStageThenKnockout
    }

    /// <summary>
    /// Match state in tournament
    /// </summary>
    public enum MatchState
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Tournament participant
    /// </summary>
    [Serializable]
    public class TournamentParticipant
    {
        public string playerId;           // Link to PlayerProfileData.profileId
        public string displayName;        // Display name
        public bool isAI;                 // Is AI opponent
        public int aiSkillLevel;          // 0=Easy, 1=Medium, 2=Hard
        public int seed;                  // Tournament seed
        public int matchesWon = 0;
        public int matchesLost = 0;
        public int framesWon = 0;
        public int framesLost = 0;
        public bool isChampion = false;   // Is tournament champion

        public TournamentParticipant() { }

        public TournamentParticipant(string id, string name, bool ai = false, int skill = 0)
        {
            playerId = id;
            displayName = name;
            isAI = ai;
            aiSkillLevel = skill;
        }
    }

    /// <summary>
    /// Single match in tournament bracket
    /// </summary>
    [Serializable]
    public class TournamentMatch
    {
        public string matchId;
        public int round;                 // 0 = first round, 1 = quarter, 2 = semi, 3 = final
        public int matchIndex;            // Index within round
        public string player1Id;
        public string player2Id;
        public int player1Score = 0;
        public int player2Score = 0;
        public string winnerId;
        public MatchState state = MatchState.Scheduled;
        public string scheduledTime;      // ISO timestamp
        public string completedTime;      // ISO timestamp
        public int framesToWin = 3;       // Best of N frames

        public TournamentMatch() 
        {
            matchId = Guid.NewGuid().ToString();
        }

        public bool IsReadyToPlay => state == MatchState.Scheduled && 
            !string.IsNullOrEmpty(player1Id) && !string.IsNullOrEmpty(player2Id);

        public bool IsComplete => state == MatchState.Completed && !string.IsNullOrEmpty(winnerId);
    }

    /// <summary>
    /// Tournament bracket structure
    /// </summary>
    [Serializable]
    public class TournamentBracket
    {
        public List<TournamentMatch> matches = new List<TournamentMatch>();
        
        public List<TournamentMatch> GetMatchesByRound(int round)
        {
            return matches.FindAll(m => m.round == round);
        }

        public int GetMaxRound()
        {
            if (matches.Count == 0) return 0;
            int max = 0;
            foreach (var m in matches) if (m.round > max) max = m.round;
            return max;
        }

        public TournamentMatch GetNextMatchForWinner(string winnerId, int currentRound)
        {
            // Find match in next round that has empty slot
            foreach (var m in matches)
            {
                if (m.round == currentRound + 1 && m.state == MatchState.Scheduled)
                {
                    if (string.IsNullOrEmpty(m.player1Id))
                    {
                        m.player1Id = winnerId;
                        return m;
                    }
                    else if (string.IsNullOrEmpty(m.player2Id))
                    {
                        m.player2Id = winnerId;
                        return m;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Main tournament data container
    /// </summary>
    [Serializable]
    public class TournamentData
    {
        public string tournamentId;
        public string tournamentName;
        public TournamentFormat format = TournamentFormat.SingleElimination;
        public int totalParticipants = 8;
        public int framesPerMatch = 3;        // Best of 3, 5, 7, etc.
        public List<TournamentParticipant> participants = new List<TournamentParticipant>();
        public TournamentBracket bracket = new TournamentBracket();
        public string createdTimestamp;
        public string startedTimestamp;
        public string completedTimestamp;
        public string championId;
        public bool isCompleted = false;
        public int currentRound = 0;

        public TournamentData()
        {
            tournamentId = Guid.NewGuid().ToString();
            createdTimestamp = DateTime.UtcNow.ToString("o");
        }

        public TournamentParticipant GetParticipant(string playerId)
        {
            return participants.Find(p => p.playerId == playerId);
        }

        public List<TournamentMatch> GetPendingMatches()
        {
            return bracket.matches.FindAll(m => m.state == MatchState.Scheduled && m.IsReadyToPlay);
        }

        public TournamentMatch GetCurrentMatch()
        {
            var pending = GetPendingMatches();
            if (pending.Count > 0) return pending[0];
            return null;
        }
    }

    /// <summary>
    /// Leaderboard entry for tournament standings
    /// </summary>
    [Serializable]
    public class TournamentLeaderboardEntry
    {
        public string playerId;
        public string displayName;
        public int matchesWon = 0;
        public int matchesLost = 0;
        public int framesWon = 0;
        public int framesLost = 0;
        public int frameDifference => framesWon - framesLost;
        public bool isChampion = false;

        public float WinRate => (matchesWon + matchesLost) > 0 ? (float)matchesWon / (matchesWon + matchesLost) : 0f;
    }
}