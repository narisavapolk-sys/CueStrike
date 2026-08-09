using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Gentleman — Century Spotlight: Spotlight brightens on Century Break (100+ points).
    /// Passive crowd reaction system.
    /// </summary>
    public class Ability_CenturySpotlight : CueStrikeCharacterAbility
    {
        [Header("Century Spotlight Settings")]
        [SerializeField] private Light _spotlight;
        [SerializeField] private float _baseIntensity = 1f;
        [SerializeField] private float _centuryIntensity = 3f;
        [SerializeField] private float _intensityLerpSpeed = 2f;

        private float _currentScore = 0f;
        private bool _centuryAchieved = false;

        public override string AbilityName => "Century Spotlight";
        public override string AbilityDescription => "Spotlight intensifies when achieving a Century Break (100+ points).";

        protected override void Awake()
        {
            base.Awake();
            if (_spotlight == null)
            {
                _spotlight = gameObject.AddComponent<Light>();
                _spotlight.type = LightType.Spot;
                _spotlight.intensity = _baseIntensity;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            float targetIntensity = _centuryAchieved ? _centuryIntensity : _baseIntensity;
            _spotlight.intensity = Mathf.Lerp(_spotlight.intensity, targetIntensity, Time.deltaTime * _intensityLerpSpeed);
        }

        public void OnScoreChanged(int newScore)
        {
            _currentScore = newScore;
            if (_currentScore >= 100 && !_centuryAchieved)
            {
                _centuryAchieved = true;
                ActivateAbility();
            }
        }

        protected override void OnAbilityActivated()
        {
            Debug.Log("[CenturySpotlight] Century Break achieved! Spotlight intensifying.");
        }

        public void ResetBreak()
        {
            _centuryAchieved = false;
            _currentScore = 0;
        }
    }
}