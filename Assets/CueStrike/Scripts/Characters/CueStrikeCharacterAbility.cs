using System;
using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Base class for all character abilities in CueStrike VR.
    /// Every playable character ability inherits from this.
    /// </summary>
    public abstract class CueStrikeCharacterAbility : MonoBehaviour
    {
        [Header("Ability Settings")]
        [SerializeField] protected bool _isActive = true;
        [SerializeField] protected float _cooldownDuration = 0f;
        [SerializeField] protected KeyCode _activationKey = KeyCode.Space;

        [Header("Visual")]
        [SerializeField] protected ParticleSystem _abilityVFX;
        [SerializeField] protected AudioClip _abilitySFX;

        protected float _lastActivationTime = -999f;
        protected bool _isOnCooldown => Time.time - _lastActivationTime < _cooldownDuration;
        protected AudioSource _audioSource;

        public bool IsActive => _isActive;
        public abstract string AbilityName { get; }
        public abstract string AbilityDescription { get; }

        protected virtual void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && _abilitySFX != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        protected virtual void Update()
        {
            if (!_isActive) return;
            HandleInput();
        }

        protected virtual void HandleInput()
        {
            if (Input.GetKeyDown(_activationKey) && !_isOnCooldown)
            {
                ActivateAbility();
            }
        }

        /// <summary>
        /// Override this to implement ability logic.
        /// </summary>
        public virtual void ActivateAbility()
        {
            if (_isOnCooldown)
            {
                Debug.Log($"[{AbilityName}] On cooldown.");
                return;
            }
            _lastActivationTime = Time.time;
            PlayEffects();
            OnAbilityActivated();
        }

        protected virtual void OnAbilityActivated() { }

        protected virtual void PlayEffects()
        {
            if (_abilityVFX != null) _abilityVFX.Play();
            if (_audioSource != null && _abilitySFX != null) _audioSource.PlayOneShot(_abilitySFX);
        }

        public virtual void DeactivateAbility()
        {
            _isActive = false;
        }

        public virtual void ReactivateAbility()
        {
            _isActive = true;
        }

        /// <summary>
        /// Self-test for this ability. Override to add specific checks.
        /// </summary>
        public virtual bool RunSelfTest()
        {
            bool pass = true;
            if (string.IsNullOrEmpty(AbilityName))
            {
                Debug.LogError($"[Self-Test] Ability name is empty on {GetType().Name}");
                pass = false;
            }
            Debug.Log($"[Self-Test] {AbilityName}: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
    }
}