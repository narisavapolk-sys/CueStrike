using UnityEngine;

[RequireComponent(typeof(Transform))]
public class CueStrikeCue : MonoBehaviour
{
    public CueProfile profile;
    public Transform tipTransform; // attach point for hit detection

    public float hitForce = 5f;

    private GameObject shaftModel;
    private GameObject tipModel;
    
    private Renderer shaftRenderer;
    private Renderer tipRenderer;

    void Start()
    {
        ApplyProfile();
    }

    void OnEnable()
    {
        CueSelectUI.OnCueChanged += HandleCueChanged;
    }

    void OnDisable()
    {
        CueSelectUI.OnCueChanged -= HandleCueChanged;
    }

    void HandleCueChanged(CueProfile newProfile)
    {
        if (newProfile == null) return;
        profile = newProfile;
        ApplyProfile();
    }

    public void ApplyProfile()
    {
        if (profile == null) return;

        // 1. Setup tip transform if missing
        if (tipTransform == null)
        {
            var tipFind = transform.Find("Tip");
            if (tipFind != null)
            {
                tipTransform = tipFind;
            }
            else
            {
                var tipGO = new GameObject("Tip");
                tipGO.transform.SetParent(transform, false);
                tipTransform = tipGO.transform;
            }
        }
        
        // The tip of the cue sits at local (0, 0, 0). The cue shaft extends backwards into -Z.
        tipTransform.localPosition = Vector3.zero;

        // 2. Setup procedural Shaft Mesh if missing
        if (shaftModel == null)
        {
            var shaftFind = transform.Find("ShaftModel");
            if (shaftFind != null)
            {
                shaftModel = shaftFind.gameObject;
            }
            else
            {
                shaftModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaftModel.name = "ShaftModel";
                shaftModel.transform.SetParent(transform, false);
                
                // Destroy default cylinder collider so it doesn't interfere with physics
                var col = shaftModel.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            shaftRenderer = shaftModel.GetComponent<Renderer>();
        }

        // 3. Setup procedural Tip visual if missing
        if (tipModel == null)
        {
            var tipModelFind = transform.Find("TipModel");
            if (tipModelFind != null)
            {
                tipModel = tipModelFind.gameObject;
            }
            else
            {
                tipModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tipModel.name = "TipModel";
                tipModel.transform.SetParent(transform, false);
                var col = tipModel.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            tipRenderer = tipModel.GetComponent<Renderer>();
        }

        // 4. Position and scale model segments according to cue length
        float totalLength = profile.length;
        float tipLength = 0.02f; // 2cm tip
        float shaftLength = totalLength - tipLength;

        // Position cylinder (defaults to 2 units high, so half-height is y-scale)
        // Rotate 90 degrees on X to align along Z axis.
        // Offset backwards so the front tip remains at local (0, 0, 0).
        shaftModel.transform.localScale = new Vector3(0.016f, shaftLength / 2.0f, 0.016f);
        shaftModel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shaftModel.transform.localPosition = new Vector3(0f, 0f, -shaftLength / 2.0f - tipLength);

        tipModel.transform.localScale = new Vector3(0.012f, tipLength / 2.0f, 0.012f);
        tipModel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        tipModel.transform.localPosition = new Vector3(0f, 0f, -tipLength / 2.0f);

        // 5. Apply URP materials and styles dynamically
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            // Apply Shaft Material
            Material shaftMat = new Material(urpLit);
            shaftMat.name = "CueShaft_" + profile.cueName;
            shaftMat.SetColor("_BaseColor", profile.cueColor);
            shaftMat.SetFloat("_Smoothness", profile.smoothness);
            shaftMat.SetFloat("_Metallic", profile.metallic);
            if (profile.material == CueProfile.MaterialType.Carbon)
            {
                // Carbon Look: Dark grey metallic weave styling
                shaftMat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.18f, 1f));
                shaftMat.SetFloat("_Smoothness", 0.9f);
                shaftMat.SetFloat("_Metallic", 0.6f);
            }
            else if (profile.cueName.ToLower().Contains("ash"))
            {
                // Generate a high-resolution Ash wood grain texture procedurally (V-grain)
                int width = 128;
                int height = 512;
                Texture2D ashTex = new Texture2D(width, height);
                Color baseWood = new Color(0.85f, 0.78f, 0.68f); // Pale cream ash wood
                Color grainColor = new Color(0.38f, 0.28f, 0.18f); // Dark brown grain lines
                
                for (int y = 0; y < height; y++)
                {
                    float yNorm = (float)y / (float)height;
                    for (int x = 0; x < width; x++)
                    {
                        float xNorm = ((float)x / (width / 2.0f)) - 1.0f; // -1.0 to 1.0
                        
                        // V-grain wave math
                        float vShape = Mathf.Abs(xNorm) * 2.2f - (yNorm * 12.0f);
                        float grainWave = Mathf.Sin(vShape * Mathf.PI);
                        
                        // Draw sharp distinct V-lines
                        float lerpFactor = Mathf.Clamp01(1.0f - Mathf.Abs(grainWave) / 0.15f);
                        Color pixelColor = Color.Lerp(baseWood, grainColor, lerpFactor * 0.5f);
                        
                        // Tiny micro noise for realistic fibers
                        float noise = Random.Range(-0.015f, 0.015f);
                        pixelColor.r = Mathf.Clamp01(pixelColor.r + noise);
                        pixelColor.g = Mathf.Clamp01(pixelColor.g + noise);
                        pixelColor.b = Mathf.Clamp01(pixelColor.b + noise);

                        ashTex.SetPixel(x, y, pixelColor);
                    }
                }
                ashTex.Apply();
                shaftMat.mainTexture = ashTex;
                shaftMat.SetFloat("_Smoothness", 0.75f);
                shaftMat.SetFloat("_Metallic", 0.05f);
            }
            if (shaftRenderer != null) shaftRenderer.sharedMaterial = shaftMat;

            // Apply Tip Material (chalk colored)
            Material tipMat = new Material(urpLit);
            tipMat.name = "CueTip_" + profile.cueName;
            tipMat.SetColor("_BaseColor", profile.tipColor);
            tipMat.SetFloat("_Smoothness", 0.1f); // matte chalk
            tipMat.SetFloat("_Metallic", 0.0f);
            if (tipRenderer != null) tipRenderer.sharedMaterial = tipMat;
        }

