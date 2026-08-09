using UnityEngine;
using System.Collections.Generic;

namespace CueStrike.MascotSystem
{
    /// <summary>
    /// Crowd System - Manages spectator crowd behavior and reactions.
    /// </summary>
    public class CueStrikeCrowdSystem : MonoBehaviour
    {
        [Header("Crowd Spawning")]
        [SerializeField] private GameObject _spectatorPrefab;
        [SerializeField] private int _crowdDensity = 50;
        [SerializeField] private float _spawnRadius = 10f;
        [SerializeField] private Transform _tableCenter;

        [Header("Reaction Settings")]
        [SerializeField] private float _applauseIntensity = 1.0f;
        [SerializeField] private float _cheerProbability = 0.3f;
        [SerializeField] private float _reactionDelay = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource _ambientSource;
        [SerializeField] private AudioClip[] _applauseClips;
        [SerializeField] private AudioClip[] _cheerClips;
        [SerializeField] private AudioClip[] _murmurClips;
        [SerializeField] private AudioClip[] _gaspClips;

        [Header("Animation")]
        [SerializeField] private string _idleAnimation = "Idle";
        [SerializeField] private string _clapAnimation = "Clap";
        [SerializeField] private string _cheerAnimation = "Cheer";
        [SerializeField] private string _gaspAnimation = "Gasp";
        [SerializeField] private string _standAnimation = "Stand";

        [Header("Performance")]
        [SerializeField] private int _maxActiveSpectators = 100;
        [SerializeField] private float _lodDistance = 20f;

        private List<GameObject> _spectators = new List<GameObject>();
        private List<Animator> _spectatorAnimators = new List<Animator>();
        private bool _isInitialized = false;
        private float _ambientTimer = 0f;
        private float _ambientInterval = 15f;

        public int ActiveSpectatorCount => _spectators.Count;
        public float ApplauseIntensity
        {
            get => _applauseIntensity;
            set => _applauseIntensity = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            if (_tableCenter == null)
            {
                _tableCenter = transform;
            }
        }

        private void Start()
        {
            InitializeCrowd();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            _ambientTimer += Time.deltaTime;
            if (_ambientTimer >= _ambientInterval)
            {
                PlayAmbientMurmur();
                _ambientTimer = 0f;
            }

            UpdateLOD();
        }

        private void InitializeCrowd()
        {
            if (_spectatorPrefab == null)
            {
                Debug.LogWarning("[CrowdSystem] No spectator prefab assigned. Creating default spectators.");
                CreateDefaultSpectators();
            }
            else
            {
                SpawnSpectators();
            }

            _isInitialized = true;
            Debug.Log($"[CrowdSystem] Initialized with {_spectators.Count} spectators.");
        }

        private void CreateDefaultSpectators()
        {
            for (int i = 0; i < _crowdDensity; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                GameObject spectator = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                spectator.name = $"Spectator_{i}";
                spectator.transform.position = spawnPos;
                spectator.transform.rotation = Quaternion.LookRotation(_tableCenter.position - spawnPos);
                spectator.transform.SetParent(transform);

                // Add a simple material
                Renderer renderer = spectator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.color = new Color(
                        Random.Range(0.2f, 0.8f),
                        Random.Range(0.2f, 0.8f),
                        Random.Range(0.2f, 0.8f)
                    );
                    renderer.material = mat;
                }

                // Scale to human-like proportions
                spectator.transform.localScale = new Vector3(0.5f, 1.8f, 0.5f);

                // Add animator
                Animator anim = spectator.AddComponent<Animator>();
                _spectatorAnimators.Add(anim);

                _spectators.Add(spectator);
            }
        }

