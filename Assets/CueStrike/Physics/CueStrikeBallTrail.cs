using UnityEngine;
using CueStrike;

public class CueStrikeBallTrail : MonoBehaviour
{
    private Rigidbody rb;
    private TrailRenderer trail;
    private float maxSpeed = 12f;

    // Custom colors for different ball types
    private Color trailColor = new Color(0.1f, 0.8f, 1.0f, 0.4f); // default cyan glow

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Add and configure TrailRenderer
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.startWidth = 0.04f;
        trail.endWidth = 0.0f;
        trail.time = 0.4f;
        trail.minVertexDistance = 0.05f;
        
        // Set material
        #if UNITY_EDITOR
        trail.sharedMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        #endif
        
        // Determine color based on ball identity/name
        var id = GetComponent<BallIdentity>();
        if (id != null)
        {
            if (id.ballId == 0) // Cue ball: clean white/gold trail
            {
                trailColor = new Color(1.0f, 0.9f, 0.7f, 0.5f);
            }
            else if (id.ballId % 3 == 0) // Red/Pink
            {
                trailColor = new Color(1.0f, 0.15f, 0.3f, 0.4f);
            }
            else if (id.ballId % 3 == 1) // Yellow/Gold
            {
                trailColor = new Color(1.0f, 0.8f, 0.1f, 0.4f);
            }
            else // Blue/Green
            {
                trailColor = new Color(0.1f, 0.9f, 0.4f, 0.4f);
            }
        }

        // Apply initial colors
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0.0f);
        trail.emitting = false;
    }

    void Update()
    {
        if (rb == null || trail == null) return;

        float speed = rb.linearVelocity.magnitude;
        
        if (speed > 0.1f)
        {
            trail.emitting = true;
            // Scale trail length and opacity based on velocity
            float factor = Mathf.Clamp01(speed / maxSpeed);
            trail.time = Mathf.Lerp(0.1f, 0.5f, factor);
            
            Color activeColor = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * factor);
            trail.startColor = activeColor;
            trail.endColor = new Color(activeColor.r, activeColor.g, activeColor.b, 0.0f);
        }
        else
        {
            trail.emitting = false;
        }
    }
}
