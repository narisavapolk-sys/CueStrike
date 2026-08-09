#if CUESTRIKE_NORMCORE
using UnityEngine;
using Normal.Realtime;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Coordinates voice chat levels and microphone states via Normcore RealtimeAvatarVoice.
    /// Supports muting the local user's microphone and silencing the remote opponent.
    /// </summary>
    public class CueStrikeVoiceManager : MonoBehaviour
    {
        public static CueStrikeVoiceManager Instance { get; private set; }

        private bool _isMicMuted = false;
        private bool _isOpponentMuted = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Read defaults
            _isMicMuted = PlayerPrefs.GetInt("CueStrike_MuteMic", 0) == 1;
            _isOpponentMuted = PlayerPrefs.GetInt("CueStrike_MuteOpponent", 0) == 1;
        }

        private void Start()
        {
            ApplyVoiceSettings();
        }

        /// <summary>
        /// Mutes or unmutes the local player's microphone stream.
        /// </summary>
        public void SetMicMute(bool muteState)
        {
            _isMicMuted = muteState;
            PlayerPrefs.SetInt("CueStrike_MuteMic", muteState ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVoiceSettings();
        }

        /// <summary>
        /// Mutes or unmutes the incoming audio volume of the opponent.
        /// </summary>
        public void SetOpponentMute(bool muteState)
        {
            _isOpponentMuted = muteState;
            PlayerPrefs.SetInt("CueStrike_MuteOpponent", muteState ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVoiceSettings();
        }

        /// <summary>
        /// Applies the volume and mute states to active Normcore Realtime voice components.
        /// </summary>
        public void ApplyVoiceSettings()
        {
            // Find voice receivers and transmitters in the scene
            var voiceTransmitters = FindObjectsByType<RealtimeAvatarVoice>(FindObjectsSortMode.None);
            foreach (var voice in voiceTransmitters)
            {
                if (voice.realtimeView.isOwnedLocally)
                {
                    // Local voice transmitter: toggle microphone capture
                    voice.mute = _isMicMuted;
                    Debug.Log($"[CueStrike Voice] Mic transmission mute set to: {_isMicMuted}");
                }
                else
                {
                    // Opponent voice receiver: toggle incoming audio playback volume
                    var audioSource = voice.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.mute = _isOpponentMuted;
                    }
                    Debug.Log($"[CueStrike Voice] Opponent playback mute set to: {_isOpponentMuted}");
                }
            }
        }
    }
}
#else
using UnityEngine;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Fallback script to explain Voice Mute controls when Normcore SDK is not present.
    /// </summary>
    public class CueStrikeVoiceManager : MonoBehaviour
    {
        public static CueStrikeVoiceManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SetMicMute(bool muteState)
        {
            PlayerPrefs.SetInt("CueStrike_MuteMic", muteState ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[CueStrike Voice Fallback] Set Mic Mute preference: {muteState}");
        }

        public void SetOpponentMute(bool muteState)
        {
            PlayerPrefs.SetInt("CueStrike_MuteOpponent", muteState ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[CueStrike Voice Fallback] Set Opponent Mute preference: {muteState}");
        }
    }
}
#endif
