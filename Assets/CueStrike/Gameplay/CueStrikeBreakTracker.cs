using UnityEngine;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// Tracks consecutive runs of potted balls (Current Break) and updates the Highest Break record.
    /// Listens to CueStrikeRulesManager events externally, ensuring zero modifications
    /// to restricted core multiplayer files.
    /// </summary>
    public class CueStrikeBreakTracker : MonoBehaviour
    {
        private CueStrikeRulesManager _rules;
        private int _lastP1Score = 0;
        private int _lastP2Score = 0;

        private void Awake()
        {
            _rules = FindFirstObjectByType<CueStrikeRulesManager>();
        }

        private void OnEnable()
        {
            if (_rules != null)
            {
                _rules.OnPlayerScore += HandlePlayerScore;
                _rules.OnTurnChanged += HandleTurnChanged;
                _rules.OnGameStateChanged += HandleGameStateChanged;
            }

            // Reset current break on start
            PlayerPrefs.SetInt("CueStrike_CurrentBreak", 0);
            PlayerPrefs.Save();
        }

        private void OnDisable()
        {
            if (_rules != null)
            {
                _rules.OnPlayerScore -= HandlePlayerScore;
                _rules.OnTurnChanged -= HandleTurnChanged;
                _rules.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandlePlayerScore(int playerIndex)
        {
            if (_rules == null) return;

            // Calculate points gained in this pot
            int pointsGained = 0;
            if (playerIndex == 0)
            {
                pointsGained = _rules.scores[0] - _lastP1Score;
                _lastP1Score = _rules.scores[0];
            }
            else if (playerIndex == 1)
            {
                pointsGained = _rules.scores[1] - _lastP2Score;
                _lastP2Score = _rules.scores[1];
            }

            // Only count positive score changes (ignore fouls/negatives if any)
            if (pointsGained > 0)
            {
                int currentBreak = PlayerPrefs.GetInt("CueStrike_CurrentBreak", 0);
                currentBreak += pointsGained;
                PlayerPrefs.SetInt("CueStrike_CurrentBreak", currentBreak);

                // Update Highest Break if exceeded
                int highestBreak = PlayerPrefs.GetInt("CueStrike_HighestBreak", 0);
                if (currentBreak > highestBreak)
                {
                    PlayerPrefs.SetInt("CueStrike_HighestBreak", currentBreak);
                }
                PlayerPrefs.Save();
                Debug.Log($"[CueStrike Break] Break updated: {currentBreak} (Highest: {Mathf.Max(currentBreak, highestBreak)})");
            }
        }

        private void HandleTurnChanged()
        {
            // Reset current break when turn shifts to other player
            PlayerPrefs.SetInt("CueStrike_CurrentBreak", 0);
            PlayerPrefs.Save();
            Debug.Log("[CueStrike Break] Turn changed. Current break reset.");

            if (_rules != null)
            {
                _lastP1Score = _rules.scores[0];
                _lastP2Score = _rules.scores[1];
            }
        }

        private void HandleGameStateChanged(CueStrikeGameState state)
        {
            // Reset current break on game over
            if (state == CueStrikeGameState.GameOver)
            {
                PlayerPrefs.SetInt("CueStrike_CurrentBreak", 0);
                PlayerPrefs.Save();
            }
        }
    }
}
