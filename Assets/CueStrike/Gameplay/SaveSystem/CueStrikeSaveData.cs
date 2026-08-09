using System;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.Gameplay.Practice;

namespace CueStrike.Gameplay.SaveSystem
{
    /// <summary>
    /// Root save data container for all game data.
    /// </summary>
    [Serializable]
    public class CueStrikeSaveData
    {
        public string version = "1.0.0";
        public string lastSavedTimestamp;
        public int activeProfileIndex = 0;
        public List<PlayerProfileData> profiles = new List<PlayerProfileData>();
        public List<CustomDrillData> customDrills = new List<CustomDrillData>();
        public GlobalSettingsData globalSettings = new GlobalSettingsData();

        public CueStrikeSaveData()
        {
            lastSavedTimestamp = DateTime.UtcNow.ToString("o");
            if (profiles.Count == 0)
            {
                profiles.Add(new PlayerProfileData { profileName = "Player 1", isActive = true });
            }
        }
    }

    /// <summary>
    /// Player profile containing stats, progress, and preferences.
    /// </summary>
    [Serializable]
    public class PlayerProfileData
    {
        public string profileId;
        public string profileName;
        public bool isActive;
        public string createdTimestamp;
        public string lastPlayedTimestamp;
        public PlayerStatsData stats = new PlayerStatsData();
        public PracticeProgressData practiceProgress = new PracticeProgressData();
        public PlayerPreferencesData preferences = new PlayerPreferencesData();
        public RCAData rcaData;

        public PlayerProfileData()
        {
            profileId = Guid.NewGuid().ToString();
            createdTimestamp = DateTime.UtcNow.ToString("o");
            lastPlayedTimestamp = createdTimestamp;
        }
    }

    /// <summary>
    /// Core gameplay statistics.
    /// </summary>
    [Serializable]
    public class PlayerStatsData
    {
        public int matchesPlayed = 0;
        public int matchesWon = 0;
        public int matchesLost = 0;
        public int framesPlayed = 0;
        public int framesWon = 0;
        public int totalBallsPotted = 0;
        public int maxBreak = 0;
        public int currentBreak = 0;
        public int totalFouls = 0;
        public int totalSafeties = 0;
        public int rageQuits = 0;
        public float totalPlayTimeSeconds = 0f;
        public float bestFrameTimeSeconds = float.MaxValue;
        public int highestRunOut = 0;
        public int centuryBreaks = 0;
        public int fiftyBreaks = 0;
    }

    /// <summary>
    /// Practice routine progress tracking.
    /// </summary>
    [Serializable]
    public class PracticeProgressData
    {
        public List<RoutineProgressEntry> routineEntries = new List<RoutineProgressEntry>();
        public int totalRoutinesCompleted = 0;
        public float totalPracticeTimeSeconds = 0f;
        public int currentStreakDays = 0;
        public string lastPracticeDate;

        public RoutineProgressEntry GetOrCreateEntry(string routineId)
        {
            var entry = routineEntries.Find(e => e.routineId == routineId);
            if (entry == null)
            {
                entry = new RoutineProgressEntry { routineId = routineId };
                routineEntries.Add(entry);
            }
            return entry;
        }
    }

    /// <summary>
    /// Individual routine progress entry.
    /// </summary>
    [Serializable]
    public class RoutineProgressEntry
    {
        public string routineId;
        public int timesCompleted = 0;
        public int bestScore = 0;
        public int currentScore = 0;
        public float bestTimeSeconds = float.MaxValue;
        public float averageTimeSeconds = 0f;
        public float totalTimeSeconds = 0f;
        public int totalAttempts = 0;
        public int successfulAttempts = 0;
        public string lastCompletedTimestamp;
        public int currentDifficultyLevel = 1;
        public bool isUnlocked = true;
        public List<DrillAttemptData> recentAttempts = new List<DrillAttemptData>();

        public float SuccessRate => totalAttempts > 0 ? (float)successfulAttempts / totalAttempts : 0f;

        public void RecordAttempt(DrillAttemptData attempt)
        {
            recentAttempts.Add(attempt);
            if (recentAttempts.Count > 50)
                recentAttempts.RemoveAt(0);

            totalAttempts++;
            totalTimeSeconds += attempt.timeSeconds;
            averageTimeSeconds = totalTimeSeconds / totalAttempts;

            if (attempt.success)
            {
                successfulAttempts++;
                timesCompleted++;
                currentScore = Mathf.Max(currentScore, attempt.score);
                bestScore = Mathf.Max(bestScore, attempt.score);
                bestTimeSeconds = Mathf.Min(bestTimeSeconds, attempt.timeSeconds);
                lastCompletedTimestamp = DateTime.UtcNow.ToString("o");
            }
        }
    }

    /// <summary>
    /// Single drill attempt data.
    /// </summary>
    [Serializable]
    public class DrillAttemptData
    {
        public string timestamp;
        public int score;
        public float timeSeconds;
        public bool success;
        public int ballsPotted;
        public int foulsCommitted;
        public float accuracyPercent;
        public int difficultyLevel;
        public Dictionary<string, float> shotMetrics = new Dictionary<string, float>();

        public DrillAttemptData()
        {
            timestamp = DateTime.UtcNow.ToString("o");
        }
    }

    /// <summary>
    /// Custom drill layout created by user.
    /// </summary>
    [Serializable]
    public class CustomDrillData
    {
        public string drillId;
        public string drillName;
        public string description;
        public string authorProfileId;
        public string authorName; // For display purposes
        public string createdTimestamp;
        public string modifiedTimestamp;
        public int tableType; // 0 = Snooker, 1 = Pool 8-Ball, 2 = Pool 9-Ball
        public List<BallPositionData> ballPositions = new List<BallPositionData>();
        public DrillSettingsData settings = new DrillSettingsData();
        public DrillStatsData stats = new DrillStatsData();
        public bool isPublic = false;
        public int downloadCount = 0;
        public int ratingSum = 0;
        public int ratingCount = 0;

