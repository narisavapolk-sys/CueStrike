using UnityEngine;
using CueStrike.Audio;

namespace CueStrike.Physics
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class CueStrikeSnookerPhysics : MonoBehaviour
    {
        [Header("Ball Specification (WPBSA Standard)")]
        [Tooltip("Mass in kg (WPBSA: 0.170 kg)")]
        public float mass = 0.170f;
        [Tooltip("Diameter in meters (WPBSA: 0.0525 m)")]
        public float diameter = 0.0525f;

        [Header("Friction Coefficients")]
        [Range(0.005f, 0.02f)] public float rollingFriction = 0.012f;
        [Range(0.1f, 0.3f)] public float slidingFriction = 0.2f;
        [Range(0.05f, 0.3f)] public float slidingToRollingThreshold = 0.15f;

        [Header("Cushion Physics")]
        [Range(0.5f, 0.7f)] public float cushionRestitution = 0.6f;
        [Range(0.2f, 0.5f)] public float cornerEnergyLoss = 0.35f;
        [Range(0.05f, 0.2f)] public float cushionFriction = 0.12f;

        [Header("Spin Physics")]
        [Range(0.7f, 0.95f)] public float spinDecayRate = 0.85f;
        [Range(0.05f, 0.3f)] public float sideSpinCurveFactor = 0.15f;
        [Range(0.85f, 0.98f)] public float ballRestitution = 0.92f;

        [Header("References")]
        public CueStrikeRealisticAudioSynth audioSynth;
        public CueStrikeFXManager fxManager;

        private Rigidbody rb;
        private SphereCollider col;
        private bool isSliding = true;
        private Vector3 lastVelocity;
        private float ballRadius;

        public bool IsSliding => isSliding;
        public float CurrentSpeed => rb.linearVelocity.magnitude;
        public Vector3 AngularVelocity => rb.angularVelocity;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<SphereCollider>();

            ballRadius = diameter * 0.5f;
            col.radius = ballRadius;

            rb.mass = mass;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = true;

            gameObject.tag = "Ball";
            gameObject.layer = LayerMask.NameToLayer("Ball");
        }

        void FixedUpdate()
        {
            ApplyTableFriction();
            ApplySpinDecay();
            ApplySideSpinCurve();
            lastVelocity = rb.linearVelocity;
        }

        void ApplyTableFriction()
        {
            Vector3 vel = rb.linearVelocity;
            float speed = vel.magnitude;

            if (speed < 0.01f)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                isSliding = false;
                return;
            }

            Vector3 contactNormal = Vector3.up;
            Vector3 slipVel = vel - Vector3.Cross(rb.angularVelocity, contactNormal * ballRadius);
            float slipSpeed = slipVel.magnitude;

            float frictionCoeff = (slipSpeed > slidingToRollingThreshold) ? slidingFriction : rollingFriction;
            isSliding = slipSpeed > slidingToRollingThreshold;

            Vector3 frictionForce = -slipVel.normalized * frictionCoeff * mass * UnityEngine.Physics.gravity.magnitude;
            rb.AddForce(frictionForce, ForceMode.Acceleration);
        }

        void ApplySpinDecay()
        {
            rb.angularVelocity *= Mathf.Pow(spinDecayRate, Time.fixedDeltaTime * 60f);
        }

        void ApplySideSpinCurve()
        {
            if (!isSliding) return;

            Vector3 spinAxis = rb.angularVelocity.normalized;
            float spinMagnitude = rb.angularVelocity.magnitude;

            if (spinMagnitude < 1f) return;

            Vector3 curveDir = Vector3.Cross(spinAxis, Vector3.up).normalized;
            float curveForce = sideSpinCurveFactor * spinMagnitude * mass;
            rb.AddForce(curveDir * curveForce, ForceMode.Acceleration);
        }

        void OnCollisionEnter(Collision collision)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;

            if (collision.collider.CompareTag("Cushion") || collision.collider.name.Contains("Cushion"))
            {
                HandleCushionCollision(collision, impactSpeed, collision.collider.name.Contains("Corner"));
            }
            else if (collision.collider.CompareTag("Ball"))
            {
                HandleBallCollision(collision, impactSpeed);
            }
            else
            {
                PlayImpactAudio(impactSpeed, false);
                SpawnImpactFX(collision.contacts[0].point, impactSpeed);
            }
        }

        public void HandleCushionCollision(Collision collision, float impactSpeed, bool isCorner = false)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;
            Vector3 vel = rb.linearVelocity;

            float normalSpeed = Vector3.Dot(vel, -normal);
            Vector3 tangentialVel = vel + normal * normalSpeed;

            float restitution = isCorner ? cushionRestitution * (1f - cornerEnergyLoss) : cushionRestitution;
            Vector3 bounceVel = normal * normalSpeed * restitution;

            tangentialVel *= (1f - cushionFriction);

            rb.linearVelocity = bounceVel + tangentialVel;

            Vector3 spinEffect = Vector3.Cross(rb.angularVelocity, normal * ballRadius);
            rb.linearVelocity += spinEffect * 0.1f;

            PlayCushionAudio(impactSpeed);
            SpawnCushionFX(contact.point);
        }

        public void HandleBallCollision(Collision collision, float impactSpeed)
        {
            Rigidbody otherRb = collision.collider.attachedRigidbody;
            if (otherRb == null) return;

            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;

            Vector3 relVel = rb.linearVelocity - otherRb.linearVelocity;
            float normalVel = Vector3.Dot(relVel, normal);

            if (normalVel > 0) return;

            float totalMass = rb.mass + otherRb.mass;
            float impulse = -(1f + ballRestitution) * normalVel / (1f / rb.mass + 1f / otherRb.mass);
            Vector3 impulseVec = normal * impulse;

            rb.linearVelocity += impulseVec / rb.mass;
            otherRb.linearVelocity -= impulseVec / otherRb.mass;

            TransferSpin(otherRb, normal);

            PlayImpactAudio(impactSpeed, false);
            SpawnImpactFX(contact.point, impactSpeed);
        }

        void TransferSpin(Rigidbody otherRb, Vector3 normal)
        {
            Vector3 spinTransfer = Vector3.Cross(rb.angularVelocity, normal) * 0.15f;
            otherRb.angularVelocity += spinTransfer;
            rb.angularVelocity -= spinTransfer * 0.5f;
        }

        public void Strike(Vector3 aimDirection, Vector2 strikePoint, float triggerPull)
        {
            float force = triggerPull * 25f;
            Vector3 cueDir = aimDirection.normalized;

            rb.linearVelocity = cueDir * force;

            float verticalOffset = strikePoint.y;
            float horizontalOffset = strikePoint.x;

            Vector3 spin = new Vector3(
                -verticalOffset * force * 15f,
                horizontalOffset * force * 10f,
                verticalOffset * force * 5f
            );

            rb.angularVelocity = spin;
            isSliding = true;

            if (audioSynth != null)
            {
                audioSynth.PlayBallImpact(force * 0.5f);
            }
            if (fxManager != null)
            {
                fxManager.SpawnCueStrikeFX(transform.position + Vector3.up * ballRadius, force);
            }
        }

        public void ArmPowerShot()
        {
            // Visual feedback handled by CueStrikePowerShot component
        }

        public float PredictStopDistance(float currentSpeed = -1f, float frictionCoeff = -1f)
        {
            float speed = currentSpeed < 0 ? rb.linearVelocity.magnitude : currentSpeed;
            float mu = frictionCoeff < 0 ? rollingFriction : frictionCoeff;

            if (speed < 0.01f || mu <= 0f) return 0f;

            float decel = mu * Mathf.Abs(UnityEngine.Physics.gravity.y);
            return (speed * speed) / (2f * decel);
        }

        public void SetSliding(bool sliding)
        {
            isSliding = sliding;
        }

        public void ApplySpin(Vector3 spin)
        {
            rb.angularVelocity += spin;
        }

        void PlayImpactAudio(float intensity, bool cushion)
        {
            if (audioSynth != null)
            {
                audioSynth.PlayBallImpact(intensity);
            }
        }

        void PlayCushionAudio(float normalSpeed)
        {
            if (audioSynth != null)
            {
                audioSynth.PlayCushionBounce(normalSpeed);
            }
        }

        void SpawnImpactFX(Vector3 point, float intensity)
        {
            if (fxManager != null)
            {
                fxManager.SpawnCollisionFX(point, intensity);
            }
        }

        void SpawnCushionFX(Vector3 point)
        {
            if (fxManager != null)
            {
                fxManager.SpawnCushionDust(point);
            }
        }

        public void ResetBall()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            isSliding = false;
        }
    }
}