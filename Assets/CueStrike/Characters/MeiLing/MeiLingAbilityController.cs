using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CueStrike.Characters.MeiLing
{
    /// <summary>
    /// MEI LING — Elegant & Sporty Champion ability controller.
    /// Orbital trajectory lines + resonant audio synthesis.
    /// </summary>
    public class MeiLingAbilityController : MonoBehaviour
    {
        [Header("Orbital Lines Settings")]
        public float orbitalLineDuration = 4.0f;
        public float pocketHeightThreshold = 0.77f;

        [Header("Procedural Audio Settings")]
        public bool enableOrbitalSynth = true;
        [Range(0.01f, 1.0f)]
        public float masterVolume = 0.6f;

        [Header("Editor Mock Keys")]
        public KeyCode mockOrbitKey = KeyCode.Alpha5;
        public KeyCode mockPocketKey = KeyCode.Alpha6;

        private bool _isOrbitActive = false;
        private MeiLingResonantSynth _synth;
        private List<Rigidbody> _trackedBalls = new List<Rigidbody>();
        private HashSet<int> _pocketedInstanceIds = new HashSet<int>();
        private Material _orbitLineMaterial;
        private LineRenderer _orbitLine;

        void Awake()
        {
            if (enableOrbitalSynth)
            {
                _synth = gameObject.AddComponent<MeiLingResonantSynth>();
                _synth.masterVolume = masterVolume;
            }

            SetupOrbitLine();
        }

        void Start()
        {
            FindAllTableBalls();
        }

        void Update()
        {
            if (_trackedBalls.Count == 0)
                FindAllTableBalls();

            HandleInputs();
            UpdateOrbitLine();
        }

        private void SetupOrbitLine()
        {
            GameObject lineObj = new GameObject("OrbitTrajectoryLine");
            lineObj.transform.SetParent(transform, false);
            _orbitLine = lineObj.AddComponent<LineRenderer>();

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _orbitLineMaterial = new Material(shader);
            _orbitLineMaterial.SetColor("_BaseColor", new Color(1f, 0.4f, 0.8f, 0.6f));
            _orbitLineMaterial.SetColor("_Color", new Color(1f, 0.4f, 0.8f, 0.6f));
            _orbitLine.material = _orbitLineMaterial;
            _orbitLine.startWidth = 0.02f;
            _orbitLine.endWidth = 0.005f;
            _orbitLine.positionCount = 30;
            _orbitLine.enabled = false;
        }

        private void FindAllTableBalls()
        {
            _trackedBalls.Clear();
            Rigidbody[] bodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (var rb in bodies)
            {
                if (rb.name.ToLower().Contains("ball") || rb.name.ToLower().Contains("cue"))
                    _trackedBalls.Add(rb);
            }
        }

        private void HandleInputs()
        {
            if (Input.GetKeyDown(mockOrbitKey))
                ToggleOrbitLine();
            if (Input.GetKeyDown(mockPocketKey))
                OnBallPocketedMock();
        }

        public void ToggleOrbitLine()
        {
            _isOrbitActive = !_isOrbitActive;
            _orbitLine.enabled = _isOrbitActive;

            if (_synth != null)
            {
                if (_isOrbitActive)
                    _synth.TriggerResonantSweep();
                else
                    _synth.StopResonantSweep();
            }

            Debug.Log($"[MeiLing] Orbital line {( _isOrbitActive ? "ACTIVATED" : "DEACTIVATED" )}");
        }

        private void UpdateOrbitLine()
        {
            if (!_isOrbitActive || _orbitLine == null) return;

            // Draw a dynamic orbital path around the nearest ball
            Rigidbody target = GetNearestBall();
            if (target == null) { _orbitLine.enabled = false; return; }

            Vector3 center = target.position;
            float radius = 0.15f + Mathf.Sin(Time.time * 0.5f) * 0.03f;
            float heightOffset = 0.05f;

            for (int i = 0; i < _orbitLine.positionCount; i++)
            {
                float t = (float)i / (_orbitLine.positionCount - 1);
                float angle = t * Mathf.PI * 2f + Time.time * 0.8f;
                Vector3 pos = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle * 2f) * 0.02f + heightOffset,
                    Mathf.Sin(angle) * radius
                );
                _orbitLine.SetPosition(i, pos);
            }
        }

        private Rigidbody GetNearestBall()
        {
            Rigidbody nearest = null;
            float minDist = float.MaxValue;

            foreach (var rb in _trackedBalls)
            {
                if (rb == null) continue;
                float dist = Vector3.Distance(transform.position, rb.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = rb;
                }
            }
            return nearest;
        }

        public void OnBallPocketed()
        {
            Debug.Log("[MeiLing] Ball potted! Graceful success.");
            if (_synth != null) _synth.TriggerResonantSweep();
        }

        private void OnBallPocketedMock()
        {
            Debug.Log("[MeiLing] Mock Pot.");
            OnBallPocketed();
        }

        public void OnMiss()
        {
            Debug.Log("[MeiLing] Missed. Stay graceful.");
            _isOrbitActive = false;
            if (_orbitLine != null) _orbitLine.enabled = false;
            if (_synth != null) _synth.StopResonantSweep();
        }
    }

    /// <summary>
    /// Resonant audio synthesizer for Mei Ling character.
    /// Produces high-energy resonant sweeps.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MeiLingResonantSynth : MonoBehaviour
    {
        public float masterVolume = 0.6f;

        private AudioSource _source;
        private double _sampleRate;
        private bool _isSweeping = false;
        private float _sweepFrequency = 440f;
        private float _sweepPhase = 0f;
        private float _sweepAmplitude = 0f;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 1f;

            _sampleRate = AudioSettings.outputSampleRate;
            if (_sampleRate <= 0) _sampleRate = 48000;

            _source.clip = null;
            _source.Play();
        }

        public void TriggerResonantSweep()
        {
            _isSweeping = true;
            _sweepFrequency = 220f;
            _sweepAmplitude = 0.8f;
        }

        public void StopResonantSweep()
        {
            _isSweeping = false;
            _sweepAmplitude = 0f;
        }

        void Update()
        {
            if (_isSweeping)
            {
                _sweepFrequency = Mathf.Lerp(_sweepFrequency, 880f, Time.deltaTime * 2f);
                _sweepAmplitude = Mathf.Lerp(_sweepAmplitude, 0f, Time.deltaTime * 0.5f);
                if (_sweepAmplitude < 0.01f) _isSweeping = false;
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                _sweepPhase += (float)(_sweepFrequency / _sampleRate);
                if (_sweepPhase > 1f) _sweepPhase -= 1f;

                float sample = Mathf.Sin(_sweepPhase * Mathf.PI * 2f) * _sweepAmplitude * 0.3f;
                // Add harmonics for richness
                sample += Mathf.Sin(_sweepPhase * Mathf.PI * 4f) * _sweepAmplitude * 0.1f;
                sample += Mathf.Sin(_sweepPhase * Mathf.PI * 6f) * _sweepAmplitude * 0.05f;

                for (int ch = 0; ch < channels; ch++)
                    data[i + ch] = sample * masterVolume;
            }
        }
    }
}