using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Phantom — Spectral Sight: See through obstacle balls. View cushion reflection paths.
    /// Ghost walk visuals. Purple glowing cue.
    /// </summary>
    public class Ability_SpectralSight : CueStrikeCharacterAbility
    {
        [Header("Spectral Sight Settings")]
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _sightRange = 5f;
        [SerializeField] private Material _spectralCueMaterial;
        [SerializeField] private GameObject _ghostBody;

        private Renderer _cueRenderer;
        private Material _originalCueMaterial;

        public override string AbilityName => "Spectral Sight";
        public override string AbilityDescription => "See through obstacle balls and view cushion reflection paths. Ghost walk with purple glow.";

        protected override void Awake()
        {
            base.Awake();
            _cueRenderer = GetComponentInChildren<Renderer>();
            if (_cueRenderer != null) _originalCueMaterial = _cueRenderer.material;
        }

        protected override void HandleInput()
        {
            if (Input.GetKeyDown(_activationKey) && !_isOnCooldown)
            {
                ToggleSpectralMode();
            }
        }

        private void ToggleSpectralMode()
        {
            _isActive = !_isActive;
            if (_isActive)
            {
                EnterSpectralMode();
            }
            else
            {
                ExitSpectralMode();
            }
        }

        private void EnterSpectralMode()
        {
            if (_cueRenderer != null && _spectralCueMaterial != null)
                _cueRenderer.material = _spectralCueMaterial;
            if (_ghostBody != null) _ghostBody.SetActive(true);
            Debug.Log("[SpectralSight] Spectral mode ON.");
            PlayEffects();
        }

        private void ExitSpectralMode()
        {
            if (_cueRenderer != null && _originalCueMaterial != null)
                _cueRenderer.material = _originalCueMaterial;
            if (_ghostBody != null) _ghostBody.SetActive(false);
            Debug.Log("[SpectralSight] Spectral mode OFF.");
        }

        public bool CanSeeThroughObstacles() => _isActive;
    }
}