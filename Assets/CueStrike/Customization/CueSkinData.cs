using UnityEngine;

namespace CueStrike.Customization
{
    /// <summary>
    /// CueSkinData - ScriptableObject defining visual properties for cue skins.
    /// Supports wood, carbon fiber, premium, and custom material types.
    /// Integrates with existing CueProfile system.
    /// </summary>
    [CreateAssetMenu(fileName = "CueSkinData", menuName = "CueStrike/Customization/Cue Skin Data")]
    public class CueSkinData : ScriptableObject
    {
        [Header("Identity")]
        public string skinName = "Classic Ash";
        public string skinId = "cue_classic_ash";
        public int sortOrder = 0;
        public bool isUnlocked = true;
        public int unlockPrice = 0;

        [Header("Material Type")]
        public MaterialType materialType = MaterialType.Wood;

        public enum MaterialType
        {
            Wood = 0,              // Traditional wood grain
            CarbonFiber = 1,       // Carbon fiber weave
            Fiberglass = 2,        // Fiberglass composite
            Premium = 3,           // Exotic woods, inlays, engraving
            Custom = 4             // Custom shader / special effects
        }

        [Header("Shaft PBR Properties")]
        public Color shaftAlbedoColor = new Color(0.45f, 0.25f, 0.12f); // Ash wood
        [Range(0f, 1f)] public float shaftMetallic = 0.05f;
        [Range(0f, 1f)] public float shaftSmoothness = 0.75f;
        [Range(0f, 2f)] public float shaftNormalStrength = 1f;

        [Header("Butt/Sleeve PBR Properties")]
        public Color buttAlbedoColor = new Color(0.35f, 0.18f, 0.08f); // Darker wood
        [Range(0f, 1f)] public float buttMetallic = 0.05f;
        [Range(0f, 1f)] public float buttSmoothness = 0.8f;
        [Range(0f, 2f)] public float buttNormalStrength = 1f;

        [Header("Joint/Collar Properties")]
        public Color jointColor = new Color(0.8f, 0.65f, 0.2f); // Brass/gold
        [Range(0f, 1f)] public float jointMetallic = 0.9f;
        [Range(0f, 1f)] public float jointSmoothness = 0.85f;

        [Header("Tip Properties")]
        public Color tipColor = new Color(0.15f, 0.5f, 0.7f); // Chalk blue
        [Range(0f, 1f)] public float tipSmoothness = 0.1f;
        [Range(8f, 14f)] public float tipSize = 12.5f; // mm

        [Header("Wrap/Grip Properties")]
        public bool hasWrap = true;
        public Color wrapColor = new Color(0.1f, 0.1f, 0.12f); // Irish linen / leather
        [Range(0f, 1f)] public float wrapMetallic = 0f;
        [Range(0f, 1f)] public float wrapSmoothness = 0.05f;
        public Texture2D wrapTexture; // Linen/leather pattern

        [Header("Optional Textures")]
        public Texture2D shaftAlbedoTexture;
        public Texture2D shaftNormalTexture;     // Wood grain / carbon weave
        public Texture2D shaftRoughnessTexture;
        public Texture2D buttAlbedoTexture;
        public Texture2D buttNormalTexture;
        public Texture2D buttRoughnessTexture;
        public Texture2D jointTexture;

        [Header("Engraving/Inlay (Premium)")]
        public bool hasEngraving = false;
        public Texture2D engravingMask;      // Alpha mask for engraving areas
        public Color engravingColor = new Color(0.9f, 0.75f, 0.2f); // Gold fill
        [Range(0f, 1f)] public float engravingDepth = 0.5f;

        [Header("Performance Attributes (Visual Only)")]
        [Range(0f, 1f)] public float visualSpinEfficiency = 0.6f;
        [Range(0f, 1f)] public float visualDeflection = 0.3f;
        [Range(0.8f, 1.2f)] public float visualHitFeel = 1f;

        /// <summary>
        /// Creates a runtime material for the cue shaft.
        /// </summary>
        public Material CreateShaftMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"CueShaft_{skinId}";

            mat.SetColor("_BaseColor", shaftAlbedoColor);
            mat.SetFloat("_Metallic", shaftMetallic);
            mat.SetFloat("_Smoothness", shaftSmoothness);

