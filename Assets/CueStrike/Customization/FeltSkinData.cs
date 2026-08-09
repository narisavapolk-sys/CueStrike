using UnityEngine;

namespace CueStrike.Customization
{
    /// <summary>
    /// FeltSkinData - ScriptableObject defining visual properties for table felt/cloth skins.
    /// Supports standard, speed cloth, tournament, and premium material types.
    /// </summary>
    [CreateAssetMenu(fileName = "FeltSkinData", menuName = "CueStrike/Customization/Felt Skin Data")]
    public class FeltSkinData : ScriptableObject
    {
        [Header("Identity")]
        public string skinName = "Classic Green";
        public string skinId = "felt_classic_green";
        public int sortOrder = 0;
        public bool isUnlocked = true;
        public int unlockPrice = 0;

        [Header("Material Type")]
        public MaterialType materialType = MaterialType.Standard;

        public enum MaterialType
        {
            Standard = 0,       // Standard wool blend
            SpeedCloth = 1,     // Low friction, fast play
            Tournament = 2,     // Simonis-style tournament grade
            Premium = 3         // Custom shader / special effects
        }

        [Header("PBR Properties")]
        public Color albedoColor = new Color(0.08f, 0.35f, 0.18f); // Classic green
        [Range(0f, 1f)] public float metallic = 0f;
        [Range(0f, 1f)] public float smoothness = 0.15f;
        [Range(0f, 2f)] public float normalStrength = 1f;

        [Header("Felt-Specific Properties")]
        [Range(0f, 1f)] public float fuzziness = 0.3f;          // Micro-fiber visibility
        [Range(0f, 1f)] public float weaveVisibility = 0.4f;    // Cloth weave pattern strength
        [Range(0.5f, 3f)] public float frictionMultiplier = 1f; // Physics friction modifier
        [Range(0.5f, 2f)] public float rollSpeedMultiplier = 1f; // Ball roll speed modifier

        [Header("Optional Textures")]
        public Texture2D albedoTexture;
        public Texture2D normalTexture;       // Weave pattern normal map
        public Texture2D fuzzTexture;         // Micro-fiber mask
        public Texture2D roughnessTexture;

        [Header("Cushion/Rail Settings")]
        public Color cushionColor = new Color(0.06f, 0.28f, 0.14f);
        [Range(0f, 1f)] public float cushionSmoothness = 0.2f;
        public Texture2D cushionAlbedoTexture;
        public Texture2D cushionNormalTexture;

        [Header("Line/Marking Settings")]
        public Color lineColor = Color.white;
        [Range(0f, 1f)] public float lineMetallic = 0f;
        [Range(0f, 1f)] public float lineSmoothness = 0.05f;
        public bool useCustomLineMaterial = false;
        public Material customLineMaterial;

        /// <summary>
        /// Creates a runtime material instance for the felt surface.
        /// </summary>
        public Material CreateFeltMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"FeltSkin_{skinId}";

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
            if (roughnessTexture != null) mat.SetTexture("_SpecGlossMap", roughnessTexture);

            // Felt-specific custom properties (for custom felt shader)
            mat.SetFloat("_Fuzziness", fuzziness);
            mat.SetFloat("_WeaveVisibility", weaveVisibility);

            // Material type specific tweaks
            switch (materialType)
            {
                case MaterialType.SpeedCloth:
                    mat.SetFloat("_Smoothness", Mathf.Max(smoothness, 0.3f));
                    mat.SetFloat("_Fuzziness", Mathf.Min(fuzziness, 0.1f));
                    break;
                case MaterialType.Tournament:
                    mat.SetFloat("_Smoothness", Mathf.Clamp(smoothness, 0.1f, 0.25f));
                    mat.SetFloat("_WeaveVisibility", Mathf.Max(weaveVisibility, 0.5f));
                    break;
                case MaterialType.Premium:
                    // Premium uses custom setup; keep base values
                    break;
            }

            return mat;
        }

        /// <summary>
        /// Creates a runtime material for cushions/rails.
        /// </summary>
        public Material CreateCushionMaterial(Shader shader = null)
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"FeltCushion_{skinId}";

            mat.SetColor("_BaseColor", cushionColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", cushionSmoothness);

            if (cushionAlbedoTexture != null) mat.SetTexture("_BaseMap", cushionAlbedoTexture);
            if (cushionNormalTexture != null)
            {
                mat.SetTexture("_BumpMap", cushionNormalTexture);
                mat.EnableKeyword("_NORMALMAP");
            }

            return mat;
        }

