using UnityEngine;

namespace CueStrike.TitleScene
{
    public class TitleSceneAtmosphere : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TitleSceneLighting lighting;
        [SerializeField] private TitleSceneParticles particles;

        [Header("Overall Intensity")]
        [SerializeField] [Range(0f, 2f)] private float atmosphereIntensity = 1f;

        // Public properties for Editor access
        public TitleSceneLighting Lighting { get => lighting; set => lighting = value; }
        public TitleSceneParticles Particles { get => particles; set => particles = value; }

        private void Update()
        {
            // Global intensity multiplier if needed
            // Can be linked to menu transitions (dim when panel opens)
        }

        public void SetIntensity(float intensity)
        {
            atmosphereIntensity = Mathf.Clamp(intensity, 0f, 2f);
            if (lighting != null)
            {
                // Access and scale if needed via public methods or direct refs
            }
        }

        public void PauseAtmosphere()
        {
            enabled = false;
        }

        public void ResumeAtmosphere()
        {
            enabled = true;
        }
    }
}
