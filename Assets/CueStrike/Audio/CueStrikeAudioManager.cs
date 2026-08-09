using System;
using UnityEngine;

namespace CueStrike.Audio
{
    public class CueStrikeAudioManager : MonoBehaviour
    {
    public static CueStrikeAudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip hitSoft;
    public AudioClip hitMedium;
    public AudioClip hitHard;
    public AudioClip cushionHit;
    public AudioClip pocketHit;
    public AudioClip pocketRollClip; // ball rolling through the wooden return track
    public AudioClip nearMissGasp;   // crowd gasp for near-miss shots
    public AudioClip ambientRoom;    // per-room ambient (e.g. AC hum, air, murmur)
    public AudioClip chalkDust;
    public AudioClip miscued;
    public AudioClip ambientLoungeMusic;
    public AudioClip whooshShot; // Power Shot whoosh sound
    public AudioClip menuClick; // Sound for UI menu clicks
    public AudioClip menuHover; // Sound for UI menu hover

    [Header("Settings")]
    public float volume = 1f;
    public bool muted = false;

    private AudioSource audioSource;
    private AudioSource ambientMusicSource;

    public event Action<bool> OnMuteChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        ambientMusicSource = gameObject.AddComponent<AudioSource>();
        ambientMusicSource.loop = true;
        ambientMusicSource.playOnAwake = false;
        ambientMusicSource.spatialBlend = 0f; // 2D ambient
        ambientMusicSource.volume = 0.2f;
    }

    void Start()
    {
        if (ambientLoungeMusic != null)
        {
            ambientMusicSource.clip = ambientLoungeMusic;
            if (!muted)
            {
                ambientMusicSource.Play();
            }
        }
    }

    public bool IsMuted => muted;

    public void ToggleMute()
    {
        SetMute(!muted);
    }

    public void SetMute(bool value)
    {
        if (muted == value) return;
        muted = value;
        if (ambientMusicSource != null)
        {
            if (muted) ambientMusicSource.Pause();
            else if (ambientLoungeMusic != null) ambientMusicSource.Play();
        }
        OnMuteChanged?.Invoke(muted);
    }

    public void PlaySound(AudioClip clip, float gain = 1f)
    {
        if (muted || clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(gain * volume));
    }

    public void PlayBallHit(float intensity, bool cushionImpact = false)
    {
        if (audioSource == null) return;

        // Modulate pitch dynamically for realistic variety
        audioSource.pitch = UnityEngine.Random.Range(0.88f, 1.12f);

        if (cushionImpact)
        {
            PlaySound(cushionHit, Mathf.Clamp01(intensity * 0.08f));
            audioSource.pitch = 1f; // reset
            return;
        }

        AudioClip clip = hitSoft;
        if (intensity > 8f) clip = hitHard;
        else if (intensity > 4f) clip = hitMedium;

        PlaySound(clip, Mathf.Clamp01(intensity * 0.06f));
        audioSource.pitch = 1f; // reset
    }

    public void PlayPocket()
    {
        PlaySound(pocketHit, 1f);
    }

    /// <summary>
    /// Plays the pocket drop sound at a specific world position (3D spatial audio).
    /// Used by PocketSoundDetector.
    /// </summary>
    public void PlayPocketAt(Vector3 position)
    {
        if (muted || pocketHit == null) return;
        AudioSource.PlayClipAtPoint(pocketHit, position, Mathf.Clamp01(volume));

        if (pocketRollClip != null)
        {
            // Rolling track sound, slightly delayed for realism.
            StartCoroutine(PlayRollingAt(position));
        }
    }

    /// <summary>
    /// Plays the crowd gasp at a specific world position (3D spatial audio).
    /// Used by NearMissDetector.
    /// </summary>
    public void PlayNearMissGasp(Vector3 position)
    {
        if (muted || nearMissGasp == null) return;
        AudioSource.PlayClipAtPoint(nearMissGasp, position, Mathf.Clamp01(volume * 0.8f));
    }

    private System.Collections.IEnumerator PlayRollingAt(Vector3 position)
    {
        yield return new WaitForSeconds(0.15f);
        if (pocketRollClip != null)
        {
            AudioSource.PlayClipAtPoint(pocketRollClip, position, Mathf.Clamp01(volume * 0.6f));
        }
    }

    public void PlayChalk()
    {
        PlaySound(chalkDust, 0.7f);
    }

    public void PlayMiscue()
    {
        PlaySound(miscued, 0.9f);
    }

    /// <summary>
    /// Play Whoosh sound for Power Shot (intensity 0..1)
    /// </summary>
    public void PlayWhoosh(float intensity)
    {
        if (audioSource == null || whooshShot == null || muted) return;

        audioSource.pitch = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(intensity));
        PlaySound(whooshShot, Mathf.Clamp01(0.5f + intensity * 0.5f));
        audioSource.pitch = 1f; // reset
    }

    public void PlayMenuClick()
    {
        PlaySound(menuClick, 1f);
    }

    public void PlayMenuHover()
    {
        PlaySound(menuHover, 0.5f);
    }

    /// <summary>
    /// Play the per-room ambient clip (looped). Called by RoomManager on scene load.
    /// </summary>
    public void PlayAmbientRoom()
    {
        if (muted || ambientRoom == null || ambientMusicSource == null) return;
        
        ambientMusicSource.clip = ambientRoom;
        ambientMusicSource.loop = true;
        if (!muted)
        {
            ambientMusicSource.Play();
        }
    }
}
}