        /// <summary>
        /// Creates a runtime material for table lines/markings.
        /// </summary>
        public Material CreateLineMaterial(Shader shader = null)
        {
            if (useCustomLineMaterial && customLineMaterial != null)
            {
                return new Material(customLineMaterial);
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = new Material(shader);
            mat.name = $"FeltLines_{skinId}";
            mat.SetColor("_BaseColor", lineColor);
            mat.SetFloat("_Metallic", lineMetallic);
            mat.SetFloat("_Smoothness", lineSmoothness);
            mat.DisableKeyword("_EMISSION");

            return mat;
        }

        /// <summary>
        /// Returns physics parameters for this felt type.
        /// </summary>
        public (float friction, float rollSpeed) GetPhysicsParameters()
        {
            return (frictionMultiplier, rollSpeedMultiplier);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only self-test for FeltSkinData.
        /// Run via: Tools/CueStrike/Debug/Test FeltSkinData
        /// </summary>
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test FeltSkinData")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test 1: Create instance
            FeltSkinData testSkin = ScriptableObject.CreateInstance<FeltSkinData>();
            testSkin.skinName = "Test Felt";
            testSkin.skinId = "felt_test";
            testSkin.albedoColor = new Color(0.1f, 0.4f, 0.2f);
            testSkin.materialType = MaterialType.Tournament;
            testSkin.frictionMultiplier = 1.2f;
            testSkin.rollSpeedMultiplier = 0.9f;
            testSkin.fuzziness = 0.4f;
            testSkin.weaveVisibility = 0.6f;

            if (testSkin == null)
            {
                UnityEngine.Debug.LogError("[FeltSkinData SelfTest] FAIL: Could not create instance");
                pass = false;
            }

            // Test 2: CreateFeltMaterial
            Material feltMat = testSkin.CreateFeltMaterial();
            if (feltMat == null)
            {
                UnityEngine.Debug.LogError("[FeltSkinData SelfTest] FAIL: CreateFeltMaterial returned null");
                pass = false;
            }
            else
            {
                if (!feltMat.HasProperty("_BaseColor") || feltMat.GetColor("_BaseColor") != testSkin.albedoColor)
                {
                    UnityEngine.Debug.LogError("[FeltSkinData SelfTest] FAIL: Felt material albedo not set correctly");
                    pass = false;
                }
                if (testSkin.materialType == MaterialType.Tournament && !feltMat.IsKeywordEnabled("_NORMALMAP"))
                {
                    // Tournament should have normal map enabled if texture assigned, but we didn't assign one
                    // Just verify material creates without error
                }
                UnityEngine.Object.DestroyImmediate(feltMat);
            }

            // Test 3: CreateCushionMaterial
            Material cushionMat = testSkin.CreateCushionMaterial();
            if (cushionMat == null)
            {
                UnityEngine.Debug.LogError("[FeltSkinData SelfTest] FAIL: CreateCushionMaterial returned null");
                pass = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(cushionMat);
            }

            // Test 4: CreateLineMaterial
            Material lineMat = testSkin.CreateLineMaterial();
            if (lineMat == null)
            {
                UnityEngine.Debug.LogError("[FeltSkinData SelfTest] FAIL: CreateLineMaterial returned null");
                pass = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(lineMat);
            }

            // Test 5: GetPhysicsParameters
            var physics = testSkin.GetPhysicsParameters();
            if (physics.friction != 1.2f || physics.rollSpeed != 0.9f)
            {
                UnityEngine.Debug.LogError("[FeltSkinData SelfTest] FAIL: GetPhysicsParameters returned wrong values");
                pass = false;
            }

            // Test 6: Validate enums
            if (testSkin.materialType != MaterialType.Tournament)
            {
                UnityEngine.Debug.LogError("[FeltSkinData SelfTest] FAIL: MaterialType enum not working");
                pass = false;
            }

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testSkin);

            if (pass)
            {
                UnityEngine.Debug.Log("[FeltSkinData SelfTest] ✅ ALL TESTS PASSED — Ready for human verify");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[FeltSkinData SelfTest] ⚠️ TESTS FAILED — Fix before proceeding");
            }
        }
#endif
    }
}