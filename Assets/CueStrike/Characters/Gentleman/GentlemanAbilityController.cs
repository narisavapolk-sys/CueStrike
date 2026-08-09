using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CueStrike.Characters.Gentleman
{
    /// <summary>
    /// THE GENTLEMAN — World Champion ability controller.
    /// Century Break spotlight + procedural audience synthesis.
    /// </summary>
    public class GentlemanAbilityController : MonoBehaviour
    {
        [Header("Gameplay Settings")]
        public int applauseThreshold = 4;
        public float pocketHeightThreshold = 0.77f;
        public int pointsPerPocket = 7;

        [Header("Spotlight Adjustments")]
        [Tooltip("Lights to brighten when reaching Century Break (100+ points)")]
        public List<Light> targetSpotlights = new List<Light>();
        public float centurySpotlightMultiplier = 1.35f;

        [Header("Procedural Audio Settings")]
        public bool enableAudienceSynth = true;
        [Range(0.01f, 1.0f)]
        public float masterVolume = 0.6f;

        [Header("Editor Mock Keys")]
        [Tooltip("Press this key in editor to mock pocketing a ball")]
        public KeyCode mockPocketKey = KeyCode.Alpha4;
        [Tooltip("Press this key to mock missing a shot")]
        public KeyCode mockMissKey = KeyCode.R;

        // Internal State
        private int _pocketedBallsCount = 0;
        private int _currentBreakScore = 0;
        private GentlemanAudienceSynth _synth;
        private List<Rigidbody> _trackedBalls = new List<Rigidbody>();
        private HashSet<int> _pocketedInstanceIds = new HashSet<int>();
        private Dictionary<Light, float> _originalLightIntensities = new Dictionary<Light, float>();
        private bool _isCenturySpotlightActive = false;

        public int CurrentBreakScore => _currentBreakScore;
        public int PocketedCount => _pocketedBallsCount;

        void Awake()
        {
            _pocketedBallsCount = 0;
            _currentBreakScore = 0;

            if (enableAudienceSynth)
            {
                _synth = gameObject.AddComponent<GentlemanAudienceSynth>();
                _synth.masterVolume = masterVolume;
            }
        }

        void Start()
        {
            FindAllTableBalls();
            FindSceneLights();
        }

        void Update()
        {
            if (_trackedBalls.Count == 0)
                FindAllTableBalls();

            HandleInputs();
            DetectPocketedBalls();
            UpdateGlowLights();
        }

        private void FindAllTableBalls()
        {
            _trackedBalls.Clear();
            Rigidbody[] bodies = FindObjectsOfType<Rigidbody>();
            foreach (var rb in bodies)
            {
                if (rb.name.ToLower().Contains("ball") || rb.name.ToLower().Contains("cue"))
                    _trackedBalls.Add(rb);
            }
        }

        private void FindSceneLights()
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
            {
                if (l.type == LightType.Spot || l.name.ToLower().Contains("key") || l.name.ToLower().Contains("spot"))
                {
                    if (!targetSpotlights.Contains(l))
                        targetSpotlights.Add(l);
                }
            }

            _originalLightIntensities.Clear();
            foreach (var l in targetSpotlights)
            {
                if (l != null)
                    _originalLightIntensities[l] = l.intensity;
            }
        }

        private void HandleInputs()
        {
            if (Input.GetKeyDown(mockPocketKey)) OnBallPocketedMock();
            if (Input.GetKeyDown(mockMissKey)) OnMissMock();
        }

        private void DetectPocketedBalls()
        {
            for (int i = _trackedBalls.Count - 1; i >= 0; i--)
            {
                Rigidbody ball = _trackedBalls[i];
                if (ball == null) { _trackedBalls.RemoveAt(i); continue; }

                int id = ball.gameObject.GetInstanceID();

                if (ball.transform.position.y < pocketHeightThreshold && !_pocketedInstanceIds.Contains(id))
                {
                    _pocketedInstanceIds.Add(id);
                    OnBallPocketed();
                }
                else if (ball.transform.position.y >= pocketHeightThreshold && _pocketedInstanceIds.Contains(id))
                {
                    _pocketedInstanceIds.Remove(id);
                }
            }
        }

        public void OnBallPocketed()
        {
            _pocketedBallsCount++;
            _currentBreakScore += pointsPerPocket;

            Debug.Log($"[Gentleman] Pot Success! Break: {_currentBreakScore} | Progress: {_pocketedBallsCount}/{applauseThreshold}");

            if (_pocketedBallsCount >= applauseThreshold)
            {
                _pocketedBallsCount = 0;
                TriggerRoundOfApplause();
            }

            if (_synth != null)
                _synth.UpdateBreakEscalation(_currentBreakScore);
        }

        private void OnBallPocketedMock()
        {
            Debug.Log("[Gentleman] Mock Pot triggered.");
            OnBallPocketed();
        }

        public void OnMiss()
        {
            if (_currentBreakScore > 0)
            {
                Debug.Log($"[Gentleman] Break finished at {_currentBreakScore} points!");
                if (_synth != null) _synth.TriggerStadiumGroan();
            }

            _currentBreakScore = 0;
            _pocketedBallsCount = 0;
            _isCenturySpotlightActive = false;
        }

        private void OnMissMock()
        {
            Debug.Log("[Gentleman] Mock Miss triggered.");
            OnMiss();
        }

        private void TriggerRoundOfApplause()
        {
            if (_synth != null) _synth.TriggerApplauseApplause();
            Debug.Log("[Gentleman] *Audience applauds!*");
        }

        private void UpdateGlowLights()
        {
            bool shouldGlow = _currentBreakScore >= 100;
            if (shouldGlow && !_isCenturySpotlightActive)
            {
                _isCenturySpotlightActive = true;
                Debug.Log("[Gentleman] CENTURY BREAK! Spotlights flare!");
            }

            foreach (var l in targetSpotlights)
            {
                if (l == null) continue;
                float originalVal = _originalLightIntensities.ContainsKey(l) ? _originalLightIntensities[l] : 1f;
                float targetIntensity = _isCenturySpotlightActive ? originalVal * centurySpotlightMultiplier : originalVal;
                l.intensity = Mathf.Lerp(l.intensity, targetIntensity, Time.deltaTime * 2.0f);
            }
        }
    }

    /// <summary>
    /// Procedural audience synthesizer for Gentleman character.
    /// Generates applause, ambient hum, and disappointment groans.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GentlemanAudienceSynth : MonoBehaviour
    {
        public enum SynthState { QuietStadium, ApplauseRoll, StadiumGroan }
        public SynthState state = SynthState.QuietStadium;
        public float masterVolume = 0.6f;

        private AudioSource _source;
        private double _sampleRate;
        private float _applauseVolume = 0f;
        private float _ambientVolume = 0.12f;
        private float _sighVolume = 0f;
        private float _sighPitch = 200f;
        private float[] _clapTimers = new float[16];
        private float[] _clapRates = new float[16];
        private float[] _clapPhases = new float[16];
        private System.Random _rand = new System.Random();

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 1f;

            _sampleRate = AudioSettings.outputSampleRate;
            if (_sampleRate <= 0) _sampleRate = 48000;

            for (int i = 0; i < 16; i++)
            {
                _clapTimers[i] = (float)_rand.NextDouble();
                _clapRates[i] = 7f + (float)_rand.NextDouble() * 8f;
                _clapPhases[i] = 0f;
            }

            _source.clip = null;
            _source.Play();
        }

        public void UpdateBreakEscalation(int breakScore)
        {
            if (breakScore < 20) _ambientVolume = 0.12f;
            else if (breakScore < 50) _ambientVolume = 0.22f;
            else if (breakScore < 100) _ambientVolume = 0.35f;
            else { _ambientVolume = 0.5f; TriggerApplauseApplause(); }
        }

        public void TriggerApplauseApplause()
        {
            state = SynthState.ApplauseRoll;
            _applauseVolume = 0.85f;
            _sighVolume = 0f;
        }

        public void TriggerStadiumGroan()
        {
            state = SynthState.StadiumGroan;
            _sighVolume = 0.9f;
            _sighPitch = 220f;
            _applauseVolume = 0f;
            _ambientVolume = 0.12f;
        }

        void Update()
        {
            if (state == SynthState.ApplauseRoll)
            {
                _applauseVolume = Mathf.Lerp(_applauseVolume, 0f, Time.deltaTime * 0.45f);
                if (_applauseVolume < 0.01f) state = SynthState.QuietStadium;
            }

            if (state == SynthState.StadiumGroan)
            {
                _sighPitch = Mathf.Lerp(_sighPitch, 110f, Time.deltaTime * 4f);
                _sighVolume = Mathf.Lerp(_sighVolume, 0f, Time.deltaTime * 2f);
                if (_sighVolume < 0.01f) state = SynthState.QuietStadium;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < 16; i++)
            {
                _clapTimers[i] += dt;
                _clapPhases[i] = Mathf.Max(0f, _clapPhases[i] - dt * 25f);
                if (_clapTimers[i] >= (1f / _clapRates[i]))
                {
                    _clapTimers[i] = 0f;
                    _clapPhases[i] = 1.0f;
                }
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                float white = (float)(_rand.NextDouble() * 2.0 - 1.0);
                float crowdHum = white * _ambientVolume * 0.15f;

                float clapsCombined = 0f;
                for (int c = 0; c < 16; c++)
                {
                    float clapEnvelope = _clapPhases[c];
                    float clapNoise = (float)(_rand.NextDouble() * 2.0 - 1.0) * clapEnvelope;
                    clapsCombined += clapNoise * 0.12f;
                }
                float applauseSignal = clapsCombined * _applauseVolume;

                float sighRes = Mathf.Sin(_sighPitch * (float)(i / _sampleRate));
                float sighSignal = Mathf.Lerp(white, sighRes, 0.45f) * _sighVolume * 0.45f;

                float output = (crowdHum + applauseSignal + sighSignal) * masterVolume;

                for (int ch = 0; ch < channels; ch++)
                    data[i + ch] = output;
            }
        }
    }
}