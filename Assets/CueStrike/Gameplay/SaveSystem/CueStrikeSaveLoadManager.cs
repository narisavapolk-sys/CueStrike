using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CueStrike.Gameplay.SaveSystem
{
    /// <summary>
    /// Main save/load manager for CueStrike game data.
    /// Handles player profiles, custom drills, practice progress, and global settings.
    /// </summary>
    public class CueStrikeSaveLoadManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _saveFileName = "CueStrikeSaveData.json";
        [SerializeField] private bool _autoSave = true;
        [SerializeField] private int _autoSaveIntervalSeconds = 300; // 5 minutes

        // Singleton
        public static CueStrikeSaveLoadManager Instance { get; private set; }

        // Events
        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;
        public event Action<PlayerProfileData> OnProfileChanged;
        public event Action<CustomDrillData> OnCustomDrillSaved;
        public event Action<CustomDrillData> OnCustomDrillDeleted;

        // Internal state
        private CueStrikeSaveData _currentSaveData;
        private string _saveFilePath;
        private float _lastAutoSaveTime;
        private bool _isDirty;

        // Properties
        public CueStrikeSaveData SaveData => _currentSaveData;
        public PlayerProfileData ActiveProfile => _currentSaveData?.profiles?.Find(p => p.isActive) ?? _currentSaveData?.profiles?[0];

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);
            Load();
        }

        private void Update()
        {
            if (_autoSave && _isDirty && Time.time - _lastAutoSaveTime > _autoSaveIntervalSeconds)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isDirty)
            {
                Save();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _isDirty)
            {
                Save();
            }
        }

        private void OnDestroy()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        /// <summary>
        /// Load save data from disk.
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    string json = File.ReadAllText(_saveFilePath);
                    _currentSaveData = JsonUtility.FromJson<CueStrikeSaveData>(json);
                    
                    // Ensure lists are initialized
                    if (_currentSaveData.profiles == null) _currentSaveData.profiles = new List<PlayerProfileData>();
                    if (_currentSaveData.customDrills == null) _currentSaveData.customDrills = new List<CustomDrillData>();
                    if (_currentSaveData.globalSettings == null) _currentSaveData.globalSettings = new GlobalSettingsData();

                    // Ensure at least one profile exists
                    if (_currentSaveData.profiles.Count == 0)
                    {
                        _currentSaveData.profiles.Add(new PlayerProfileData { profileName = "Player 1", isActive = true });
                    }

                    // Migrate old data if needed
                    MigrateSaveData();

                    _isDirty = false;
                    UnityEngine.Debug.Log($"[CueStrikeSaveLoadManager] Loaded save data from {_saveFilePath}");
                }
                else
                {
                    // Create new save data
                    _currentSaveData = new CueStrikeSaveData();
                    _isDirty = true;
                    Save();
                    UnityEngine.Debug.Log($"[CueStrikeSaveLoadManager] Created new save data");
                }

                OnLoadCompleted?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CueStrikeSaveLoadManager] Failed to load save data: {e.Message}");
                _currentSaveData = new CueStrikeSaveData();
                _isDirty = true;
            }
        }

        /// <summary>
        /// Save current data to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                _currentSaveData.lastSavedTimestamp = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(_currentSaveData, true);
                File.WriteAllText(_saveFilePath, json);
                _isDirty = false;
                _lastAutoSaveTime = Time.time;
                UnityEngine.Debug.Log($"[CueStrikeSaveLoadManager] Saved save data to {_saveFilePath}");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CueStrikeSaveLoadManager] Failed to save data: {e.Message}");
            }
        }

        /// <summary>
        /// Mark data as dirty (needs saving).
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Migrate save data from older versions.
        /// </summary>
        private void MigrateSaveData()
        {
            // Version migration logic here if needed
            if (string.IsNullOrEmpty(_currentSaveData.version))
            {
                _currentSaveData.version = "1.0.0";
                _isDirty = true;
            }
        }

        // ============================================================
        // Profile Management
        // ============================================================

        /// <summary>
        /// Get all profiles.
        /// </summary>
        public List<PlayerProfileData> GetProfiles()
        {
            return _currentSaveData?.profiles ?? new List<PlayerProfileData>();
        }

        /// <summary>
        /// Set active profile by index.
        /// </summary>
        public void SetActiveProfile(int index)
        {
            if (_currentSaveData?.profiles == null || index < 0 || index >= _currentSaveData.profiles.Count) return;

            foreach (var profile in _currentSaveData.profiles)
            {
                profile.isActive = false;
            }
            _currentSaveData.profiles[index].isActive = true;
            _currentSaveData.activeProfileIndex = index;
            MarkDirty();
            OnProfileChanged?.Invoke(_currentSaveData.profiles[index]);
        }

        /// <summary>
        /// Set active profile by profile ID.
        /// </summary>
        public bool SetActiveProfile(string profileId)
        {
            if (_currentSaveData?.profiles == null || string.IsNullOrEmpty(profileId)) return false;

            var index = _currentSaveData.profiles.FindIndex(p => p.profileId == profileId);
            if (index >= 0)
            {
                SetActiveProfile(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create new profile.
        /// </summary>
        public PlayerProfileData CreateProfile(string name)
        {
            var profile = new PlayerProfileData { profileName = name };
            _currentSaveData.profiles.Add(profile);
            SetActiveProfile(_currentSaveData.profiles.Count - 1);
            return profile;
        }

        /// <summary>
        /// Delete profile.
        /// </summary>
        public bool DeleteProfile(string profileId)
        {
            var profile = _currentSaveData.profiles.Find(p => p.profileId == profileId);
            if (profile != null && _currentSaveData.profiles.Count > 1)
            {
                _currentSaveData.profiles.Remove(profile);
                // If deleted was active, activate first remaining
                if (profile.isActive && _currentSaveData.profiles.Count > 0)
                {
                    _currentSaveData.profiles[0].isActive = true;
                    _currentSaveData.activeProfileIndex = 0;
                }
                MarkDirty();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update profile name.
        /// </summary>
        public void UpdateProfileName(string profileId, string newName)
        {
            var profile = _currentSaveData.profiles.Find(p => p.profileId == profileId);
            if (profile != null)
            {
                profile.profileName = newName;
                profile.lastPlayedTimestamp = DateTime.UtcNow.ToString("o");
                MarkDirty();
                OnProfileChanged?.Invoke(profile);
            }
        }

        /// <summary>
        /// Rename a profile.
        /// </summary>
        public bool RenameProfile(string profileId, string newName)
        {
            var profile = _currentSaveData?.profiles?.Find(p => p.profileId == profileId);
            if (profile != null)
            {
                profile.profileName = newName;
                profile.lastPlayedTimestamp = DateTime.UtcNow.ToString("o");
                MarkDirty();
                OnProfileChanged?.Invoke(profile);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Record practice session completion.
        /// </summary>
        public void RecordPracticeComplete(string routineId, int score, float timeSeconds, bool success, int ballsPotted = 0, int fouls = 0, float accuracy = 0f, int difficultyLevel = 1, Dictionary<string, float> shotMetrics = null)
        {
            if (ActiveProfile == null) return;

            var entry = ActiveProfile.practiceProgress.GetOrCreateEntry(routineId);
            var attempt = new DrillAttemptData
            {
                score = score,
                timeSeconds = timeSeconds,
                success = success,
                ballsPotted = ballsPotted,
                foulsCommitted = fouls,
                accuracyPercent = accuracy,
                difficultyLevel = difficultyLevel
            };
            entry.RecordAttempt(attempt);

            // Update stats
            ActiveProfile.stats.totalPlayTimeSeconds += timeSeconds;
            if (success)
            {
                ActiveProfile.stats.framesWon++;
                ActiveProfile.stats.totalBallsPotted += ballsPotted;
            }
            ActiveProfile.stats.framesPlayed++;
            ActiveProfile.stats.totalFouls += fouls;

            ActiveProfile.lastPlayedTimestamp = DateTime.UtcNow.ToString("o");
            MarkDirty();
        }

        // Alias for compatibility with CueStrikeSaveSystemIntegration
        public void RecordPracticeAttempt(string routineId, int score, float timeSeconds, bool success, int ballsPotted = 0, int fouls = 0, float accuracy = 0f, int difficultyLevel = 1, Dictionary<string, float> shotMetrics = null)
        {
            RecordPracticeComplete(routineId, score, timeSeconds, success, ballsPotted, fouls, accuracy, difficultyLevel, shotMetrics);
        }

        /// <summary>
        /// Record match result.
        /// </summary>
        public void RecordMatchResult(bool won, int ballsPotted, int fouls, int breakScore, float frameTimeSeconds)
        {
            if (ActiveProfile == null) return;

            ActiveProfile.stats.matchesPlayed++;
            if (won) ActiveProfile.stats.matchesWon++;
            else ActiveProfile.stats.matchesLost++;

            ActiveProfile.stats.totalBallsPotted += ballsPotted;
            ActiveProfile.stats.totalFouls += fouls;
            ActiveProfile.stats.totalPlayTimeSeconds += frameTimeSeconds;

            if (breakScore > ActiveProfile.stats.maxBreak)
            {
                ActiveProfile.stats.maxBreak = breakScore;
            }
            if (breakScore >= 100) ActiveProfile.stats.centuryBreaks++;
            if (breakScore >= 50) ActiveProfile.stats.fiftyBreaks++;

            if (frameTimeSeconds < ActiveProfile.stats.bestFrameTimeSeconds)
            {
                ActiveProfile.stats.bestFrameTimeSeconds = frameTimeSeconds;
            }

            ActiveProfile.lastPlayedTimestamp = DateTime.UtcNow.ToString("o");
            MarkDirty();
        }

        // Overload for compatibility with CueStrikeSaveSystemIntegration
        public void RecordMatchResult(bool won, int framesWon, int framesLost, int ballsPotted, int maxBreak, int fouls, float playTimeSeconds)
        {
            if (ActiveProfile == null) return;

            ActiveProfile.stats.matchesPlayed++;
            if (won) ActiveProfile.stats.matchesWon++;
            else ActiveProfile.stats.matchesLost++;

            ActiveProfile.stats.framesPlayed += framesWon + framesLost;
            ActiveProfile.stats.framesWon += framesWon;
            ActiveProfile.stats.totalBallsPotted += ballsPotted;
            ActiveProfile.stats.totalFouls += fouls;
            ActiveProfile.stats.totalPlayTimeSeconds += playTimeSeconds;

            if (maxBreak > ActiveProfile.stats.maxBreak)
            {
                ActiveProfile.stats.maxBreak = maxBreak;
            }
            if (maxBreak >= 100) ActiveProfile.stats.centuryBreaks++;
            if (maxBreak >= 50) ActiveProfile.stats.fiftyBreaks++;

            if (playTimeSeconds < ActiveProfile.stats.bestFrameTimeSeconds)
            {
                ActiveProfile.stats.bestFrameTimeSeconds = playTimeSeconds;
            }

            ActiveProfile.lastPlayedTimestamp = DateTime.UtcNow.ToString("o");
            MarkDirty();
        }

        /// <summary>
        /// Record a foul during gameplay.
        /// </summary>
        public void RecordFoul()
        {
            if (ActiveProfile != null)
            {
                ActiveProfile.stats.totalFouls++;
                MarkDirty();
            }
        }

        /// <summary>
        /// Record a safety shot.
        /// </summary>
        public void RecordSafety()
        {
            if (ActiveProfile != null)
            {
                ActiveProfile.stats.totalSafeties++;
                MarkDirty();
            }
        }

        /// <summary>
        /// Record a rage quit.
        /// </summary>
        public void RecordRageQuit()
        {
            if (ActiveProfile != null)
            {
                ActiveProfile.stats.rageQuits++;
                MarkDirty();
            }
        }

        /// <summary>
        /// Update current break during a frame.
        /// </summary>
        public void UpdateCurrentBreak(int breakValue)
        {
            if (ActiveProfile != null)
            {
                ActiveProfile.stats.currentBreak = breakValue;
                MarkDirty();
            }
        }

        /// <summary>
        /// Reset current break (end of visit).
        /// </summary>
        public void ResetCurrentBreak()
        {
            if (ActiveProfile != null)
            {
                ActiveProfile.stats.currentBreak = 0;
                MarkDirty();
            }
        }

        /// <summary>
        /// Unlock a practice routine.
        /// </summary>
        public void UnlockRoutine(string routineId)
        {
            if (ActiveProfile != null)
            {
                var entry = ActiveProfile.practiceProgress.GetOrCreateEntry(routineId);
                entry.isUnlocked = true;
                MarkDirty();
            }
        }

        /// <summary>
        /// Set the difficulty level for a routine.
        /// </summary>
        public void SetRoutineDifficulty(string routineId, int level)
        {
            if (ActiveProfile != null)
            {
                var entry = ActiveProfile.practiceProgress.GetOrCreateEntry(routineId);
                entry.currentDifficultyLevel = Mathf.Max(1, level);
                MarkDirty();
            }
        }

        // ============================================================
        // Custom Drill Management
        // ============================================================

        /// <summary>
        /// Get all custom drills.
        /// </summary>
        public List<CustomDrillData> GetAllCustomDrills()
        {
            return _currentSaveData?.customDrills ?? new List<CustomDrillData>();
        }

        /// <summary>
        /// Get custom drill by ID.
        /// </summary>
        public CustomDrillData GetCustomDrill(string drillId)
        {
            if (string.IsNullOrEmpty(drillId)) return null;
            return _currentSaveData?.customDrills?.Find(d => d.drillId == drillId);
        }

        /// <summary>
        /// Get custom drills by author.
        /// </summary>
        public List<CustomDrillData> GetCustomDrillsByAuthor(string profileId)
        {
            return _currentSaveData?.customDrills?.Where(d => d.authorProfileId == profileId).ToList() ?? new List<CustomDrillData>();
        }

        /// <summary>
        /// Save a custom drill (create or update).
        /// </summary>
        public CustomDrillData SaveCustomDrill(CustomDrillData drill)
        {
            if (_currentSaveData == null || drill == null) return null;

            var existing = GetCustomDrill(drill.drillId);
            if (existing != null)
            {
                // Update existing
                existing.drillName = drill.drillName;
                existing.description = drill.description;
                existing.tableType = drill.tableType;
                existing.ballPositions = drill.ballPositions;
                existing.settings = drill.settings;
                existing.modifiedTimestamp = DateTime.UtcNow.ToString("o");
                existing.isPublic = drill.isPublic;
            }
            else
            {
                // Create new
                drill.authorProfileId = ActiveProfile?.profileId ?? "";
                drill.createdTimestamp = DateTime.UtcNow.ToString("o");
                drill.modifiedTimestamp = drill.createdTimestamp;
                _currentSaveData.customDrills.Add(drill);
                existing = drill;
            }

            MarkDirty();
            OnCustomDrillSaved?.Invoke(existing);
            return existing;
        }

        /// <summary>
        /// Create a new custom drill from current ball layout.
        /// </summary>
        public CustomDrillData CreateCustomDrill(string name, string description, int tableType, List<BallPositionData> ballPositions, DrillSettingsData settings = null)
        {
            var drill = new CustomDrillData
            {
                drillName = name,
                description = description,
                tableType = tableType,
                ballPositions = ballPositions ?? new List<BallPositionData>(),
                settings = settings ?? new DrillSettingsData()
            };

            return SaveCustomDrill(drill);
        }

        /// <summary>
        /// Delete a custom drill.
        /// </summary>
        public bool DeleteCustomDrill(string drillId)
        {
            var drill = GetCustomDrill(drillId);
            if (drill != null)
            {
                _currentSaveData.customDrills.Remove(drill);
                MarkDirty();
                OnCustomDrillDeleted?.Invoke(drill);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get recent custom drills (by modified timestamp).
        /// </summary>
        public List<CustomDrillData> GetRecentDrills(int count = 10)
        {
            return _currentSaveData?.customDrills?
                .OrderByDescending(d => d.modifiedTimestamp)
                .Take(count)
                .ToList() ?? new List<CustomDrillData>();
        }

        // ============================================================
        // Practice Progress
        // ============================================================

        /// <summary>
        /// Get routine progress for active profile.
        /// </summary>
        public RoutineProgressEntry GetRoutineProgress(string routineId)
        {
            return ActiveProfile?.practiceProgress?.GetOrCreateEntry(routineId);
        }

        /// <summary>
        /// Get all routine progress for active profile.
        /// </summary>
        public List<RoutineProgressEntry> GetAllRoutineProgress()
        {
            return ActiveProfile?.practiceProgress?.routineEntries ?? new List<RoutineProgressEntry>();
        }

        // ============================================================
        // Settings
        // ============================================================

        /// <summary>
        /// Get player preferences.
        /// </summary>
        public PlayerPreferencesData GetPreferences()
        {
            var prefs = ActiveProfile?.preferences;
            if (prefs == null)
            {
                if (ActiveProfile != null)
                {
                    ActiveProfile.preferences = new PlayerPreferencesData();
                    prefs = ActiveProfile.preferences;
                    MarkDirty();
                }
                else
                {
                    prefs = new PlayerPreferencesData();
                }
            }
            return prefs;
        }

        /// <summary>
        /// Update player preferences.
        /// </summary>
        public void UpdatePreferences(PlayerPreferencesData preferences)
        {
            if (ActiveProfile != null)
            {
                ActiveProfile.preferences = preferences;
                MarkDirty();
            }
        }

        /// <summary>
        /// Get global settings.
        /// </summary>
        public GlobalSettingsData GetGlobalSettings()
        {
            return _currentSaveData?.globalSettings ?? new GlobalSettingsData();
        }

        /// <summary>
        /// Update global settings.
        /// </summary>
        public void UpdateGlobalSettings(GlobalSettingsData settings)
        {
            if (_currentSaveData != null)
            {
                _currentSaveData.globalSettings = settings;
                MarkDirty();
            }
        }

        // ============================================================
        // RCA Data
        // ============================================================

        /// <summary>
        /// Save RCA calibration data.
        /// </summary>
        public void SaveRCAData(string profileId, RCAData rcaData)
        {
            var profile = _currentSaveData?.profiles?.Find(p => p.profileId == profileId) ?? ActiveProfile;
            if (profile != null)
            {
                profile.rcaData = rcaData;
                MarkDirty();
            }
        }

        /// <summary>
        /// Get RCA calibration data for profile.
        /// </summary>
        public RCAData GetRCAData(string profileId)
        {
            var profile = _currentSaveData?.profiles?.Find(p => p.profileId == profileId) ?? ActiveProfile;
            return profile?.rcaData;
        }

        // ============================================================
        // Utility
        // ============================================================

        /// <summary>
        /// Export save data to JSON string.
        /// </summary>
        public string ExportToJson()
        {
            return JsonUtility.ToJson(_currentSaveData, true);
        }

        /// <summary>
        /// Import save data from JSON string.
        /// </summary>
        public bool ImportFromJson(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<CueStrikeSaveData>(json);
                if (data != null)
                {
                    _currentSaveData = data;
                    MarkDirty();
                    Save();
                    OnLoadCompleted?.Invoke();
                    return true;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CueStrikeSaveLoadManager] Import failed: {e.Message}");
            }
            return false;
        }

        /// <summary>
        /// Record a custom drill play session.
        /// </summary>
        public void RecordCustomDrillPlay(string drillId, int score, float timeSeconds, bool completed)
        {
            if (ActiveProfile == null) return;

            var drill = GetCustomDrill(drillId);
            if (drill != null)
            {
                drill.stats.timesPlayed++;
                if (completed) drill.stats.timesCompleted++;
                
                drill.stats.averageScore = (drill.stats.averageScore * (drill.stats.timesPlayed - 1) + score) / drill.stats.timesPlayed;
                drill.stats.averageTimeSeconds = (drill.stats.averageTimeSeconds * (drill.stats.timesPlayed - 1) + timeSeconds) / drill.stats.timesPlayed;
                
                if (score > drill.stats.globalBestScore)
                    drill.stats.globalBestScore = score;
                
                if (completed && timeSeconds < drill.stats.globalBestTimeSeconds)
                    drill.stats.globalBestTimeSeconds = timeSeconds;

                MarkDirty();
            }

            // Also record as practice attempt if it's linked to a routine
            // This could be expanded based on drill settings
        }

        /// <summary>
        /// Set a preference value by field name.
        /// </summary>
        public void SetPreference<T>(string fieldName, T value)
        {
            if (ActiveProfile == null) return;
            
            var prefs = GetPreferences();
            var field = typeof(PlayerPreferencesData).GetField(fieldName);
            if (field != null)
            {
                field.SetValue(prefs, value);
                MarkDirty();
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SaveLoadManager] Preference field '{fieldName}' not found");
            }
        }

        /// <summary>
        /// Apply preferences to game systems.
        /// </summary>
        public void ApplyPreferences()
        {
            var prefs = GetPreferences();
            // Apply preferences to game systems
            // This would typically call into other game systems to apply settings
            UnityEngine.Debug.Log("[SaveLoadManager] Preferences applied");
        }

        /// <summary>
        /// Set last selected routine in global settings.
        /// </summary>
        public void SetLastSelectedRoutine(string routineId)
        {
            var settings = GetGlobalSettings();
            settings.lastSelectedRoutineId = routineId;
            UpdateGlobalSettings(settings);
        }

        /// <summary>
        /// Set last selected table type in global settings.
        /// </summary>
        public void SetLastSelectedTableType(int tableType)
        {
            var settings = GetGlobalSettings();
            settings.lastSelectedTableType = tableType;
            UpdateGlobalSettings(settings);
        }

        /// <summary>
        /// Add a recently played drill to global settings.
        /// </summary>
        public void AddRecentDrill(string drillId)
        {
            if (string.IsNullOrEmpty(drillId)) return;
            
            var settings = GetGlobalSettings();
            if (settings.recentlyPlayedDrillIds == null)
                settings.recentlyPlayedDrillIds = new List<string>();

            // Remove if already exists
            settings.recentlyPlayedDrillIds.Remove(drillId);
            // Add to front
            settings.recentlyPlayedDrillIds.Insert(0, drillId);
            // Keep only last 20
            if (settings.recentlyPlayedDrillIds.Count > 20)
                settings.recentlyPlayedDrillIds.RemoveAt(settings.recentlyPlayedDrillIds.Count - 1);

            UpdateGlobalSettings(settings);
        }

        /// <summary>
        /// Check if there are unsaved changes.
        /// </summary>
        public bool HasUnsavedChanges => _isDirty;

        /// <summary>
        /// Export save data to file.
        /// </summary>
        public bool ExportSaveData(string filePath)
        {
            try
            {
                string json = ExportToJson();
                File.WriteAllText(filePath, json);
                UnityEngine.Debug.Log($"[CueStrikeSaveLoadManager] Exported save data to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CueStrikeSaveLoadManager] Export failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import save data from file.
        /// </summary>
        public bool ImportSaveData(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return ImportFromJson(json);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CueStrikeSaveLoadManager] Import failed: {e.Message}");
            }
            return false;
        }

        /// <summary>
        /// Delete all save data (factory reset).
        /// </summary>
        public void DeleteAllData()
        {
            ResetAllData();
        }

        /// <summary>
        /// Force immediate save.
        /// </summary>
        public void ForceSave()
        {
            Save();
        }

        /// <summary>
        /// Reset all save data (factory reset).
        /// </summary>
        public void ResetAllData()
        {
            _currentSaveData = new CueStrikeSaveData();
            MarkDirty();
            Save();
            UnityEngine.Debug.Log("[CueStrikeSaveLoadManager] All save data reset");
        }
    }
}
