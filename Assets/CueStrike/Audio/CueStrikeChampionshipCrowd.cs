using UnityEngine;

namespace CueStrike.Audio
{
    /// <summary>
    /// Manages stadium/championship crowd audio responses (applause, sighs, cheers).
    /// Subscribes to CueStrikeRulesManager events to trigger appropriate crowd reactions.
    /// </summary>
    public class CueStrikeChampionshipCrowd : MonoBehaviour
    {
        [Header("Crowd Audio Clips")]
        [Tooltip("Applause clip played when a ball is successfully potted.")]
        public AudioClip crowdApplause;
        [Tooltip("Groan/sigh clip played when a player commits a foul.")]
        public AudioClip crowdGroan;
        [Tooltip("Ambient murmur clip played continuously in the background.")]
        public AudioClip crowdMurmur;
        [Tooltip("Collective gasp/shock clip played on a near-miss shot (NearMissDetector).")]
        public AudioClip[] crowdGaspClips;

        [Header("Settings")]
        [Range(0f, 1f)]
        public float ambientVolume = 0.15f;
        [Range(0f, 1f)]
        public float reactionVolume = 0.7f;

        private AudioSource _ambientSource;
        private AudioSource _reactionSource;
        private CueStrikeRulesManager _rules;

        private void Awake()
        {
            _rules = FindFirstObjectByType<CueStrikeRulesManager>();

            // Setup audio sources
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
            _ambientSource.volume = ambientVolume;

            _reactionSource = gameObject.AddComponent<AudioSource>();
            _reactionSource.loop = false;
            _reactionSource.playOnAwake = false;
            _reactionSource.volume = reactionVolume;
        }

        private void Start()
        {
            if (crowdMurmur != null)
            {
                _ambientSource.clip = crowdMurmur;
                _ambientSource.Play();
            }
        }

        private void OnEnable()
        {
            if (_rules != null)
            {
                _rules.OnStatusMessage += HandleStatusMessage;
            }
        }

        private void OnDisable()
        {
            if (_rules != null)
            {
                _rules.OnStatusMessage -= HandleStatusMessage;
            }
        }

        private void HandleStatusMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            string lowerMessage = message.ToLower();

            // Detect if a ball was potted
            if (lowerMessage.Contains("potted ball"))
            {
                PlayReaction(crowdApplause);
                Debug.Log("[CueStrike Crowd] Crowd Applauds!");
            }
            // Detect if a foul occurred
            else if (lowerMessage.Contains("foul"))
            {
                PlayReaction(crowdGroan);
                Debug.Log("[CueStrike Crowd] Crowd Groans (Sigh)...");
            }
        }

        private void PlayReaction(AudioClip clip)
        {
            if (clip == null || _reactionSource == null) return;
            
            // Randomize pitch slightly for more natural crowd responses
            _reactionSource.pitch = Random.Range(0.92f, 1.08f);
            _reactionSource.PlayOneShot(clip, reactionVolume);
        }

        /// <summary>
        /// Plays a collective crowd gasp at the given world position (3D spatial audio).
        /// Called by NearMissDetector when a ball barely misses a pocket.
        /// </summary>
        public void PlayGasp(Vector3 position)
        {
            if (crowdGaspClips == null || crowdGaspClips.Length == 0 || _reactionSource == null) return;

            AudioClip clip = crowdGaspClips[Random.Range(0, crowdGaspClips.Length)];
            if (clip == null) return;

            // This PlayGasp is for a 2D UI/general event, not a specific spatialized near miss.
            // So we use PlaySound from AudioManager, which handles muting and general volume.
            // If spatialized gasp is needed, NearMissDetector already calls AudioManager's PlayNearMissGasp.

            if (CueStrikeAudioManager.Instance != null)
            {
                CueStrikeAudioManager.Instance.PlaySound(clip, reactionVolume * 0.85f);
                Debug.Log("[CueStrike Crowd] General Crowd Gasp triggered.");
            }
        }
    }
}
