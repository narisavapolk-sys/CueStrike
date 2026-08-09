using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Cassidy — Quick Draw: Rapid shot after potting. No re-aim needed within 3 seconds.
    /// Golden aim line. Score bonus.
    /// </summary>
    public class Ability_QuickDraw : CueStrikeCharacterAbility
    {
        [Header("Quick Draw Settings")]
        [SerializeField] private float _quickWindow = 3f;
        [SerializeField] private float _scoreBonus = 1.3f;
        [SerializeField] private GameObject _goldenAimLine;
        [SerializeField] private ParticleSystem _sparkleVFX;

        private float _potTime = -1f;
        private bool _quickDrawAvailable = false;

        public override string AbilityName => "Quick Draw";
        public override string AbilityDescription => "After potting, shoot next shot within 3 seconds without re-aiming. Score bonus applied.";

        public void OnBallPotted()
        {
            _potTime = Time.time;
            _quickDrawAvailable = true;
            if (_goldenAimLine != null) _goldenAimLine.SetActive(true);
            Debug.Log("[QuickDraw] Quick Draw available! 3 seconds...");
        }

        private void Update()
        {
            if (!_isActive) return;

            if (_quickDrawAvailable && Time.time - _potTime > _quickWindow)
            {
                _quickDrawAvailable = false;
                if (_goldenAimLine != null) _goldenAimLine.SetActive(false);
                Debug.Log("[QuickDraw] Window expired.");
            }
        }

        public bool IsQuickDrawActive() => _quickDrawAvailable && _isActive;

        public float GetScoreMultiplier()
        {
            return IsQuickDrawActive() ? _scoreBonus : 1f;
        }

        public void OnQuickShotFired()
        {
            if (_quickDrawAvailable)
            {
                PlayEffects();
                Debug.Log("[QuickDraw] Quick shot fired! Bonus applied.");
                _quickDrawAvailable = false;
                if (_goldenAimLine != null) _goldenAimLine.SetActive(false);
            }
        }
    }
}