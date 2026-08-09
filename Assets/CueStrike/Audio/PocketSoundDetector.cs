using UnityEngine;

namespace CueStrike.Audio
{
    public class PocketSoundDetector : MonoBehaviour
    {
        // This script is placed on a trigger collider inside a pocket.
        // When a billiard ball enters the trigger, it signals the AudioManager to play a pocket sound.

        void OnTriggerEnter(Collider other)
        {
            // Check if the entering collider is a billiard ball (e.g., by tag or component)
            // Assuming balls have a "Ball" tag or a Rigidbody component.
            if (other.CompareTag("Ball") || other.GetComponent<Rigidbody>() != null)
            {
                if (CueStrikeAudioManager.Instance != null)
                {
                    CueStrikeAudioManager.Instance.PlayPocketAt(transform.position);
                }
            }
        }
    }
}