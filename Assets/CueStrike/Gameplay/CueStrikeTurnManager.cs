using UnityEngine;

public class CueStrikeTurnManager : MonoBehaviour
{
    public static CueStrikeTurnManager Instance { get; private set; }

    public string[] players = new[] { "Player 1", "Player 2" };
    public int currentPlayer = 0;

    private CueStrikeRulesManager rules;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }

    void OnEnable()
    {
        rules = CueStrikeRulesManager.Instance;
        if (rules != null)
        {
            rules.OnShotResolved += HandleShotResolved;
            rules.OnTurnChanged += HandleTurnChanged;
        }
    }

    void OnDisable()
    {
        if (rules != null)
        {
            rules.OnShotResolved -= HandleShotResolved;
            rules.OnTurnChanged -= HandleTurnChanged;
        }
    }

    void HandleShotResolved(bool potted, bool foul)
    {
        if (rules == null) return;

        if (foul)
        {
            rules.NextPlayer();
            return;
        }

        if (!potted)
        {
            rules.NextPlayer();
            return;
        }

        Debug.Log($"TurnManager: {players[rules.currentPlayer]} keeps the turn.");
    }

    void HandleTurnChanged()
    {
        currentPlayer = rules != null ? rules.currentPlayer : currentPlayer;
        Debug.Log($"TurnManager: switched to player {currentPlayer} ({players[currentPlayer]})");
    }
}
