Shader "Custom/URP/FeltShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color (Felt Tint)", Color) = (0.05, 0.35, 0.12, 1)
        [MainTexture] _BaseMap("Felt Texture (Grayscale/Detail)", 2D) = "white" {}
        
        _FuzzColor("Fuzz Color (Fresnel)", Color) = (0.1, 0.55, 0.22, 1)
        _FuzzPower("Fuzz Power", Range(1, 10)) = 4.0
        
        _BumpMap("Normal Map (Felt Micro-Grain)", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 0.5
        
        _Roughness("Roughness", Range(0, 1)) = 0.95
        _Metallic("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_l9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP

            // -------------------------------------
            // Register Passes
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD3;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD4;
                float3 viewDirWS    : TEXCOORD5;
                #if defined(_NORMALMAP)
                float4 tangentWS    : TEXCOORD6;
                #endif
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _FuzzColor;
                float _FuzzPower;
                float _BumpScale;
                float _Roughness;
                float _Metallic;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

                #if defined(_NORMALMAP)
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                #endif

                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                #if defined(_NORMALMAP)
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * input.tangentWS.w;
                float3 dp = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
                normalWS = tangentWS * dp.x + bitangentWS * dp.y + normalWS * dp.z;
                normalWS = normalize(normalWS);
                #endif

                // Fetch Base Texture & Color
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float4 baseColor = baseTex * _BaseColor;

                // Fuzzy / Velvet lighting model (Fresnel edge backscattering)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fuzz = pow(1.0 - NdotV, _FuzzPower);
                float3 finalAlbedo = lerp(baseColor.rgb, _FuzzColor.rgb, fuzz);

                // Lighting calculation using URP's main light
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDir));

                // Diffuse lighting (Half-Lambert for softer shadow transition on cloth)
                float halfLambert = NdotL * 0.5 + 0.5;
                float3 diffuse = finalAlbedo * mainLight.color * halfLambert * mainLight.shadowAttenuation;

                // Ambient lighting
                float3 ambient = baseColor.rgb * 0.15;

                float3 finalColor = diffuse + ambient;
                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
}
