Shader "Custom/URP/ClearCoatBall"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color (Ball Color)", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Ball Texture (Numbers/Design)", 2D) = "white" {}
        
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Roughness("Base Roughness", Range(0, 1)) = 0.1
        
        [Header(Clear Coat)]
        _ClearCoat("Clear Coat Strength", Range(0, 1)) = 1.0
        _ClearCoatRoughness("Clear Coat Roughness", Range(0, 1)) = 0.02
        
        [Header(Reflections)]
        _ReflectionIntensity("Reflection Intensity", Range(0, 1)) = 0.8
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
            #pragma target 3.0

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD3;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD4;
                float3 viewDirWS    : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Roughness;
                float _ClearCoat;
                float _ClearCoatRoughness;
                float _ReflectionIntensity;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(0,0,0,0));
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

                return output;
            }

            float3 GetSpecularHighlight(float3 normalWS, float3 lightDirWS, float3 viewDirWS, float roughness, float3 specularColor)
            {
                float3 halfDir = SafeNormalize(lightDirWS + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float NdotL = saturate(dot(normalWS, lightDirWS));
                
                // Simplified Blinn-Phong/GGX-like specular for runtime efficiency in URP
                float alpha = roughness * roughness;
                float a2 = alpha * alpha;
                float d = NdotH * NdotH * (a2 - 1.0) + 1.0;
                float D = a2 / (PI * d * d + 0.0001);
                
                return D * specularColor * NdotL;
            }

            float4 LitPassFragment(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float4 baseAlbedo = baseTex * _BaseColor;

                // Lighting inputs
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDir));

                // Diffuse lighting
                float3 diffuse = baseAlbedo.rgb * mainLight.color * NdotL * mainLight.shadowAttenuation;

                // Base Specular Highlight (rougher resin base)
                float3 specularColor = float3(0.04, 0.04, 0.04);
                float3 baseSpec = GetSpecularHighlight(normalWS, lightDir, viewDirWS, _Roughness, specularColor);

                // Clear Coat Layer Specular Highlight (extremely shiny outer lacquer)
                float3 clearCoatSpec = GetSpecularHighlight(normalWS, lightDir, viewDirWS, _ClearCoatRoughness, float3(1.0, 1.0, 1.0)) * _ClearCoat;

                // Environment Reflection Probe lookup (for glossy look)
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                float4 envSample = GLOSSY_ENVIRONMENT_REFLECTION_TEX2D(reflectDir, _ClearCoatRoughness);
                float3 environmentReflections = envSample.rgb * _ReflectionIntensity;

                // Fresnel factor for clear coat reflection mix
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, 5.0) * 0.9 + 0.1;

                // Combine layers
                float3 baseLit = diffuse + baseSpec;
                float3 finalColor = lerp(baseLit, environmentReflections, fresnel * _ClearCoat) + clearCoatSpec;

                // Ambient light
                float3 ambient = baseAlbedo.rgb * 0.15;
                finalColor += ambient;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
}
