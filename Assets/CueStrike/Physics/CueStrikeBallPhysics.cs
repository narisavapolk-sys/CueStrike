using UnityEngine;
using CueStrike.Audio;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class CueStrikeBallPhysics : MonoBehaviour
{
    public float mass = 0.17f; // typical snooker ball ~170g
    public float linearDrag = 0.02f;
    public float angularDrag = 0.05f;
    public float spinFriction = 0.3f; // how fast spin decays

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        gameObject.tag = "Ball";
    }

    void FixedUpdate()
    {
        // simple spin decay (top/back/side spin)
        rb.angularVelocity *= Mathf.Clamp01(1f - spinFriction * Time.fixedDeltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactMagnitude = collision.relativeVelocity.magnitude;
        var audioMgr = CueStrikeAudioManager.Instance;
        var fxMgr = CueStrikeFXManager.Instance;

        if (collision.collider.CompareTag("Cushion") || collision.collider.name.Contains("Cushion"))
        {
            // apply slightly reduced energy loss to simulate rubberized cushions
            var v = rb.linearVelocity;
            rb.linearVelocity = Vector3.Reflect(v, collision.contacts[0].normal) * 0.9f;
            audioMgr?.PlayBallHit(impactMagnitude, cushionImpact: true);
            fxMgr?.SpawnCushionDust(collision.contacts[0].point);
        }
        else if (collision.collider.CompareTag("Ball"))
        {
            var otherRb = collision.collider.attachedRigidbody;
            if (otherRb != null)
            {
                var rel = rb.linearVelocity - otherRb.linearVelocity;
                var impulse = rel * 0.02f; // tweak factor
                rb.AddForce(-impulse, ForceMode.Impulse);
                otherRb.AddForce(impulse, ForceMode.Impulse);
            }
            audioMgr?.PlayBallHit(impactMagnitude, cushionImpact: false);
            fxMgr?.SpawnCollisionFX(collision.contacts[0].point, impactMagnitude);
        }
        else
        {
            audioMgr?.PlayBallHit(impactMagnitude, cushionImpact: false);
            fxMgr?.SpawnCollisionFX(collision.contacts[0].point, impactMagnitude);
        }
    }

    public void ApplyCueImpact(Vector3 direction, float force, float spinAmount)
    {
        rb.AddForce(direction * force, ForceMode.Impulse);
        // apply spin perpendicular to hit direction to simulate english
        rb.AddTorque(new Vector3(0, spinAmount, 0), ForceMode.Impulse);
    }
}
