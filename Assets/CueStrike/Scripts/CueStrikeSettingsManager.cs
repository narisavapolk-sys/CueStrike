using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using CueStrike.VR.Input;

namespace CueStrike
{
    /// <summary>
    /// Global Settings Manager for CueStrike VR.
    /// Handles loading, saving, and applying VR preferences:
    /// - Master Volume
    /// - Comfort Vignette
    /// - Turn Mode (Snap vs Smooth)
    /// - Dominant Hand (Right vs Left)
    /// </summary>
    public class CueStrikeSettingsManager : MonoBehaviour
    {
        public static CueStrikeSettingsManager Instance { get; private set; }

        [Header("Global Preferences")]
        public float masterVolume = 1.0f;
        public bool enableComfortVignette = true;
        public int turnMode = 0; // 0 = Snap Turn, 1 = Smooth Turn
        public int dominantHand = 0; // 0 = Right Hand, 1 = Left Hand

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
        }

        private void Start()
        {
            ApplyAllSettings();
        }

        public void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("CueStrike_Volume", 1.0f);
            enableComfortVignette = PlayerPrefs.GetInt("CueStrike_ComfortVignette", 1) == 1;
            turnMode = PlayerPrefs.GetInt("CueStrike_TurnMode", 0);
            dominantHand = PlayerPrefs.GetInt("CueStrike_DominantHand", 0);
        }

        public void SaveSettings(float vol, bool vignette, int turn, int hand)
        {
            masterVolume = vol;
            enableComfortVignette = vignette;
            turnMode = turn;
            dominantHand = hand;

            PlayerPrefs.SetFloat("CueStrike_Volume", vol);
            PlayerPrefs.SetInt("CueStrike_ComfortVignette", vignette ? 1 : 0);
            PlayerPrefs.SetInt("CueStrike_TurnMode", turn);
            PlayerPrefs.SetInt("CueStrike_DominantHand", hand);
            PlayerPrefs.Save();

            ApplyAllSettings();
        }

        public void ApplyAllSettings()
        {
            // 1. Apply Volume
            AudioListener.volume = masterVolume;
            Debug.Log($"[CueStrike Settings] Applied Master Volume: {masterVolume * 100f:F0}%");

            // 2. Apply Comfort Vignette
            ApplyComfortVignetteSetting();

            // 3. Apply Turn Mode (Snap vs Smooth)
            ApplyTurnModeSetting();

            // 4. Apply Dominant Hand
            ApplyDominantHandSetting();
        }

        private void ApplyComfortVignetteSetting()
        {
            // Try to find any Vignette post process or XRI Vignette Provider
            var vignettes = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var v in vignettes)
            {
                string name = v.GetType().Name.ToLower();
                if (name.Contains("vignette") || name.Contains("tunneling"))
                {
                    v.enabled = enableComfortVignette;
                }
            }
            Debug.Log($"[CueStrike Settings] Comfort Vignette state: {enableComfortVignette}");
        }

        private void ApplyTurnModeSetting()
        {
            // In XRI, we typically have SnapTurnProvider and ContinuousTurnProvider components on the XR Origin/Rig
            var snapTurn = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider>();
            var smoothTurn = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>();

            if (snapTurn != null) snapTurn.enabled = (turnMode == 0);
            if (smoothTurn != null) smoothTurn.enabled = (turnMode == 1);

            // Also check CueStrike custom comfort action
            var customComfort = FindFirstObjectByType<CueStrike.VR.CueStrikeVRComfortActions>();
            if (customComfort != null)
            {
                customComfort.turnSpeed = (turnMode == 1) ? 45f : 0f; // Enable smooth turn only when mode is 1
            }

            Debug.Log($"[CueStrike Settings] Turn Mode: {(turnMode == 0 ? "Snap Turn" : "Smooth Turn")}");
        }

        private void ApplyDominantHandSetting()
        {
            var localCue = FindFirstObjectByType<CueStrikeCue>();
            if (localCue != null)
            {
                // Find Left/Right XR Controller attachment targets
                var rightHand = GameObject.Find("RightHand Controller") ?? GameObject.Find("Right Controller") ?? GameObject.Find("RightHand");
                var leftHand = GameObject.Find("LeftHand Controller") ?? GameObject.Find("Left Controller") ?? GameObject.Find("LeftHand");

                if (dominantHand == 0 && rightHand != null) // Right Hand Dominant
                {
                    localCue.transform.SetParent(rightHand.transform, false);
                    localCue.transform.localPosition = Vector3.zero;
                    localCue.transform.localRotation = Quaternion.identity;
                }
                else if (dominantHand == 1 && leftHand != null) // Left Hand Dominant
                {
                    localCue.transform.SetParent(leftHand.transform, false);
                    localCue.transform.localPosition = Vector3.zero;
                    localCue.transform.localRotation = Quaternion.identity;
                }
            }

            // Wire dominant hand to VRInputManager
            var vrInputManager = CueStrikeVRInputManager.Instance;
            if (vrInputManager != null)
            {
                var handType = (dominantHand == 0)
                    ? CueStrikeVRInputManager.HandType.Right
                    : CueStrikeVRInputManager.HandType.Left;
                vrInputManager.DominantHand = handType;
                Debug.Log($"[CueStrike Settings] Dominant Hand sent to VRInputManager: {handType}");
            }

            Debug.Log($"[CueStrike Settings] Dominant Hand: {(dominantHand == 0 ? "Right Hand" : "Left Hand")}");
        }
    }
}
