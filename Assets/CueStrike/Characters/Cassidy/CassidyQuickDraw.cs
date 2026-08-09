using UnityEngine;

namespace CueStrike.Characters.Cassidy
{
    /// <summary>
    /// Cassidy — Quick Draw ability.
    /// Shoot immediately after potting for bonus points. Build quick meter.
    /// </summary>
    public class CassidyQuickDraw : MonoBehaviour, ICharacterAbility
    {
        [Header("Quick Draw Settings")]
        public float quickDrawWindow = 3f;
        public float bonusMultiplier = 1.3f;
        public KeyCode quickDrawKey = KeyCode.Q;

        [Header("Visual")]
        public GameObject drawEffect;
        public LineRenderer aimGuideLine;

        // State
        private bool _isActive = false;
        private float _lastPotTime = -10f;
        private bool _isQuickDrawReady = false;

        public string AbilityName => "Quick Draw";
        public string AbilityDescription => $"Shoot within {quickDrawWindow}s after potting for {bonusMultiplier}x score bonus.";

        public void OnCharacterSpawned()
        {
            _isActive = true;
            _lastPotTime = -10f;

            if (aimGuideLine != null)
            {
                aimGuideLine.startColor = Color.yellow;
                aimGuideLine.endColor = new Color(1f, 0.5f, 0f, 0.3f);
                aimGuideLine.startWidth = 0.01f;
                aimGuideLine.endWidth = 0.003f;
                aimGuideLine.enabled = true;
            }

            Debug.Log("[Cassidy] Quick Draw ready! Pot then shoot fast!");
        }

        public float GetAccuracyModifier() => 0f;
        public float GetPowerModifier() => IsQuickDrawActive() ? bonusMultiplier : 1f;
        public float GetSpeedModifier() => 1f;
        public float GetVisibilityBonus() => IsQuickDrawActive() ? 0.2f : 0f;
        public bool IsAbilityActive() => _isActive;

        /// <summary>
        /// Register a pot — start quick draw window
        /// </summary>
        public void RegisterPot()
        {
            _lastPotTime = Time.time;
            _isQuickDrawReady = true;
            Debug.Log("[Cassidy] Pot! Quick Draw ready — shoot fast!");
            TriggerDrawEffect();
        }

        /// <summary>
        /// Is quick draw currently active?
        /// </summary>
        public bool IsQuickDrawActive()
        {
            return _isActive && _isQuickDrawReady && (Time.time - _lastPotTime) <= quickDrawWindow;
        }

        /// <summary>
        /// Get remaining quick draw time
        /// </summary>
        public float GetRemainingTime()
        {
            float elapsed = Time.time - _lastPotTime;
            return Mathf.Max(0f, quickDrawWindow - elapsed);
        }

        /// <summary>
        /// Register a shot taken with quick draw
        /// </summary>
        public void RegisterQuickShot()
        {
            if (IsQuickDrawActive())
            {
                Debug.Log($"[Cassidy] QUICK DRAW! Score x{bonusMultiplier}!");
                _isQuickDrawReady = false;
            }
        }

        /// <summary>
        /// Register a miss
        /// </summary>
        public void RegisterMiss()
        {
            _isQuickDrawReady = false;
            Debug.Log("[Cassidy] Miss. Quick Draw reset.");
        }

        void Update()
        {
            if (!_isActive) return;

            // Show aim guide when quick draw is ready
            if (aimGuideLine != null)
            {
                aimGuideLine.enabled = IsQuickDrawActive();
                if (aimGuideLine.enabled)
                {
                    // Draw aim line
                    Vector3 start = transform.position + transform.forward * 0.5f;
                    Vector3 end = start + transform.forward * 4f;
                    aimGuideLine.SetPosition(0, start);
                    aimGuideLine.SetPosition(1, end);
                }
            }

            // Quick draw key
            if (Input.GetKeyDown(quickDrawKey) && IsQuickDrawActive())
            {
                RegisterQuickShot();
            }
        }

        private void TriggerDrawEffect()
        {
            if (drawEffect != null)
            {
                drawEffect.SetActive(true);
                Invoke(nameof(HideDrawEffect), 0.3f);
            }
        }

        private void HideDrawEffect()
        {
            if (drawEffect != null)
                drawEffect.SetActive(false);
        }
    }
}