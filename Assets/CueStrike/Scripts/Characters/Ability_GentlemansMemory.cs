using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Tusker — Gentleman's Memory: Ghost replay of last shot (3 seconds).
    /// Hold key to review. Analyzes mistakes.
    /// </summary>
    public class Ability_GentlemansMemory : CueStrikeCharacterAbility
    {
        [Header("Gentleman's Memory Settings")]
        [SerializeField] private float _replayDuration = 3f;
        [SerializeField] private GameObject _ghostPrefab;
        [SerializeField] private Material _ghostMaterial;

        private Queue<Vector3> _positionHistory = new Queue<Vector3>();
        private Queue<Quaternion> _rotationHistory = new Queue<Quaternion>();
        private float _recordInterval = 0.05f;
        private float _lastRecordTime;
        private bool _isReplaying = false;

        public override string AbilityName => "Gentleman's Memory";
        public override string AbilityDescription => "Hold to replay your last 3 seconds as a golden ghost. Analyze your mistakes.";

        protected override void Update()
        {
            base.Update();
            if (!_isReplaying)
            {
                RecordFrame();
            }
        }

        protected override void HandleInput()
        {
            if (Input.GetKey(_activationKey) && !_isOnCooldown && !_isReplaying)
            {
                StartReplay();
            }
            if (Input.GetKeyUp(_activationKey) && _isReplaying)
            {
                StopReplay();
            }
        }

        private void RecordFrame()
        {
            if (Time.time - _lastRecordTime < _recordInterval) return;
            _lastRecordTime = Time.time;

            _positionHistory.Enqueue(transform.position);
            _rotationHistory.Enqueue(transform.rotation);

            int maxFrames = Mathf.CeilToInt(_replayDuration / _recordInterval);
            while (_positionHistory.Count > maxFrames)
            {
                _positionHistory.Dequeue();
                _rotationHistory.Dequeue();
            }
        }

        private void StartReplay()
        {
            _isReplaying = true;
            StartCoroutine(ReplayCoroutine());
        }

        private System.Collections.IEnumerator ReplayCoroutine()
        {
            Vector3[] positions = _positionHistory.ToArray();
            Quaternion[] rotations = _rotationHistory.ToArray();

            GameObject ghost = null;
            if (_ghostPrefab != null)
            {
                ghost = Instantiate(_ghostPrefab);
                if (_ghostMaterial != null)
                {
                    Renderer r = ghost.GetComponentInChildren<Renderer>();
                    if (r != null) r.material = _ghostMaterial;
                }
            }

            float elapsed = 0f;
            int totalFrames = positions.Length;
            if (totalFrames == 0) yield break;

            while (elapsed < _replayDuration && Input.GetKey(_activationKey))
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _replayDuration;
                int frameIndex = Mathf.FloorToInt(t * (totalFrames - 1));
                frameIndex = Mathf.Clamp(frameIndex, 0, totalFrames - 1);

                if (ghost != null)
                {
                    ghost.transform.position = positions[frameIndex];
                    ghost.transform.rotation = rotations[frameIndex];
                }
                yield return null;
            }

            if (ghost != null) Destroy(ghost);
            _isReplaying = false;
        }

        private void StopReplay()
        {
            _isReplaying = false;
        }
    }
}