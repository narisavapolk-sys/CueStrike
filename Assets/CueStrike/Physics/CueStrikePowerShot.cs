using UnityEngine;
using UnityEngine.XR;
using CueStrike.Audio;

namespace CueStrike.Physics
{
    /// <summary>
    /// CueStrikePowerShot - Power Shot Fire Effect & Haptics
    /// Created by Nari for P'Mong | 2026-07-21
    ///
    /// Attach this script to the Cue Ball
    /// - Detects speed > 8.0f to activate "Power Shot"
    /// - Creates procedural fire Particle System (red/orange HDR glow)
    /// - Triggers controller haptics
    /// - Plays whoosh sound
    /// - Gradually fades fire when speed drops below 0.5f
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CueStrikeBallTrail))]
    public class CueStrikePowerShot : MonoBehaviour
    {
        [Header("Power Shot Settings")]
        [Tooltip("Minimum speed to activate Power Shot (units/sec)")]
        public float powerShotThreshold = 8.0f;

        [Tooltip("Speed at which fire starts to fade out (units/sec)")]
        public float fireFadeOutSpeed = 0.5f;

        [Header("Fire Particle Settings")]
        public int maxParticles = 300;
        public float startSize = 0.08f;
        public float startSpeed = 0.4f;
        public Color fireColorStart = new Color(1.0f, 0.5f, 0.05f, 1f);
        public Color fireColorEnd = new Color(1.0f, 0.15f, 0.0f, 0f);
        public Color trailColor = new Color(1.0f, 0.6f, 0.2f, 0.8f);
        public float particleLifetime = 0.6f;
        public float emissionRateHigh = 120f;
        public float emissionRateLow = 10f;

        [Header("Haptics")]
        [Tooltip("Haptic amplitude (0-1)")]
        public float hapticAmplitude = 0.8f;
        [Tooltip("Haptic duration in seconds")]
        public float hapticDuration = 0.4f;

        [Header("Audio")]
        public bool autoPlayWhoosh = true;

        [Header("Visual")]
        [Tooltip("Enable HDR Emission for Bloom (URP)")]
        public bool useHDREmission = true;
        public float emissionIntensity = 4f;

        // Internal state
        private Rigidbody rb;
        private CueStrikeBallTrail ballTrail;
        private ParticleSystem ps;
        private ParticleSystem.EmissionModule emission;
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.TrailModule trailModule;

        private bool isPowerShotActive = false;
        private bool hapticFiredThisShot = false;
        private float currentSpeed = 0f;
        private float targetEmissionRate = 0f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ballTrail = GetComponent<CueStrikeBallTrail>();
            CreateFireParticleSystem();
        }

        void FixedUpdate()
        {
            currentSpeed = rb != null ? rb.linearVelocity.magnitude : 0f;

            // Activate Power Shot when speed exceeds threshold
            if (currentSpeed > powerShotThreshold)
            {
                if (!isPowerShotActive)
                {
                    isPowerShotActive = true;
                    OnPowerShotStart();
                }

                // Adjust emission rate based on speed (faster = more intense fire)
                targetEmissionRate = Mathf.Lerp(emissionRateLow, emissionRateHigh,
                    Mathf.InverseLerp(powerShotThreshold, powerShotThreshold * 2.5f, currentSpeed));

                // Fire haptic once per power shot
                if (!hapticFiredThisShot)
                {
                    CueStrike.VR.CueStrikeHapticManager.SendHapticToAll(hapticAmplitude, hapticDuration);
                    hapticFiredThisShot = true;
                }
            }
            else
            {
                // Fade out fire when speed drops
                if (currentSpeed < fireFadeOutSpeed)
                {
                    isPowerShotActive = false;
                    targetEmissionRate = 0f;
                    hapticFiredThisShot = false;
                }
                else
                {
                    // Gradually reduce fire
                    targetEmissionRate = Mathf.Lerp(targetEmissionRate, 0f, Time.fixedDeltaTime * 3f);
                }
            }

            UpdateParticleSystem();
        }

        /// <summary>
        /// Creates a procedural Particle System (no assets required)
        /// </summary>
        private void CreateFireParticleSystem()
        {
            var go = new GameObject("PowerShotFire");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            // Use default particle material from Unity
            if (renderer != null)
            {
                var defaultMat = new Material(Shader.Find("Universal/Particles/Lit") ?? Shader.Find("Particles/Standard Unlit"));
                if (defaultMat != null)
                {
                    defaultMat.SetColor("_BaseColor", fireColorStart);
                    if (defaultMat.HasProperty("_EmissionColor"))
                    {
                        defaultMat.EnableKeyword("_EMISSION");
                        defaultMat.SetColor("_EmissionColor", fireColorStart * (useHDREmission ? emissionIntensity : 1f));
                    }
                    renderer.material = defaultMat;
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.alignment = ParticleSystemRenderSpace.View;
                }
            }

            mainModule = ps.main;
            mainModule.loop = true;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            mainModule.maxParticles = maxParticles;
            mainModule.startSize = startSize;
            mainModule.startSpeed = startSpeed;
            mainModule.startLifetime = particleLifetime;
            mainModule.startColor = fireColorStart;
            mainModule.gravityModifier = 0.15f;
            mainModule.startRotation3D = true;
            mainModule.startRotationX = 0f;
            mainModule.startRotationY = 0f;
            mainModule.startRotationZ = Random.Range(0, 360f) * Mathf.Deg2Rad;

            mainModule.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.5f, startSize);

            // Color over lifetime: orange-red to fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(fireColorStart, 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.0f, 1f), 0.4f),
                    new GradientColorKey(fireColorEnd, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            // Size over lifetime: expand then contract
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Emission
            emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f; // Start disabled

            // Shape: hemisphere
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.04f;
            shape.angle = 90f;

            // Trails
            trailModule = ps.trails;
            trailModule.enabled = true;
            trailModule.mode = ParticleSystemTrailMode.PerParticle;
            trailModule.ratio = 0.5f;
            trailModule.lifetime = new ParticleSystem.MinMaxCurve(0.25f);
            trailModule.widthOverTrail = new ParticleSystem.MinMaxCurve(0.02f);
            trailModule.colorOverLifetime = new ParticleSystem.MinMaxGradient(trailColor);

            // Noise for fire turbulence
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.05f;
            noise.frequency = 0.2f;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void UpdateParticleSystem()
        {
            if (ps == null) return;

            // Smoothly adjust emission rate
            float currentRate = emission.rateOverTime.constant;
            float newRate = Mathf.Lerp(currentRate, targetEmissionRate, Time.fixedDeltaTime * 8f);
            emission.rateOverTime = newRate;

            // Enable/disable Particle System
            if (targetEmissionRate > 1f && !ps.isPlaying)
            {
                ps.Play();
            }
            else if (targetEmissionRate < 0.1f && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// Starts Power Shot: plays whoosh sound, disables normal trail
        /// </summary>
        private void OnPowerShotStart()
        {
            if (autoPlayWhoosh)
            {
                float intensity = Mathf.Clamp01(currentSpeed / (powerShotThreshold * 2f));
                CueStrike.Audio.CueStrikeAudioManager.Instance?.PlayWhoosh(intensity);
            }

            // Disable normal trail while power shot is active
            if (ballTrail != null)
            {
                ballTrail.enabled = false;
            }

            Debug.Log($"[PowerShot] Power Shot activated! Speed: {currentSpeed:F2}");
        }

        /// <summary>
        /// Resets haptic state (called from CueStrikeShotManager when starting a new shot)
        /// </summary>
        public void ResetPowerShot()
        {
            hapticFiredThisShot = false;
            
            // Re-enable normal trail
            if (ballTrail != null)
            {
                ballTrail.enabled = true;
            }
        }

        void OnDestroy()
        {
            if (ps != null)
            {
                Destroy(ps.gameObject);
            }
        }
    }
}