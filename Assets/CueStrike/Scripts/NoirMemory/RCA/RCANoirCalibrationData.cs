using System;
using UnityEngine;

namespace CueStrike.NoirMemory.RCA
{
    /// <summary>
    /// Stores calibration data for the RCA (Real Cue Adapter) system in Noir Memory mode.
    /// Persisted via PlayerPrefs using JSON serialization.
    /// </summary>
    [Serializable]
    public class RCANoirCalibrationData
    {
        private const string PlayerPrefsKey = "RCANoirCalibration";
        private static readonly TimeSpan MaxCalibrationAge = TimeSpan.FromHours(24);

        [SerializeField] private Vector3 cueRestPosition;
        [SerializeField] private Quaternion cueRestRotation;
        [SerializeField] private float powerScale = 1.0f;
        [SerializeField] private string calibratedAtRaw;

        /// <summary>
        /// The world-space rest position of the cue during calibration.
        /// </summary>
        public Vector3 CueRestPosition
        {
            get => cueRestPosition;
            set => cueRestPosition = value;
        }

        /// <summary>
        /// The world-space rest rotation of the cue during calibration.
        /// </summary>
        public Quaternion CueRestRotation
        {
            get => cueRestRotation;
            set => cueRestRotation = value;
        }

        /// <summary>
        /// Scaling factor applied to shot power. Default = 1.0.
        /// </summary>
        public float PowerScale
        {
            get => powerScale;
            set => powerScale = Mathf.Clamp(value, 0.1f, 3.0f);
        }

        /// <summary>
        /// Timestamp of when this calibration was performed.
        /// </summary>
        public DateTime CalibratedAt
        {
            get
            {
                if (DateTime.TryParse(calibratedAtRaw, out var dt))
                    return dt;
                return DateTime.MinValue;
            }
            set => calibratedAtRaw = value.ToString("O");
        }

        /// <summary>
        /// Returns true if calibration data exists and is not too old.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(calibratedAtRaw)) return false;
                var age = DateTime.UtcNow - CalibratedAt;
                return age >= TimeSpan.Zero && age <= MaxCalibrationAge;
            }
        }

        /// <summary>
        /// Returns true if any calibration has been saved to PlayerPrefs.
        /// </summary>
        public static bool HasSavedData => PlayerPrefs.HasKey(PlayerPrefsKey);

        /// <summary>
        /// Saves calibration data to PlayerPrefs.
        /// </summary>
        public static void Save(RCANoirCalibrationData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[RCANoir] Cannot save null calibration data.");
                return;
            }
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
            Debug.Log($"[RCANoir] Calibration saved: pos={data.cueRestPosition}, scale={data.powerScale}");
        }

        /// <summary>
        /// Loads calibration data from PlayerPrefs. Returns null if none exists.
        /// </summary>
        public static RCANoirCalibrationData Load()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                Debug.Log("[RCANoir] No saved calibration found.");
                return null;
            }

            try
            {
                string json = PlayerPrefs.GetString(PlayerPrefsKey);
                var data = JsonUtility.FromJson<RCANoirCalibrationData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[RCANoir] Failed to deserialize calibration data.");
                    return null;
                }
                Debug.Log($"[RCANoir] Calibration loaded: age={(DateTime.UtcNow - data.CalibratedAt).TotalMinutes:F1} min");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RCANoir] Error loading calibration: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clears saved calibration data.
        /// </summary>
        public static void Clear()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
                PlayerPrefs.Save();
                Debug.Log("[RCANoir] Calibration data cleared.");
            }
        }
    }
}