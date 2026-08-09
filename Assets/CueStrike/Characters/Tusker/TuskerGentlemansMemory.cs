using UnityEngine;

namespace CueStrike.Characters.Tusker
{
    /// <summary>
    /// Tusker — Gentleman's Memory ability.
    /// Replays last 3 seconds of shot as ghost replay. Learn from mistakes.
    /// </summary>
    public class TuskerGentlemansMemory : MonoBehaviour, ICharacterAbility
    {
        [Header("Memory Settings")]
        public float replayDuration = 3f;
        public KeyCode replayKey = KeyCode.T;

        [Header("Visual")]
        public GameObject ghostPrefab;
        public Material ghostMaterial;

        // State
        private bool _isActive = false;
        private GhostReplayData _lastShotData;
        private GameObject _ghostInstance;

        // Internal replay data structure
        public struct GhostReplayData
        {
            public Vector3[] positions;
            public Quaternion[] rotations;
            public float duration;
            public bool hasData;
        }

        public string AbilityName => "Gentleman's Memory";
        public string AbilityDescription => $"Replay last {replayDuration}s of your shot as ghost. Learn from mistakes.";

        public void OnCharacterSpawned()
        {
            _isActive = true;
            _lastShotData = new GhostReplayData { hasData = false };
            Debug.Log("[Tusker] Gentleman's Memory ready! Press T to replay.");
        }

        public float GetAccuracyModifier() => 0f;
        public float GetPowerModifier() => 1f;
        public float GetSpeedModifier() => 1f;
        public float GetVisibilityBonus() => 0.2f;
        public bool IsAbilityActive() => _isActive;

        /// <summary>
        /// Start recording a shot
        /// </summary>
        public void StartRecording()
        {
            if (!_isActive) return;
            // Recording is triggered automatically
        }

        /// <summary>
        /// Register shot data for replay
        /// </summary>
        public void RegisterShotData(Vector3[] positions, Quaternion[] rotations, float duration)
        {
            _lastShotData = new GhostReplayData
            {
                positions = positions,
                rotations = rotations,
                duration = duration > 0 ? duration : replayDuration,
                hasData = true
            };

            Debug.Log($"[Tusker] Shot recorded ({positions.Length} frames, {duration:F1}s)");
        }

        /// <summary>
        /// Play ghost replay
        /// </summary>
        public void PlayReplay()
        {
            if (!_lastShotData.hasData)
            {
                Debug.Log("[Tusker] No shot data to replay!");
                return;
            }

            if (_ghostInstance != null)
                Destroy(_ghostInstance);

            if (ghostPrefab != null)
            {
                _ghostInstance = Instantiate(ghostPrefab, transform.position, transform.rotation);
                var ghostReplay = _ghostInstance.AddComponent<GhostReplayPlayer>();
                ghostReplay.Initialize(_lastShotData, ghostMaterial);
                Debug.Log("[Tusker] Replaying last shot...");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(replayKey))
                PlayReplay();
        }

        /// <summary>
        /// Register a shot completed
        /// </summary>
        public void RegisterShotComplete()
        {
            // Could capture final position data here
        }
    }

    /// <summary>
    /// Plays back recorded ghost positions
    /// </summary>
    public class GhostReplayPlayer : MonoBehaviour
    {
        private Vector3[] _positions;
        private Quaternion[] _rotations;
        private float _totalDuration;
        private float _timer = 0f;
        private float _playbackSpeed = 1f;
        private bool _isPlaying = false;

        public void Initialize(TuskerGentlemansMemory.GhostReplayData data, Material mat)
        {
            _positions = data.positions;
            _rotations = data.rotations;
            _totalDuration = data.duration;
            _playbackSpeed = _totalDuration > 0 ? _positions.Length / _totalDuration : 1f;
            _timer = 0f;
            _isPlaying = true;

            // Apply ghost material
            if (mat != null)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                    r.material = mat;
            }

            Destroy(gameObject, _totalDuration + 1f);
        }

        void Update()
        {
            if (!_isPlaying || _positions == null || _positions.Length == 0) return;

            _timer += Time.deltaTime;
            int index = Mathf.FloorToInt(_timer * _playbackSpeed);
            index = Mathf.Clamp(index, 0, _positions.Length - 1);

            transform.position = _positions[index];
            transform.rotation = _rotations[index];

            // Fade out near end
            if (_timer >= _totalDuration - 0.5f)
            {
                float alpha = (_totalDuration - _timer) / 0.5f;
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            c.a = alpha;
                            mat.SetColor("_BaseColor", c);
                        }
                    }
                }

                if (_timer >= _totalDuration)
                    _isPlaying = false;
            }
        }
    }
}