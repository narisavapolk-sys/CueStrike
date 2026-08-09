using UnityEngine;
using UnityEngine.UI;
using CueStrike.Audio;

public class CueStrikeHUD : MonoBehaviour
{
    public Text playerNameLeft;
    public Text playerNameRight;
    public Text scoreLeft;
    public Text scoreRight;
    public Text frameScoreLeft;
    public Text frameScoreRight;
    public Text matchScoreLeft;
    public Text matchScoreRight;
    public Text foulText;
    public Text breakCount;
    public Text currentPlayerText;
    public Text shotPowerText;
    public Text shotSpinText;
    public Text statusText;
    public Text muteStatusText;
    public Button muteButton;

    private CueStrikeRulesManager rules;
    private CueStrikeShotManager shotManager;
    private CueStrikeAudioManager audioManager;

    void Start()
    {
        rules = CueStrikeRulesManager.Instance;
        shotManager = FindObjectOfType<CueStrikeShotManager>();
        audioManager = CueStrikeAudioManager.Instance;

        if (rules != null)
        {
            rules.OnPlayerScore += OnPlayerScoreChanged;
            rules.OnTurnChanged += UpdateHUD;
            rules.OnStatusMessage += UpdateStatus;
        }

        if (shotManager != null)
        {
            shotManager.OnShotAimingUpdate += UpdateShotPreview;
            shotManager.OnShotEnd += ClearShotPreview;
            shotManager.OnShotResult += HandleShotResult;
        }

        if (audioManager != null && muteButton != null)
        {
            muteButton.onClick.AddListener(ToggleMute);
            audioManager.OnMuteChanged += UpdateMuteState;
        }

        UpdateHUD();
        UpdateMuteState(audioManager != null && audioManager.IsMuted);
    }

    void OnDestroy()
    {
        if (rules != null)
        {
            rules.OnPlayerScore -= OnPlayerScoreChanged;
            rules.OnTurnChanged -= UpdateHUD;
            rules.OnStatusMessage -= UpdateStatus;
        }

        if (shotManager != null)
        {
            shotManager.OnShotAimingUpdate -= UpdateShotPreview;
            shotManager.OnShotEnd -= ClearShotPreview;
            shotManager.OnShotResult -= HandleShotResult;
        }
    }

    public void UpdateHUD()
    {
        if (rules == null) rules = CueStrikeRulesManager.Instance;
        if (rules == null) return;

        if (playerNameLeft != null) playerNameLeft.text = rules.playerNames.Length > 0 ? rules.playerNames[0] : "Player 1";
        if (playerNameRight != null) playerNameRight.text = rules.playerNames.Length > 1 ? rules.playerNames[1] : "Player 2";

        if (scoreLeft != null) scoreLeft.text = rules.scores.Length > 0 ? rules.scores[0].ToString() : "0";
        if (scoreRight != null) scoreRight.text = rules.scores.Length > 1 ? rules.scores[1].ToString() : "0";
        if (frameScoreLeft != null) frameScoreLeft.text = rules.scores.Length > 0 ? rules.scores[0].ToString() : "0";
        if (frameScoreRight != null) frameScoreRight.text = rules.scores.Length > 1 ? rules.scores[1].ToString() : "0";
        if (matchScoreLeft != null) matchScoreLeft.text = rules.framesWon.Length > 0 ? rules.framesWon[0].ToString() : "0";
        if (matchScoreRight != null) matchScoreRight.text = rules.framesWon.Length > 1 ? rules.framesWon[1].ToString() : "0";
        if (breakCount != null) breakCount.text = rules.currentBreak.ToString();
        if (foulText != null) foulText.text = $"Foul: {rules.foulPoints}";

        if (currentPlayerText != null) currentPlayerText.text = $"Turn: {rules.playerNames[rules.currentPlayer]}";
    }

    void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    void OnPlayerScoreChanged(int playerIndex)
    {
        UpdateHUD();
    }

    void UpdateShotPreview(float force, float spin, bool ready)
    {
        if (shotPowerText != null) shotPowerText.text = $"Power: {force:F1}";
        if (shotSpinText != null) shotSpinText.text = $"Spin: {spin:F1}";
        if (statusText != null) statusText.text = ready ? "Ready to fire" : "Pull back to shoot";
    }

    void ClearShotPreview()
    {
        if (shotPowerText != null) shotPowerText.text = "";
        if (shotSpinText != null) shotSpinText.text = "";
    }

    void HandleShotResult(bool potted, bool foul)
    {
        if (statusText != null)
        {
            if (foul)
            {
                statusText.text = "Foul committed.";
            }
            else if (potted)
            {
                statusText.text = "Ball potted! Continue turn.";
            }
            else
            {
                statusText.text = "No pot. Turn ends.";
            }
        }

        UpdateHUD();
    }

    void ToggleMute()
    {
        if (audioManager == null) audioManager = CueStrikeAudioManager.Instance;
        audioManager?.ToggleMute();
    }

    void UpdateMuteState(bool muted)
    {
        if (muteStatusText != null) muteStatusText.text = muted ? "SOUND: MUTED" : "SOUND: ON";
    }
}
