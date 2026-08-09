//
// CueStrikeRealisticAudioSynth — Procedural Snooker Audio Synthesizer (no clips needed)
// Created by Nari for P'Mong | 2026-07-19
// Phase 1: Realistic Sounds — synthesized ball/cushion/pot/chalk/felt SFX with physical parameters
//
using UnityEngine;

namespace CueStrike.Audio
{
    /// <summary>
    /// Generates realistic snooker sound effects procedurally via short impulse responses
    /// that are fed into an AudioSource buffer. Models ball-ball impact, cushion rubber bounce,
    /// pot rail, chalk tap and felt brushing drag. All values are physically inspired.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CueStrikeRealisticAudioSynth : MonoBehaviour
    {
        // ---------------- TUNABLE PHYSICAL PARAMETERS ----------------
        [Header("Ball–Ball Impact")]
        [Tooltip("Base resonant frequency of a snooker ball (Hz).")]
        public float ballResonanceHz = 2200f;
        [Tooltip("Impact decay time (s).")]
        [Range(0.02f, 0.20f)] public float ballDecay = 0.08f;

        [Header("Cushion Bounce")]
        public float cushionResonanceHz = 180f;
        [Range(0.05f, 0.30f)] public float cushionDecay = 0.14f;

        [Header("Pot / Pocket Drop")]
        public float potResonanceHz = 90f;
        [Range(0.10f, 0.60f)] public float potDecay = 0.28f;

        [Header("Chalk Tap")]
        public float chalkResonanceHz = 4200f;
        [Range(0.01f, 0.10f)] public float chalkDecay = 0.04f;

        [Header("Felt Drag (sustained)")]
        public float feltNoiseLevel = 0.04f;

        // ---------------- INTERNAL ----------------
        private AudioSource _src;
        private int _voiceId = 0;
        private float _playbackTime = 999f;
        private float _playbackDur = 0f;
        private float _curFreq = 2200f;
        private float _curDecay = 0.08f;
        private float _curAmplitude = 0.5f;
        private int _curType = 0; // 0 = ball, 1 = cushion, 2 = pot, 3 = chalk
        private float _feltSeed = 0f;

        private void Awake()
        {
            _src = GetComponent<AudioSource>();
            // Must use empty mixer if none exists, create default
            _src.playOnAwake = false;
            _src.loop = true; // Enable loop so OnAudioFilterRead runs continuously
            _src.spatialBlend = 1.0f; // 3D spatial
            _feltSeed = Random.value * 1000f;
            _src.Play();
        }

        // ---------------- PUBLIC API (called from Physics Manager) ----------------

        /// <summary>Play a ball-ball impact sound. intensity in m/s (typical 0.2..6).</summary>
        public void PlayBallImpact(float intensity)
        {
            _curType = 0;
            _curFreq = ballResonanceHz * (1f + Mathf.Clamp(intensity, 0f, 6f) * 0.04f);
            _curDecay = ballDecay * (1f - Mathf.Clamp01(intensity * 0.08f));
            _curAmplitude = Mathf.Clamp01(0.25f + intensity * 0.12f);
            _playbackTime = 0f;
            _playbackDur = _curDecay * 4f;
            _voiceId++;
        }

        /// <summary>Play a cushion rubber bounce. normalSpeed = impact speed normal to cushion.</summary>
        public void PlayCushionBounce(float normalSpeed)
        {
            _curType = 1;
            _curFreq = cushionResonanceHz * (1f + normalSpeed * 0.03f);
            _curDecay = cushionDecay;
            _curAmplitude = Mathf.Clamp01(0.2f + normalSpeed * 0.1f);
            _playbackTime = 0f;
            _playbackDur = _curDecay * 4f;
            _voiceId++;
        }

        /// <summary>Play a pot (ball dropping into pocket rail).</summary>
        public void PlayPot()
        {
            _curType = 2;
            _curFreq = potResonanceHz;
            _curDecay = potDecay;
            _curAmplitude = 0.85f;
            _playbackTime = 0f;
            _playbackDur = _curDecay * 4f;
            _voiceId++;
        }

        /// <summary>Play a chalk tap sound.</summary>
        public void PlayChalk()
        {
            _curType = 3;
            _curFreq = chalkResonanceHz;
            _curDecay = chalkDecay;
            _curAmplitude = 0.35f;
            _playbackTime = 0f;
            _playbackDur = _curDecay * 4f;
            _voiceId++;
        }

        // ---------------- DSP CORE ----------------

        private void OnAudioFilterRead(float[] data, int channels)
        {
            float sr = AudioSettings.outputSampleRate;
            float dt = 1f / sr;

            for (int n = 0; n < data.Length; n += channels)
            {
                float sample = 0f;

                if (_curType == 0 || _curType == 1 || _curType == 2)
                {
                    // Damped sine (resonant impact)
                    if (_playbackTime < _playbackDur)
                    {
                        float env = Mathf.Exp(-_playbackTime / _curDecay);
                        float s = Mathf.Sin(_playbackTime * _curFreq * 2f * Mathf.PI) * env * _curAmplitude;
                        // Add 2nd harmonic for ball impact
                        if (_curType == 0)
                            s += Mathf.Sin(_playbackTime * _curFreq * 2f * 2f * Mathf.PI) * env * _curAmplitude * 0.25f;
                        // Add click transient at start
                        if (_playbackTime < 0.002f)
                            s += (Random.value * 2f - 1f) * _curAmplitude * (1f - _playbackTime / 0.002f);
                        sample = s;
                    }
                }
                else if (_curType == 3)
                {
                    // Chalk — filtered noise
                    if (_playbackTime < _playbackDur)
                    {
                        float env = Mathf.Exp(-_playbackTime / _curDecay);
                        float noise = (Random.value * 2f - 1f);
                        // Simple low-pass
                        sample = noise * env * _curAmplitude * 0.6f;
                    }
                }

                // Felt brushing noise lightly throughout (when any ball is moving)
                // Controlled by SetFeltActive(true/false)
                sample += _feltCurrent * feltNoiseLevel * (Random.value * 2f - 1f) * 0.25f;

                _playbackTime += dt;

                // Write to every channel
                for (int c = 0; c < channels; c++)
                    data[n + c] = sample;

                // Clamp to prevent clipping
                if (data[n] > 1f) data[n] = 1f;
                else if (data[n] < -1f) data[n] = -1f;
                for (int c = 1; c < channels; c++) data[n + c] = data[n];
            }
        }

        private float _feltCurrent = 0f;
        public void SetFeltActive(bool on) { _feltCurrent = on ? 1f : 0f; }
    }
}