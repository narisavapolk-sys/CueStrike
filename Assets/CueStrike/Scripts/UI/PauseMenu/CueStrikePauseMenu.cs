using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.Tournament;
using CueStrike.Gameplay;
using CueStrike.Gameplay.SaveSystem;
using CueStrike.VR.Input;

namespace CueStrike.UI
{
    /// <summary>
    /// Pause Menu Controller - handles pause/resume, surrender, rematch, settings, quit
    /// Also listens for VR Options button via CueStrikeVRInputManager.
    /// </summary>
    public class CueStrikePauseMenu : MonoBehaviour
    {
        public static CueStrikePauseMenu Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject surrenderConfirmPanel;
        [SerializeField] private GameObject rematchConfirmPanel;
        [SerializeField] private GameObject quitConfirmPanel;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button surrenderButton;
        [SerializeField] private UnityEngine.UI.Button rematchButton;
        [SerializeField] private UnityEngine.UI.Button settingsButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        [Header("Confirm Buttons")]
        [SerializeField] private UnityEngine.UI.Button confirmSurrenderButton;
        [SerializeField] private UnityEngine.UI.Button cancelSurrenderButton;
        [SerializeField] private UnityEngine.UI.Button confirmRematchButton;
        [SerializeField] private UnityEngine.UI.Button cancelRematchButton;
        [SerializeField] private UnityEngine.UI.Button confirmQuitButton;
        [SerializeField] private UnityEngine.UI.Button cancelQuitButton;

        [Header("References")]
        [SerializeField] private CueStrikeRulesManager rulesManager;
        [SerializeField] private CueStrikeShotManager shotManager;

        // State
        private bool isPaused = false;
        private float previousTimeScale = 1f;
        private bool isTournamentMatch = false;
        private TournamentMatch currentTournamentMatch;

        // Events
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<bool> OnSurrenderConfirmed; // true = player surrendered
        public event Action OnRematchRequested;
        public event Action OnQuitToMenu;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-find references
            if (rulesManager == null) rulesManager = FindFirstObjectByType<CueStrikeRulesManager>();
            if (shotManager == null) shotManager = FindFirstObjectByType<CueStrikeShotManager>();

            // Setup button listeners
            SetupButtons();

            // Hide all panels initially
            HideAllPanels();

            // Wire VR Input Manager events
            WireVRInputEvents();
        }

        private void Start()
        {
            // Listen for tournament match start
            var tournamentManager = FindFirstObjectByType<CueStrike.Tournament.CueStrikeTournamentManager>();
            if (tournamentManager != null)
            {
                tournamentManager.OnMatchStarted += OnTournamentMatchStarted;
                tournamentManager.OnMatchCompleted += OnTournamentMatchCompleted;
            }
        }

        private void OnDestroy()
        {
            var tournamentManager = FindFirstObjectByType<CueStrike.Tournament.CueStrikeTournamentManager>();
            if (tournamentManager != null)
            {
                tournamentManager.OnMatchStarted -= OnTournamentMatchStarted;
                tournamentManager.OnMatchCompleted -= OnTournamentMatchCompleted;
            }

            // Unsubscribe VR input events
            UnwireVRInputEvents();
        }

        /// <summary>
        /// Subscribe to VR Input Manager events for Options and Pause.
        /// </summary>
        private void WireVRInputEvents()
        {
            var vrInputManager = CueStrikeVRInputManager.Instance;
            if (vrInputManager != null)
            {
                vrInputManager.OnOptionsPressed += TogglePause;
                Debug.Log("[PauseMenu] VR Input events wired: Options → TogglePause");
            }
            else
            {
                Debug.Log("[PauseMenu] VRInputManager not found — keyboard/mouse input only.");
            }
        }

        /// <summary>
        /// Unsubscribe from VR Input Manager events.
        /// </summary>
        private void UnwireVRInputEvents()
        {
            var vrInputManager = CueStrikeVRInputManager.Instance;
            if (vrInputManager != null)
            {
                vrInputManager.OnOptionsPressed -= TogglePause;
            }
        }

        private void Update()
        {
            // Toggle pause with Escape key (or Start button on controller)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (surrenderButton != null)
                surrenderButton.onClick.AddListener(() => ShowPanel(surrenderConfirmPanel));

            if (rematchButton != null)
                rematchButton.onClick.AddListener(() => ShowPanel(rematchConfirmPanel));

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);

            if (quitButton != null)
                quitButton.onClick.AddListener(() => ShowPanel(quitConfirmPanel));

            if (confirmSurrenderButton != null)
                confirmSurrenderButton.onClick.AddListener(ConfirmSurrender);

            if (cancelSurrenderButton != null)
                cancelSurrenderButton.onClick.AddListener(() => HidePanel(surrenderConfirmPanel));

            if (confirmRematchButton != null)
                confirmRematchButton.onClick.AddListener(ConfirmRematch);

            if (cancelRematchButton != null)
                cancelRematchButton.onClick.AddListener(() => HidePanel(rematchConfirmPanel));

            if (confirmQuitButton != null)
                confirmQuitButton.onClick.AddListener(ConfirmQuit);

