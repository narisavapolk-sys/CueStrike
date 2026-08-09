using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Finn — AQUA Rush: Speed bonus for quick shots. Timer-based.
    /// Pot within 8 seconds → score x1.2. Faster = up to x1.5.
    /// </summary>
    public class Ability_AQUARush : CueStrikeCharacterAbility
    {
        [Header("AQUA Rush Settings")]
        [SerializeField] private float _rushWindow = 8f;
        [SerializeField] private float _scoreMultiplier = 1.2f;
        [SerializeField] private float _maxMultiplier = 1.5f;
        [SerializeField] private AnimationCurve _multiplierCurve;

        private float _potTime = -1f;
        private float _shotStartTime = -1f;

        public override string AbilityName => "AQUA Rush";
        public override string AbilityDescription => "Pot within 8 seconds for score bonus. Faster shots = higher multiplier (up to x1.5).";

        public void OnShotStarted()
        {
            _shotStartTime = Time.time;
        }

        public void OnBallPotted()
        {
            if (_shotStartTime < 0) return;

            float elapsed = Time.time - _shotStartTime;
            if (elapsed <= _rushWindow)
            {
                float t = 1f - (elapsed / _rushWindow);
                float currentMultiplier = Mathf.Lerp(1f, _maxMultiplier, _multiplierCurve.Evaluate(t));
                Debug.Log($"[AQUARush] Speed: {elapsed:F1}s, Multiplier: {currentMultiplier:F2}x");
                PlayEffects();
            }
            _shotStartTime = -1f;
        }

        public float GetCurrentMultiplier()
        {
            if (_shotStartTime < 0) return 1f;
            float elapsed = Time.time - _shotStartTime;
            if (elapsed > _rushWindow) return 1f;
            float t = 1f - (elapsed / _rushWindow);
            return Mathf.Lerp(1f, _maxMultiplier, _multiplierCurve.Evaluate(t));
        }
    }
}