        // Scale root transform to match Z length scale if needed by external gameplay components
        transform.localScale = Vector3.one;
    }

    public void Strike(Rigidbody ballRb, Vector3 direction, float force, float spin)
    {
        bool isSnooker = PlayerPrefs.GetInt("CueStrike_TableStyle", 0) == 0;
        Vector3 finalDirection = direction;

        // Jump Shot detection (downward strike angle)
        float downwardAngle = -direction.y;
        if (downwardAngle > 0.15f)
        {
            if (isSnooker)
            {
                // Snooker: Jump shots are illegal. Clamp downward angle and trigger foul
                finalDirection.y = 0f;
                finalDirection = finalDirection.normalized;

                var rules = FindFirstObjectByType<CueStrikeRulesManager>();
                if (rules != null)
                {
                    rules.gameObject.SendMessage("OnStatusMessage", "Foul: Jump Shots are illegal in Snooker!", SendMessageOptions.DontRequireReceiver);
                }
                Debug.Log("[CueStrike Rules] Blocked illegal Snooker jump shot.");
            }
            else
            {
                // Pool: Allow jump shots. Translate downward squeeze into vertical upward velocity
                float jumpImpulse = force * downwardAngle * 0.35f;
                ballRb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
                Debug.Log($"[CueStrike Physics] Allowed Pool jump shot: vertical velocity impulse = {jumpImpulse:F3}");
            }
        }

        // Apply primary linear strike force
        ballRb.AddForce(finalDirection * force, ForceMode.Impulse);

        // Apply sidespin (English) around the vertical Y-axis
        ballRb.AddTorque(Vector3.up * spin, ForceMode.Impulse);

        // Apply topspin (follow) / backspin (draw) around the lateral horizontal axis
        Vector3 lateralAxis = Vector3.Cross(finalDirection, Vector3.up).normalized;
        float verticalSpinAmount = -direction.y * force * 5.0f; // Calculate spin proportional to downward cue angle
        ballRb.AddTorque(lateralAxis * verticalSpinAmount, ForceMode.Impulse);
    }
}
