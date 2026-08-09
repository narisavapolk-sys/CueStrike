using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Somchay — Glove Master: Increases accuracy, reduces cue vibration.
    /// Passive ability. Always active.
    /// </summary>
    public class Ability_GloveMaster : CueStrikeCharacterAbility
    {
        [Header("Glove Master Settings")]
        [SerializeField] private float _accuracyMultiplier = 1.15f;
        [SerializeField] private float _vibrationDampening = 0.5f;
        [SerializeField] private GameObject _gloveVisual;

        public override string AbilityName => "Glove Master";
        public override string AbilityDescription => "Enhanced gloves increase shot accuracy by 15% and reduce cue vibration.";

        protected override void Awake()
        {
            base.Awake();
            _isActive = true; // Passive
        }

        protected override void OnAbilityActivated()
        {
            // Passive — no activation needed
            Debug.Log("[GloveMaster] Passive accuracy boost active.");
        }

        public float GetAccuracyMultiplier() => _isActive ? _accuracyMultiplier : 1f;
        public float GetVibrationDampening() => _isActive ? _vibrationDampening : 0f;

        public override bool RunSelfTest()
        {
            bool pass = base.RunSelfTest();
            if (_accuracyMultiplier <= 1f)
            {
                Debug.LogWarning("[Self-Test] GloveMaster: Accuracy multiplier should be > 1.");
            }
            return pass;
        }
    }
}