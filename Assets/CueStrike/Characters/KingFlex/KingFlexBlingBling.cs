using UnityEngine;

namespace CueStrike.Characters.KingFlex
{
    /// <summary>
    /// King Flex — Bling Bling ability.
    /// Style points from crowd reactions. Golden cue trail. More audience = more power.
    /// </summary>
    public class KingFlexBlingBling : MonoBehaviour, ICharacterAbility
    {
        [Header("Bling Settings")]
        public float stylePointMultiplier = 1.2f;
        public int crowdThreshold = 50;

        [Header("Visual")]
        public GameObject goldenTrailEffect;
        public ParticleSystem sparkleParticles;
        public LineRenderer goldenTrailLine;

        // State
        private bool _isActive = false;
        private int _stylePoints = 0;

        public string AbilityName => "Bling Bling";
        public string AbilityDescription => $"Style points from crowd reactions, golden cue trail, {stylePointMultiplier}x power bonus";

        public void OnCharacterSpawned()
        {
            _isActive = true;
            _stylePoints = 0;

            if (goldenTrailLine != null)
                goldenTrailLine.enabled = true;

            Debug.Log("[KingFlex] Bling Bling ready! Let's shine!");
        }

        public float GetAccuracyModifier() => 0f;
        public float GetPowerModifier() => _isActive ? stylePointMultiplier : 1f;
        public float GetSpeedModifier() => 1f;
        public float GetVisibilityBonus() => 0.1f;
        public bool IsAbilityActive() => _isActive;

        /// <summary>
        /// Add style points from crowd reaction
        /// </summary>
        public void AddStylePoints(int points)
        {
            _stylePoints += points;
            Debug.Log($"[KingFlex] Style points: {_stylePoints}!");

            if (_stylePoints >= crowdThreshold)
            {
                TriggerFlex();
                _stylePoints = 0;
            }
        }

        /// <summary>
        /// Register a great shot for style
        /// </summary>
        public void RegisterGreatShot(int potCount)
        {
            AddStylePoints(potCount * 10);
        }

        /// <summary>
        /// Register a clutch shot
        /// </summary>
        public void RegisterClutch()
        {
            AddStylePoints(50);
        }

        private void TriggerFlex()
        {
            Debug.Log("[KingFlex] FLEX! Crowd goes wild!");
            if (sparkleParticles != null)
                sparkleParticles.Play();

            if (goldenTrailEffect != null)
            {
                goldenTrailEffect.SetActive(true);
                Invoke(nameof(HideTrailEffect), 1.5f);
            }
        }

        private void HideTrailEffect()
        {
            if (goldenTrailEffect != null)
                goldenTrailEffect.SetActive(false);
        }
    }
}