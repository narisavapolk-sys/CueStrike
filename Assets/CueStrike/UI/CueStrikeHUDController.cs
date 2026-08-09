using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CueStrike.Gameplay;
using CueStrike.Gameplay.Practice;

public class CueStrikeHUDController : MonoBehaviour
{
    public Text modeText;
    public Button toggleEnvButton;
    
    [Header("Glove Selection Option")]
    public Button toggleGloveButton;
    public Text gloveStatusText;

    [Header("Aim Assist Option")]
    public Button toggleAimAssistButton;
    public Text aimAssistStatusText;

    [Header("Sim Mode Option")]
    public Button toggleSimModeButton;
    public Text simModeStatusText;

    [Header("Voice & Ghost Options")]
    public Button toggleMicMuteButton;
    public Text micMuteStatusText;
    public Button toggleOpponentMuteButton;
    public Text opponentMuteStatusText;
    public Button toggleGhostModeButton;
    public Text ghostModeStatusText;

    [Header("Personal Stats Options")]
    public Button toggleMyStatsButton;
    public GameObject myStatsPanel;
    public Text myStatsText;

    [Header("Ball Status Panel (8-Ball / 9-Ball / Snooker)")]
    public Button toggleBallStatusButton;
    public GameObject ballStatusPanel;
    public Text ballStatusText;

    [Header("Practice & Training Option")]
    public Dropdown routineDropdown;
    public Dropdown tableTypeDropdown;

    [Header("Skins Selection (AAA)")]
    public Dropdown feltDropdown;
    public Dropdown ballDropdown;
    
    public List<Material> feltMaterials = new List<Material>();
    public List<Material> ballMaterials = new List<Material>();

    // Subtitle display
    private GameObject _subtitlePanel;
    private Text _subtitleText;
    private Coroutine _subtitleCoroutine;

    void Start()
    {
        if (toggleEnvButton != null)
            toggleEnvButton.onClick.AddListener(ToggleEnv);
            
        if (toggleGloveButton != null)
            toggleGloveButton.onClick.AddListener(ToggleGlove);

        if (toggleAimAssistButton != null)
            toggleAimAssistButton.onClick.AddListener(ToggleAimAssist);

        if (toggleSimModeButton != null)
            toggleSimModeButton.onClick.AddListener(ToggleSimMode);

        if (toggleMicMuteButton != null)
            toggleMicMuteButton.onClick.AddListener(ToggleMicMute);

        if (toggleOpponentMuteButton != null)
            toggleOpponentMuteButton.onClick.AddListener(ToggleOpponentMute);

        if (toggleGhostModeButton != null)
            toggleGhostModeButton.onClick.AddListener(ToggleGhostMode);

        if (toggleMyStatsButton != null)
            toggleMyStatsButton.onClick.AddListener(ToggleMyStats);

        if (myStatsPanel != null)
            myStatsPanel.SetActive(false); // Closed by default

        if (toggleBallStatusButton != null)
            toggleBallStatusButton.onClick.AddListener(ToggleBallStatus);

        if (ballStatusPanel != null)
            ballStatusPanel.SetActive(false);

        if (feltDropdown != null)
        {
            feltDropdown.onValueChanged.AddListener(OnFeltChanged);
            feltDropdown.value = PlayerPrefs.GetInt("CueStrike_FeltSkin", 0);
        }

        if (ballDropdown != null)
        {
            ballDropdown.onValueChanged.AddListener(OnBallChanged);
            ballDropdown.value = PlayerPrefs.GetInt("CueStrike_BallSkin", 0);
        }

        if (routineDropdown != null)
        {
            routineDropdown.onValueChanged.AddListener(OnRoutineChanged);
            routineDropdown.value = PlayerPrefs.GetInt("CueStrike_PracticeRoutine", 0);
        }

        if (tableTypeDropdown != null)
        {
            tableTypeDropdown.onValueChanged.AddListener(OnTableTypeChanged);
            tableTypeDropdown.value = PlayerPrefs.GetInt("CueStrike_TableStyle", 0);
        }

        UpdateModeText();
        UpdateGloveStatusText();
        UpdateAimAssistStatusText();
        UpdateSimModeStatusText();
        UpdateMicMuteStatusText();
        UpdateOpponentMuteStatusText();
        UpdateGhostModeStatusText();

        // Seed ball status panel from tracker if already running
        RefreshBallStatusPanel(CueStrikePottedBallTracker.Instance?.GetStatusString() ?? "");

        // Apply saved skins on start
        ApplyFeltSkin(PlayerPrefs.GetInt("CueStrike_FeltSkin", 0));
        ApplyBallSkin(PlayerPrefs.GetInt("CueStrike_BallSkin", 0));
    }

