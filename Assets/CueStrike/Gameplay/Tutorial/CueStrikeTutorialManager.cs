using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CueStrike.Gameplay.Rules;

namespace CueStrike.Gameplay.Tutorial
{
    /// <summary>
    /// Manages the in-game tutorial system for 8-Ball and 9-Ball.
    /// Handles step progression, validation, and integration with gameplay systems.
    /// </summary>
    public class CueStrikeTutorialManager : MonoBehaviour
    {
        public enum TutorialState
        {
            Inactive,
            Running,
            Paused,
            Completed,
            Failed
        }

        // Singleton
        public static CueStrikeTutorialManager Instance { get; private set; }

        // Events
        public event Action<CueStrikeTutorialSteps.TutorialStep> OnStepStarted;
        public event Action<CueStrikeTutorialSteps.TutorialStep> OnStepCompleted;
        public event Action<CueStrikeTutorialSteps.TutorialStep> OnStepFailed;
        public event Action<int, int> OnProgressChanged; // currentStep, totalSteps
        public event Action OnTutorialCompleted;
        public event Action OnTutorialCancelled;

        // State
        private CueStrikeTutorialSteps.TutorialMode _currentMode;
        private List<CueStrikeTutorialSteps.TutorialStep> _currentSteps;
        private int _currentStepIndex = 0;
        private TutorialState _state = TutorialState.Inactive;
        private bool _waitingForShot = false;
        private float _stepStartTime = 0f;

        // References
        private CueStrikeShotManager _shotManager;
        private CueStrikeWPARulesManager _rulesManager;
        private CueStrikeShotValidator _shotValidator;
        private CueStrikePottedBallTracker _pottedBallTracker;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Find required components
            _shotManager = FindObjectOfType<CueStrikeShotManager>();
            _rulesManager = FindObjectOfType<CueStrikeWPARulesManager>();
            _shotValidator = FindObjectOfType<CueStrikeShotValidator>();
            _pottedBallTracker = FindObjectOfType<CueStrikePottedBallTracker>();

            // Subscribe to shot events
            if (_shotManager != null)
            {
                _shotManager.OnShotEnd += OnShotEnded;
                _shotManager.OnShotResult += OnShotResult;
            }

            // Subscribe to rules events
            if (_rulesManager != null)
            {
                _rulesManager.OnEightBallShotResolved += OnEightBallShotResolved;
                _rulesManager.OnNineBallShotResolved += OnNineBallShotResolved;
            }
        }

        private void OnDestroy()
        {
            if (_shotManager != null)
            {
                _shotManager.OnShotEnd -= OnShotEnded;
                _shotManager.OnShotResult -= OnShotResult;
            }

            if (_rulesManager != null)
            {
                _rulesManager.OnEightBallShotResolved -= OnEightBallShotResolved;
                _rulesManager.OnNineBallShotResolved -= OnNineBallShotResolved;
            }
        }

        /// <summary>
        /// Starts the tutorial for the specified game mode.
        /// </summary>
        public void StartTutorial(CueStrikeTutorialSteps.TutorialMode mode)
        {
            if (_state == TutorialState.Running) return;

            _currentMode = mode;
            _currentSteps = CueStrikeTutorialSteps.GetSteps(mode);
            _currentStepIndex = 0;
            _state = TutorialState.Running;

            // Set up game mode in rules manager
            if (_rulesManager != null)
            {
                _rulesManager.SetMode(mode == CueStrikeTutorialSteps.TutorialMode.EightBall 
                    ? CueStrikeWPARulesManager.GameMode.EightBall 
                    : CueStrikeWPARulesManager.GameMode.NineBall);
                _rulesManager.StartNewFrame();
            }

            // Start first step
            StartCurrentStep();
        }

        /// <summary>
        /// Starts the current tutorial step.
        /// </summary>
        private void StartCurrentStep()
        {
            if (_currentStepIndex >= _currentSteps.Count)
            {
                CompleteTutorial();
                return;
            }

            var step = _currentSteps[_currentStepIndex];
            _stepStartTime = Time.time;
            _waitingForShot = step.requireShot;

            // Fire step start callbacks
            step.onStepStart?.Invoke();
            OnStepStarted?.Invoke(step);
            OnProgressChanged?.Invoke(_currentStepIndex + 1, _currentSteps.Count);

            // Handle auto-advance for instruction steps
            if (step.stepType == CueStrikeTutorialSteps.StepType.Instruction && step.autoAdvance)
            {
                // Auto-advance after a short delay for reading
                Invoke(nameof(AdvanceStep), 2f);
            }
        }

