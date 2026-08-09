using UnityEngine;

namespace CueStrike.TitleScene
{
    public class TitleSceneLighting : MonoBehaviour
    {
        [Header("Chandelier Pulse")]
        [SerializeField] private Light chandelierLight;
        [SerializeField] private float pulseSpeed = 1.2f;
        [SerializeField] private float pulseMinIntensity = 0.7f;
        [SerializeField] private float pulseMaxIntensity = 1.3f;

        [Header("Spotlight Rotation")]
        [SerializeField] private Light[] spotlights;
        [SerializeField] private float rotateSpeed = 1.5f;
        [SerializeField] private float rotateAngle = 15f;

        [Header("Ambient Color Shift")]
        [SerializeField] private Light ambientLight;
        [SerializeField] private Color warmColor = new Color(1f, 0.85f, 0.65f, 1f);   // warm gold
        [SerializeField] private Color coolColor = new Color(0.65f, 0.75f, 0.95f, 1f); // cool blue
        [SerializeField] private float colorShiftSpeed = 0.08f;

        [Header("Flicker")]
        [SerializeField] private bool enableFlicker = true;
        [SerializeField] private float flickerChance = 0.02f;

        // Public properties for Editor access
        public Light ChandelierLight { get => chandelierLight; set => chandelierLight = value; }
        public Light[] Spotlights { get => spotlights; set => spotlights = value; }
        public Light AmbientLight { get => ambientLight; set => ambientLight = value; }

        private float[] spotlightBaseRotation;
        private float[] spotlightOffsets;

        private void Start()
        {
            if (spotlights != null && spotlights.Length > 0)
            {
                spotlightBaseRotation = new float[spotlights.Length];
                spotlightOffsets = new float[spotlights.Length];
                for (int i = 0; i < spotlights.Length; i++)
                {
                    if (spotlights[i] != null)
                    {
                        spotlightBaseRotation[i] = spotlights[i].transform.eulerAngles.y;
                        spotlightOffsets[i] = Random.Range(0f, 360f);
                    }
                }
            }
        }

        private void Update()
        {
            // Chandelier pulse
            if (chandelierLight != null)
            {
                float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
                chandelierLight.intensity = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity, t);

                if (enableFlicker && Random.value < flickerChance)
                    chandelierLight.intensity *= Random.Range(0.9f, 1.1f);
            }

            // Spotlights sway
            if (spotlights != null)
            {
                for (int i = 0; i < spotlights.Length; i++)
                {
                    if (spotlights[i] == null) continue;
                    float angle = Mathf.Sin(Time.time * rotateSpeed + spotlightOffsets[i]) * rotateAngle;
                    Vector3 euler = spotlights[i].transform.eulerAngles;
                    euler.y = spotlightBaseRotation[i] + angle;
                    spotlights[i].transform.eulerAngles = euler;
                }
            }

            // Ambient color shift
            if (ambientLight != null)
            {
                float t = Mathf.PingPong(Time.time * colorShiftSpeed, 1f);
                ambientLight.color = Color.Lerp(warmColor, coolColor, t);
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test TitleSceneLighting")]
        public static void SelfTest()
        {
            bool pass = true;
            
            var lighting = FindFirstObjectByType<TitleSceneLighting>();
            if (lighting == null)
            {
                Debug.LogError("❌ FAIL: TitleSceneLighting missing in scene!");
                pass = false;
            }
            else
            {
                // Check if at least one light is assigned
                if (lighting.ChandelierLight == null && 
                    (lighting.Spotlights == null || lighting.Spotlights.Length == 0) && 
                    lighting.AmbientLight == null)
                {
                    Debug.LogWarning("⚠️ WARNING: No lights assigned to TitleSceneLighting. Please assign in Inspector.");
                }
            }
            
            if (pass) Debug.Log("✅ ALL TESTS PASSED — Ready for human verify");
            else Debug.LogWarning("⚠️ TESTS FAILED — Fix before proceeding");
        }
#endif
    }
}
