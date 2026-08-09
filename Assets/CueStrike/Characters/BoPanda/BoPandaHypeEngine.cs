using UnityEngine;

namespace CueStrike.Characters.BoPanda
{
    /// <summary>
    /// Bo Panda — Hype Engine ability.
    /// Consecutive pots build combo meter for accuracy boost.
    /// </summary>
    public class BoPandaHypeEngine : MonoBehaviour, ICharacterAbility
    {
        [Header("Hype Settings")]
        public int potsPerLevel = 3;
        public float accuracyBoostPerLevel = 0.05f;
        public int maxLevel = 4;

        [Header("Visual")]
        public GameObject hypeGlowEffect;
        public GameObject comboIndicator;

        // State
        private int _currentCombo = 0;
        private int _currentLevel = 0;
        private bool _isActive = false;

        public string AbilityName => "Hype Engine";
        public string AbilityDescription => $"Every {potsPerLevel} consecutive pots build Hype, +{accuracyBoostPerLevel * 100:F0}% accuracy per level (max {maxLevel})";

        public void OnCharacterSpawned()
        {
            _currentCombo = 0;
            _currentLevel = 0;
            _isActive = true;
            UpdateVisuals();
            Debug.Log("[BoPanda] Hype Engine ready!");
        }

        public float GetAccuracyModifier()
        {
            return _isActive ? _currentLevel * accuracyBoostPerLevel : 0f;
        }

        public float GetPowerModifier() => 1f;
        public float GetSpeedModifier() => 1f;
        public float GetVisibilityBonus() => 0f;
        public bool IsAbilityActive() => _isActive;

        /// <summary>
        /// Register a successful pot
        /// </summary>
        public void RegisterPot()
        {
            _currentCombo++;
            int newLevel = Mathf.Min(_currentCombo / potsPerLevel, maxLevel);

            if (newLevel > _currentLevel)
            {
                _currentLevel = newLevel;
                Debug.Log($"[BoPanda] HYPE LEVEL {_currentLevel}! Accuracy +{_currentLevel * accuracyBoostPerLevel * 100:F0}%");
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Register a miss — reset combo
        /// </summary>
        public void RegisterMiss()
        {
            if (_currentLevel > 0)
                Debug.Log($"[BoPanda] Miss! Hype reset from level {_currentLevel}.");

            _currentCombo = 0;
            _currentLevel = 0;
            UpdateVisuals();
        }

        /// <summary>
        /// Register a foul — reset combo
        /// </summary>
        public void RegisterFoul()
        {
            RegisterMiss();
        }

        private void UpdateVisuals()
        {
            if (hypeGlowEffect != null)
                hypeGlowEffect.SetActive(_currentLevel > 0);

            if (comboIndicator != null)
                comboIndicator.SetActive(_currentLevel >= maxLevel);
        }
    }
}