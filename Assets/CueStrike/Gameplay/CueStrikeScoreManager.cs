using UnityEngine;

public class CueStrikeScoreManager : MonoBehaviour
{
    public static CueStrikeScoreManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }

    public void RecordPotted(int ballId, int points)
    {
        Debug.Log($"ScoreManager: Ball {ballId} potted for {points} points");
    }
}
