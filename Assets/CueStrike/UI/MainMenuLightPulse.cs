using UnityEngine;

[RequireComponent(typeof(Light))]
public class MainMenuLightPulse : MonoBehaviour
{
    public float minIntensity = 12f;
    public float maxIntensity = 18f;
    public float pulseSpeed = 0.8f;

    private Light pulseLight;

    void Start()
    {
        pulseLight = GetComponent<Light>();
    }

    void Update()
    {
        if (pulseLight == null) return;
        
        // Dynamic sine wave pulse to create breathing spotlight shadows
        float factor = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) * 0.5f;
        pulseLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, factor);
    }
}
