using UnityEngine;

#if UNITY_URP_AVAILABLE
using UnityEngine.Rendering.Universal;
#endif

namespace CueStrike.Visuals
{
    /// <summary>
    /// Noir mode controller: Cinematic / Full Noir with ball labels.
    /// STUB — implement full URP Volume integration when ready.
    /// </summary>
    public class CueStrikeNoirMode : MonoBehaviour
    {
        public static CueStrikeNoirMode Instance { get; private set; }

        [Header("Noir Settings")]
        [Range(0, 2)] public int currentIntensity = 0; // 0=Off, 1=Cinematic, 2=FullNoir

        /// <summary>Property for UI to check if Noir mode is active.</summary>
        public bool NoirEnabled => currentIntensity > 0;

#if UNITY_URP_AVAILABLE
        [Header("URP Volume")]
        public Volume globalVolume;
#endif

        [Header("Ball Labels")]
        public bool autoEnableLabels = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Cycles through noir intensity levels.
        /// </summary>
        public void CycleNoirIntensity()
        {
            currentIntensity = (currentIntensity + 1) % 3;
            ApplyNoirSettings();
        }

        /// <summary>
        /// Toggle Noir mode on/off (for UI integration).
        /// </summary>
        public void ToggleNoirMode()
        {
            currentIntensity = currentIntensity > 0 ? 0 : 2;
            ApplyNoirSettings();
        }

        /// <summary>
        /// Applies current noir settings.
        /// STUB: Implement URP Volume override when package is installed.
        /// </summary>
        private void ApplyNoirSettings()
        {
            Debug.Log($"[CueStrike] Noir mode set to: {currentIntensity} — STUB (URP integration pending)");

            if (autoEnableLabels && currentIntensity == 2)
            {
                EnableBallLabels(true);
            }
            else if (currentIntensity == 0)
            {
                EnableBallLabels(false);
            }
        }

        private void EnableBallLabels(bool enable)
        {
            var labels = FindFirstObjectByType<CueStrikeBallLabels>();
            if (labels != null)
            {
                // labels.SetEnabled(enable); // Uncomment when method exists
            }
        }
    }
}