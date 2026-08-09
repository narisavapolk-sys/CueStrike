using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Gameplay.SaveSystem
{
    /// <summary>
    /// Integration helper to connect SaveLoadManager with PracticeManager and other gameplay systems.
    /// Provides convenient static methods for common save/load operations.
    /// </summary>
    public static class CueStrikeSaveSystemIntegration
    {
        private static CueStrikeSaveLoadManager SaveManager => CueStrikeSaveLoadManager.Instance;

        #region Practice Routine Integration

        /// <summary>
        /// Record a practice routine completion.
        /// Call this when a practice routine ends (success or failure).
        /// </summary>
        public static void RecordPracticeRoutineComplete(string routineId, int score, float timeSeconds, bool success, 
            int ballsPotted = 0, int fouls = 0, float accuracy = 0f, int difficultyLevel = 1,
            Dictionary<string, float> shotMetrics = null)
        {
            if (SaveManager == null)
            {
                Debug.LogWarning("[SaveSystemIntegration] SaveManager not initialized");
                return;
            }

            SaveManager.RecordPracticeAttempt(routineId, score, timeSeconds, success, ballsPotted, fouls, accuracy, difficultyLevel, shotMetrics);
        }

        /// <summary>
        /// Get progress for a specific practice routine.
        /// </summary>
        public static RoutineProgressEntry GetRoutineProgress(string routineId)
        {
            return SaveManager?.GetRoutineProgress(routineId);
        }

        /// <summary>
        /// Check if a routine is unlocked for the active profile.
        /// </summary>
        public static bool IsRoutineUnlocked(string routineId)
        {
            var entry = GetRoutineProgress(routineId);
            return entry?.isUnlocked ?? true; // Default to unlocked if no data
        }

        /// <summary>
        /// Unlock a practice routine.
        /// </summary>
        public static void UnlockRoutine(string routineId)
        {
            SaveManager?.UnlockRoutine(routineId);
        }

        /// <summary>
        /// Get the current difficulty level for a routine.
        /// </summary>
        public static int GetRoutineDifficulty(string routineId)
        {
            var entry = GetRoutineProgress(routineId);
            return entry?.currentDifficultyLevel ?? 1;
        }

        /// <summary>
        /// Set the difficulty level for a routine.
        /// </summary>
        public static void SetRoutineDifficulty(string routineId, int level)
        {
            SaveManager?.SetRoutineDifficulty(routineId, level);
        }

        /// <summary>
        /// Get all routine progress for the active profile.
        /// </summary>
        public static List<RoutineProgressEntry> GetAllRoutineProgress()
        {
            return SaveManager?.GetAllRoutineProgress() ?? new List<RoutineProgressEntry>();
        }

        /// <summary>
        /// Get practice streak days.
        /// </summary>
        public static int GetPracticeStreak()
        {
            var profile = SaveManager?.ActiveProfile;
            return profile?.practiceProgress?.currentStreakDays ?? 0;
        }

        /// <summary>
        /// Get total practice time in seconds.
        /// </summary>
        public static float GetTotalPracticeTime()
        {
            var profile = SaveManager?.ActiveProfile;
            return profile?.practiceProgress?.totalPracticeTimeSeconds ?? 0f;
        }

        #endregion

        #region Match/Frame Integration

        /// <summary>
        /// Record a completed match result.
        /// </summary>
        public static void RecordMatchResult(bool won, int framesWon, int framesLost, int ballsPotted, 
            int maxBreak, int fouls, float playTimeSeconds)
        {
            SaveManager?.RecordMatchResult(won, framesWon, framesLost, ballsPotted, maxBreak, fouls, playTimeSeconds);
        }

        /// <summary>
        /// Record a foul during gameplay.
        /// </summary>
        public static void RecordFoul()
        {
            SaveManager?.RecordFoul();
        }

        /// <summary>
        /// Record a safety shot.
        /// </summary>
        public static void RecordSafety()
        {
            SaveManager?.RecordSafety();
        }

        /// <summary>
        /// Record a rage quit.
        /// </summary>
        public static void RecordRageQuit()
        {
            SaveManager?.RecordRageQuit();
        }

        /// <summary>
        /// Update current break during a frame.
        /// </summary>
        public static void UpdateCurrentBreak(int breakValue)
        {
            SaveManager?.UpdateCurrentBreak(breakValue);
        }

        /// <summary>
        /// Reset current break (end of visit).
        /// </summary>
        public static void ResetCurrentBreak()
        {
            SaveManager?.ResetCurrentBreak();
        }

        #endregion

        #region Custom Drill Integration

        /// <summary>
        /// Create a custom drill from current ball positions.
        /// </summary>
        public static CustomDrillData CreateCustomDrill(string name, string description, int tableType, 
            List<BallPositionData> ballPositions, DrillSettingsData settings = null)
        {
            return SaveManager?.CreateCustomDrill(name, description, tableType, ballPositions, settings);
        }

        /// <summary>
        /// Save/update a custom drill.
        /// </summary>
        public static CustomDrillData SaveCustomDrill(CustomDrillData drill)
        {
            return SaveManager?.SaveCustomDrill(drill);
        }

        /// <summary>
        /// Delete a custom drill.
        /// </summary>
        public static bool DeleteCustomDrill(string drillId)
        {
            return SaveManager?.DeleteCustomDrill(drillId) ?? false;
        }

        /// <summary>
        /// Get all custom drills.
        /// </summary>
        public static List<CustomDrillData> GetAllCustomDrills()
        {
            return SaveManager?.GetAllCustomDrills() ?? new List<CustomDrillData>();
        }

        /// <summary>
        /// Get a specific custom drill by ID.
        /// </summary>
        public static CustomDrillData GetCustomDrill(string drillId)
        {
            return SaveManager?.GetCustomDrill(drillId);
        }

        /// <summary>
        /// Get custom drills created by the active profile.
        /// </summary>
        public static List<CustomDrillData> GetMyCustomDrills()
        {
            var profile = SaveManager?.ActiveProfile;
            if (profile == null) return new List<CustomDrillData>();
            return SaveManager?.GetCustomDrillsByAuthor(profile.profileId) ?? new List<CustomDrillData>();
        }

        /// <summary>
        /// Record a custom drill play session.
        /// </summary>
        public static void RecordCustomDrillPlay(string drillId, int score, float timeSeconds, bool completed)
        {
            SaveManager?.RecordCustomDrillPlay(drillId, score, timeSeconds, completed);
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// Get all profiles.
        /// </summary>
        public static List<PlayerProfileData> GetAllProfiles()
        {
            return SaveManager?.GetProfiles() ?? new List<PlayerProfileData>();
        }

        /// <summary>
        /// Get active profile.
        /// </summary>
        public static PlayerProfileData GetActiveProfile()
        {
            return SaveManager?.ActiveProfile;
        }

        /// <summary>
        /// Create a new profile.
        /// </summary>
        public static PlayerProfileData CreateProfile(string name)
        {
            return SaveManager?.CreateProfile(name);
        }

        /// <summary>
        /// Switch active profile by profile ID.
        /// </summary>
        public static bool SetActiveProfile(string profileId)
        {
            return SaveManager?.SetActiveProfile(profileId) ?? false;
        }

        /// <summary>
        /// Switch active profile by index.
        /// </summary>
        public static void SetActiveProfile(int index)
        {
            SaveManager?.SetActiveProfile(index);
        }

        /// <summary>
        /// Delete a profile.
        /// </summary>
        public static bool DeleteProfile(string profileId)
        {
            return SaveManager?.DeleteProfile(profileId) ?? false;
        }

        /// <summary>
        /// Rename a profile.
        /// </summary>
        public static bool RenameProfile(string profileId, string newName)
        {
            return SaveManager?.RenameProfile(profileId, newName) ?? false;
        }

        #endregion

        #region Stats & Preferences

        /// <summary>
        /// Get active profile stats.
        /// </summary>
        public static PlayerStatsData GetPlayerStats()
        {
            var profile = SaveManager?.ActiveProfile;
            return profile?.stats;
        }

        /// <summary>
        /// Get active profile preferences.
        /// </summary>
        public static PlayerPreferencesData GetPreferences()
        {
            return SaveManager?.GetPreferences();
        }

        /// <summary>
        /// Set a preference value.
        /// </summary>
        public static void SetPreference<T>(string fieldName, T value)
        {
            SaveManager?.SetPreference(fieldName, value);
        }

        /// <summary>
        /// Apply preferences to game systems.
        /// </summary>
        public static void ApplyPreferences()
        {
            SaveManager?.ApplyPreferences();
        }

        #endregion

        #region Global Settings

        /// <summary>
        /// Set last selected routine.
        /// </summary>
        public static void SetLastSelectedRoutine(string routineId)
        {
            SaveManager?.SetLastSelectedRoutine(routineId);
        }

        /// <summary>
        /// Get last selected routine.
        /// </summary>
        public static string GetLastSelectedRoutine()
        {
            return SaveManager?.GetGlobalSettings()?.lastSelectedRoutineId;
        }

        /// <summary>
        /// Set last selected table type.
        /// </summary>
        public static void SetLastSelectedTableType(int tableType)
        {
            SaveManager?.SetLastSelectedTableType(tableType);
        }

        /// <summary>
        /// Get last selected table type.
        /// </summary>
        public static int GetLastSelectedTableType()
        {
            return SaveManager?.GetGlobalSettings()?.lastSelectedTableType ?? 0;
        }

        /// <summary>
        /// Add a recently played drill.
        /// </summary>
        public static void AddRecentDrill(string drillId)
        {
            SaveManager?.AddRecentDrill(drillId);
        }

        /// <summary>
        /// Get recently played drills.
        /// </summary>
        public static List<string> GetRecentDrills()
        {
            return SaveManager?.GetGlobalSettings()?.recentlyPlayedDrillIds ?? new List<string>();
        }

        #endregion

        #region Save/Load Control

        /// <summary>
        /// Force immediate save.
        /// </summary>
        public static void ForceSave()
        {
            SaveManager?.ForceSave();
        }

        /// <summary>
        /// Check if there are unsaved changes.
        /// </summary>
        public static bool HasUnsavedChanges()
        {
            return SaveManager?.HasUnsavedChanges ?? false;
        }

        /// <summary>
        /// Export save data to file.
        /// </summary>
        public static bool ExportSaveData(string filePath)
        {
            return SaveManager?.ExportSaveData(filePath) ?? false;
        }

        /// <summary>
        /// Import save data from file.
        /// </summary>
        public static bool ImportSaveData(string filePath)
        {
            return SaveManager?.ImportSaveData(filePath) ?? false;
        }

        /// <summary>
        /// Delete all save data (factory reset).
        /// </summary>
        public static void DeleteAllData()
        {
            SaveManager?.DeleteAllData();
        }

        #endregion

        #region Ball Position Helpers

        /// <summary>
        /// Create BallPositionData from a ball GameObject.
        /// </summary>
        public static BallPositionData CreateBallPositionData(GameObject ball, int ballId, string ballName)
        {
            if (ball == null) return null;

            return new BallPositionData
            {
                ballId = ballId,
                ballName = ballName,
                position = new Vector3Serializable(ball.transform.position),
                velocity = new Vector3Serializable(ball.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero),
                isActive = ball.activeInHierarchy,
                isPocketed = false,
                pocketIndex = -1
            };
        }

        /// <summary>
        /// Create BallPositionData from Transform and Rigidbody.
        /// </summary>
        public static BallPositionData CreateBallPositionData(Transform transform, Rigidbody rb, int ballId, string ballName, bool isActive = true)
        {
            return new BallPositionData
            {
                ballId = ballId,
                ballName = ballName,
                position = new Vector3Serializable(transform.position),
                velocity = new Vector3Serializable(rb?.linearVelocity ?? Vector3.zero),
                isActive = isActive,
                isPocketed = false,
                pocketIndex = -1
            };
        }

        /// <summary>
        /// Apply BallPositionData to a ball GameObject.
        /// </summary>
        public static void ApplyBallPositionData(GameObject ball, BallPositionData data)
        {
            if (ball == null || data == null) return;

            ball.transform.position = data.position.ToVector3();
            
            var rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = data.velocity.ToVector3();
                rb.angularVelocity = Vector3.zero;
            }

            ball.SetActive(data.isActive);
        }

        /// <summary>
        /// Capture current ball layout for all balls in scene.
        /// </summary>
        public static List<BallPositionData> CaptureCurrentBallLayout(string ballTag = "Ball")
        {
            var layout = new List<BallPositionData>();
            var balls = GameObject.FindGameObjectsWithTag(ballTag);

            foreach (var ball in balls)
            {
                // Try to determine ball ID from name or component
                int ballId = 0;
                string ballName = ball.name;

                // Check for Ball component or similar
                var ballComponent = ball.GetComponent<CueStrikeBall>();
                if (ballComponent != null)
                {
                    ballId = ballComponent.BallId;
                    ballName = ballComponent.BallName;
                }
                else
                {
                    // Try parsing from name
                    var nameParts = ball.name.Split('_', ' ', '-');
                    foreach (var part in nameParts)
                    {
                        if (int.TryParse(part, out int parsedId))
                        {
                            ballId = parsedId;
                            break;
                        }
                    }
                }

                var data = CreateBallPositionData(ball, ballId, ballName);
                if (data != null)
                {
                    layout.Add(data);
                }
            }

            // Also capture cue ball if not tagged
            var cueBall = GameObject.Find("CueBall") ?? GameObject.Find("Cue Ball");
            if (cueBall != null && !cueBall.CompareTag(ballTag))
            {
                var data = CreateBallPositionData(cueBall, 0, "Cue Ball");
                if (data != null)
                {
                    layout.Add(data);
                }
            }

            return layout;
        }

        #endregion

        #region Routine ID Constants

        /// <summary>
        /// Standard routine IDs for consistent tracking.
        /// </summary>
        public static class RoutineIds
        {
            public const string StraightIn = "routine_straight_in";
            public const string CutShots = "routine_cut_shots";
            public const string FollowDraw = "routine_follow_draw";
            public const string SideSpin = "routine_side_spin";
            public const string PositionPlay = "routine_position_play";
            public const string BreakPractice = "routine_break_practice";
            public const string SafetyPlay = "routine_safety_play";
            public const string PatternPlay = "routine_pattern_play";
            public const string PressureDrills = "routine_pressure_drills";
            public const string CustomBuilder = "routine_custom_builder";
            public const string FreePlacement = "routine_free_placement";
            public const string LineUp = "routine_line_up";
            public const string DZoneClearance = "routine_dzone_clearance";
            public const string CushionKiss = "routine_cushion_kiss";
            public const string AroundTheBlack = "routine_around_the_black";
            public const string SpiralCurve = "routine_spiral_curve";
        }

        /// <summary>
        /// Table type constants.
        /// </summary>
        public static class TableTypes
        {
            public const int Snooker = 0;
            public const int Pool8Ball = 1;
            public const int Pool9Ball = 2;
        }

        #endregion
    }

    /// <summary>
    /// Helper component to attach to balls for identification.
    /// </summary>
    public class CueStrikeBall : MonoBehaviour
    {
        public int BallId;
        public string BallName;
        public BallType Type = BallType.ObjectBall;

        public enum BallType
        {
            CueBall = 0,
            ObjectBall = 1,
            ColorBall = 2
        }
    }
}