            if (cancelQuitButton != null)
                cancelQuitButton.onClick.AddListener(() => HidePanel(quitConfirmPanel));
        }

        private void OnTournamentMatchStarted(TournamentMatch match)
        {
            isTournamentMatch = true;
            currentTournamentMatch = match;
            UpdateRematchButtonVisibility();
        }

        private void OnTournamentMatchCompleted(TournamentMatch match)
        {
            isTournamentMatch = false;
            currentTournamentMatch = null;
        }

        private void UpdateRematchButtonVisibility()
        {
            if (rematchButton != null)
            {
                // In tournament, rematch only available after match completes
                // In local play, always available
                rematchButton.interactable = !isTournamentMatch || (currentTournamentMatch != null && currentTournamentMatch.IsComplete);
            }
        }

        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        public void PauseGame()
        {
            if (isPaused) return;

            isPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            ShowPanel(pausePanel);
            OnGamePaused?.Invoke();

            // Disable player input
            if (shotManager != null)
                shotManager.enabled = false;

            Debug.Log("[PauseMenu] Game Paused");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = previousTimeScale;

            HideAllPanels();
            OnGameResumed?.Invoke();

            // Re-enable player input
            if (shotManager != null)
                shotManager.enabled = true;

            Debug.Log("[PauseMenu] Game Resumed");
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel == null) return;
            HideAllPanels();
            panel.SetActive(true);
        }

        private void HidePanel(GameObject panel)
        {
            if (panel == null) return;
            panel.SetActive(false);
        }

        private void HideAllPanels()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (surrenderConfirmPanel != null) surrenderConfirmPanel.SetActive(false);
            if (rematchConfirmPanel != null) rematchConfirmPanel.SetActive(false);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        }

        // ==================== SURRENDER ====================

        private void ConfirmSurrender()
        {
            HidePanel(surrenderConfirmPanel);
            ResumeGame(); // Resume briefly to process surrender

            bool playerSurrendered = true;
            OnSurrenderConfirmed?.Invoke(playerSurrendered);

            // Record surrender in stats
            var statsManager = FindFirstObjectByType<CueStrike.Managers.CueStrikeStatsManager>();
            if (statsManager != null)
            {
                statsManager.RecordMatchResult(false); // surrender = loss
            }

            // Handle tournament surrender
            if (isTournamentMatch && currentTournamentMatch != null)
            {
                // Tournament manager will handle bracket advancement
                var tournamentManager = FindFirstObjectByType<CueStrike.Tournament.CueStrikeTournamentManager>();
                if (tournamentManager != null)
                {
                    // Force complete current match with opponent as winner
                    currentTournamentMatch.state = MatchState.Completed;
                    currentTournamentMatch.winnerId = currentTournamentMatch.player1Id == GetLocalPlayerId() 
                        ? currentTournamentMatch.player2Id 
                        : currentTournamentMatch.player1Id;
                    tournamentManager.SaveTournamentProgress();
                }
            }

            Debug.Log("[PauseMenu] Player surrendered");
        }

        // ==================== REMATCH ====================

        private void ConfirmRematch()
        {
            HidePanel(rematchConfirmPanel);
            ResumeGame();

            OnRematchRequested?.Invoke();

            // If tournament, need to create a new match between same players
            if (isTournamentMatch)
            {
                var tournamentManager = FindFirstObjectByType<CueStrike.Tournament.CueStrikeTournamentManager>();
                if (tournamentManager != null && currentTournamentMatch != null)
                {
                    // Create a new match with same participants (friendly/exhibition)
                    Debug.Log("[PauseMenu] Rematch requested in tournament context");
                }
            }
            else
            {
                // Local play - just reset the current frame
                ResetCurrentFrame();
            }
        }

        private void ResetCurrentFrame()
        {
            if (rulesManager != null)
            {
                rulesManager.scores[0] = 0;
                rulesManager.scores[1] = 0;
                rulesManager.framesWon[0] = 0;
                rulesManager.framesWon[1] = 0;
                rulesManager.currentBreak = 0;
            }

            // Reset balls via physics manager
            var physicsManager = FindFirstObjectByType<CueStrikePhysicsManager>();
            if (physicsManager != null)
            {
                physicsManager.ResetBalls();
            }

            Debug.Log("[PauseMenu] Frame reset for rematch");
        }

        // ==================== QUIT ====================

        private void ConfirmQuit()
        {
            HidePanel(quitConfirmPanel);
            ResumeGame();

            OnQuitToMenu?.Invoke();

            // Load main menu scene
            SceneManager.LoadScene("Title_NoksGrandHall");
            Debug.Log("[PauseMenu] Quit to menu");
        }

        // ==================== SETTINGS ====================

        private void OpenSettings()
        {
            // Could open a settings sub-panel or the main settings menu
            Debug.Log("[PauseMenu] Open Settings");
            // For now just close pause menu
            ResumeGame();
            
            // TODO: Open settings UI
            var settingsManager = FindFirstObjectByType<CueStrikeSettingsManager>();
            if (settingsManager != null)
            {
                // Trigger settings UI open event if exists
            }
        }

        // ==================== HELPERS ====================

        private string GetLocalPlayerId()
        {
            // Get from save system or tournament
            var saveManager = FindFirstObjectByType<CueStrikeSaveLoadManager>();
            if (saveManager != null && saveManager.ActiveProfile != null)
            {
                return saveManager.ActiveProfile.profileId;
            }
            return "local_player";
        }

        public bool IsPaused => isPaused;
        public bool IsTournamentMatch => isTournamentMatch;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Create Pause Menu")]
        private static void CreatePauseMenu()
        {
            var go = new GameObject("PauseMenu");
            go.AddComponent<CueStrikePauseMenu>();
            UnityEditor.EditorUtility.SetDirty(go);
        }
#endif
    }
}
