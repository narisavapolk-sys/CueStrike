using UnityEngine;

namespace CueStrike.Characters.Somchay
{
    /// <summary>
    /// SOMCHAY — Local Champion ability controller.
    /// Glove Master: leather gloves improve cue ball control, reduce vibration.
    /// </summary>
    public class SomchayAbilityController : MonoBehaviour
    {
        [Header("Glove Settings")]
        [Tooltip("Enable glove visual and effects")]
        public bool useGlove = true;

        [Tooltip("Accuracy bonus when glove is active (0-1)")]
        [Range(0f, 0.15f)]
        public float gloveAccuracyBonus = 0.05f;

        [Tooltip("Cue vibration reduction when glove is active (0-1)")]
        [Range(0f, 1f)]
        public float vibrationReduction = 0.3f;

        [Header("Visual Settings")]
        public Color gloveColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        public float gloveSmoothness = 0.75f;

        [Header("Editor Mock Keys")]
        public KeyCode toggleGloveKey = KeyCode.G;

        // Internal state
        private bool _isGloveActive = false;
        private CueIKController _ikController;

        void Start()
        {
            _ikController = GetComponent<CueIKController>();
            if (_ikController == null)
                _ikController = FindFirstObjectByType<CueIKController>();

            // Load saved glove preference
            _isGloveActive = PlayerPrefs.GetInt("CueStrike_UseGlove", 1) == 1;
            if (_isGloveActive)
                EnableGlove();
            else
                DisableGlove();

            Debug.Log($"[Somchay] Glove Master initialized. Glove active: {_isGloveActive}");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleGloveKey))
                ToggleGlove();
        }

        /// <summary>
        /// Toggle glove on/off
        /// </summary>
        public void ToggleGlove()
        {
            if (_isGloveActive)
                DisableGlove();
            else
                EnableGlove();
        }

        /// <summary>
        /// Enable glove effects
        /// </summary>
        public void EnableGlove()
        {
            _isGloveActive = true;
            PlayerPrefs.SetInt("CueStrike_UseGlove", 1);

            if (_ikController != null)
            {
                _ikController.ApplyGloveSettings();
                // Update glove material color
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.name.Contains("Glove"))
                    {
                        var mat = r.sharedMaterial;
                        if (mat != null)
                        {
                            mat.SetColor("_BaseColor", gloveColor);
                            mat.SetFloat("_Smoothness", gloveSmoothness);
                        }
                    }
                }
            }

            Debug.Log("[Somchay] Glove ENABLED — Accuracy +" + (gloveAccuracyBonus * 100).ToString("F0") + "%");
        }

        /// <summary>
        /// Disable glove effects
        /// </summary>
        public void DisableGlove()
        {
            _isGloveActive = false;
            PlayerPrefs.SetInt("CueStrike_UseGlove", 0);

            if (_ikController != null)
                _ikController.ApplyGloveSettings();

            Debug.Log("[Somchay] Glove DISABLED");
        }

        /// <summary>
        /// Get current accuracy bonus modifier
        /// </summary>
        public float GetAccuracyModifier()
        {
            return _isGloveActive ? gloveAccuracyBonus : 0f;
        }

        /// <summary>
        /// Get current vibration reduction
        /// </summary>
        public float GetVibrationReduction()
        {
            return _isGloveActive ? vibrationReduction : 0f;
        }

        /// <summary>
        /// Is glove currently active?
        /// </summary>
        public bool IsGloveActive() => _isGloveActive;
    }
}