        private void SpawnSpectators()
        {
            for (int i = 0; i < _crowdDensity && _spectators.Count < _maxActiveSpectators; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                GameObject spectator = Instantiate(_spectatorPrefab, spawnPos, Quaternion.LookRotation(_tableCenter.position - spawnPos), transform);
                spectator.name = $"Spectator_{i}";

                Animator anim = spectator.GetComponent<Animator>();
                if (anim != null)
                {
                    _spectatorAnimators.Add(anim);
                }

                _spectators.Add(spectator);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(_spawnRadius * 0.5f, _spawnRadius);
            Vector3 pos = _tableCenter.position + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
            return pos;
        }

        private void UpdateLOD()
        {
            if (_tableCenter == null) return;

            for (int i = 0; i < _spectators.Count; i++)
            {
                if (_spectators[i] == null) continue;

                float dist = Vector3.Distance(_spectators[i].transform.position, _tableCenter.position);
                bool shouldBeActive = dist <= _lodDistance;

                if (_spectators[i].activeSelf != shouldBeActive)
                {
                    _spectators[i].SetActive(shouldBeActive);
                }
            }
        }

        // ============================================================
        // Public API for Game Events
        // ============================================================

        /// <summary>
        /// Trigger crowd reaction to a shot.
        /// </summary>
        /// <param name="shotQuality">Quality of the shot (0-1)</param>
        public void OnShotResult(float shotQuality)
        {
            if (!_isInitialized) return;

            float intensity = Mathf.Clamp01(shotQuality * _applauseIntensity);
            TriggerApplause(intensity);

            if (shotQuality > 0.7f && Random.value < _cheerProbability)
            {
                TriggerCheer();
            }
        }

        /// <summary>
        /// Trigger crowd reaction to a foul.
        /// </summary>
        public void OnFoulCommitted()
        {
            if (!_isInitialized) return;

            TriggerGasp();
            Invoke(nameof(PlayMurmur), _reactionDelay);
        }

        /// <summary>
        /// Trigger crowd reaction to a century break.
        /// </summary>
        public void OnCenturyBreak(int breakValue)
        {
            if (!_isInitialized) return;

            TriggerCheer();
            Invoke(nameof(TriggerApplause), 1f);
        }

        /// <summary>
        /// Trigger crowd reaction to a maximum break (147).
        /// </summary>
        public void OnMaximumBreak()
        {
            if (!_isInitialized) return;

            TriggerStandingOvation();
        }

        /// <summary>
        /// Trigger crowd reaction to frame start.
        /// </summary>
        public void OnFrameStart()
        {
            if (!_isInitialized) return;

            PlayAmbientMurmur();
        }

        /// <summary>
        /// Trigger crowd reaction to frame end.
        /// </summary>
        public void OnFrameEnd(int winnerIndex)
        {
            if (!_isInitialized) return;

            TriggerApplause(0.8f);
        }

        /// <summary>
        /// Trigger crowd reaction to match end.
        /// </summary>
        public void OnMatchEnd(int winnerIndex)
        {
            if (!_isInitialized) return;

            TriggerStandingOvation();
        }

        // ============================================================
        // Reaction Methods
        // ============================================================

        private void TriggerApplause(float intensity)
        {
            if (_applauseClips != null && _applauseClips.Length > 0 && _ambientSource != null)
            {
                AudioClip clip = _applauseClips[Random.Range(0, _applauseClips.Length)];
                _ambientSource.volume = intensity;
                _ambientSource.PlayOneShot(clip);
            }

            foreach (Animator anim in _spectatorAnimators)
            {
                if (anim != null && Random.value < intensity)
                {
                    anim.SetTrigger(_clapAnimation);
                }
            }
        }

        private void TriggerCheer()
        {
            if (_cheerClips != null && _cheerClips.Length > 0 && _ambientSource != null)
            {
                AudioClip clip = _cheerClips[Random.Range(0, _cheerClips.Length)];
                _ambientSource.PlayOneShot(clip);
            }

            foreach (Animator anim in _spectatorAnimators)
            {
                if (anim != null && Random.value < 0.5f)
                {
                    anim.SetTrigger(_cheerAnimation);
                }
            }
        }

        private void TriggerGasp()
        {
            if (_gaspClips != null && _gaspClips.Length > 0 && _ambientSource != null)
            {
                AudioClip clip = _gaspClips[Random.Range(0, _gaspClips.Length)];
                _ambientSource.PlayOneShot(clip);
            }

            foreach (Animator anim in _spectatorAnimators)
            {
                if (anim != null)
                {
                    anim.SetTrigger(_gaspAnimation);
                }
            }
        }

        private void TriggerStandingOvation()
        {
            TriggerCheer();
            Invoke(nameof(TriggerApplause), 0.5f);
            Invoke(nameof(TriggerCheer), 1f);
            Invoke(nameof(TriggerApplause), 2f);

            foreach (Animator anim in _spectatorAnimators)
            {
                if (anim != null)
                {
                    anim.SetTrigger(_standAnimation);
                }
            }
        }

        private void PlayAmbientMurmur()
        {
            if (_murmurClips != null && _murmurClips.Length > 0 && _ambientSource != null)
            {
                AudioClip clip = _murmurClips[Random.Range(0, _murmurClips.Length)];
                _ambientSource.volume = 0.3f;
                _ambientSource.PlayOneShot(clip);
            }
        }

        private void PlayMurmur()
        {
            PlayAmbientMurmur();
        }

        // ============================================================
        // Settings
        // ============================================================

        /// <summary>
        /// Set crowd density (respawns crowd).
        /// </summary>
        public void SetCrowdDensity(int density)
        {
            _crowdDensity = Mathf.Clamp(density, 0, _maxActiveSpectators);
            ClearCrowd();
            InitializeCrowd();
        }

        /// <summary>
        /// Enable or disable crowd reactions.
        /// </summary>
        public void SetCrowdEnabled(bool enabled)
        {
            _isInitialized = enabled;
            foreach (GameObject spec in _spectators)
            {
                if (spec != null)
                {
                    spec.SetActive(enabled);
                }
            }
        }

        private void ClearCrowd()
        {
            foreach (GameObject spec in _spectators)
            {
                if (spec != null)
                {
                    Destroy(spec);
                }
            }
            _spectators.Clear();
            _spectatorAnimators.Clear();
        }

        private void OnDestroy()
        {
            ClearCrowd();
        }
    }
}