using UnityEngine;

namespace CueStrike.Characters.PanPan
{
    /// <summary>
    /// PanPan — Zen Stance ability.
    /// Stand still to build focus meter. Full focus = perfect aim.
    /// </summary>
    public class PanPanZenStance : MonoBehaviour, ICharacterAbility
    {
        [Header("Focus Settings")]
        public float focusBuildTime = 2f;
        public float focusDecayRate = 0.5f;
        public float maxAccuracyBonus = 0.15f;
        public float movementThreshold = 0.05f;

        [Header("Visual")]
        public GameObject focusRingEffect;
        public Light focusLight;

        // State
        private bool _isActive = false;
        private float _currentFocus = 0f;
        private Vector3 _lastPosition;
        private bool _wasMoving = false;

        public string AbilityName => "Zen Stance";
        public string AbilityDescription => $"Stand still for {focusBuildTime}s to build focus. Full focus = +{maxAccuracyBonus * 100:F0}% accuracy.";

        public void OnCharacterSpawned()
        {
            _isActive = true;
            _currentFocus = 0f;
            _lastPosition = transform.position;
            UpdateVisuals();
            Debug.Log("[PanPan] Zen Stance ready. Stay still to focus.");
        }

        public float GetAccuracyModifier()
        {
            return _isActive ? _currentFocus * maxAccuracyBonus : 0f;
        }

        public float GetPowerModifier() => 1f;
        public float GetSpeedModifier() => 1f;
        public float GetVisibilityBonus() => _currentFocus * 0.3f;
        public bool IsAbilityActive() => _isActive;

        /// <summary>
        /// Get current focus level (0-1)
        /// </summary>
        public float GetFocusLevel() => _currentFocus;

        /// <summary>
        /// Register movement for focus decay
        /// </summary>
        public void RegisterMovement(Vector3 currentPosition)
        {
            float movement = Vector3.Distance(currentPosition, _lastPosition);
            _lastPosition = currentPosition;

            if (movement > movementThreshold)
            {
                _wasMoving = true;
                _currentFocus = Mathf.Max(0f, _currentFocus - Time.deltaTime * focusDecayRate);
            }
            else
            {
                _wasMoving = false;
                _currentFocus = Mathf.Min(1f, _currentFocus + Time.deltaTime / focusBuildTime);
            }

            UpdateVisuals();
        }

        void Update()
        {
            if (!_isActive) return;

            // Track position changes
            RegisterMovement(transform.position);
        }

        /// <summary>
        /// Break focus when shot taken (partial retain)
        /// </summary>
        public void OnShotTaken()
        {
            _currentFocus *= 0.3f;
            Debug.Log($"[PanPan] Shot taken! Focus retained: {_currentFocus * 100:F0}%");
            UpdateVisuals();
        }

        /// <summary>
        /// Full focus reset on miss
        /// </summary>
        public void OnMiss()
        {
            _currentFocus = 0f;
            Debug.Log("[PanPan] Miss! Focus broken.");
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (focusRingEffect != null)
            {
                focusRingEffect.SetActive(_currentFocus > 0.1f);
                float scale = 0.5f + _currentFocus * 0.5f;
                focusRingEffect.transform.localScale = Vector3.one * scale;
            }

            if (focusLight != null)
            {
                focusLight.intensity = _currentFocus * 2f;
                focusLight.enabled = _currentFocus > 0.1f;
            }
        }
    }
}