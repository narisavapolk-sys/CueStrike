using UnityEngine;

namespace CueStrike.RCA
{
    /// <summary>
    /// Physics profile for cue behavior in controller-less RCA.
    /// Defines cue physical properties, collision settings, and strike physics parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "CuePhysicsProfile", menuName = "CueStrike/RCA/Cue Physics Profile")]
    public class CueStrikeCuePhysicsProfile : ScriptableObject
    {
        [Header("Cue Physical Properties")]
        [SerializeField] private float cueMass = 0.55f;              // Cue mass in kg (standard ~19oz)
        [SerializeField] private float cueLength = 1.45f;            // Cue length in meters
        [SerializeField] private float cueTipRadius = 0.006f;        // Tip radius in meters (6mm)
        [SerializeField] private float cueButtRadius = 0.015f;       // Butt radius in meters (15mm)
        [SerializeField] private float taperStart = 0.3f;            // Where taper starts from tip (meters)
        
        [Header("Cue Material Properties")]
        [SerializeField] private float youngsModulus = 1.0e10f;      // Young's modulus (Pa) - wood ~10GPa
        [SerializeField] private float shearModulus = 0.6e9f;        // Shear modulus (Pa)
        [SerializeField] private float density = 750f;               // Density kg/m^3 (maple/ash)
        [SerializeField] private float coefficientOfRestitution = 0.85f; // Tip COR with ball
        [SerializeField] private float frictionCoefficient = 0.2f;   // Tip-ball friction
        
        [Header("Strike Physics")]
        [SerializeField] private float maxStrikeForce = 50f;         // Maximum strike force (N)
        [SerializeField] private float minStrikeForce = 0.5f;        // Minimum strike force (N)
        [SerializeField] private float forceVelocityCurve = 1.2f;    // Force vs velocity curve exponent
        [SerializeField] private float strikeDuration = 0.001f;      // Contact duration (seconds)
        [SerializeField] private float maxCueSpeed = 15f;            // Maximum cue tip speed (m/s)
        [SerializeField] private float followThroughDistance = 0.1f; // Follow-through after impact (m)
        
        [Header("Squirt/Deflection")]
        [SerializeField] private float squirtFactor = 0.0f;          // Squirt/deflection factor (0-1)
        [SerializeField] private float swerveFactor = 0.0f;          // Swerve factor (cloth interaction)
        [SerializeField] private bool enableSquirt = false;          // Enable squirt simulation
        [SerializeField] private bool enableSwerve = false;          // Enable swerve simulation
        
        [Header("Vibration & Feedback")]
        [SerializeField] private float vibrationFrequency = 150f;    // Vibration frequency (Hz)
        [SerializeField] private float vibrationDecay = 5f;          // Vibration decay rate
        [SerializeField] private float hapticStrength = 1f;          // Haptic feedback strength
        [SerializeField] private bool enableHaptics = true;          // Enable haptic feedback
        
        [Header("Collision Settings")]
        [SerializeField] private LayerMask ballLayer;                // Ball layer mask
        [SerializeField] private LayerMask tableLayer;               // Table layer mask
        [SerializeField] private float collisionDetectionRadius = 0.02f; // CCD radius
        [SerializeField] private bool continuousCollisionDetection = true; // Enable CCD
        
        [Header("Audio")]
        [SerializeField] private AudioClip[] strikeSounds;           // Strike sound variations
        [SerializeField] private AudioClip[] miscueSounds;           // Miscue sound variations
        [SerializeField] private float soundVolume = 1f;             // Base sound volume
        [SerializeField] private float soundPitchVariation = 0.1f;   // Pitch randomness
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject strikeEffectPrefab;      // Strike particle effect
        [SerializeField] private GameObject miscueEffectPrefab;      // Miscue particle effect
        [SerializeField] private float effectScale = 1f;             // Effect scale multiplier
        
        // Properties
        public float CueMass => cueMass;
        public float CueLength => cueLength;
        public float CueTipRadius => cueTipRadius;
        public float CueButtRadius => cueButtRadius;
        public float TaperStart => taperStart;
        public float YoungsModulus => youngsModulus;
        public float ShearModulus => shearModulus;
        public float Density => density;
        public float CoefficientOfRestitution => coefficientOfRestitution;
        public float FrictionCoefficient => frictionCoefficient;
        public float MaxStrikeForce => maxStrikeForce;
        public float MinStrikeForce => minStrikeForce;
        public float ForceVelocityCurve => forceVelocityCurve;
        public float StrikeDuration => strikeDuration;
        public float MaxCueSpeed => maxCueSpeed;
        public float FollowThroughDistance => followThroughDistance;
        public float SquirtFactor => squirtFactor;
        public float SwerveFactor => swerveFactor;
        public bool EnableSquirt => enableSquirt;
        public bool EnableSwerve => enableSwerve;
        public float VibrationFrequency => vibrationFrequency;
        public float VibrationDecay => vibrationDecay;
        public float HapticStrength => hapticStrength;
        public bool EnableHaptics => enableHaptics;
        public LayerMask BallLayer => ballLayer;
        public LayerMask TableLayer => tableLayer;
        public float CollisionDetectionRadius => collisionDetectionRadius;
        public bool ContinuousCollisionDetection => continuousCollisionDetection;
        public AudioClip[] StrikeSounds => strikeSounds;
        public AudioClip[] MiscueSounds => miscueSounds;
        public float SoundVolume => soundVolume;
        public float SoundPitchVariation => soundPitchVariation;
        public GameObject StrikeEffectPrefab => strikeEffectPrefab;
        public GameObject MiscueEffectPrefab => miscueEffectPrefab;
        public float EffectScale => effectScale;
        
