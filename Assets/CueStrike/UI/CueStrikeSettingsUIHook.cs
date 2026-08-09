using UnityEngine;
using UnityEngine.UI;

namespace CueStrike
{
    /// <summary>
    /// UI Hook script that attaches to the MainMenu scene options UI elements
    /// and feeds user inputs directly into the CueStrikeSettingsManager.
    /// </summary>
    public class CueStrikeSettingsUIHook : MonoBehaviour
    {
        [Header("Settings UI Controls")]
        public Slider volumeSlider;
        public Toggle comfortToggle;
        public Dropdown turnDropdown;
        public Dropdown handDropdown;

        private void Start()
        {
            LoadCurrentSettingsToUI();

            // Wire Listeners to save on user change
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(delegate { OnSettingsChanged(); });
            if (comfortToggle != null) comfortToggle.onValueChanged.AddListener(delegate { OnSettingsChanged(); });
            if (turnDropdown != null) turnDropdown.onValueChanged.AddListener(delegate { OnSettingsChanged(); });
            if (handDropdown != null) handDropdown.onValueChanged.AddListener(delegate { OnSettingsChanged(); });
        }

        private void LoadCurrentSettingsToUI()
        {
            var manager = CueStrikeSettingsManager.Instance;
            if (manager == null) return;

            manager.LoadSettings();

            if (volumeSlider != null) volumeSlider.value = manager.masterVolume;
            if (comfortToggle != null) comfortToggle.isOn = manager.enableComfortVignette;
            if (turnDropdown != null) turnDropdown.value = manager.turnMode;
            if (handDropdown != null) handDropdown.value = manager.dominantHand;

            Debug.Log("[CueStrike UI] Loaded current user preferences to settings panel UI.");
        }

        public void OnSettingsChanged()
        {
            var manager = CueStrikeSettingsManager.Instance;
            if (manager == null) return;

            float vol = volumeSlider != null ? volumeSlider.value : 1.0f;
            bool vignette = comfortToggle != null ? comfortToggle.isOn : true;
            int turn = turnDropdown != null ? turnDropdown.value : 0;
            int hand = handDropdown != null ? handDropdown.value : 0;

            manager.SaveSettings(vol, vignette, turn, hand);
        }
    }
}
