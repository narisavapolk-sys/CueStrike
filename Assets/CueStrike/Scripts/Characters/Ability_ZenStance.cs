using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// PanPan — Zen Stance: Stand still for 2 seconds to gain focus.
    /// Perfect aim line. Small pocket preview.
    /// </summary>
    public class Ability_ZenStance : CueStrikeCharacterAbility
    {
        [Header("Zen Stance Settings")]
        [SerializeField] private float _focusBuildTime = 2f;
        [SerializeField] private float _focusDecayRate = 0.5f;
        [SerializeField] private GameObject _zenCircleVFX;
        [SerializeField] private LineRenderer _perfectAimLine;

        private float _stillTimer = 0f;
        private float _focusLevel = 0f;
        private Vector3 _lastPosition;
        private bool _isFocused => _focusLevel >= 1f;

        public override string AbilityName => "Zen Stance";
        public override string AbilityDescription => "Stand still for 2 seconds to enter Zen Focus. Perfect aim line and pocket preview.";

        protected override void Awake()
        {
            base.Awake();
            _lastPosition = transform.position;
            if (_perfectAimLine == null)
            {
                _perfectAimLine = gameObject.AddComponent<LineRenderer>();
                _perfectAimLine.startWidth = 0.01f;
                _perfectAimLine.endWidth = 0.01f;
                _perfectAimLine.material = new Material(Shader.Find("Sprites/Default"));
                _perfectAimLine.startColor = new Color(0.5f, 0.8f, 1f, 0.6f);
                _perfectAimLine.endColor = new Color(0.5f, 0.8f, 1f, 0f);
                _perfectAimLine.positionCount = 2;
                _perfectAimLine.enabled = false;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            float moveDistance = Vector3.Distance(transform.position, _lastPosition);
            _lastPosition = transform.position;

            if (moveDistance < 0.001f)
            {
                _stillTimer += Time.deltaTime;
                if (_stillTimer >= _focusBuildTime && !_isFocused)
                {
                    _focusLevel = 1f;
                    EnterZenFocus();
                }
            }
            else
            {
                _stillTimer = 0f;
                if (_focusLevel > 0f)
                {
                    _focusLevel -= Time.deltaTime * _focusDecayRate;
                    if (_focusLevel <= 0f) ExitZenFocus();
                }
            }

            if (_isFocused && _perfectAimLine != null)
            {
                UpdateAimLine();
            }
        }

        private void EnterZenFocus()
        {
            Debug.Log("[ZenStance] Zen Focus achieved!");
            if (_zenCircleVFX != null) _zenCircleVFX.SetActive(true);
            if (_perfectAimLine != null) _perfectAimLine.enabled = true;
            PlayEffects();
        }

        private void ExitZenFocus()
        {
            _focusLevel = 0f;
            if (_zenCircleVFX != null) _zenCircleVFX.SetActive(false);
            if (_perfectAimLine != null) _perfectAimLine.enabled = false;
        }

        private void UpdateAimLine()
        {
            _perfectAimLine.SetPosition(0, transform.position);
            _perfectAimLine.SetPosition(1, transform.position + transform.forward * 5f);
        }

        public bool IsFocused() => _isFocused;
        public float GetFocusLevel() => _focusLevel;
    }
}