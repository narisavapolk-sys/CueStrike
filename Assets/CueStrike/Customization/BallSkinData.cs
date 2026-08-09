using UnityEngine;

namespace CueStrike.Customization
{
    /// <summary>
    /// BallSkinData - ScriptableObject defining visual properties for ball skins.
    /// Supports standard, neon, metallic, and premium material types.
    /// </summary>
    [CreateAssetMenu(fileName = "BallSkinData", menuName = "CueStrike/Customization/Ball Skin Data")]
    public class BallSkinData : ScriptableObject
    {
        [Header("Identity")]
        public string skinName = "Classic";
        public string skinId = "ball_classic";
        public int sortOrder = 0;
        public bool isUnlocked = true;
        public int unlockPrice = 0; // 0 = free / default

        [Header("Material Type")]
        public MaterialType materialType = MaterialType.Standard;

        public enum MaterialType
        {
            Standard = 0,    // Standard PBR
            Neon = 1,        // Emissive glow
            Metallic = 2,    // High metallic
            Premium = 3      // Custom shader / special effects
        }

        [Header("PBR Properties")]
        public Color albedoColor = Color.white;
        [Range(0f, 1f)] public float metallic = 0.05f;
        [Range(0f, 1f)] public float smoothness = 0.85f;
        [Range(0f, 1f)] public float normalStrength = 1f;

        [Header("Emission (Neon/Premium)")]
        public Color emissionColor = Color.black;
        [Range(0f, 5f)] public float emissionIntensity = 0f;

        [Header("Optional Textures")]
        public Texture2D albedoTexture;
        public Texture2D normalTexture;
        public Texture2D metallicTexture;
        public Texture2D roughnessTexture;
        public Texture2D emissionTexture;

        [Header("Number Decal Settings")]
        public Color numberColor = Color.black;
        public FontStyle numberFontStyle = FontStyle.Bold;
        public int numberFontSize = 48;
        public bool useCustomNumberMaterial = false;
        public Material customNumberMaterial;

        [Header("Trail / VFX (Optional)")]
        public bool enableTrail = false;
        public Gradient trailColor;
        public float trailDuration = 0.3f;
        public float trailWidth = 0.05f;

        /// <summary>
        /// Creates a runtime material instance based on this skin data.
        /// </summary>
        public Material CreateMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"BallSkin_{skinId}";

            // Base PBR
            mat.SetColor("_BaseColor", albedoColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            // Textures
            if (albedoTexture != null) mat.SetTexture("_BaseMap", albedoTexture);
            if (normalTexture != null)
            {
                mat.SetTexture("_BumpMap", normalTexture);
                mat.SetFloat("_BumpScale", normalStrength);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (metallicTexture != null) mat.SetTexture("_MetallicGlossMap", metallicTexture);
            if (roughnessTexture != null) mat.SetTexture("_SpecGlossMap", roughnessTexture);

            // Emission
            if (emissionIntensity > 0f && emissionColor != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                Color finalEmission = emissionColor * emissionIntensity;
                mat.SetColor("_EmissionColor", finalEmission);
                if (emissionTexture != null) mat.SetTexture("_EmissionMap", emissionTexture);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            // Material type specific tweaks
            switch (materialType)
            {
                case MaterialType.Neon:
                    mat.EnableKeyword("_EMISSION");
                    mat.SetFloat("_Smoothness", Mathf.Max(smoothness, 0.9f));
                    break;
                case MaterialType.Metallic:
                    mat.SetFloat("_Metallic", Mathf.Max(metallic, 0.8f));
                    mat.SetFloat("_Smoothness", Mathf.Max(smoothness, 0.9f));
                    break;
                case MaterialType.Premium:
                    // Premium uses custom setup; keep base values
                    break;
            }

            return mat;
        }

        /// <summary>
        /// Creates a material for the number decal on the ball.
        /// </summary>
        public Material CreateNumberMaterial(Shader shader = null)
        {
            if (useCustomNumberMaterial && customNumberMaterial != null)
            {
                return new Material(customNumberMaterial);
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"BallNumber_{skinId}";
            mat.SetColor("_BaseColor", numberColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.1f);
            mat.DisableKeyword("_EMISSION");
            return mat;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only self-test for BallSkinData.
        /// Run via: Tools/CueStrike/Debug/Test BallSkinData
        /// </summary>
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test BallSkinData")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test 1: Create instance
            BallSkinData testSkin = ScriptableObject.CreateInstance<BallSkinData>();
            testSkin.skinName = "Test Skin";
            testSkin.skinId = "ball_test";
            testSkin.albedoColor = Color.red;
            testSkin.metallic = 0.5f;
            testSkin.smoothness = 0.9f;
            testSkin.emissionColor = Color.yellow;
            testSkin.emissionIntensity = 2f;
            testSkin.materialType = MaterialType.Neon;

            if (testSkin == null)
            {
                UnityEngine.Debug.LogError("[BallSkinData SelfTest] FAIL: Could not create instance");
                pass = false;
            }

            // Test 2: CreateMaterial
            Material mat = testSkin.CreateMaterial();
            if (mat == null)
            {
                UnityEngine.Debug.LogError("[BallSkinData SelfTest] FAIL: CreateMaterial returned null");
                pass = false;
            }
            else
            {
                // Verify material properties
                if (!mat.HasProperty("_BaseColor") || mat.GetColor("_BaseColor") != Color.red)
                {
                    UnityEngine.Debug.LogError("[BallSkinData SelfTest] FAIL: Material albedo not set correctly");
                    pass = false;
                }
                if (testSkin.materialType == MaterialType.Neon && !mat.IsKeywordEnabled("_EMISSION"))
                {
                    UnityEngine.Debug.LogError("[BallSkinData SelfTest] FAIL: Neon material should have EMISSION enabled");
                    pass = false;
                }
                UnityEngine.Object.DestroyImmediate(mat);
            }

            // Test 3: CreateNumberMaterial
            Material numMat = testSkin.CreateNumberMaterial();
            if (numMat == null)
            {
                UnityEngine.Debug.LogError("[BallSkinData SelfTest] FAIL: CreateNumberMaterial returned null");
                pass = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(numMat);
            }

            // Test 4: Validate enums and defaults
            if (testSkin.materialType != MaterialType.Neon)
            {
                UnityEngine.Debug.LogError("[BallSkinData SelfTest] FAIL: MaterialType enum not working");
                pass = false;
            }

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testSkin);

            if (pass)
            {
                UnityEngine.Debug.Log("[BallSkinData SelfTest] ✅ ALL TESTS PASSED — Ready for human verify");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[BallSkinData SelfTest] ⚠️ TESTS FAILED — Fix before proceeding");
            }
        }
#endif
    }
}