using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CueStrike.Managers
{
    public class CueStrikePostProcessingManager : MonoBehaviour
    {
        public static CueStrikePostProcessingManager Instance { get; private set; }

        [Header("Volume")]
        [SerializeField] private Volume globalVolume;

        [Header("Noir Settings")]
        [SerializeField] private float noirVignetteIntensity = 0.5f;
        [SerializeField] private float noirFilmGrainIntensity = 0.7f;
        [SerializeField] private float noirBloomIntensity = 0.3f;
        [SerializeField] private float noirContrast = 30f;
        [SerializeField] private float noirSaturation = -50f;

        [Header("Normal Settings")]
        [SerializeField] private float normalVignetteIntensity = 0.25f;
        [SerializeField] private float normalBloomIntensity = 0.8f;
        [SerializeField] private float normalContrast = 5f;
        [SerializeField] private float normalSaturation = 10f;

        private Bloom bloom;
        private Vignette vignette;
        private FilmGrain filmGrain;
        private DepthOfField depthOfField;
        private ColorAdjustments colorAdjustments;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out bloom);
                globalVolume.profile.TryGet(out vignette);
                globalVolume.profile.TryGet(out filmGrain);
                globalVolume.profile.TryGet(out depthOfField);
                globalVolume.profile.TryGet(out colorAdjustments);
            }
        }

        public void SetNoirMode(bool enabled)
        {
            if (vignette != null) vignette.intensity.value = enabled ? noirVignetteIntensity : normalVignetteIntensity;
            if (filmGrain != null) filmGrain.intensity.value = enabled ? noirFilmGrainIntensity : 0f;
            if (bloom != null) bloom.intensity.value = enabled ? noirBloomIntensity : normalBloomIntensity;
            if (colorAdjustments != null)
            {
                colorAdjustments.contrast.value = enabled ? noirContrast : normalContrast;
                colorAdjustments.saturation.value = enabled ? noirSaturation : normalSaturation;
            }
        }

        public void SetDepthOfField(bool enabled, float focusDistance = 2.5f)
        {
            if (depthOfField != null)
            {
                depthOfField.active = enabled;
                if (enabled) depthOfField.focusDistance.value = focusDistance;
            }
        }

        public void SetBloomIntensity(float intensity)
        {
            if (bloom != null) bloom.intensity.value = intensity;
        }

        public void SetVignetteIntensity(float intensity)
        {
            if (vignette != null) vignette.intensity.value = intensity;
        }

        public void SetFilmGrainIntensity(float intensity)
        {
            if (filmGrain != null) filmGrain.intensity.value = intensity;
        }
    }
}