    void OnEnable()
    {
        CueStrikePottedBallTracker.OnBallStatusChanged += RefreshBallStatusPanel;
    }

    void OnDisable()
    {
        CueStrikePottedBallTracker.OnBallStatusChanged -= RefreshBallStatusPanel;
    }

    void ToggleEnv()
    {
        var mgr = CueStrikeEnvironmentManager.Instance;
        if (mgr == null) return;
        if (mgr.mode == CueStrikeEnvMode.VR) mgr.SetMode(CueStrikeEnvMode.MR);
        else mgr.SetMode(CueStrikeEnvMode.VR);
        UpdateModeText();
    }

    void UpdateModeText()
    {
        var mgr = CueStrikeEnvironmentManager.Instance;
        if (mgr == null) return;
        if (modeText != null) modeText.text = "Mode: " + mgr.mode.ToString();
    }

    void ToggleGlove()
    {
        int currentGlove = PlayerPrefs.GetInt("CueStrike_UseGlove", 0);
        int nextGlove = currentGlove == 0 ? 1 : 0;
        PlayerPrefs.SetInt("CueStrike_UseGlove", nextGlove);
        PlayerPrefs.Save();
        
        UpdateGloveStatusText();
        Debug.Log("CueStrike: Toggled glove use to: " + (nextGlove == 1 ? "ON" : "OFF"));
    }

    void UpdateGloveStatusText()
    {
        if (gloveStatusText != null)
        {
            int useGlove = PlayerPrefs.GetInt("CueStrike_UseGlove", 0);
            gloveStatusText.text = "Glove: " + (useGlove == 1 ? "ON" : "OFF");
        }
    }

    void ToggleAimAssist()
    {
        int currentAssist = PlayerPrefs.GetInt("CueStrike_EnableAimAssist", 0);
        int nextAssist = currentAssist == 0 ? 1 : 0;
        PlayerPrefs.SetInt("CueStrike_EnableAimAssist", nextAssist);
        PlayerPrefs.Save();

        var assistScript = FindFirstObjectByType<CueStrike.VR.CueStrikeAimAssist>();
        if (assistScript != null)
        {
            assistScript.ToggleAimAssist(nextAssist == 1);
        }

        UpdateAimAssistStatusText();
        Debug.Log("CueStrike: Toggled aim assist to: " + (nextAssist == 1 ? "ON" : "OFF"));
    }

    void UpdateAimAssistStatusText()
    {
        if (aimAssistStatusText != null)
        {
            int useAssist = PlayerPrefs.GetInt("CueStrike_EnableAimAssist", 0);
            aimAssistStatusText.text = "Aim Assist: " + (useAssist == 1 ? "ON" : "OFF");
        }
    }

    void ToggleSimMode()
    {
        int currentSim = PlayerPrefs.GetInt("CueStrike_SimMode", 1);
        int nextSim = currentSim == 0 ? 1 : 0;
        PlayerPrefs.SetInt("CueStrike_SimMode", nextSim);
        PlayerPrefs.Save();

        var passThrough = FindFirstObjectByType<CueStrike.Physics.CueStrikeTablePassThrough>();
        if (passThrough != null)
        {
            passThrough.ApplyColliderMode();
        }

        UpdateSimModeStatusText();
        Debug.Log("CueStrike: Toggled Sim Mode to: " + (nextSim == 1 ? "ON" : "OFF"));
    }

    void UpdateSimModeStatusText()
    {
        if (simModeStatusText != null)
        {
            int useSim = PlayerPrefs.GetInt("CueStrike_SimMode", 1);
            simModeStatusText.text = "Sim Mode: " + (useSim == 1 ? "ON" : "OFF");
        }
    }