        // Convenience properties for UI
        public string authorId => authorProfileId;
        public string createdDate => createdTimestamp;
        public int playCount => stats?.timesPlayed ?? 0;
        public int bestScore => stats?.globalBestScore ?? 0;
        public float bestTime => stats?.globalBestTimeSeconds ?? float.MaxValue;
        public List<string> tags => settings?.tags ?? new List<string>();

        public float AverageRating => ratingCount > 0 ? (float)ratingSum / ratingCount : 0f;

        public CustomDrillData()
        {
            drillId = Guid.NewGuid().ToString();
            createdTimestamp = DateTime.UtcNow.ToString("o");
            modifiedTimestamp = createdTimestamp;
        }
    }

    /// <summary>
    /// Ball position data for custom drills.
    /// </summary>
    [Serializable]
    public class BallPositionData
    {
        public int ballId; // 0 = Cue Ball, 1-15 = Object balls, 16-21 = Colors (Snooker)
        public string ballName;
        public Vector3Serializable position;
        public Vector3Serializable velocity; // For moving balls
        public bool isActive = true;
        public bool isPocketed = false;
        public int pocketIndex = -1;

        public BallPositionData() { }

        public BallPositionData(int id, string name, Vector3 pos)
        {
            ballId = id;
            ballName = name;
            position = new Vector3Serializable(pos);
            velocity = new Vector3Serializable(Vector3.zero);
        }
    }

    /// <summary>
    /// Serializable Vector3.
    /// </summary>
    [Serializable]
    public struct Vector3Serializable
    {
        public float x, y, z;

        public Vector3Serializable(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);

        public static Vector3Serializable zero => new Vector3Serializable(Vector3.zero);
        public static Vector3Serializable one => new Vector3Serializable(Vector3.one);

        public static implicit operator Vector3(Vector3Serializable v) => v.ToVector3();
        public static implicit operator Vector3Serializable(Vector3 v) => new Vector3Serializable(v);
    }

    /// <summary>
    /// Custom drill settings.
    /// </summary>
    [Serializable]
    public class DrillSettingsData
    {
        public float timeLimitSeconds = 0f; // 0 = no limit
        public int targetScore = 0; // 0 = no target
        public int maxFoulsAllowed = 3;
        public bool requireCallShot = false;
        public bool allowBallInHand = true;
        public int difficultyLevel = 1; // 1-5
        public List<string> tags = new List<string>();

        // Additional fields for UI compatibility
        private bool _isTimed = false;
        public bool isTimed
        {
            get => _isTimed || timeLimitSeconds > 0f;
            set => _isTimed = value;
        }
        public int maxShots = 0; // 0 = unlimited
        public bool requireAllBallsPotted = false;
    }

    /// <summary>
    /// Custom drill aggregate stats.
    /// </summary>
    [Serializable]
    public class DrillStatsData
    {
        public int timesPlayed = 0;
        public int timesCompleted = 0;
        public int globalBestScore = 0;
        public float globalBestTimeSeconds = float.MaxValue;
        public float averageScore = 0f;
        public float averageTimeSeconds = 0f;
    }

    /// <summary>
    /// Player preferences (video, audio, controls, accessibility).
    /// </summary>
    [Serializable]
    public class PlayerPreferencesData
    {
        // Video
        public int renderScale = 100;
        public bool vsync = true;
        public int targetFPS = 72;
        public bool bloomEnabled = true;
        public bool shadowsEnabled = true;

        // Audio
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 0.5f;
        public float voiceVolume = 1f;

        // Controls
        public float cueSensitivity = 1f;
        public bool invertYAxis = false;
        public bool leftHandedMode = false;
        public float hapticIntensity = 1f;

        // Accessibility
        public int colorBlindMode = 0; // 0=Off, 1=Protanopia, 2=Deuteranopia, 3=Tritanopia, 4=Monochrome
        public bool subtitlesEnabled = true;
        public int hudScale = 100;
        public bool comfortVignette = true;
        public bool reduceMotion = false;
        public bool highContrast = false;
        public bool oneHandedMode = false;

        // Gameplay
        public bool aimAssistEnabled = false;
        public int aimAssistStrength = 1; // 1-3
        public bool ghostBallEnabled = true;
        public bool shotPredictionEnabled = false;
        public bool autoChalk = true;
        public int tableClothSpeed = 2; // 1=Slow, 2=Medium, 3=Fast
    }

    /// <summary>
    /// Global settings not tied to profile.
    /// </summary>
    [Serializable]
    public class GlobalSettingsData
    {
        public string language = "en";
        public bool telemetryOptIn = true;
        public bool autoSaveEnabled = true;
        public int autoSaveIntervalMinutes = 5;
        public List<string> recentlyPlayedDrillIds = new List<string>();
        public string lastSelectedRoutineId;
        public int lastSelectedTableType = 0;
    }

    /// <summary>
    /// RCA calibration data for controller-less play.
    /// </summary>
    [Serializable]
    public class RCAData
    {
        public Vector3 leftWristOffset;
        public Vector3 rightWristOffset;
        public float cueLength;
        public float virtualCueLength;
        public float lengthScale;
        public float tipRadius;
        public TipProfile tipProfile;
        public float cueWeightOz;
        public float balancePoint;
        public float kalmanLatencyMs;
        public Vector3 tableAnchorOffset;
        public bool isCalibrated;
        public long calibrationTimestamp;
    }

    public enum TipProfile { Flat, Rounded, Custom }
}
