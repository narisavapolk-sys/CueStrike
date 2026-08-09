using System;
using System.IO;
using UnityEngine;
using CueStrike.Data;
using CueStrike.Gameplay;
using CueStrike.Tournament;

namespace CueStrike.Managers
{
    public class CueStrikeStatsManager : MonoBehaviour
    {
        public static CueStrikeStatsManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CueStrikeRulesManager rulesManager;

        [Header("Tier Definitions")]
        [SerializeField] private CueStrikeBadgeInfo[] badges = new[]
        {
            new CueStrikeBadgeInfo { tierName = "Rookie", minWinsRequired = 0, description = "Just starting out", colorHex = "#CD7F32" },
            new CueStrikeBadgeInfo { tierName = "Amateur", minWinsRequired = 5, description = "Getting the hang of it", colorHex = "#C0C0C0" },
            new CueStrikeBadgeInfo { tierName = "Semi-Pro", minWinsRequired = 15, description = "Solid player", colorHex = "#FFD700" },
            new CueStrikeBadgeInfo { tierName = "Pro", minWinsRequired = 30, description = "Consistent winner", colorHex = "#00BFFF" },
            new CueStrikeBadgeInfo { tierName = "Master", minWinsRequired = 50, description = "Elite tier", colorHex = "#9932CC" },
            new CueStrikeBadgeInfo { tierName = "Legend", minWinsRequired = 100, description = "Hall of fame", colorHex = "#FF4500" }
        };

        private CueStrikeStatsData stats;
        private string SavePath => Path.Combine(Application.persistentDataPath, "CueStrike_Stats.json");

        // Events
        public event Action<CueStrikeStatsData> OnStatsUpdated;
        public event Action<string> OnTierChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadStats();
        }

        private void OnEnable()
        {
            if (rulesManager != null)
            {
                rulesManager.OnFrameWon += OnFrameWon;
            }
            else
            {
                // Try to find if not assigned
                rulesManager = FindFirstObjectByType<CueStrikeRulesManager>();
                if (rulesManager != null)
                {
                    rulesManager.OnFrameWon += OnFrameWon;
                }
            }
        }

        private void OnDisable()
        {
            if (rulesManager != null)
            {
                rulesManager.OnFrameWon -= OnFrameWon;
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void OnFrameWon(int winnerIndex)
        {
            if (rulesManager == null) return;

            int framesToWin = (GetCurrentBestOf() / 2) + 1;
            bool matchEnded = false;
            bool playerWon = false;

            // Player is assumed to be index 0 (local player)
            if (rulesManager.framesWon[0] >= framesToWin)
            {
                matchEnded = true;
                playerWon = (winnerIndex == 0);
            }
            else if (rulesManager.framesWon[1] >= framesToWin)
            {
                matchEnded = true;
                playerWon = (winnerIndex == 0);
            }

            if (matchEnded)
            {
                RecordMatchResult(playerWon);
            }
        }

        // ==================== PUBLIC METHODS ====================

        public void RecordMatchResult(bool won)
        {
            stats.totalMatchesPlayed++;
            if (won)
            {
                stats.totalMatchesWon++;
                stats.currentWinStreak++;
                if (stats.currentWinStreak > stats.highestWinStreak)
                    stats.highestWinStreak = stats.currentWinStreak;
            }
            else
            {
                stats.totalMatchesLost++;
                stats.currentWinStreak = 0;
            }

            // Update frames from rules manager
            if (rulesManager != null)
            {
                stats.totalFramesWon += rulesManager.framesWon[0];
                stats.totalFramesLost += rulesManager.framesWon[1];
            }

            UpdateTier();
            stats.lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            SaveStats();
            OnStatsUpdated?.Invoke(stats);
        }

        public void RecordBreak(int breakScore)
        {
            if (breakScore > stats.highestBreak)
                stats.highestBreak = breakScore;

            if (breakScore >= 100)
                stats.centuryBreakCount++;

            SaveStats();
            OnStatsUpdated?.Invoke(stats);
        }

        public float GetFrameWinRate()
        {
            int total = stats.totalFramesWon + stats.totalFramesLost;
            if (total == 0) return 0f;
            return (float)stats.totalFramesWon / total * 100f;
        }

        public CueStrikeStatsData GetStats() => stats;
        public int GetCurrentWinStreak() => stats.currentWinStreak;
        public int GetHighestWinStreak() => stats.highestWinStreak;
        public int GetCenturyCount() => stats.centuryBreakCount;
        public int GetHighestBreak() => stats.highestBreak;

        public string GetCurrentTierName() => stats.currentTier;
        public string GetCurrentTierColor()
        {
            var badge = Array.Find(badges, b => b.tierName == stats.currentTier);
            return badge?.colorHex ?? "#FFFFFF";
        }

        public CueStrikeBadgeInfo GetNextTier()
        {
            int currentIdx = Array.FindIndex(badges, b => b.tierName == stats.currentTier);
            if (currentIdx >= 0 && currentIdx < badges.Length - 1)
                return badges[currentIdx + 1];
            return null;
        }

        public void ResetStats()
        {
            stats = new CueStrikeStatsData { currentTier = badges[0].tierName };
            SaveStats();
            OnStatsUpdated?.Invoke(stats);
        }

        // ==================== TIER LOGIC ====================

        private void UpdateTier()
        {
            string oldTier = stats.currentTier;
            for (int i = badges.Length - 1; i >= 0; i--)
            {
                if (stats.totalMatchesWon >= badges[i].minWinsRequired)
                {
                    stats.currentTier = badges[i].tierName;
                    stats.currentTierLevel = i;
                    break;
                }
            }

            if (oldTier != stats.currentTier)
            {
                OnTierChanged?.Invoke(stats.currentTier);
                Debug.Log($"[StatsManager] Tier Up! {oldTier} -> {stats.currentTier}");
            }
        }

        // ==================== SAVE / LOAD ====================

        private void SaveStats()
        {
            try
            {
                string json = JsonUtility.ToJson(stats, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StatsManager] Save failed: {e.Message}");
            }
        }

        private void LoadStats()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    stats = JsonUtility.FromJson<CueStrikeStatsData>(json);
                    if (stats == null) stats = new CueStrikeStatsData();
                }
                catch
                {
                    stats = new CueStrikeStatsData();
                }
            }
            else
            {
                stats = new CueStrikeStatsData();
                stats.currentTier = badges[0].tierName;
            }
        }

        // ==================== HELPERS ====================

        private int GetCurrentBestOf()
        {
            var tm = FindFirstObjectByType<CueStrikeTournamentManager>();
            if (tm != null)
            {
                var match = tm.CurrentMatch;
                if (match != null)
                    return match.framesToWin;
            }
            return 3; // default
        }
    }
}