        /// <summary>
        /// Calculates the strike force based on cue velocity.
        /// </summary>
        public float CalculateStrikeForce(float cueSpeed)
        {
            // Clamp speed
            float clampedSpeed = Mathf.Clamp(cueSpeed, 0f, maxCueSpeed);
            
            // Normalize speed (0-1)
            float normalizedSpeed = clampedSpeed / maxCueSpeed;
            
            // Apply curve
            float forceMultiplier = Mathf.Pow(normalizedSpeed, forceVelocityCurve);
            
            // Calculate force
            float force = Mathf.Lerp(minStrikeForce, maxStrikeForce, forceMultiplier);
            
            return force;
        }
        
        /// <summary>
        /// Calculates the cue ball velocity after impact.
        /// </summary>
        public Vector3 CalculateBallVelocity(Vector3 cueVelocity, Vector3 cueDirection, Vector3 contactNormal, float spinFactor = 0f)
        {
            float cueSpeed = cueVelocity.magnitude;
            float force = CalculateStrikeForce(cueSpeed);
            
            // Basic momentum transfer
            float ballMass = 0.17f; // Standard cue ball mass (kg)
            float effectiveMass = (cueMass * ballMass) / (cueMass + ballMass);
            
            // Velocity transfer
            float velocityTransfer = (2f * effectiveMass / ballMass) * coefficientOfRestitution;
            Vector3 ballVelocity = cueDirection * cueSpeed * velocityTransfer;
            
            // Apply spin (English)
            if (spinFactor != 0f)
            {
                Vector3 spinAxis = Vector3.Cross(cueDirection, contactNormal).normalized;
                ballVelocity += spinAxis * spinFactor * cueSpeed * 0.5f;
            }
            
            // Apply squirt (deflection)
            if (enableSquirt && squirtFactor > 0f)
            {
                Vector3 squirtDirection = Vector3.Cross(cueDirection, Vector3.up).normalized;
                ballVelocity += squirtDirection * squirtFactor * cueSpeed * 0.1f;
            }
            
            return ballVelocity;
        }
        
        /// <summary>
        /// Calculates the spin imparted on the cue ball.
        /// </summary>
        public Vector3 CalculateSpin(Vector3 cueVelocity, Vector3 cueDirection, Vector3 contactPoint, Vector3 ballCenter)
        {
            Vector3 offset = contactPoint - ballCenter;
            float offsetMagnitude = offset.magnitude;
            
            // Maximum offset is ball radius (~0.0286m)
            float maxOffset = 0.0286f;
            float normalizedOffset = Mathf.Clamp01(offsetMagnitude / maxOffset);
            
            // Spin axis is perpendicular to cue direction and offset
            Vector3 spinAxis = Vector3.Cross(cueDirection, offset).normalized;
            
            // Spin magnitude
            float spinMagnitude = cueVelocity.magnitude * normalizedOffset * frictionCoefficient * 50f;
            
            return spinAxis * spinMagnitude;
        }
        
        /// <summary>
        /// Gets a random strike sound.
        /// </summary>
        public AudioClip GetRandomStrikeSound()
        {
            if (strikeSounds == null || strikeSounds.Length == 0) return null;
            return strikeSounds[Random.Range(0, strikeSounds.Length)];
        }
        
        /// <summary>
        /// Gets a random miscue sound.
        /// </summary>
        public AudioClip GetRandomMiscueSound()
        {
            if (miscueSounds == null || miscueSounds.Length == 0) return null;
            return miscueSounds[Random.Range(0, miscueSounds.Length)];
        }
        
        /// <summary>
        /// Validates the profile settings.
        /// </summary>
        public bool ValidateProfile()
        {
            bool valid = true;
            
            if (cueMass <= 0f)
            {
                Debug.LogError("[CuePhysicsProfile] Cue mass must be positive.");
                valid = false;
            }
            
            if (cueLength <= 0f)
            {
                Debug.LogError("[CuePhysicsProfile] Cue length must be positive.");
                valid = false;
            }
            
            if (cueTipRadius <= 0f)
            {
                Debug.LogError("[CuePhysicsProfile] Cue tip radius must be positive.");
                valid = false;
            }
            
            if (coefficientOfRestitution < 0f || coefficientOfRestitution > 1f)
            {
                Debug.LogError("[CuePhysicsProfile] Coefficient of restitution must be between 0 and 1.");
                valid = false;
            }
            
            if (maxStrikeForce <= minStrikeForce)
            {
                Debug.LogError("[CuePhysicsProfile] Max strike force must be greater than min strike force.");
                valid = false;
            }
            
            return valid;
        }
        
        private void OnValidate()
        {
            ValidateProfile();
        }
    }
}