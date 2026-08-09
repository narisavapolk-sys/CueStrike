using UnityEngine;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// Tracks and saves personal gameplay statistics: Matches Played, Won, Lost, Rage Quits, and Max Break.
    /// Handles persistent storage using PlayerPrefs.
    /// </summary>
    public class CueStrikePlayerStats : MonoBehaviour
    {
        public static CueStrikePlayerStats Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // --- Fetch Statistics ---

        public int MatchesPlayed => PlayerPrefs.GetInt("CueStrike_Stats_Played", 0);
        public int MatchesWon => PlayerPrefs.GetInt("CueStrike_Stats_Won", 0);
        public int MatchesLost => PlayerPrefs.GetInt("CueStrike_Stats_Lost", 0);
        public int RageQuits => PlayerPrefs.GetInt("CueStrike_Stats_RageQuits", 0);
        public int MaxBreak => PlayerPrefs.GetInt("CueStrike_HighestBreak", 0);

        // --- Mutator Functions ---

        public void IncrementMatchesPlayed()
        {
            PlayerPrefs.SetInt("CueStrike_Stats_Played", MatchesPlayed + 1);
            PlayerPrefs.Save();
        }

        public void IncrementMatchesWon()
        {
            PlayerPrefs.SetInt("CueStrike_Stats_Won", MatchesWon + 1);
            PlayerPrefs.Save();
        }

        public void IncrementMatchesLost()
        {
            PlayerPrefs.SetInt("CueStrike_Stats_Lost", MatchesLost + 1);
            PlayerPrefs.Save();
        }

        public void IncrementRageQuits()
        {
            PlayerPrefs.SetInt("CueStrike_Stats_RageQuits", RageQuits + 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resets all statistics back to zero.
        /// </summary>
        public void ResetStats()
        {
            PlayerPrefs.DeleteKey("CueStrike_Stats_Played");
            PlayerPrefs.DeleteKey("CueStrike_Stats_Won");
            PlayerPrefs.DeleteKey("CueStrike_Stats_Lost");
            PlayerPrefs.DeleteKey("CueStrike_Stats_RageQuits");
            PlayerPrefs.DeleteKey("CueStrike_HighestBreak");
            PlayerPrefs.Save();
            Debug.Log("[CueStrike Stats] Personal statistics reset to default.");
        }
    }
}