        /// <summary>
        /// Advances to the next tutorial step.
        /// </summary>
        public void AdvanceStep()
        {
            if (_state != TutorialState.Running) return;

            var completedStep = _currentSteps[_currentStepIndex];
            completedStep.onStepComplete?.Invoke();
            OnStepCompleted?.Invoke(completedStep);

            _currentStepIndex++;
            StartCurrentStep();
        }

        /// <summary>
        /// Goes back to the previous tutorial step.
        /// </summary>
        public void PreviousStep()
        {
            if (_state != TutorialState.Running) return;
            if (_currentStepIndex <= 0) return;

            _currentStepIndex--;
            StartCurrentStep();
        }

        /// <summary>
        /// Handles step failure.
        /// </summary>
        private void FailStep(string reason = "")
        {
            if (_state != TutorialState.Running) return;

            var failedStep = _currentSteps[_currentStepIndex];
            failedStep.onStepFail?.Invoke();
            OnStepFailed?.Invoke(failedStep);

            // Show failure message and retry or continue based on step type
            if (failedStep.stepType == CueStrikeTutorialSteps.StepType.Validation)
            {
                // For validation steps, allow retry
                _waitingForShot = true;
            }
            else
            {
                // For other steps, advance anyway but mark as failed
                AdvanceStep();
            }
        }

        /// <summary>
        /// Completes the entire tutorial.
        /// </summary>
        private void CompleteTutorial()
        {
            _state = TutorialState.Completed;
            OnTutorialCompleted?.Invoke();
        }

        /// <summary>
        /// Cancels the tutorial.
        /// </summary>
        public void CancelTutorial()
        {
            _state = TutorialState.Inactive;
            OnTutorialCancelled?.Invoke();
        }

        /// <summary>
        /// Pauses the tutorial.
        /// </summary>
        public void PauseTutorial()
        {
            if (_state == TutorialState.Running)
            {
                _state = TutorialState.Paused;
            }
        }

        /// <summary>
        /// Resumes the tutorial.
        /// </summary>
        public void ResumeTutorial()
        {
            if (_state == TutorialState.Paused)
            {
                _state = TutorialState.Running;
            }
        }

        /// <summary>
        /// Gets the current tutorial step.
        /// </summary>
        public CueStrikeTutorialSteps.TutorialStep GetCurrentStep()
        {
            if (_currentStepIndex >= 0 && _currentStepIndex < _currentSteps.Count)
            {
                return _currentSteps[_currentStepIndex];
            }
            return null;
        }

        /// <summary>
        /// Gets the current tutorial state.
        /// </summary>
        public TutorialState GetState() => _state;

        /// <summary>
        /// Gets the current tutorial mode.
        /// </summary>
        public CueStrikeTutorialSteps.TutorialMode GetCurrentMode() => _currentMode;

        /// <summary>
        /// Gets progress as 0-1 float.
        /// </summary>
        public float GetProgress()
        {
            if (_currentSteps.Count == 0) return 0f;
            return (float)_currentStepIndex / _currentSteps.Count;
        }

        /// <summary>
        /// Called when a shot ends (balls settled).
        /// </summary>
        private void OnShotEnded()
        {
            if (!_waitingForShot || _state != TutorialState.Running) return;

            // Shot completed, now validate based on step requirements
            ValidateCurrentStep();
        }

        /// <summary>
        /// Called when shot result is known (potted/foul).
        /// </summary>
        private void OnShotResult(bool potted, bool foul)
        {
            // This is called immediately after shot, before balls settle
            // We'll do final validation in OnShotEnded
        }

