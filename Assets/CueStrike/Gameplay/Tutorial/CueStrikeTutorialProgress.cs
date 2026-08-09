using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Gameplay.Tutorial
{
    /// <summary>
    /// Tracks and persists tutorial progress for each game mode.
    /// Handles completion status, step completion, and statistics.
    /// </summary>
    public class CueStrikeTutorialProgress : MonoBehaviour
    {
        // Singleton
        public static CueStrikeTutorialProgress Instance { get; private set; }

        [Header("Progress Data")]
        [SerializeField] private TutorialProgressData _eightBallProgress = new TutorialProgressData();
        [SerializeField] private TutorialProgressData _nineBallProgress = new TutorialProgressData();

        [Header("Settings")]
        [SerializeField] private bool _autoSave = true;
        [SerializeField] private float _autoSaveInterval = 30f;

        // Events
        public event Action<CueStrikeTutorialSteps.TutorialMode, int> OnStepCompleted; // mode, stepIndex
        public event Action<CueStrikeTutorialSteps.TutorialMode> OnTutorialCompleted;
        public event Action<CueStrikeTutorialSteps.TutorialMode> OnProgressReset;

        private float _lastSaveTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        private void Update()
        {
            if (_autoSave && Time.time - _lastSaveTime > _autoSaveInterval)
            {
                SaveProgress();
                _lastSaveTime = Time.time;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _autoSave)
            {
                SaveProgress();
            }
        }

        private void OnApplicationQuit()
        {
            if (_autoSave)
            {
                SaveProgress();
            }
        }

        /// <summary>
        /// Marks a tutorial step as completed for the specified mode.
        /// </summary>
        public void CompleteStep(CueStrikeTutorialSteps.TutorialMode mode, int stepIndex)
        {
            var progress = GetProgress(mode);
            if (stepIndex >= 0 && stepIndex < progress.completedSteps.Count)
            {
                if (!progress.completedSteps[stepIndex])
                {
                    progress.completedSteps[stepIndex] = true;
                    progress.lastCompletedStep = stepIndex;
                    progress.completionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    
                    // Update total completed count
                    progress.totalCompletedSteps = 0;
                    foreach (bool completed in progress.completedSteps)
                    {
                        if (completed) progress.totalCompletedSteps++;
                    }

                    // Check if tutorial is complete
                    if (progress.totalCompletedSteps >= progress.completedSteps.Count)
                    {
                        progress.isCompleted = true;
                        progress.completionDate = DateTime.Now.ToString("yyyy-MM-dd");
                        OnTutorialCompleted?.Invoke(mode);
                    }

                    OnStepCompleted?.Invoke(mode, stepIndex);
                    
                    if (_autoSave) SaveProgress();
                }
            }
        }

        /// <summary>
        /// Marks a tutorial step as failed (for statistics).
        /// </summary>
        public void FailStep(CueStrikeTutorialSteps.TutorialMode mode, int stepIndex)
        {
            var progress = GetProgress(mode);
            if (stepIndex >= 0 && stepIndex < progress.stepFailures.Count)
            {
                progress.stepFailures[stepIndex]++;
                progress.totalFailures++;
                
                if (_autoSave) SaveProgress();
            }
        }

        /// <summary>
        /// Records time spent on a step.
        /// </summary>
        public void RecordStepTime(CueStrikeTutorialSteps.TutorialMode mode, int stepIndex, float timeSeconds)
        {
            var progress = GetProgress(mode);
            if (stepIndex >= 0 && stepIndex < progress.stepTimes.Count)
            {
                progress.stepTimes[stepIndex] += timeSeconds;
                progress.totalTimeSeconds += timeSeconds;
                
                if (_autoSave) SaveProgress();
            }
        }

        /// <summary>
        /// Gets progress data for a mode.
        /// </summary>
        public TutorialProgressData GetProgress(CueStrikeTutorialSteps.TutorialMode mode)
        {
            return mode == CueStrikeTutorialSteps.TutorialMode.EightBall ? _eightBallProgress : _nineBallProgress;
        }

        /// <summary>
        /// Checks if a specific step is completed.
        /// </summary>
        public bool IsStepCompleted(CueStrikeTutorialSteps.TutorialMode mode, int stepIndex)
        {
            var progress = GetProgress(mode);
            return stepIndex >= 0 && stepIndex < progress.completedSteps.Count && progress.completedSteps[stepIndex];
        }

        /// <summary>
        /// Checks if the entire tutorial is completed for a mode.
        /// </summary>
        public bool IsTutorialCompleted(CueStrikeTutorialSteps.TutorialMode mode)
        {
            return GetProgress(mode).isCompleted;
        }

        /// <summary>
        /// Gets the next incomplete step index.
        /// </summary>
        public int GetNextIncompleteStep(CueStrikeTutorialSteps.TutorialMode mode)
        {
            var progress = GetProgress(mode);
            for (int i = 0; i < progress.completedSteps.Count; i++)
            {
                if (!progress.completedSteps[i])
                {
                    return i;
                }
            }
            return -1; // All completed
        }

        /// <summary>
        /// Gets completion percentage (0-1).
        /// </summary>
        public float GetCompletionPercentage(CueStrikeTutorialSteps.TutorialMode mode)
        {
            var progress = GetProgress(mode);
            if (progress.completedSteps.Count == 0) return 0f;
            return (float)progress.totalCompletedSteps / progress.completedSteps.Count;
        }

        /// <summary>
        /// Gets total time spent on tutorial (seconds).
        /// </summary>
        public float GetTotalTimeSeconds(CueStrikeTutorialSteps.TutorialMode mode)
        {
            return GetProgress(mode).totalTimeSeconds;
        }

        /// <summary>
        /// Gets number of failures for a specific step.
        /// </summary>
        public int GetStepFailures(CueStrikeTutorialSteps.TutorialMode mode, int stepIndex)
        {
            var progress = GetProgress(mode);
            if (stepIndex >= 0 && stepIndex < progress.stepFailures.Count)
            {
                return progress.stepFailures[stepIndex];
            }
            return 0;
        }

        /// <summary>
        /// Resets progress for a specific mode.
        /// </summary>
        public void ResetProgress(CueStrikeTutorialSteps.TutorialMode mode)
        {
            var progress = GetProgress(mode);
            progress.Reset();
            OnProgressReset?.Invoke(mode);
            
            if (_autoSave) SaveProgress();
        }

        /// <summary>
        /// Resets all tutorial progress.
        /// </summary>
        public void ResetAllProgress()
        {
            _eightBallProgress.Reset();
            _nineBallProgress.Reset();
            OnProgressReset?.Invoke(CueStrikeTutorialSteps.TutorialMode.EightBall);
            OnProgressReset?.Invoke(CueStrikeTutorialSteps.TutorialMode.NineBall);
            
            if (_autoSave) SaveProgress();
        }

        /// <summary>
        /// Initializes progress arrays based on tutorial step count.
        /// </summary>
        public void InitializeProgress()
        {
            int eightBallSteps = CueStrikeTutorialSteps.GetStepCount(CueStrikeTutorialSteps.TutorialMode.EightBall);
            int nineBallSteps = CueStrikeTutorialSteps.GetStepCount(CueStrikeTutorialSteps.TutorialMode.NineBall);

            _eightBallProgress.Initialize(eightBallSteps);
            _nineBallProgress.Initialize(nineBallSteps);

            SaveProgress();
        }

        /// <summary>
        /// Saves progress to PlayerPrefs.
        /// </summary>
        public void SaveProgress()
        {
            try
            {
                string eightBallJson = JsonUtility.ToJson(_eightBallProgress);
                string nineBallJson = JsonUtility.ToJson(_nineBallProgress);

                PlayerPrefs.SetString("CueStrike_Tutorial_8Ball", eightBallJson);
                PlayerPrefs.SetString("CueStrike_Tutorial_9Ball", nineBallJson);
                PlayerPrefs.Save();

                _lastSaveTime = Time.time;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CueStrikeTutorialProgress] Failed to save progress: {e.Message}");
            }
        }

        /// <summary>
        /// Loads progress from PlayerPrefs.
        /// </summary>
        public void LoadProgress()
        {
            try
            {
                string eightBallJson = PlayerPrefs.GetString("CueStrike_Tutorial_8Ball", "");
                string nineBallJson = PlayerPrefs.GetString("CueStrike_Tutorial_9Ball", "");

                if (!string.IsNullOrEmpty(eightBallJson))
                {
                    _eightBallProgress = JsonUtility.FromJson<TutorialProgressData>(eightBallJson);
                }

                if (!string.IsNullOrEmpty(nineBallJson))
                {
                    _nineBallProgress = JsonUtility.FromJson<TutorialProgressData>(nineBallJson);
                }

                // Ensure arrays are properly sized
                InitializeProgress();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CueStrikeTutorialProgress] Failed to load progress: {e.Message}");
                InitializeProgress();
            }
        }

        /// <summary>
        /// Gets a summary of tutorial statistics.
        /// </summary>
        public TutorialStatistics GetStatistics(CueStrikeTutorialSteps.TutorialMode mode)
        {
            var progress = GetProgress(mode);
            return new TutorialStatistics
            {
                mode = mode,
                isCompleted = progress.isCompleted,
                completionPercentage = GetCompletionPercentage(mode),
                totalTimeSeconds = progress.totalTimeSeconds,
                totalFailures = progress.totalFailures,
                totalCompletedSteps = progress.totalCompletedSteps,
                totalSteps = progress.completedSteps.Count,
                completionDate = progress.completionDate,
                lastCompletedStep = progress.lastCompletedStep,
                stepFailures = new List<int>(progress.stepFailures),
                stepTimes = new List<float>(progress.stepTimes)
            };
        }

        /// <summary>
        /// Tutorial progress data structure (serializable).
        /// </summary>
        [Serializable]
        public class TutorialProgressData
        {
            public bool isCompleted = false;
            public string completionDate = "";
            public string completionTime = "";
            public int lastCompletedStep = -1;
            public int totalCompletedSteps = 0;
            public int totalFailures = 0;
            public float totalTimeSeconds = 0f;

            public List<bool> completedSteps = new List<bool>();
            public List<int> stepFailures = new List<int>();
            public List<float> stepTimes = new List<float>();

            /// <summary>
            /// Initializes arrays for the given step count.
            /// </summary>
            public void Initialize(int stepCount)
            {
                if (completedSteps.Count != stepCount)
                {
                    completedSteps = new List<bool>(new bool[stepCount]);
                }
                if (stepFailures.Count != stepCount)
                {
                    stepFailures = new List<int>(new int[stepCount]);
                }
                if (stepTimes.Count != stepCount)
                {
                    stepTimes = new List<float>(new float[stepCount]);
                }
            }

            /// <summary>
            /// Resets all progress data.
            /// </summary>
            public void Reset()
            {
                isCompleted = false;
                completionDate = "";
                completionTime = "";
                lastCompletedStep = -1;
                totalCompletedSteps = 0;
                totalFailures = 0;
                totalTimeSeconds = 0f;

                for (int i = 0; i < completedSteps.Count; i++)
                {
                    completedSteps[i] = false;
                }
                for (int i = 0; i < stepFailures.Count; i++)
                {
                    stepFailures[i] = 0;
                }
                for (int i = 0; i < stepTimes.Count; i++)
                {
                    stepTimes[i] = 0f;
                }
            }
        }

        /// <summary>
        /// Statistics summary for UI display.
        /// </summary>
        [Serializable]
        public class TutorialStatistics
        {
            public CueStrikeTutorialSteps.TutorialMode mode;
            public bool isCompleted;
            public float completionPercentage;
            public float totalTimeSeconds;
            public int totalFailures;
            public int totalCompletedSteps;
            public int totalSteps;
            public string completionDate;
            public int lastCompletedStep;
            public List<int> stepFailures;
            public List<float> stepTimes;

            public string GetFormattedTime()
            {
                TimeSpan time = TimeSpan.FromSeconds(totalTimeSeconds);
                return $"{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }
    }
}