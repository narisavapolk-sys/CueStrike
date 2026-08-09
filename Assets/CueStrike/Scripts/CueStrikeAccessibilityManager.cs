using UnityEngine;
using CueStrike;
using UnityEngine.UI;

#if UNITY_XR_AVAILABLE
using UnityEngine.XR;
#endif

namespace CueStrike.Accessibility
{
    /// <summary>
    /// Central accessibility manager for player settings.
    /// Singleton hub: HUD scale, colorblind modes, subtitles, comfort options.
    /// </summary>
    public class CueStrikeAccessibilityManager : MonoBehaviour
    {
        public static CueStrikeAccessibilityManager Instance { get; private set; }

        [Header("Settings")]
        public float hudScale = 1.0f;
        public int colorblindMode = 0; // 0=None, 1=Deuteranopia, 2=Protanopia, 3=Tritanopia
        public bool subtitlesEnabled = true;
        public bool oneHandedMode = false;
        public bool comfortTurnEnabled = false;
        public bool vignetteEnabled = false;
        public bool motionSicknessReduction = false;
        public bool highContrast = false;
        public float hapticScale = 1.0f;

        // Properties for UI binding
        public bool ReduceMotion => motionSicknessReduction;
        public bool HighContrastMode => highContrast;
        public bool OneHandedMode => oneHandedMode;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadSettings();
        }

        /// <summary>
        /// Returns haptic amplitude scaled by accessibility setting.
        /// </summary>
        public float GetHapticAmplitude(float baseAmplitude)
        {
            return baseAmplitude * hapticScale;
        }

        /// <summary>
        /// Cycles through colorblind modes.
        /// </summary>
        public void CycleColorblindMode()
        {
            colorblindMode = (colorblindMode + 1) % 4;
            SaveSettings();
        }

        /// <summary>
        /// Toggles high contrast mode.
        /// </summary>
        public void ToggleHighContrast()
        {
            highContrast = !highContrast;
            SaveSettings();
        }

        /// <summary>
        /// Toggles reduce motion setting.
        /// </summary>
        public void ToggleReduceMotion()
        {
            motionSicknessReduction = !motionSicknessReduction;
            SaveSettings();
        }

        public void ToggleOneHandedMode()
        {
            oneHandedMode = !oneHandedMode;
            SaveSettings();
        }

        public void ResetToDefaults()
        {
            hudScale = 1.0f;
            colorblindMode = 0;
            subtitlesEnabled = true;
            oneHandedMode = false;
            comfortTurnEnabled = false;
            vignetteEnabled = false;
            motionSicknessReduction = false;
            highContrast = false;
            hapticScale = 1.0f;
            SaveSettings();
        }

        public float HapticAmplitude => hapticScale;

        private void LoadSettings()
        {
            hudScale = PlayerPrefs.GetFloat("CueStrike_A11y_HUDScale", 1.0f);
            colorblindMode = PlayerPrefs.GetInt("CueStrike_A11y_Colorblind", 0);
            subtitlesEnabled = PlayerPrefs.GetInt("CueStrike_A11y_Subtitles", 1) == 1;
            oneHandedMode = PlayerPrefs.GetInt("CueStrike_A11y_OneHanded", 0) == 1;
            comfortTurnEnabled = PlayerPrefs.GetInt("CueStrike_A11y_ComfortTurn", 0) == 1;
            vignetteEnabled = PlayerPrefs.GetInt("CueStrike_A11y_Vignette", 0) == 1;
            motionSicknessReduction = PlayerPrefs.GetInt("CueStrike_A11y_MotionSickness", 0) == 1;
            highContrast = PlayerPrefs.GetInt("CueStrike_A11y_HighContrast", 0) == 1;
            hapticScale = PlayerPrefs.GetFloat("CueStrike_A11y_HapticScale", 1.0f);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("CueStrike_A11y_HUDScale", hudScale);
            PlayerPrefs.SetInt("CueStrike_A11y_Colorblind", colorblindMode);
            PlayerPrefs.SetInt("CueStrike_A11y_Subtitles", subtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetInt("CueStrike_A11y_OneHanded", oneHandedMode ? 1 : 0);
            PlayerPrefs.SetInt("CueStrike_A11y_ComfortTurn", comfortTurnEnabled ? 1 : 0);
            PlayerPrefs.SetInt("CueStrike_A11y_Vignette", vignetteEnabled ? 1 : 0);
            PlayerPrefs.SetInt("CueStrike_A11y_MotionSickness", motionSicknessReduction ? 1 : 0);
            PlayerPrefs.SetInt("CueStrike_A11y_HighContrast", highContrast ? 1 : 0);
            PlayerPrefs.SetFloat("CueStrike_A11y_HapticScale", hapticScale);
            PlayerPrefs.Save();
        }
    }
}