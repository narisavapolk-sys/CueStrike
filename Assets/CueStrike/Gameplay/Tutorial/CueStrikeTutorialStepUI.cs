using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CueStrike.Gameplay.Tutorial
{
    /// <summary>
    /// UI component for displaying tutorial step instructions and controls.
    /// </summary>
    public class CueStrikeTutorialStepUI : MonoBehaviour
    {
        // Singleton
        public static CueStrikeTutorialStepUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _descriptionText;
    [SerializeField] private Text _instructionText;
    [SerializeField] private Text _progressText;
    [SerializeField] private Text _stepCounterText;
    [SerializeField] private Image _progressFillImage;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _tryAgainButton;
    [SerializeField] private GameObject _vrControlsHint;
    [SerializeField] private GameObject _desktopControlsHint;
    [SerializeField] private GameObject _controllerDiagram;
    [SerializeField] private Image _targetBallImage;
    [SerializeField] private Text _ballsRemainingText;
    [SerializeField] private GameObject _cueAlignmentGuide;
    [SerializeField] private Slider _powerSlider;
    [SerializeField] private Text _powerValueText;
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _successPanel;
    [SerializeField] private GameObject _failurePanel;
    [SerializeField] private Text _successMessageText;
    [SerializeField] private Text _failureMessageText;
    [SerializeField] private Image _progressBar;

    [Header("Animation Settings")]
    [SerializeField] private float _fadeDuration = 0.3f;

    // State
    private CueStrikeTutorialSteps.TutorialStep _currentStep;
    private bool _isVRMode = false;
    private Coroutine _typewriterCoroutine;
    private bool _isVisible = false;
    private int _currentStepIndex = 0;
    private int _totalSteps = 0;
    private Coroutine _animationCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup button listeners
        if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
        if (_previousButton != null) _previousButton.onClick.AddListener(OnPreviousClicked);
        if (_skipButton != null) _skipButton.onClick.AddListener(OnSkipClicked);
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        if (_retryButton != null) _retryButton.onClick.AddListener(OnRetryClicked);
        if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueClicked);
        if (_tryAgainButton != null) _tryAgainButton.onClick.AddListener(OnTryAgainClicked);

        // Initially hidden
        if (_panel != null) _panel.SetActive(false);
        if (_successPanel != null) _successPanel.SetActive(false);
        if (_failurePanel != null) _failurePanel.SetActive(false);
    }

    private void Start()
    {
        // Subscribe to tutorial manager events
        if (CueStrikeTutorialManager.Instance != null)
        {
            CueStrikeTutorialManager.Instance.OnStepStarted += OnStepStarted;
            CueStrikeTutorialManager.Instance.OnStepCompleted += OnStepCompleted;
            CueStrikeTutorialManager.Instance.OnStepFailed += OnStepFailed;
            CueStrikeTutorialManager.Instance.OnProgressChanged += OnProgressChanged;
            CueStrikeTutorialManager.Instance.OnTutorialCompleted += OnTutorialCompleted;
            CueStrikeTutorialManager.Instance.OnTutorialCancelled += OnTutorialCancelled;
        }
    }

    private void OnDestroy()
    {
        if (CueStrikeTutorialManager.Instance != null)
        {
            CueStrikeTutorialManager.Instance.OnStepStarted -= OnStepStarted;
            CueStrikeTutorialManager.Instance.OnStepCompleted -= OnStepCompleted;
            CueStrikeTutorialManager.Instance.OnStepFailed -= OnStepFailed;
            CueStrikeTutorialManager.Instance.OnProgressChanged -= OnProgressChanged;
            CueStrikeTutorialManager.Instance.OnTutorialCompleted -= OnTutorialCompleted;
            CueStrikeTutorialManager.Instance.OnTutorialCancelled -= OnTutorialCancelled;
        }
    }

        /// <summary>
        /// Called when a tutorial step starts.
        /// </summary>
        private void OnStepStarted(CueStrikeTutorialSteps.TutorialStep step)
        {
            _currentStep = step;
            ShowStep(step);
        }

        /// <summary>
        /// Called when a tutorial step completes successfully.
        /// </summary>
        private void OnStepCompleted(CueStrikeTutorialSteps.TutorialStep step)
        {
            if (step.autoAdvance && step.stepType == CueStrikeTutorialSteps.StepType.Instruction)
            {
                // Auto-advancing instruction steps don't need success panel
                return;
            }

            ShowSuccessMessage(step.successMessage);
        }

        /// <summary>
        /// Called when a tutorial step fails.
        /// </summary>
        private void OnStepFailed(CueStrikeTutorialSteps.TutorialStep step)
        {
            ShowFailureMessage(step.failureMessage);
        }

        /// <summary>
        /// Called when tutorial progress changes.
        /// </summary>
        private void OnProgressChanged(int currentStep, int totalSteps)
        {
            UpdateStepCounter(currentStep, totalSteps);
            UpdateProgressBar(currentStep, totalSteps);
        }

        /// <summary>
        /// Called when tutorial completes.
        /// </summary>
        private void OnTutorialCompleted()
        {
            ShowCompletionMessage();
        }

        /// <summary>
        /// Called when tutorial is cancelled.
        /// </summary>
        private void OnTutorialCancelled()
        {
            Hide();
        }

        /// <summary>
        /// Shows the UI for a tutorial step.
        /// </summary>
        private void ShowStep(CueStrikeTutorialSteps.TutorialStep step)
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateShow());

            // Update text content
            if (_titleText != null) _titleText.text = step.title;
            if (_descriptionText != null) _descriptionText.text = step.description;
            if (_instructionText != null) _instructionText.text = step.detailedInstruction;

            // Update step counter (will be updated by OnProgressChanged)
            // UpdateProgressBar handled by OnProgressChanged

            // Show/hide buttons based on step type
            UpdateButtonVisibility(step);

            // Hide success/failure panels
            if (_successPanel != null) _successPanel.SetActive(false);
            if (_failurePanel != null) _failurePanel.SetActive(false);
        }

        /// <summary>
        /// Updates button visibility based on step type.
        /// </summary>
        private void UpdateButtonVisibility(CueStrikeTutorialSteps.TutorialStep step)
        {
            bool isInstruction = step.stepType == CueStrikeTutorialSteps.StepType.Instruction;
            bool isValidation = step.stepType == CueStrikeTutorialSteps.StepType.Validation;
            bool isPractice = step.stepType == CueStrikeTutorialSteps.StepType.Practice;
            bool isLastStep = CueStrikeTutorialManager.Instance != null && 
                CueStrikeTutorialManager.Instance.GetProgress() >= 0.9f;

            // Next button: show for instruction steps that don't auto-advance
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(isInstruction && !step.autoAdvance);
                _nextButton.interactable = true;
            }

            // Skip button: show for non-instruction steps
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(!isInstruction);
            }

            // Retry button: show for validation steps after failure
            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(isValidation);
            }
        }

        /// <summary>
        /// Shows success message panel.
        /// </summary>
        private void ShowSuccessMessage(string message)
        {
            if (_successPanel != null)
            {
                _successPanel.SetActive(true);
                if (_successMessageText != null) _successMessageText.text = message;
            }
        }

        /// <summary>
        /// Shows failure message panel.
        /// </summary>
        private void ShowFailureMessage(string message)
        {
            if (_failurePanel != null)
            {
                _failurePanel.SetActive(true);
                if (_failureMessageText != null) _failureMessageText.text = message;
            }
        }

        /// <summary>
        /// Shows tutorial completion message.
        /// </summary>
        private void ShowCompletionMessage()
        {
            if (_panel != null) _panel.SetActive(true);
            if (_titleText != null) _titleText.text = "Tutorial Complete!";
            if (_descriptionText != null) _descriptionText.text = "Congratulations! You've completed the tutorial.";
            if (_instructionText != null) _instructionText.text = "You're now ready to play matches against other players or AI.";

            // Hide all buttons except continue
            if (_nextButton != null) _nextButton.gameObject.SetActive(false);
            if (_skipButton != null) _skipButton.gameObject.SetActive(false);
            if (_retryButton != null) _retryButton.gameObject.SetActive(false);
            if (_continueButton != null) _continueButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Updates step counter text.
        /// </summary>
        private void UpdateStepCounter(int current, int total)
        {
            if (_stepCounterText != null)
            {
                _stepCounterText.text = $"Step {current} of {total}";
            }
        }

        /// <summary>
        /// Updates progress bar.
        /// </summary>
        private void UpdateProgressBar(int current, int total)
        {
            if (_progressBar != null && total > 0)
            {
                _progressBar.fillAmount = (float)current / total;
            }
        }

        /// <summary>
        /// Animates panel show.
        /// </summary>
        private System.Collections.IEnumerator AnimateShow()
        {
            if (_panel == null) yield break;

            _panel.SetActive(true);
            _isVisible = true;

            // Fade in
            var canvasGroup = _panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = _panel.AddComponent<CanvasGroup>();

            float timer = 0f;
            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Animates panel hide.
        /// </summary>
        private System.Collections.IEnumerator AnimateHide()
        {
            if (_panel == null) yield break;

            var canvasGroup = _panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = _panel.AddComponent<CanvasGroup>();

            float timer = 0f;
            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            _panel.SetActive(false);
            _isVisible = false;
        }

        /// <summary>
        /// Hides the UI.
        /// </summary>
        public void Hide()
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateHide());

            if (_successPanel != null) _successPanel.SetActive(false);
            if (_failurePanel != null) _failurePanel.SetActive(false);
        }

        // Button event handlers
        private void OnNextClicked()
        {
            if (CueStrikeTutorialManager.Instance != null)
            {
                CueStrikeTutorialManager.Instance.AdvanceStep();
            }
        }

        private void OnSkipClicked()
        {
            if (CueStrikeTutorialManager.Instance != null)
            {
                CueStrikeTutorialManager.Instance.SkipStep();
            }
        }

        private void OnRetryClicked()
        {
            if (CueStrikeTutorialManager.Instance != null)
            {
                // Retry current step - just hide failure panel
                if (_failurePanel != null) _failurePanel.SetActive(false);
                // Step validation will re-trigger on next shot
            }
        }

        private void OnContinueClicked()
        {
            if (CueStrikeTutorialManager.Instance != null)
            {
                CueStrikeTutorialManager.Instance.AdvanceStep();
            }
        }

        private void OnTryAgainClicked()
        {
            if (CueStrikeTutorialManager.Instance != null)
            {
                CueStrikeTutorialManager.Instance.RestartTutorial();
            }
        }

        private void OnPreviousClicked()
        {
            if (CueStrikeTutorialManager.Instance != null)
            {
                CueStrikeTutorialManager.Instance.PreviousStep();
            }
        }

        private void OnCloseClicked()
        {
            if (CueStrikeTutorialManager.Instance != null)
            {
                CueStrikeTutorialManager.Instance.CancelTutorial();
            }
        }

        /// <summary>
        /// Updates UI for accessibility (high contrast, large text).
        /// </summary>
        public void UpdateAccessibilitySettings(bool highContrast, bool largeText)
        {
            if (_titleText != null)
            {
                _titleText.fontSize = largeText ? 36 : 28;
                _titleText.color = highContrast ? Color.white : Color.black;
            }
            if (_descriptionText != null)
            {
                _descriptionText.fontSize = largeText ? 24 : 18;
                _descriptionText.color = highContrast ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            }
            if (_instructionText != null)
            {
                _instructionText.fontSize = largeText ? 22 : 16;
                _instructionText.color = highContrast ? new Color(1f, 1f, 0.5f) : new Color(0.1f, 0.1f, 0.1f);
            }
        }

        public bool IsVisible => _isVisible;
        public CueStrikeTutorialSteps.TutorialStep CurrentStep => _currentStep;
    }
}