    void ToggleMicMute()
    {
        int currentMute = PlayerPrefs.GetInt("CueStrike_MuteMic", 0);
        int nextMute = currentMute == 0 ? 1 : 0;
        PlayerPrefs.SetInt("CueStrike_MuteMic", nextMute);
        PlayerPrefs.Save();

        var voice = FindFirstObjectByType<CueStrike.Multiplayer.CueStrikeVoiceManager>();
        if (voice != null)
        {
            voice.SetMicMute(nextMute == 1);
        }

        UpdateMicMuteStatusText();
    }

    void UpdateMicMuteStatusText()
    {
        if (micMuteStatusText != null)
        {
            int mute = PlayerPrefs.GetInt("CueStrike_MuteMic", 0);
            micMuteStatusText.text = "Mic: " + (mute == 1 ? "MUTED" : "ON");
        }
    }

    void ToggleOpponentMute()
    {
        int currentMute = PlayerPrefs.GetInt("CueStrike_MuteOpponent", 0);
        int nextMute = currentMute == 0 ? 1 : 0;
        PlayerPrefs.SetInt("CueStrike_MuteOpponent", nextMute);
        PlayerPrefs.Save();

        var voice = FindFirstObjectByType<CueStrike.Multiplayer.CueStrikeVoiceManager>();
        if (voice != null)
        {
            voice.SetOpponentMute(nextMute == 1);
        }

        UpdateOpponentMuteStatusText();
    }

    void UpdateOpponentMuteStatusText()
    {
        if (opponentMuteStatusText != null)
        {
            int mute = PlayerPrefs.GetInt("CueStrike_MuteOpponent", 0);
            opponentMuteStatusText.text = "Opponent: " + (mute == 1 ? "MUTED" : "ON");
        }
    }

    void ToggleGhostMode()
    {
        int currentGhost = PlayerPrefs.GetInt("CueStrike_InvisibleOpponent", 0);
        int nextGhost = currentGhost == 0 ? 1 : 0;
        PlayerPrefs.SetInt("CueStrike_InvisibleOpponent", nextGhost);
        PlayerPrefs.Save();

        UpdateGhostModeStatusText();
        Debug.Log("CueStrike: Toggled Ghost Mode to: " + (nextGhost == 1 ? "ON" : "OFF"));
    }

    void UpdateGhostModeStatusText()
    {
        if (ghostModeStatusText != null)
        {
            int useGhost = PlayerPrefs.GetInt("CueStrike_InvisibleOpponent", 0);
            ghostModeStatusText.text = "Ghost Mode: " + (useGhost == 1 ? "ON" : "OFF");
        }
    }

    void ToggleMyStats()
    {
        if (myStatsPanel != null)
        {
            bool isActive = !myStatsPanel.activeSelf;
            myStatsPanel.SetActive(isActive);
            if (isActive)
            {
                UpdateMyStatsDisplay();
            }
        }
    }

    void ToggleBallStatus()
    {
        if (ballStatusPanel != null)
        {
            bool isActive = !ballStatusPanel.activeSelf;
            ballStatusPanel.SetActive(isActive);
            // Content is refreshed live via event; force refresh on open
            if (isActive)
                RefreshBallStatusPanel(CueStrikePottedBallTracker.Instance?.GetStatusString() ?? "No game active.");
        }
    }

    void RefreshBallStatusPanel(string status)
    {
        if (ballStatusText != null)
            ballStatusText.text = string.IsNullOrEmpty(status) ? "No game active." : status;
    }

    void UpdateMyStatsDisplay()
    {
        if (myStatsText != null)
        {
            var stats = FindFirstObjectByType<CueStrike.Gameplay.CueStrikePlayerStats>();
            if (stats != null)
            {
                int played = stats.MatchesPlayed;
                int won = stats.MatchesWon;
                int lost = stats.MatchesLost;
                int rageQuits = stats.RageQuits;
                int maxBreak = stats.MaxBreak;

                myStatsText.text = $"Matches: {played}   |   W/L: {won}/{lost}\n" +
                                     $"Rage Quits: {rageQuits}   |   Max Break: {maxBreak}";
            }
            else
            {
                myStatsText.text = "Stats Tracker Unavailable";
            }
        }
    }

    void OnFeltChanged(int index)
    {
        PlayerPrefs.SetInt("CueStrike_FeltSkin", index);
        PlayerPrefs.Save();
        ApplyFeltSkin(index);
    }

