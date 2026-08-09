using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// King Flex — Bling Bling: Golden cue trail. Style points from crowd reactions.
    /// Passive. Scales with crowd intensity.
    /// </summary>
    public class Ability_BlingBling : CueStrikeCharacterAbility
    {
        [Header("Bling Bling Settings")]
        [SerializeField] private TrailRenderer _goldenTrail;
        [SerializeField] private float _styleMultiplier = 1.0f;
        [SerializeField] private ParticleSystem _diamondSparkle;

        public override string AbilityName => "Bling Bling";
        public override string AbilityDescription => "Golden cue trail. Earn style points from crowd reactions.";

        protected override void Awake()
        {
            base.Awake();
            if (_goldenTrail == null)
            {
                _goldenTrail = gameObject.AddComponent<TrailRenderer>();
                _goldenTrail.startWidth = 0.05f;
                _goldenTrail.endWidth = 0.01f;
                _goldenTrail.time = 0.5f;
                _goldenTrail.material = new Material(Shader.Find("Sprites/Default"));
                _goldenTrail.startColor = new Color(1f, 0.84f, 0f, 1f);
                _goldenTrail.endColor = new Color(1f, 0.84f, 0f, 0f);
            }
            _goldenTrail.enabled = _isActive;
        }

        public void OnCrowdReaction(float intensity)
        {
            if (!_isActive) return;
            _styleMultiplier = 1f + (intensity * 0.5f);
            if (_diamondSparkle != null) _diamondSparkle.Play();
            Debug.Log($"[BlingBling] Crowd intensity: {intensity:F2}, Style multiplier: {_styleMultiplier:F2}x");
        }

        public float GetStyleMultiplier() => _styleMultiplier;
    }
}