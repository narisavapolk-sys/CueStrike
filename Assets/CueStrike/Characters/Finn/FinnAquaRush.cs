using UnityEngine;

namespace CueStrike.Characters.Finn
{
    /// <summary>
    /// Finn — AQUA Racing ability.
    /// Speed bonus: faster shots get score multiplier. Time pressure = reward.
    /// </summary>
    public class FinnAquaRush : MonoBehaviour, ICharacterAbility
    {
        [Header("Speed Settings")]
        public float fastShotThreshold = 8f;
        public float speedMultiplierMax = 1.5f;
        public float timerDecayRate = 0.5f;

        [Header("Visual")]
        public GameObject waterEffect;
        public ParticleSystem bubbleParticles;

        // State
        private float _lastShotTime = 0f;
        private bool _isActive = false;

        public string AbilityName => "AQUA Rush";
        public string AbilityDescription => $"Fast shots (< {fastShotThreshold}s) get score multiplier up to {speedMultiplierMax}x";

        public void OnCharacterSpawned()
        {
            _lastShotTime = Time.time;
            _isActive = true;
            Debug.Log("[Finn] AQUA Rush ready! Go fast!");
        }

        public float GetAccuracyModifier() => 0f;
        public float GetPowerModifier() => 1f;
        public float GetSpeedModifier() => GetCurrentSpeedBonus();
        public float GetVisibilityBonus() => 0f;
        public bool IsAbilityActive() => _isActive;

        /// <summary>
        /// Get current speed bonus multiplier based on time since last shot
        /// </summary>
        public float GetCurrentSpeedBonus()
        {
            float timeSinceLast = Time.time - _lastShotTime;
            if (timeSinceLast <= 0f) return speedMultiplierMax;

            float bonus = speedMultiplierMax - (timeSinceLast / fastShotThreshold) * (speedMultiplierMax - 1f);
            return Mathf.Clamp(bonus, 1f, speedMultiplierMax);
        }

        /// <summary>
        /// Register a shot taken
        /// </summary>
        public void RegisterShot()
        {
            float timeSinceLast = Time.time - _lastShotTime;
            if (timeSinceLast <= fastShotThreshold)
            {
                float multiplier = GetCurrentSpeedBonus();
                Debug.Log($"[Finn] AQUA RUSH! Shot within {timeSinceLast:F1}s — Score x{multiplier:F2}!");
                TriggerSplash();
            }
            else
            {
                Debug.Log($"[Finn] Shot at {timeSinceLast:F1}s — no speed bonus.");
            }

            _lastShotTime = Time.time;
        }

        /// <summary>
        /// Register a pot (extends the rush timer)
        /// </summary>
        public void RegisterPot()
        {
            // Potting extends the rush window
            _lastShotTime = Mathf.Max(_lastShotTime, Time.time - fastShotThreshold * 0.5f);
            Debug.Log("[Finn] Pot! Rush extended!");
        }

        /// <summary>
        /// Register a miss (reset rush penalty)
        /// </summary>
        public void RegisterMiss()
        {
            _lastShotTime = Time.time;
            Debug.Log("[Finn] Miss! Rush reset.");
        }

        private void TriggerSplash()
        {
            if (waterEffect != null)
            {
                waterEffect.SetActive(true);
                Invoke(nameof(HideSplash), 0.5f);
            }

            if (bubbleParticles != null)
                bubbleParticles.Play();
        }

        private void HideSplash()
        {
            if (waterEffect != null)
                waterEffect.SetActive(false);
        }
    }
}