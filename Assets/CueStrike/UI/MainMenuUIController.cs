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
        Debug.Log("MainMenu: Loading offline practice hub scene asynchronously.");
        CueStrike.VR.CueStrikeLoadingScreen.LoadScene("hub");
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
