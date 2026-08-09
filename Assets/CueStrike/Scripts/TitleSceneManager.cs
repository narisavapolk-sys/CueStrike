using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CueStrike.UI
{
    /// <summary>
    /// Manages the Title Scene (Nok's Grand Hall) — handles button navigation, scene loading, and panel UI.
    /// </summary>
    public class TitleSceneManager : MonoBehaviour
    {
        [Header("Scene Names")]
        [Tooltip("Main gameplay scene (snooker/pool)")]
        public string mainSceneName = "MainScene";

        [Tooltip("Practice mode scene")]
        public string practiceSceneName = "PracticeHub";

        [Tooltip("Multiplayer lobby scene")]
        public string multiplayerSceneName = "MultiplayerLobby";

        [Tooltip("Settings scene")]
        public string settingsSceneName = "Settings";

        [Tooltip("Credits scene")]
        public string creditsSceneName = "Credits";

        [Header("Button References")]
        public Button btnPlay;
        public Button btnPractice;
        public Button btnMultiplayer;
        public Button btnSettings;
        public Button btnCredits;

        [Header("Panel References")]
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        public GameObject creditsPanel;

        [Header("Coming Soon UI")]
        public GameObject comingSoonPanel;
        public TextMeshProUGUI comingSoonText;

        private GameObject _lastOpenedPanel;

        private void Start()
        {
            BindButtons();
            InitializePanels();
        }

        /// <summary>
        /// Binds button onClick listeners to TitleSceneManager methods.
        /// Can be called from Editor scripts to wire up buttons after scene creation.
        /// </summary>
        public void BindButtons()
        {
            if (btnPlay != null) btnPlay.onClick.AddListener(() => LoadScene(mainSceneName));
            if (btnPractice != null) btnPractice.onClick.AddListener(() => ShowComingSoon("Practice"));
            if (btnMultiplayer != null) btnMultiplayer.onClick.AddListener(() => ShowComingSoon("Multiplayer"));
            if (btnSettings != null) btnSettings.onClick.AddListener(() => ShowPanel("SettingsPanel"));
            if (btnCredits != null) btnCredits.onClick.AddListener(() => ShowPanel("CreditsPanel"));
        }

        private void InitializePanels()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (comingSoonPanel != null) comingSoonPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Loads the specified scene by name.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                Debug.Log($"[TitleSceneManager] Loading scene: {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("[TitleSceneManager] Attempted to load empty scene name");
            }
        }

        /// <summary>
        /// Shows a panel and hides the main menu panel.
        /// </summary>
        public void ShowPanel(string panelName)
        {
            GameObject panel = panelName switch
            {
                "SettingsPanel" => settingsPanel,
                "CreditsPanel" => creditsPanel,
                _ => null
            };

            if (panel != null)
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                panel.SetActive(true);
                _lastOpenedPanel = panel;
                Debug.Log($"[TitleSceneManager] Showed panel: {panelName}");
            }
            else
            {
                Debug.LogWarning($"[TitleSceneManager] Panel not found: {panelName}");
            }
        }

        /// <summary>
        /// Shows "Coming Soon" message for the specified feature.
        /// </summary>
        public void ShowComingSoon(string feature)
        {
            if (comingSoonPanel != null && comingSoonText != null)
            {
                comingSoonText.text = $"Coming Soon: {feature}";
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                comingSoonPanel.SetActive(true);
                _lastOpenedPanel = comingSoonPanel;
                
                // Auto-hide after 3 seconds
                CancelInvoke(nameof(HideComingSoon));
                Invoke(nameof(HideComingSoon), 3f);
                Debug.Log($"[TitleSceneManager] Coming Soon: {feature}");
            }
            else
            {
                Debug.LogWarning("[TitleSceneManager] ComingSoonPanel or Text not assigned");
            }
        }

        private void HideComingSoon()
        {
            if (comingSoonPanel != null) comingSoonPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            _lastOpenedPanel = null;
        }

        /// <summary>
        /// Called by Back button in any panel to return to main menu.
        /// </summary>
        public void OnBackButton()
        {
            if (_lastOpenedPanel != null) _lastOpenedPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            _lastOpenedPanel = null;
            Debug.Log("[TitleSceneManager] Back to main menu");
        }

        /// <summary>
        /// Quits the application (works in build and editor).
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[TitleSceneManager] Quit requested");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}