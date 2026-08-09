using System;
using UnityEngine;

namespace CueStrike.Audio
{
    /// <summary>
    /// Handles 3D spatialized billiard sound effects with dynamic pitch and volume scaling.
    /// Spawns localized sound sources at ball impact points to maximize VR immersion.
    /// </summary>
    public class CueStrikeDynamicPhysicsSFX : MonoBehaviour
    {
        public static CueStrikeDynamicPhysicsSFX Instance { get; private set; }

        /// <summary>
        /// Raised whenever a ball collision is detected (position, relativeVelocity, isCushion).
        /// BallSoundController subscribes to this to play randomized real .wav clips.
        /// </summary>
        public static event Action<Vector3, float, bool> OnBallHit;

        [Header("Tuning")]
        [Range(0.1f, 2.0f)]
        public float pitchMin = 0.88f;
        [Range(0.1f, 2.0f)]
        public float pitchMax = 1.12f;

        private void Awake()
        {
            Instance = this;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if the collision involves a billiard ball (e.g., by tag or component)
            // You might need more specific logic to determine if it's a "ball on ball" or "ball on cushion" hit.
            if (collision.gameObject.CompareTag("Ball"))
            {
                // Calculate relative velocity and determine if it's a cushion impact
                float relativeVelocity = collision.relativeVelocity.magnitude;
                bool isCushion = collision.gameObject.CompareTag("Cushion"); // Assuming cushions have a "Cushion" tag

                // Play the sound
                Play3DHit(collision.contacts[0].point, relativeVelocity, isCushion);
            }
        }

        /// <summary>
        /// Plays a spatialized 3D impact sound at a specific position with velocity-based volume and random pitch.
        /// </summary>
        public static void Play3DHit(Vector3 position, float relativeVelocity, bool isCushion)
        {
            // Notify subscribers (e.g. BallSoundController) with the raw impact data.
            OnBallHit?.Invoke(position, relativeVelocity, isCushion);

            var audioMgr = CueStrikeAudioManager.Instance;
            if (audioMgr == null || audioMgr.muted) return;

            // Choose appropriate clip
            AudioClip clip = audioMgr.hitSoft;
            if (isCushion)
            {
                clip = audioMgr.cushionHit;
            }
            else
            {
                if (relativeVelocity > 8f) clip = audioMgr.hitHard;
                else if (relativeVelocity > 4f) clip = audioMgr.hitMedium;
            }

            if (clip == null) return;

            // Spawn a temporary spatialized 3D audio source object
            GameObject sfxGO = new GameObject("Temp3D_SFX");
            sfxGO.transform.position = position;
            AudioSource source = sfxGO.AddComponent<AudioSource>();

            source.clip = clip;
            source.spatialBlend = 1.0f; // Full 3D spatial sound
            source.minDistance = 1.0f;
            source.maxDistance = 15.0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;

            // Modulate pitch and volume based on collision strength
            source.pitch = UnityEngine.Random.Range(Instance != null ? Instance.pitchMin : 0.9f, Instance != null ? Instance.pitchMax : 1.1f);
            float volScale = isCushion ? relativeVelocity * 0.08f : relativeVelocity * 0.06f;
            source.volume = Mathf.Clamp(volScale * audioMgr.volume, 0.05f, 1.0f);

            source.Play();

            // Destroy the temporary object when done
            Destroy(sfxGO, clip.length + 0.1f);
        }
    }
}