    void OnBallChanged(int index)
    {
        PlayerPrefs.SetInt("CueStrike_BallSkin", index);
        PlayerPrefs.Save();
        ApplyBallSkin(index);
    }

    void OnRoutineChanged(int index)
    {
        PlayerPrefs.SetInt("CueStrike_PracticeRoutine", index);
        PlayerPrefs.Save();
        var pm = FindFirstObjectByType<CueStrike.Gameplay.CueStrikePracticeManager>();
        if (pm != null)
        {
            pm.ApplyRoutine((CueStrike.Gameplay.PracticeRoutine)index);
        }
    }

    void OnTableTypeChanged(int index)
    {
        PlayerPrefs.SetInt("CueStrike_TableStyle", index);
        PlayerPrefs.Save();
        var pm = FindFirstObjectByType<CueStrike.Gameplay.CueStrikePracticeManager>();
        if (pm != null)
        {
            pm.SwapTable(index); // 0 = Snooker, 1 = 8-Ball Pool, 2 = 9-Ball Pool
        }
    }

    public void ApplyFeltSkin(int index)
    {
        if (feltMaterials == null || index >= feltMaterials.Count) return;
        Material mat = feltMaterials[index];
        if (mat == null) return;

        // Find TableSurface dynamically in the scene
        var ts = GameObject.Find("TableSurface");
        if (ts != null)
        {
            var rend = ts.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
                Debug.Log("CueStrike AAA: Applied felt material: " + mat.name);
            }
        }
        else
        {
            // Fallback: search all game objects for "TableSurface" or "Felt"
            foreach (var go in GameObject.FindObjectsOfType<MeshRenderer>())
            {
                if (go.name.ToLower().Contains("tablesurface") || go.name.ToLower().Contains("felt"))
                {
                    go.sharedMaterial = mat;
                }
            }
        }
    }

    public void ApplyBallSkin(int index)
    {
        if (ballMaterials == null || index >= ballMaterials.Count) return;
        Material mat = ballMaterials[index];
        if (mat == null) return;

        // Apply to all active balls in the scene
        var balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (var ball in balls)
        {
            var rend = ball.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
            }
        }
        Debug.Log("CueStrike AAA: Applied ball material: " + mat.name);
    }

    /// <summary>
    /// Show a subtitle message on screen for a duration.
    /// </summary>
    public void ShowSubtitle(string text, float duration)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Create subtitle panel if it doesn't exist
        if (_subtitlePanel == null)
        {
            CreateSubtitlePanel();
        }

        if (_subtitleText != null)
        {
            _subtitleText.text = text;
            _subtitlePanel.SetActive(true);
        }

        // Stop any existing coroutine
        if (_subtitleCoroutine != null)
        {
            StopCoroutine(_subtitleCoroutine);
        }

        _subtitleCoroutine = StartCoroutine(HideSubtitleAfterDelay(duration));
    }

    private void CreateSubtitlePanel()
    {
        _subtitlePanel = new GameObject("SubtitlePanel");
        _subtitlePanel.transform.SetParent(transform, false);

        // Add Canvas
        var canvas = _subtitlePanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var canvasScaler = _subtitlePanel.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        _subtitlePanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Create background panel
        var bg = new GameObject("Background");
        bg.transform.SetParent(_subtitlePanel.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0, 100);
        bgRect.sizeDelta = new Vector2(800, 80);

        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // Create text
        var textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(bg.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);

        _subtitleText = textObj.AddComponent<Text>();
        _subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _subtitleText.fontSize = 28;
        _subtitleText.color = Color.white;
        _subtitleText.alignment = TextAnchor.MiddleCenter;
        _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleText.verticalOverflow = VerticalWrapMode.Overflow;

        _subtitlePanel.SetActive(false);
    }

    private System.Collections.IEnumerator HideSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_subtitlePanel != null)
        {
            _subtitlePanel.SetActive(false);
        }
        _subtitleCoroutine = null;
    }

    void OnDestroy()
    {
        if (_subtitleCoroutine != null)
        {
            StopCoroutine(_subtitleCoroutine);
        }
        if (_subtitlePanel != null)
        {
            Destroy(_subtitlePanel);
        }
    }
}