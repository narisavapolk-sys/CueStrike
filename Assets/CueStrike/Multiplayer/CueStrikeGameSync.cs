#if CUESTRIKE_NORMCORE
using UnityEngine;
using Normal.Realtime;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Synchronizes local game rules, turns, and scores with other players.
    /// Acts as an event-driven bridge between CueStrike managers and Normcore.
    /// </summary>
    public class CueStrikeGameSync : RealtimeComponent<CueStrikeGameSyncModel>
    {
        private CueStrikeRulesManager _rules;
        private CueStrikeTurnManager _turns;

        private void Awake()
        {
            _rules = FindFirstObjectByType<CueStrikeRulesManager>();
            _turns = FindFirstObjectByType<CueStrikeTurnManager>();
        }

        protected override void OnRealtimeModelReplaced(CueStrikeGameSyncModel previousModel, CueStrikeGameSyncModel newModel)
        {
            if (previousModel != null)
            {
                previousModel.currentPlayerIndexDidChange -= CurrentPlayerIndexDidChange;
                previousModel.currentGameStateDidChange -= CurrentGameStateDidChange;
                previousModel.player1ScoreDidChange -= Player1ScoreDidChange;
                previousModel.player2ScoreDidChange -= Player2ScoreDidChange;
            }

            if (newModel != null)
            {
                if (newModel.isFreshModel)
                {
                    // If we are the room creator, write initial state
                    if (_rules != null)
                    {
                        newModel.currentPlayerIndex = _rules.currentPlayer;
                        newModel.currentGameState = (int)_rules.gameState;
                        newModel.player1Score = _rules.scores[0];
                        newModel.player2Score = _rules.scores[1];
                    }
                }
                else
                {
                    // Sync initial values from network to local
                    SyncNetworkToLocal();
                }

                newModel.currentPlayerIndexDidChange += CurrentPlayerIndexDidChange;
                newModel.currentGameStateDidChange += CurrentGameStateDidChange;
                newModel.player1ScoreDidChange += Player1ScoreDidChange;
                newModel.player2ScoreDidChange += Player2ScoreDidChange;
            }
        }

        private void OnEnable()
        {
            if (_rules != null)
            {
                _rules.OnPlayerScore += HandleLocalScoreChanged;
                _rules.OnTurnChanged += HandleLocalTurnChanged;
                _rules.OnGameStateChanged += HandleLocalGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_rules != null)
            {
                _rules.OnPlayerScore -= HandleLocalScoreChanged;
                _rules.OnTurnChanged -= HandleLocalTurnChanged;
                _rules.OnGameStateChanged -= HandleLocalGameStateChanged;
            }
        }

        // --- Local Managers -> Normcore Network ---

        private void HandleLocalScoreChanged(int playerIndex)
        {
            if (model == null) return;
            
            if (playerIndex == 0) model.player1Score = _rules.scores[0];
            else if (playerIndex == 1) model.player2Score = _rules.scores[1];
        }

        private void HandleLocalTurnChanged()
        {
            if (model == null) return;
            model.currentPlayerIndex = _rules.currentPlayer;
        }

        private void HandleLocalGameStateChanged(CueStrikeGameState newState)
        {
            if (model == null) return;
            model.currentGameState = (int)newState;
        }

        // --- Normcore Network -> Local Managers ---

        private void CurrentPlayerIndexDidChange(CueStrikeGameSyncModel model, int playerIndex)
        {
            if (_rules != null)
            {
                _rules.SetCurrentPlayer(playerIndex);
                if (_turns != null) _turns.currentPlayer = playerIndex;
            }
        }

        private void CurrentGameStateDidChange(CueStrikeGameSyncModel model, int gameStateInt)
        {
            if (_rules != null)
            {
                _rules.SetGameState((CueStrikeGameState)gameStateInt);
            }
        }

        // Changed to override event handlers to comply with Normcore model sync requirements
        private void Player1ScoreDidChange(CueStrikeGameSyncModel model, int score)
        {
            if (_rules != null)
            {
                _rules.SetPlayerScore(0, score);
            }
        }

        private void Player2ScoreDidChange(CueStrikeGameSyncModel model, int score)
        {
            if (_rules != null)
            {
                _rules.SetPlayerScore(1, score);
            }
        }

        private void SyncNetworkToLocal()
        {
            if (model == null || _rules == null) return;

            _rules.SetCurrentPlayer(model.currentPlayerIndex);
            _rules.SetGameState((CueStrikeGameState)model.currentGameState);
            _rules.SetPlayerScore(0, model.player1Score);
            _rules.SetPlayerScore(1, model.player2Score);
            if (_turns != null) _turns.currentPlayer = model.currentPlayerIndex;
        }
    }
}
#else
using UnityEngine;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Fallback script to explain Turn/Score sync setup when Normcore SDK is not present.
    /// </summary>
    public class CueStrikeGameSync : MonoBehaviour
    {
        [Header("Normcore SDK Missing")]
        public string notice = "This component synchronizes turn transitions and scores once the Normcore SDK is imported.";
    }
}
#endif
