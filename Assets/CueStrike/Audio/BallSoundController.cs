using UnityEngine;

namespace CueStrike.Audio
{
    public class BallSoundController : MonoBehaviour
    {
        // This script will be responsible for playing sounds associated with individual billiard balls.
        // For example, when a ball is hit, when it hits another ball, or when it pockets.

        [SerializeField] private AudioClip ballHitClip; // Sound when this ball hits another ball
        [SerializeField] private AudioClip pocketedClip; // Sound when this ball is pocketed

        private AudioSource audioSource;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D spatial audio
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.maxDistance = 20f; // Adjust as needed
            }
        }

        public void PlayBallHitSound()
        {
            if (ballHitClip != null && CueStrikeAudioManager.Instance != null && !CueStrikeAudioManager.Instance.IsMuted)
            {
                audioSource.PlayOneShot(ballHitClip, CueStrikeAudioManager.Instance.volume);
            }
        }

        public void PlayPocketedSound()
        {
            if (pocketedClip != null && CueStrikeAudioManager.Instance != null && !CueStrikeAudioManager.Instance.IsMuted)
            {
                audioSource.PlayOneShot(pocketedClip, CueStrikeAudioManager.Instance.volume);
            }
        }
    }
}