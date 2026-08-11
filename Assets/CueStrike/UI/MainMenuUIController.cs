using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using CueStrike.Audio;

public class MainMenuUIController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;

    [Header("Interactive Buttons")]
    public Button playButton;
    public Button practiceButton;
    public Button optionsButton;
    public Button closeOptionsButton;
    public Button exitButton;

    [Header("Room Selection Panel")]
    public GameObject roomSelectionPanel;
    public Button backFromRoomsButton;

    [Header("R26 — Mode Selection")]
    [Tooltip("ปุ่มเลือกโหมด — ต่อกับ CueStrikeGameModeSelector.SelectModeAndLoad")]
    public Button[] modeButtons; // 6 ปุ่ม: Snooker15/10/6, 8-Ball, 9-Ball, ChinesePool (ลำดับตาม enum)

    private void Start()
    {
        // Audio will use CueStrikeAudioManager singleton

        // 2. Wire Button Events
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
            AddHoverEffects(playButton.gameObject);
        }
        
        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(OnOptionsClicked);
            AddHoverEffects(optionsButton.gameObject);
        }

        if (closeOptionsButton != null)
        {
            closeOptionsButton.onClick.AddListener(OnCloseOptionsClicked);
            AddHoverEffects(closeOptionsButton.gameObject);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
            AddHoverEffects(exitButton.gameObject);
        }

        if (practiceButton != null)
        {
            practiceButton.onClick.AddListener(OnPracticeClicked);
            AddHoverEffects(practiceButton.gameObject);
        }

        if (backFromRoomsButton != null)
        {
            backFromRoomsButton.onClick.AddListener(OnBackFromRoomsClicked);
            AddHoverEffects(backFromRoomsButton.gameObject);
        }

        // R26 — bind mode selection buttons (ถ้ามี) ตามลำดับ enum
        if (modeButtons != null)
        {
            var modes = (CueStrike.UI.CueStrikeGameModeSelector.GameMode[])System.Enum.GetValues(typeof(CueStrike.UI.CueStrikeGameModeSelector.GameMode));
            for (int i = 0; i < modeButtons.Length && i < modes.Length; i++)
            {
                int index = i; // capture
                if (modeButtons[i] != null)
                {
                    var mode = modes[index];
                    modeButtons[i].onClick.AddListener(() => SelectModeAndLoad(mode));
                    AddHoverEffects(modeButtons[i].gameObject);
                }
            }
        }

        // Initialize panel states
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (roomSelectionPanel != null) roomSelectionPanel.SetActive(false);
    }

    private void OnPlayClicked()
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();

        // Show room selection instead of loading scene directly
        if (mainPanel != null) mainPanel.SetActive(false);
        if (roomSelectionPanel != null) roomSelectionPanel.SetActive(true);
        Debug.Log("MainMenu: Opening Room Selection panel.");
    }

    public void LoadRoom(string sceneName)
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();
        Debug.Log("MainMenu: Loading room scene asynchronously: " + sceneName);
        CueStrike.VR.CueStrikeLoadingScreen.LoadScene(sceneName);
    }

    public void OnBackFromRoomsClicked()
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();
        if (roomSelectionPanel != null) roomSelectionPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        Debug.Log("MainMenu: Back to main panel from Room Selection.");
    }

    /// <summary>
    /// R26 — เลือกโหมด → ตั้งค่า → โหลดฉากห้องที่ถูกต้อง.
    /// ใช้ CueStrikeGameModeSelector (Snooker 15/10/6 เป็นโหมดหลัก + 8/9-Ball + Chinese Pool).
    /// </summary>
    public void SelectModeAndLoad(CueStrike.UI.CueStrikeGameModeSelector.GameMode mode)
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();
        CueStrike.UI.CueStrikeGameModeSelector.SelectedMode = mode;

        string sceneName = CueStrike.UI.CueStrikeGameModeSelector.ModeToSceneName(mode);
        string label = CueStrike.UI.CueStrikeGameModeSelector.GetModeLabel(mode);
        Debug.Log($"MainMenu: Mode '{label}' selected → loading '{sceneName}'.");

        CueStrike.UI.CueStrikeGameModeSelector.ApplyModeToScene();
        CueStrike.VR.CueStrikeLoadingScreen.LoadScene(sceneName);
    }

    private void OnOptionsClicked()
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    private void OnCloseOptionsClicked()
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void OnExitClicked()
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();
        Debug.Log("MainMenu: Exiting Game...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void OnPracticeClicked()
    {
        CueStrikeAudioManager.Instance?.PlayMenuClick();
        Debug.Log("MainMenu: Loading offline practice scene (Snooker_Demo) asynchronously.");
        CueStrike.VR.CueStrikeLoadingScreen.LoadScene("Snooker_Demo");
    }

    // Add interactive micro-scale transitions on mouse hover
    private void AddHoverEffects(GameObject go)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        // Hover Enter (Scale up slightly)
        EventTrigger.Entry entryHover = new EventTrigger.Entry();
        entryHover.eventID = EventTriggerType.PointerEnter;
        entryHover.callback.AddListener((data) => {
            go.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            CueStrikeAudioManager.Instance?.PlayMenuHover();
        });
        trigger.triggers.Add(entryHover);

        // Hover Exit (Reset scale)
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => {
            go.transform.localScale = Vector3.one;
        });
        trigger.triggers.Add(entryExit);
    }
}