        /// <summary>
        /// Validates the current step based on shot outcome.
        /// </summary>
        private void ValidateCurrentStep()
        {
            var step = GetCurrentStep();
            if (step == null) return;

            _waitingForShot = false;

            // Get shot validation info
            var validationInfo = _shotValidator?.GetValidationInfo();
            if (validationInfo == null) return;

            bool success = true;
            string failureReason = "";

            // Check if required ball was potted
            if (step.requiredBallId >= 0)
            {
                bool ballPotted = validationInfo.pottedBalls.Contains(step.requiredBallId);
                if (!ballPotted)
                {
                    success = false;
                    failureReason = $"Required ball {step.requiredBallId} was not pocketed.";
                }
            }

            // Check if shot was legal (if required)
            if (success && step.requireLegalShot)
            {
                // Check for fouls
                if (validationInfo.cueBallPotted)
                {
                    success = false;
                    failureReason = "Cue ball was potted (scratch).";
                }
                else if (!validationInfo.cueBallHitObjectBall)
                {
                    success = false;
                    failureReason = "Cue ball did not hit any object ball.";
                }
                else if (!validationInfo.anyBallHitCushion && validationInfo.pottedBalls.Count == 0)
                {
                    success = false;
                    failureReason = "No ball hit a cushion and no ball was pocketed.";
                }

                // Mode-specific validation
                if (success && _rulesManager != null)
                {
                    if (_currentMode == CueStrikeTutorialSteps.TutorialMode.EightBall)
                    {
                        var eightBallRuleset = _rulesManager.GetEightBallRuleset();
                        var result = _shotValidator.ValidateEightBallShot(
                            _rulesManager.GetCurrentPlayer(),
                            eightBallRuleset.IsBreakShot(),
                            eightBallRuleset.IsOpenTable(),
                            eightBallRuleset.GetPlayerGroup(_rulesManager.GetCurrentPlayer()),
                            step.requiredBallId,
                            step.requiredPocketIndex
                        );

                        if (result == CueStrikeEightBallWPARuleset.ShotResult.Foul)
                        {
                            success = false;
                            failureReason = "Shot was a foul per 8-Ball rules.";
                        }
                        else if (result == CueStrikeEightBallWPARuleset.ShotResult.Loss)
                        {
                            success = false;
                            failureReason = "Shot resulted in loss of frame.";
                        }
                    }
                    else if (_currentMode == CueStrikeTutorialSteps.TutorialMode.NineBall)
                    {
                        var nineBallRuleset = _rulesManager.GetNineBallRuleset();
                        var result = _shotValidator.ValidateNineBallShot(
                            _rulesManager.GetCurrentPlayer(),
                            nineBallRuleset.IsBreakShot(),
                            nineBallRuleset.GetPushOutState(),
                            nineBallRuleset.GetLowestBallOnTable(),
                            step.requiredBallId,
                            step.requiredPocketIndex
                        );

                        if (result == CueStrikeNineBallWPARuleset.ShotResult.Foul)
                        {
                            success = false;
                            failureReason = "Shot was a foul per 9-Ball rules.";
                        }
                        else if (result == CueStrikeNineBallWPARuleset.ShotResult.Loss)
                        {
                            success = false;
                            failureReason = "Shot resulted in loss of frame.";
                        }
                    }
                }
            }

            // Check required pocket (simplified - would need pocket tracking)
            if (success && step.requiredPocketIndex >= 0)
            {
                // In full implementation, would check which pocket the ball went in
                // For now, assume success if ball was potted
            }

            if (success)
            {
                AdvanceStep();
            }
            else
            {
                FailStep(failureReason);
            }
        }

        /// <summary>
        /// Handles 8-Ball shot resolution from rules manager.
        /// </summary>
        private void OnEightBallShotResolved(CueStrikeEightBallWPARuleset.ShotResult result, CueStrikeEightBallWPARuleset.FoulType foul)
        {
            // Additional validation if needed
        }

        /// <summary>
        /// Handles 9-Ball shot resolution from rules manager.
        /// </summary>
        private void OnNineBallShotResolved(CueStrikeNineBallWPARuleset.ShotResult result, CueStrikeNineBallWPARuleset.FoulType foul)
        {
            // Additional validation if needed
        }

        /// <summary>
        /// Forces advancement to next step (for testing/debugging).
        /// </summary>
        public void ForceAdvanceStep()
        {
            AdvanceStep();
        }

        /// <summary>
        /// Restarts the current tutorial from the beginning.
        /// </summary>
        public void RestartTutorial()
        {
            if (_state != TutorialState.Inactive)
            {
                CancelTutorial();
            }
            StartTutorial(_currentMode);
        }

        /// <summary>
        /// Skips the current step.
        /// </summary>
        public void SkipStep()
        {
            if (_state == TutorialState.Running)
            {
                var step = GetCurrentStep();
                step.onStepFail?.Invoke();
                OnStepFailed?.Invoke(step);
                AdvanceStep();
            }
        }
    }
}