using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI
{
    /// <summary>
    /// AAA UI Manager for CueStrike VR.
    /// Singleton. Event-driven. Manages all UI panels with premium animations.
    /// </summary>
    public class CueStrikeUIManager : MonoBehaviour
    {
        #region Singleton
        private static CueStrikeUIManager _instance;
        public static CueStrikeUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CueStrikeUIManager>();
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public event Action<string> OnPanelOpened;
        public event Action<string> OnPanelClosed;
        public event Action<bool> OnPauseStateChanged;
        #endregion

        #region Serialized Fields
        [Header("UI Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _scoreboardPanel;
        [SerializeField] private GameObject _notificationPanel;

        [Header("Animation Settings")]
        [SerializeField] private float _defaultTransitionDuration = 0.35f;
        [SerializeField] private AnimationCurve _easeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        #endregion

        #region Private Fields
        private readonly Dictionary<string, GameObject> _panelRegistry = new Dictionary<string, GameObject>();
        private readonly Stack<string> _panelHistory = new Stack<string>();
        private bool _isPaused;
        private CueStrikeUIAnimations _animator;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureUIAnimations();
            RegisterPanels();
        }

        private void Start()
        {
            // Re-register in case panels were assigned after Awake (e.g. via Editor script)
            RegisterPanels();
        }

        private void Update()
        {
            if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }
        #endregion

        #region Public Setup API (for Editor Tools)
        /// <summary>
        /// Call this after assigning panels via Editor script to re-register everything.
        /// </summary>
        public void RefreshPanels()
        {
            RegisterPanels();
            EnsureUIAnimations();
            Debug.Log("[CueStrikeUIManager] Panels refreshed. Registered: " + _panelRegistry.Count);
        }

        public void SetPanel(string panelName, GameObject panel)
        {
            switch (panelName)
            {
                case "MainMenu": _mainMenuPanel = panel; break;
                case "Pause": _pausePanel = panel; break;
                case "GameOver": _gameOverPanel = panel; break;
                case "Settings": _settingsPanel = panel; break;
                case "Scoreboard": _scoreboardPanel = panel; break;
                case "Notification": _notificationPanel = panel; break;
            }
            RegisterPanels();
        }
        #endregion

        #region Panel Management
        private void EnsureUIAnimations()
        {
            _animator = GetComponent<CueStrikeUIAnimations>();
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<CueStrikeUIAnimations>();
                Debug.Log("[CueStrikeUIManager] Auto-added CueStrikeUIAnimations component.");
            }
        }

        private void RegisterPanels()
        {
            _panelRegistry.Clear();
            if (_mainMenuPanel != null) _panelRegistry["MainMenu"] = _mainMenuPanel;
            if (_pausePanel != null) _panelRegistry["Pause"] = _pausePanel;
            if (_gameOverPanel != null) _panelRegistry["GameOver"] = _gameOverPanel;
            if (_settingsPanel != null) _panelRegistry["Settings"] = _settingsPanel;
            if (_scoreboardPanel != null) _panelRegistry["Scoreboard"] = _scoreboardPanel;
            if (_notificationPanel != null) _panelRegistry["Notification"] = _notificationPanel;
        }

        public void OpenPanel(string panelName, bool animate = true)
        {
            if (!_panelRegistry.TryGetValue(panelName, out GameObject panel))
            {
                Debug.LogError($"[CueStrikeUIManager] Panel \"{panelName}\" not found in registry.");
                return;
            }
            if (panel == null)
            {
                Debug.LogError($"[CueStrikeUIManager] Panel \"{panelName}\" is null.");
                return;
            }

            if (_panelHistory.Count > 0)
            {
                string current = _panelHistory.Peek();
                if (current != panelName)
                {
                    ClosePanelInternal(current, animate);
                }
            }

            panel.SetActive(true);
            _panelHistory.Push(panelName);

            if (animate && _animator != null)
            {
                _animator.ScaleIn(panel.transform, _defaultTransitionDuration, _easeOutCurve);
            }

            OnPanelOpened?.Invoke(panelName);
            Debug.Log($"[CueStrikeUIManager] Opened panel: {panelName}");
        }

        public void ClosePanel(string panelName, bool animate = true)
        {
            if (!_panelRegistry.ContainsKey(panelName))
            {
                Debug.LogWarning($"[CueStrikeUIManager] Cannot close unknown panel: {panelName}");
                return;
            }

            ClosePanelInternal(panelName, animate);

            if (_panelHistory.Count > 0 && _panelHistory.Peek() == panelName)
            {
                _panelHistory.Pop();
                if (_panelHistory.Count > 0)
                {
                    string previous = _panelHistory.Peek();
                    OpenPanel(previous, animate);
                }
            }
        }

        private void ClosePanelInternal(string panelName, bool animate)
        {
            if (!_panelRegistry.TryGetValue(panelName, out GameObject panel) || panel == null) return;

            if (animate && _animator != null)
            {
                _animator.ScaleOut(panel.transform, _defaultTransitionDuration, _easeOutCurve, () =>
                {
                    panel.SetActive(false);
                    OnPanelClosed?.Invoke(panelName);
                });
            }
            else
            {
                panel.SetActive(false);
                OnPanelClosed?.Invoke(panelName);
            }
        }

        public void CloseAllPanels(bool animate = true)
        {
            while (_panelHistory.Count > 0)
            {
                string panelName = _panelHistory.Pop();
                ClosePanelInternal(panelName, animate);
            }
        }
        #endregion

        #region Quick Access Methods
        public void ShowMainMenu() => OpenPanel("MainMenu");
        public void ShowPause() => OpenPanel("Pause");
        public void ShowGameOver() => OpenPanel("GameOver");
        public void ShowSettings() => OpenPanel("Settings");
        public void ShowScoreboard() => OpenPanel("Scoreboard");

        public void TogglePause()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            if (_isPaused)
            {
                ShowPause();
            }
            else
            {
                ClosePanel("Pause");
            }
            OnPauseStateChanged?.Invoke(_isPaused);
        }

        public void ShowNotification(string message, float duration = 2.5f)
        {
            if (_notificationPanel == null)
            {
                Debug.LogWarning("[CueStrikeUIManager] Notification panel not assigned.");
                return;
            }

            Text notificationText = _notificationPanel.GetComponentInChildren<Text>();
            if (notificationText != null)
            {
                notificationText.text = message;
            }

            _notificationPanel.SetActive(true);
            if (_animator != null)
            {
                _animator.FadeIn(_notificationPanel.transform, 0.2f);
            }

            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), duration);
        }

        private void HideNotification()
        {
            if (_notificationPanel == null) return;
            if (_animator != null)
            {
                _animator.FadeOut(_notificationPanel.transform, 0.3f, () =>
                {
                    _notificationPanel.SetActive(false);
                });
            }
            else
            {
                _notificationPanel.SetActive(false);
            }
        }
        #endregion

        #region Self-Test
        public bool RunSelfTest()
        {
            bool pass = true;
            
            if (_panelRegistry.Count == 0)
            {
                Debug.LogError("[Self-Test] UIManager: No panels registered. Run Setup or assign panels in Inspector.");
                pass = false;
            }
            else
            {
                Debug.Log($"[Self-Test] UIManager: {_panelRegistry.Count} panels registered.");
            }
            
            if (_animator == null)
            {
                Debug.LogError("[Self-Test] UIManager: UIAnimations component missing.");
                pass = false;
            }
            else
            {
                Debug.Log("[Self-Test] UIManager: UIAnimations component found.");
            }
            
            Debug.Log($"[Self-Test] UIManager: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}