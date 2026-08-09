using System;

namespace CueStrike.Data
{
    /// <summary>
    /// Standalone statistics data for career tracking.
    /// Stored separately from PlayerProfileData to prevent save incompatibility.
    /// </summary>
    [Serializable]
    public class CueStrikeStatsData
    {
        // Match stats
        public int totalMatchesPlayed = 0;
        public int totalMatchesWon = 0;
        public int totalMatchesLost = 0;

        // Frame stats
        public int totalFramesWon = 0;
        public int totalFramesLost = 0;

        // Streak
        public int currentWinStreak = 0;
        public int highestWinStreak = 0;

        // Break stats
        public int highestBreak = 0;
        public int centuryBreakCount = 0; // break 100+

        // Tier
        public string currentTier = "Rookie";
        public int currentTierLevel = 0; // 0-5

        // Misc
        public string lastUpdated = "";
    }

    /// <summary>
    /// Badge/tier information for UI display.
    /// </summary>
    [Serializable]
    public class CueStrikeBadgeInfo
    {
        public string tierName = "";
        public int minWinsRequired = 0;
        public string description = "";
        public string colorHex = "#FFFFFF"; // for UI
    }
}