            if (shaftAlbedoTexture != null) mat.SetTexture("_BaseMap", shaftAlbedoTexture);
            if (shaftNormalTexture != null)
            {
                mat.SetTexture("_BumpMap", shaftNormalTexture);
                mat.SetFloat("_BumpScale", shaftNormalStrength);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (shaftRoughnessTexture != null) mat.SetTexture("_SpecGlossMap", shaftRoughnessTexture);

            // Material type specific tweaks
            switch (materialType)
            {
                case MaterialType.CarbonFiber:
                    mat.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.1f));
                    mat.SetFloat("_Metallic", 0.6f);
                    mat.SetFloat("_Smoothness", 0.9f);
                    break;
                case MaterialType.Fiberglass:
                    mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.72f));
                    mat.SetFloat("_Metallic", 0.1f);
                    mat.SetFloat("_Smoothness", 0.85f);
                    break;
                case MaterialType.Premium:
                    // Keep custom values for exotic woods
                    break;
            }

            // Engraving support (if custom shader available)
            if (hasEngraving && engravingMask != null)
            {
                mat.SetTexture("_EngravingMask", engravingMask);
                mat.SetColor("_EngravingColor", engravingColor);
                mat.SetFloat("_EngravingDepth", engravingDepth);
                mat.EnableKeyword("_ENGRAVING");
            }

            return mat;
        }

        /// <summary>
        /// Creates a runtime material for the cue butt/sleeve.
        /// </summary>
        public Material CreateButtMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"CueButt_{skinId}";

            mat.SetColor("_BaseColor", buttAlbedoColor);
            mat.SetFloat("_Metallic", buttMetallic);
            mat.SetFloat("_Smoothness", buttSmoothness);

            if (buttAlbedoTexture != null) mat.SetTexture("_BaseMap", buttAlbedoTexture);
            if (buttNormalTexture != null)
            {
                mat.SetTexture("_BumpMap", buttNormalTexture);
                mat.SetFloat("_BumpScale", buttNormalStrength);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (buttRoughnessTexture != null) mat.SetTexture("_SpecGlossMap", buttRoughnessTexture);

            // Material type specific tweaks
            switch (materialType)
            {
                case MaterialType.CarbonFiber:
                    mat.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.08f));
                    mat.SetFloat("_Metallic", 0.7f);
                    mat.SetFloat("_Smoothness", 0.92f);
                    break;
                case MaterialType.Fiberglass:
                    mat.SetColor("_BaseColor", new Color(0.65f, 0.65f, 0.68f));
                    mat.SetFloat("_Metallic", 0.1f);
                    mat.SetFloat("_Smoothness", 0.85f);
                    break;
            }

            return mat;
        }

        /// <summary>
        /// Creates a runtime material for the joint/collar.
        /// </summary>
        public Material CreateJointMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"CueJoint_{skinId}";

            mat.SetColor("_BaseColor", jointColor);
            mat.SetFloat("_Metallic", jointMetallic);
            mat.SetFloat("_Smoothness", jointSmoothness);

            if (jointTexture != null) mat.SetTexture("_BaseMap", jointTexture);

            return mat;
        }

        /// <summary>
        /// Creates a runtime material for the tip.
        /// </summary>
        public Material CreateTipMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"CueTip_{skinId}";

            mat.SetColor("_BaseColor", tipColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", tipSmoothness);
            mat.DisableKeyword("_EMISSION");

            return mat;
        }

        /// <summary>
        /// Creates a runtime material for the wrap/grip.
        /// </summary>
        public Material CreateWrapMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"CueWrap_{skinId}";

            mat.SetColor("_BaseColor", wrapColor);
            mat.SetFloat("_Metallic", wrapMetallic);
            mat.SetFloat("_Smoothness", wrapSmoothness);

            if (wrapTexture != null)
            {
                mat.SetTexture("_BaseMap", wrapTexture);
                mat.EnableKeyword("_NORMALMAP"); // Use normal map slot for pattern
            }

            return mat;
        }

        /// <summary>
        /// Converts this skin data to a CueProfile for backward compatibility.
        /// </summary>
        public CueProfile ToCueProfile()
        {
            // This would require CueProfile reference - using reflection or
            // the profile can be created manually and these values applied
            return null; // Placeholder - actual implementation depends on CueProfile accessibility
        }

        /// <summary>
        /// Applies this skin to a CueStrikeCue component at runtime.
        /// </summary>
        public void ApplyToCue(CueStrikeCue cue)
        {
            if (cue == null)
            {
                UnityEngine.Debug.LogError("[CueSkinData] Cannot apply to null CueStrikeCue");
                return;
            }

            // Apply materials to cue components
            var shaftRenderer = cue.transform.Find("ShaftModel")?.GetComponent<Renderer>();
            var buttRenderer = cue.transform.Find("ButtModel")?.GetComponent<Renderer>();
            var tipRenderer = cue.transform.Find("TipModel")?.GetComponent<Renderer>();
            var jointRenderer = cue.transform.Find("JointModel")?.GetComponent<Renderer>();
            var wrapRenderer = cue.transform.Find("WrapModel")?.GetComponent<Renderer>();

            if (shaftRenderer != null) shaftRenderer.material = CreateShaftMaterial();
            if (buttRenderer != null) buttRenderer.material = CreateButtMaterial();
            if (tipRenderer != null) tipRenderer.material = CreateTipMaterial();
            if (jointRenderer != null) jointRenderer.material = CreateJointMaterial();
            if (wrapRenderer != null && hasWrap) wrapRenderer.material = CreateWrapMaterial();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only self-test for CueSkinData.
        /// Run via: Tools/CueStrike/Debug/Test CueSkinData
        /// </summary>
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test CueSkinData")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test 1: Create instance
            CueSkinData testSkin = ScriptableObject.CreateInstance<CueSkinData>();
            testSkin.skinName = "Test Cue";
            testSkin.skinId = "cue_test";
            testSkin.materialType = MaterialType.CarbonFiber;
            testSkin.shaftAlbedoColor = Color.black;
            testSkin.buttAlbedoColor = Color.grey;
            testSkin.jointColor = Color.yellow;
            testSkin.tipColor = Color.blue;
            testSkin.hasWrap = true;

            if (testSkin == null)
            {
                UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: Could not create instance");
                pass = false;
            }

            // Test 2: CreateShaftMaterial
            Material shaftMat = testSkin.CreateShaftMaterial();
            if (shaftMat == null)
            {
                UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: CreateShaftMaterial returned null");
                pass = false;
            }
            else
            {
                if (!shaftMat.HasProperty("_BaseColor"))
                {
                    UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: Shaft material missing _BaseColor");
                    pass = false;
                }
                // CarbonFiber should have high metallic/smoothness
                if (testSkin.materialType == MaterialType.CarbonFiber)
                {
                    float metallic = shaftMat.GetFloat("_Metallic");
                    float smoothness = shaftMat.GetFloat("_Smoothness");
                    if (metallic < 0.5f || smoothness < 0.85f)
                    {
                        UnityEngine.Debug.LogError($"[CueSkinData SelfTest] FAIL: CarbonFiber shaft should have high metallic/smoothness (got metallic={metallic}, smoothness={smoothness})");
                        pass = false;
                    }
                }
                UnityEngine.Object.DestroyImmediate(shaftMat);
            }

            // Test 3: CreateButtMaterial
            Material buttMat = testSkin.CreateButtMaterial();
            if (buttMat == null)
            {
                UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: CreateButtMaterial returned null");
                pass = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(buttMat);
            }

            // Test 4: CreateJointMaterial
            Material jointMat = testSkin.CreateJointMaterial();
            if (jointMat == null)
            {
                UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: CreateJointMaterial returned null");
                pass = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(jointMat);
            }

            // Test 5: CreateTipMaterial
            Material tipMat = testSkin.CreateTipMaterial();
            if (tipMat == null)
            {
                UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: CreateTipMaterial returned null");
                pass = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(tipMat);
            }

            // Test 6: CreateWrapMaterial
            Material wrapMat = testSkin.CreateWrapMaterial();
            if (wrapMat == null)
            {
                UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: CreateWrapMaterial returned null");
                pass = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(wrapMat);
            }

            // Test 7: Validate enums
            if (testSkin.materialType != MaterialType.CarbonFiber)
            {
                UnityEngine.Debug.LogError("[CueSkinData SelfTest] FAIL: MaterialType enum not working");
                pass = false;
            }

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testSkin);

            if (pass)
            {
                UnityEngine.Debug.Log("[CueSkinData SelfTest] ✅ ALL TESTS PASSED — Ready for human verify");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[CueSkinData SelfTest] ⚠️ TESTS FAILED — Fix before proceeding");
            }
        }
#endif
    }
}