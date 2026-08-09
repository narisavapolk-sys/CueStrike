using UnityEngine;

public class CueStrikeFXManager : MonoBehaviour
{
    public static CueStrikeFXManager Instance { get; private set; }

    [Header("Prefabs (Optional)")]
    public ParticleSystem chalkDustPrefab;
    public ParticleSystem cushionDustPrefab;
    public ParticleSystem pocketGlowPrefab;
    public ParticleSystem ballImpactPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void SpawnChalkDust(Vector3 position)
    {
        // Chalk color: Neon cyan/blue-green (RGB: 0.1, 0.7, 0.8)
        SpawnEffect(chalkDustPrefab, position, new Color(0.1f, 0.7f, 0.8f, 0.6f), 0.15f, 0.6f, 30, true);
    }

    public void SpawnCushionDust(Vector3 position)
    {
        // Felt color: Soft olive beige (RGB: 0.85, 0.8, 0.7)
        SpawnEffect(cushionDustPrefab, position, new Color(0.85f, 0.8f, 0.7f, 0.4f), 0.25f, 0.5f, 20, false);
    }

    public void SpawnPocketGlow(Vector3 position)
    {
        // Pocket success color: Vibrant golden glow (RGB: 1.0, 0.85, 0.2)
        SpawnEffect(pocketGlowPrefab, position, new Color(1.0f, 0.85f, 0.2f, 0.8f), 0.35f, 0.8f, 50, true);
    }

    public void SpawnCollisionFX(Vector3 position, float velocity)
    {
        // Scale particle counts and lifetime based on collision speed
        float normalizedVel = Mathf.Clamp01(velocity / 15f);
        if (normalizedVel < 0.1f) return; // ignore minor bumps

        int count = Mathf.RoundToInt(normalizedVel * 25f);
        // Spark/Impact color: White/Light Amber (RGB: 1.0, 0.95, 0.8)
        SpawnEffect(ballImpactPrefab, position, new Color(1.0f, 0.95f, 0.8f, 0.9f), 0.08f, 0.3f, count, false);
    }

    public void SpawnCueStrikeFX(Vector3 position, float velocity)
    {
        // Power Shot whoosh effect - bright cyan/white sparks
        float normalizedVel = Mathf.Clamp01(velocity / 25f);
        int count = Mathf.RoundToInt(normalizedVel * 40f);
        // Power Shot color: Bright cyan-white (RGB: 0.8, 1.0, 1.0)
        SpawnEffect(ballImpactPrefab, position, new Color(0.8f, 1.0f, 1.0f, 1.0f), 0.12f, 0.5f, count, true);
    }

    void SpawnEffect(ParticleSystem prefab, Vector3 position, Color color, float size, float lifetime, int burstCount, bool rises)
    {
        ParticleSystem effect = null;
        if (prefab != null)
        {
            effect = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            var go = new GameObject("FX_Generated_Effect");
            go.transform.position = position;
            effect = go.AddComponent<ParticleSystem>();
            
            // Adjust renderer
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
#if UNITY_EDITOR
                renderer.sharedMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
#endif
            }

            // Configure Main Module
            var main = effect.main;
            main.startColor = color;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
            main.startLifetime = lifetime;
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            // Gravity if it rises/floats
            if (rises)
            {
                main.gravityModifier = -0.05f; // float up gently like chalk dust
            }
            else
            {
                main.gravityModifier = 0.2f; // fall down slightly like sparks
            }

            // Configure Emission Module
            var emission = effect.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            // Configure Shape Module
            var shape = effect.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            // Configure Size over Lifetime Module (shrink particles)
            var sizeModule = effect.sizeOverLifetime;
            sizeModule.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 1.0f);
            curve.AddKey(1.0f, 0.0f);
            sizeModule.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            // Configure Color over Lifetime Module (fade out)
            var colorModule = effect.colorOverLifetime;
            colorModule.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorModule.color = gradient;
        }

        effect.Play();
        Destroy(effect.gameObject, lifetime + 0.5f);
    }
}

