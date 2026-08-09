using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.Characters
{
    /// <summary>
    /// Bo Panda — Hype Engine: Combo meter increases accuracy. Reset on miss.
    /// Every 3 consecutive pots → +5% accuracy (max +20%).
    /// </summary>
    public class Ability_HypeEngine : CueStrikeCharacterAbility
    {
        [Header("Hype Engine Settings")]
        [SerializeField] private int _potsPerCombo = 3;
        [SerializeField] private float _accuracyBonusPerCombo = 0.05f;
        [SerializeField] private float _maxAccuracyBonus = 0.20f;
        [SerializeField] private GameObject _hypeMeterUI;

        private int _consecutivePots = 0;
        private int _comboLevel = 0;

        public override string AbilityName => "Hype Engine";
        public override string AbilityDescription => "Build combo with consecutive pots. Every 3 pots = +5% accuracy (max +20%). Reset on miss.";

        public void OnBallPotted()
        {
            _consecutivePots++;
            UpdateComboLevel();
            PlayEffects();
            Debug.Log($"[HypeEngine] Combo: {_consecutivePots} pots, Level: {_comboLevel}, Bonus: {GetAccuracyBonus():P0}");
        }

        public void OnShotMissed()
        {
            if (_consecutivePots > 0)
            {
                Debug.Log("[HypeEngine] Combo broken!");
            }
            _consecutivePots = 0;
            _comboLevel = 0;
        }

        private void UpdateComboLevel()
        {
            _comboLevel = _consecutivePots / _potsPerCombo;
            float maxLevel = Mathf.Floor(_maxAccuracyBonus / _accuracyBonusPerCombo);
            _comboLevel = Mathf.Min(_comboLevel, (int)maxLevel);
        }

        public float GetAccuracyBonus()
        {
            return _comboLevel * _accuracyBonusPerCombo;
        }

        public int GetComboLevel() => _comboLevel;
        public int GetConsecutivePots() => _consecutivePots;
    }
}