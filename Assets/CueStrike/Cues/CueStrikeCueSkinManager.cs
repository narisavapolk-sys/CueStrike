using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CueStrikeCueSkinManager - Cue Skin Selection System + AAA PBR Materials
/// Created by Nari for P'Mong | 2026-07-19
/// </summary>
public class CueStrikeCueSkinManager : MonoBehaviour
{
    public static CueStrikeCueSkinManager Instance { get; private set; }

    /// <summary>
    /// AAA-grade Cue Skin Types
    /// </summary>
    public enum CueSkinType
    {
        AshWoodPremium = 0,    // Premium Ash Wood Grain
        CarbonFiberSport = 1,  // Carbon Fiber Sport
        EbonyGold = 2,         // Ebony Gold Classic
        MapleClassic = 3,      // Maple Classic
        NeonCyber = 4,         // Neon Cyber (Emissive)
        DragonSnooker = 5      // Golden Dragon Engraving
    }

    [System.Serializable]
    public class CueSkinPreset
    {
        public string name;
        public CueSkinType skinType;
        public Color albedo = Color.white;
        public Color normalTint = new Color(0.5f, 0.5f, 1f);
        [Range(0f, 1f)] public float smoothness = 0.8f;
        [Range(0f, 1f)] public float metallic = 0.05f;
        [Range(0f, 5f)] public float emissionIntensity = 0f;
        public Color emissionColor = Color.black;
        public float normalScale = 1f;
        public Texture albedoTexture;
        public Texture normalTexture;
        public Texture roughnessTexture;
    }

    [Header("Skin Presets")]
    public List<CueSkinPreset> skins = new List<CueSkinPreset>();

    [Header("Runtime")]
    public CueSkinType currentSkin = CueSkinType.AshWoodPremium;
    public Renderer cueRenderer;

    private const string PrefKey = "CueStrike_CurrentCueSkin";

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        InitializeDefaultSkins();
        LoadSavedSkin();
    }

    /// <summary>
    /// Creates all 6 default skin presets
    /// </summary>
    private void InitializeDefaultSkins()
    {
        if (skins.Count == 0)
        {
            skins.Add(new CueSkinPreset
            {
                name = "Ash Wood Premium",
                skinType = CueSkinType.AshWoodPremium,
                albedo = new Color(0.62f, 0.45f, 0.28f, 1f),
                smoothness = 0.78f,
                metallic = 0.06f,
                emissionIntensity = 0f,
                normalScale = 1.2f
            });
            skins.Add(new CueSkinPreset
            {
                name = "Carbon Fiber Sport",
                skinType = CueSkinType.CarbonFiberSport,
                albedo = new Color(0.08f, 0.08f, 0.1f, 1f),
                smoothness = 0.92f,
                metallic = 0.85f,
                emissionIntensity = 0f,
                normalScale = 0.6f
            });
            skins.Add(new CueSkinPreset
            {
                name = "Ebony Gold",
                skinType = CueSkinType.EbonyGold,
                albedo = new Color(0.04f, 0.03f, 0.03f, 1f),
                smoothness = 0.88f,
                metallic = 0.4f,
                emissionColor = new Color(1f, 0.78f, 0.3f),
                emissionIntensity = 0.6f,
                normalScale = 0.8f
            });
            skins.Add(new CueSkinPreset
            {
                name = "Maple Classic",
                skinType = CueSkinType.MapleClassic,
                albedo = new Color(0.92f, 0.78f, 0.55f, 1f),
                smoothness = 0.82f,
                metallic = 0.04f,
                emissionIntensity = 0f,
                normalScale = 1f
            });
            skins.Add(new CueSkinPreset
            {
                name = "Neon Cyber",
                skinType = CueSkinType.NeonCyber,
                albedo = new Color(0.05f, 0.05f, 0.08f, 1f),
                smoothness = 0.95f,
                metallic = 0.7f,
                emissionColor = new Color(0f, 1f, 0.8f),
                emissionIntensity = 3.5f,
                normalScale = 0.5f
            });
            skins.Add(new CueSkinPreset
            {
                name = "Dragon Snooker",
                skinType = CueSkinType.DragonSnooker,
                albedo = new Color(0.35f, 0.12f, 0.08f, 1f),
                smoothness = 0.86f,
                metallic = 0.5f,
                emissionColor = new Color(1f, 0.5f, 0f),
                emissionIntensity = 1.5f,
                normalScale = 1.4f
            });
        }
    }

    /// <summary>
    /// Switches cue skin at runtime
    /// </summary>
    public void SetSkin(CueSkinType skinType)
    {
        currentSkin = skinType;
        PlayerPrefs.SetInt(PrefKey, (int)skinType);
        PlayerPrefs.Save();
        ApplySkinToRenderer();
        Debug.Log($"[CueSkin] Switched to: {skinType}");
    }

    /// <summary>
    /// Applies selected skin to the cue Renderer
    /// </summary>
    public void ApplySkinToRenderer()
    {
        if (cueRenderer == null)
        {
            // Auto-detect cue Renderer
            cueRenderer = GetComponentInChildren<Renderer>();
        }
        if (cueRenderer == null) return;

        var preset = skins.Find(s => s.skinType == currentSkin);
        if (preset == null) return;

        // Use URP Lit shader for PBR
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = $"CueSkin_{preset.name}";

        // PBR properties
        mat.SetColor("_BaseColor", preset.albedo);

        if (preset.albedoTexture != null) mat.SetTexture("_BaseMap", preset.albedoTexture);
        if (preset.normalTexture != null)
        {
            mat.SetTexture("_BumpMap", preset.normalTexture);
            mat.EnableKeyword("_NORMALMAP");
        }

        mat.SetFloat("_Smoothness", preset.smoothness);
        mat.SetFloat("_Metallic", preset.metallic);

        // Emission (for Neon Cyber, Dragon, etc.)
        if (preset.emissionIntensity > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", preset.emissionColor * preset.emissionIntensity);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
        }

        cueRenderer.material = mat;
    }

    /// <summary>
    /// Loads saved skin from PlayerPrefs
    /// </summary>
    private void LoadSavedSkin()
    {
        if (PlayerPrefs.HasKey(PrefKey))
        {
            currentSkin = (CueSkinType)PlayerPrefs.GetInt(PrefKey, 0);
        }
        ApplySkinToRenderer();
    }

    /// <summary>
    /// Gets all skin names (for UI)
    /// </summary>
    public List<string> GetAllSkinNames()
    {
        var names = new List<string>();
        foreach (var s in skins) names.Add(s.name);
        return names;
    }

    /// <summary>
    /// Cycles to next skin
    /// </summary>
    public void NextSkin()
    {
        int next = ((int)currentSkin + 1) % skins.Count;
        SetSkin((CueSkinType)next);
    }
}